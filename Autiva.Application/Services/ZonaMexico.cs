namespace Autiva.Application.Services;

/// <summary>
/// Zona horaria de México, usada para calcular "hoy" y "mes actual" en reportes
/// (rendimiento HU-009, gastos HU-019, estadísticas) sin depender de la hora UTC del servidor.
/// </summary>
public static class ZonaMexico
{
    public static readonly TimeZoneInfo Info = TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");

    public static DateTime ConvertirDesdeUtc(DateTime fechaHoraUtc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(fechaHoraUtc, DateTimeKind.Utc), Info);

    public static DateTime Ahora() => ConvertirDesdeUtc(DateTime.UtcNow);
}
