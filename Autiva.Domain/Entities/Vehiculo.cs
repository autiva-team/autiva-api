using System;
using System.Collections.Generic;

namespace Autiva.Domain.Entities;

/// <summary>
/// Vehículo del usuario; centro del expediente.
/// </summary>
public partial class Vehiculo
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }

    public string Marca { get; set; } = null!;

    public string Modelo { get; set; } = null!;

    public short Anio { get; set; }

    public string Placas { get; set; } = null!;

    public string? Vin { get; set; }

    public string? Color { get; set; }

    public decimal? CapacidadTanqueL { get; set; }

    public int KilometrajeInicial { get; set; }

    public int OdometroActual { get; set; }

    public string Estatus { get; set; } = null!;

    public bool EsPrincipal { get; set; }

    public string? FotoUrl { get; set; }

    public string? NotasTecnicas { get; set; }

    public DateTime CreadoEn { get; set; }

    public DateTime ActualizadoEn { get; set; }

    public DateTime? EliminadoEn { get; set; }

    public virtual ICollection<CargaCombustible> CargaCombustible { get; set; } = new List<CargaCombustible>();

    public virtual ICollection<DocumentoVehicular> DocumentoVehicular { get; set; } = new List<DocumentoVehicular>();

    public virtual ICollection<Gasto> Gasto { get; set; } = new List<Gasto>();

    public virtual ICollection<PolizaSeguro> PolizaSeguro { get; set; } = new List<PolizaSeguro>();

    public virtual ICollection<Recordatorio> Recordatorio { get; set; } = new List<Recordatorio>();

    public virtual ICollection<ServicioMantenimiento> ServicioMantenimiento { get; set; } = new List<ServicioMantenimiento>();

    public virtual Usuario Usuario { get; set; } = null!;

    public virtual ICollection<VerificacionVehicular> VerificacionVehicular { get; set; } = new List<VerificacionVehicular>();
}
