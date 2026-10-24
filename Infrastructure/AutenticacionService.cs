using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;

namespace TransMetroUMG.Infrastructure;

public sealed class AutenticacionService(MunitransDbContext dbContext, IPasswordHasher<Usuario> hasher,
    ILogger<AutenticacionService> logger) : IAutenticacionService
{
    public async Task<UsuarioAutenticado?> ValidarAsync(string nombre, string contrasena, CancellationToken cancellationToken)
    {
        var usuario = await dbContext.Usuarios.SingleOrDefaultAsync(x => x.NombreUsuario == nombre.Trim(), cancellationToken);
        var resultado = PasswordVerificationResult.Failed;
        if (usuario is not null)
        {
            try { resultado = hasher.VerifyHashedPassword(usuario, usuario.ContrasenaHash, contrasena); }
            catch (FormatException)
            {
                logger.LogWarning("Usuario {Codigo}: formato de credencial incompatible con el inicio de sesión.", usuario.CodUsuario);
            }
        }
        if (usuario is null || resultado == PasswordVerificationResult.Failed
            || !await SigueVigenteAsync(usuario.CodUsuario, cancellationToken))
        {
            logger.LogWarning("Intento de inicio de sesión rechazado.");
            return null;
        }
        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
            usuario.ContrasenaHash = hasher.HashPassword(usuario, contrasena);
        // Usar el mismo reloj de SQL Server que las fechas persistidas existentes.
        usuario.UltimoAcceso = await dbContext.Database.SqlQuery<DateTime>($"SELECT SYSDATETIME() AS Value").SingleAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Usuario {Codigo} inició sesión.", usuario.CodUsuario);
        return new UsuarioAutenticado(usuario.CodUsuario, usuario.NombreCompleto);
    }

    public async Task<bool> SigueVigenteAsync(int codigo, CancellationToken cancellationToken)
        => await dbContext.Database.SqlQuery<int>($"""
            SELECT COUNT(1) AS Value FROM dbo.USUARIO
            WHERE COD_USUARIO = {codigo} AND COD_ESTADO_USUARIO = {(byte)EstadoUsuario.Activo}
              AND (FECHA_EXPIRACION_CONTRASENA IS NULL OR FECHA_EXPIRACION_CONTRASENA > SYSDATETIME())
            """).SingleAsync(cancellationToken) == 1;
}
