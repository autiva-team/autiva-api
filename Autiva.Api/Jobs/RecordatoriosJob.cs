using Autiva.Application.Services;
using Autiva.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Autiva.Api.Jobs;

/// <summary>
/// Tarea en segundo plano que evalúa periódicamente recordatorios próximos o vencidos
/// y despacha notificaciones automáticas a los usuarios.
/// </summary>
public class RecordatoriosJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RecordatoriosJob> _logger;
    private readonly TimeSpan _periodo = TimeSpan.FromHours(1);

    public RecordatoriosJob(
        IServiceProvider serviceProvider,
        ILogger<RecordatoriosJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Servicio en segundo plano RecordatoriosJob iniciado.");

        // Breve espera inicial antes de la primera evaluación para permitir que la app arranque
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        using var timer = new PeriodicTimer(_periodo);

        do
        {
            try
            {
                await EvaluarRecordatoriosAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la ejecución periódica de RecordatoriosJob.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));

        _logger.LogInformation("Servicio en segundo plano RecordatoriosJob detenido.");
    }

    private async Task EvaluarRecordatoriosAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AutivaDbContext>();
        var pushService = scope.ServiceProvider.GetRequiredService<IPushService>();

        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var hace24Horas = DateTime.UtcNow.AddHours(-24);

        var recordatorios = await context.Recordatorio
            .Include(r => r.Vehiculo)
            .Where(r => r.Estado == "pendiente" &&
                        r.EliminadoEn == null &&
                        r.Vehiculo != null &&
                        r.Vehiculo.EliminadoEn == null)
            .ToListAsync(cancellationToken);

        foreach (var r in recordatorios)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            string? tituloAlerta = null;
            string? mensajeAlerta = null;

            if (r.Criterio == "por_fecha" && r.FechaLimite.HasValue)
            {
                var diasRestantes = r.FechaLimite.Value.DayNumber - hoy.DayNumber;

                if (diasRestantes <= r.AnticipacionDias)
                {
                    tituloAlerta = diasRestantes < 0
                        ? $"Recordatorio vencido: {r.Titulo}"
                        : $"Recordatorio próximo: {r.Titulo}";

                    mensajeAlerta = diasRestantes switch
                    {
                        < 0 => $"Tu recordatorio '{r.Titulo}' para {r.Vehiculo.Marca} {r.Vehiculo.Modelo} venció hace {Math.Abs(diasRestantes)} día(s).",
                        0 => $"Hoy vence tu recordatorio '{r.Titulo}' para {r.Vehiculo.Marca} {r.Vehiculo.Modelo}.",
                        _ => $"Tu recordatorio '{r.Titulo}' para {r.Vehiculo.Marca} {r.Vehiculo.Modelo} vence en {diasRestantes} día(s)."
                    };
                }
            }
            else if (r.Criterio == "por_kilometraje" && r.KmObjetivo.HasValue)
            {
                var kmRestantes = r.KmObjetivo.Value - r.Vehiculo.OdometroActual;

                if (kmRestantes <= 500)
                {
                    tituloAlerta = kmRestantes <= 0
                        ? $"Meta de km alcanzada: {r.Titulo}"
                        : $"Recordatorio por km próximo: {r.Titulo}";

                    mensajeAlerta = kmRestantes <= 0
                        ? $"Tu vehículo {r.Vehiculo.Marca} {r.Vehiculo.Modelo} alcanzó los {r.KmObjetivo:N0} km del recordatorio '{r.Titulo}'."
                        : $"Faltan solo {kmRestantes:N0} km para alcanzar el objetivo del recordatorio '{r.Titulo}' en {r.Vehiculo.Marca}.";
                }
            }

            if (tituloAlerta is not null && mensajeAlerta is not null)
            {
                // Evitar notificaciones repetidas en las últimas 24 horas para el mismo recordatorio
                var yaNotificado = await context.Notificacion
                    .AnyAsync(n => n.RecordatorioId == r.Id && n.FechaEnvio >= hace24Horas, cancellationToken);

                if (!yaNotificado)
                {
                    await pushService.EnviarNotificacionUsuarioAsync(
                        r.Vehiculo.UsuarioId,
                        tituloAlerta,
                        mensajeAlerta,
                        r.Id,
                        cancellationToken);

                    _logger.LogInformation("Notificación enviada para recordatorio {RecordatorioId} del usuario {UsuarioId}", r.Id, r.Vehiculo.UsuarioId);
                }
            }
        }
    }
}
