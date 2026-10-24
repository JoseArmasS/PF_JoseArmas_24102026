using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;

namespace TransMetroUMG.Infrastructure;

public sealed class UsuariosService(
    MunitransDbContext dbContext,
    IPasswordHasher<Usuario> passwordHasher,
    ILogger<UsuariosService> logger) : IUsuariosService
{
    public async Task<ResultadoCrearUsuario> CrearAsync(
        CrearUsuarioDatos datos,
        CancellationToken cancellationToken)
    {
        var nombreUsuario = datos.NombreUsuario.Trim();
        var nombreCompleto = datos.NombreCompleto.Trim();
        var correo = datos.CorreoElectronico.Trim();

        if (string.IsNullOrWhiteSpace(nombreUsuario)
            || nombreUsuario.Length > 100
            || string.IsNullOrWhiteSpace(nombreCompleto)
            || nombreCompleto.Length > 200
            || string.IsNullOrWhiteSpace(correo)
            || correo.Length > 150
            || !new EmailAddressAttribute().IsValid(correo)
            || string.IsNullOrWhiteSpace(datos.Contrasena)
            || !Enum.IsDefined(datos.Estado))
        {
            throw new ArgumentException("Los datos inválidos.");
        }

        var nombreDuplicado = await dbContext.Usuarios.AnyAsync(
            x => x.NombreUsuario == nombreUsuario,
            cancellationToken);

        if (nombreDuplicado)
            return ResultadoCrearUsuario.NombreDuplicado;

        var existeMunicipalidad = await dbContext.Municipalidades.AnyAsync(
            x => x.CodMunicipalidad == datos.CodMunicipalidad,
            cancellationToken);

        if (!existeMunicipalidad)
            return ResultadoCrearUsuario.MunicipalidadNoEncontrada;

        var existeRol = await dbContext.Roles.AnyAsync(
            x => x.CodRol == datos.CodRol,
            cancellationToken);

        if (!existeRol)
            return ResultadoCrearUsuario.RolNoEncontrado;

        var usuario = new Usuario
        {
            NombreUsuario = nombreUsuario,
            NombreCompleto = nombreCompleto,
            CorreoElectronico = correo,
            CodMunicipalidad = datos.CodMunicipalidad,
            CodRol = datos.CodRol,
            Estado = datos.Estado,
            FechaExpiracionContrasena = datos.FechaExpiracionContrasena
        };

        usuario.ContrasenaHash = passwordHasher.HashPassword(
            usuario, datos.Contrasena);

        dbContext.Usuarios.Add(usuario);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException sql
                  && (sql.Number == 2601 || sql.Number == 2627))
        {
            dbContext.Entry(usuario).State = EntityState.Detached;

            logger.LogWarning(
                "error, usuario duplicado");

            return ResultadoCrearUsuario.NombreDuplicado;
        }

        logger.LogInformation(
            "Usuario {Codigo} creado",
            usuario.CodUsuario);

        return ResultadoCrearUsuario.Creado;
    }
}