using Autiva.Application.Interfaces;
using Autiva.Domain.Entities;
using Autiva.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Autiva.Infrastructure.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly AutivaDbContext _context;

    public UsuarioRepository(AutivaDbContext context)
    {
        _context = context;
    }

    // La comparación del correo no distingue mayúsculas porque Azure SQL usa una
    // collation CI (SQL_Latin1_General_CP1_CI_AS). Sin ToLower() la consulta puede
    // usar el índice UQ_usuario_correo.
    public Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancellationToken = default) =>
        _context.Usuario.AnyAsync(u => u.Correo == correo, cancellationToken);

    public Task<Usuario?> ObtenerPorCorreoAsync(string correo, CancellationToken cancellationToken = default) =>
        _context.Usuario.FirstOrDefaultAsync(u => u.Correo == correo, cancellationToken);

    public async Task<bool> AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        _context.Usuario.Add(usuario);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // Carrera sobre el índice único UQ_usuario_correo: alguien registró
            // el mismo correo entre la validación anterior y el guardado.
            var correo = usuario.Correo;
            var conflicto = await _context.Usuario
                .AnyAsync(u => u.Correo == correo, cancellationToken);

            if (conflicto)
                return false;

            throw;
        }
    }
}