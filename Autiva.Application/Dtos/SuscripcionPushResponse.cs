namespace Autiva.Application.Dtos;

public class SuscripcionPushResponse
{
    public Guid Id { get; set; }
    public string Endpoint { get; set; } = null!;
    public string? Dispositivo { get; set; }
    public bool Activa { get; set; }
    public DateTime CreadoEn { get; set; }
}
