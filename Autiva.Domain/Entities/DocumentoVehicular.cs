using System;
using System.Collections.Generic;

namespace Autiva.Domain.Entities;

/// <summary>
/// Bóveda de documentos del vehículo (URL en Blob Storage).
/// </summary>
public partial class DocumentoVehicular
{
    public Guid Id { get; set; }

    public Guid VehiculoId { get; set; }

    public string Tipo { get; set; } = null!;

    public string NombreArchivo { get; set; } = null!;

    public string ArchivoUrl { get; set; } = null!;

    public int? TamanoKb { get; set; }

    public DateOnly? Vigencia { get; set; }

    public DateTime CreadoEn { get; set; }

    public DateTime ActualizadoEn { get; set; }

    public DateTime? EliminadoEn { get; set; }

    public virtual Vehiculo Vehiculo { get; set; } = null!;
}
