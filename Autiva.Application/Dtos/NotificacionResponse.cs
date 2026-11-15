namespace Autiva.Application.Dtos;

public class NotificacionResponse
{
    public Guid Id { get; set; }
    public Guid? RecordatorioId { get; set; }
    public string Titulo { get; set; } = null!;
    public string Mensaje { get; set; } = null!;
    public DateTime FechaEnvio { get; set; }
    public bool Leida { get; set; }
}
