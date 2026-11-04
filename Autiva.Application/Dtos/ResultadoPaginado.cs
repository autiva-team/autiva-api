namespace Autiva.Application.Dtos;

/// <summary>
/// Respuesta paginada genérica, reutilizable por cualquier listado del API.
/// </summary>
public class ResultadoPaginado<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

    public int Pagina { get; set; }

    public int Tamano { get; set; }

    public int TotalRegistros { get; set; }

    public int TotalPaginas { get; set; }
}
