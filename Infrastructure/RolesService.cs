using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;

namespace TransMetroUMG.Infrastructure;

public sealed class RolesService(
    MunitransDbContext dbContext,
    ILogger<RolesService> logger) : IRolesService
{
    public async Task<IReadOnlyList<Rol>> ConsultarAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.Roles
            .AsNoTracking()
            .OrderBy(x => x.NombreRol)
            .ThenBy(x => x.CodRol)
            .ToListAsync(cancellationToken);
    }

    public Task<Rol?> ObtenerAsync(
        int codigo,
        CancellationToken cancellationToken)
    {
        return dbContext.Roles
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.CodRol == codigo,
                cancellationToken);
    }

    public async Task<ResultadoGuardarRol> GuardarAsync(
        Rol datos,
        CancellationToken cancellationToken)
    {
        var nombre = datos.NombreRol.Trim();

        if (string.IsNullOrWhiteSpace(nombre) || nombre.Length > 100)
            throw new ArgumentException("nombre del rol inválido.");

        if (datos.DescripcionRol?.Length > 250)
            throw new ArgumentException("tamaño maximo, 250 caracteres.");

        if (!Enum.IsDefined(datos.Estado))
            throw new ArgumentException("estado del rol inválido.");

        var registro = datos.CodRol == 0
            ? new Rol()
            : await dbContext.Roles.SingleOrDefaultAsync(
                x => x.CodRol == datos.CodRol,
                cancellationToken);

        if (registro is null)
            return ResultadoGuardarRol.NoEncontrado;

        var nombreDuplicado = await dbContext.Roles.AnyAsync(
            x => x.NombreRol == nombre && x.CodRol != datos.CodRol,
            cancellationToken);

        if (nombreDuplicado)
            return ResultadoGuardarRol.NombreDuplicado;

        registro.NombreRol = nombre;
        registro.DescripcionRol =
            string.IsNullOrWhiteSpace(datos.DescripcionRol)
                ? null
                : datos.DescripcionRol.Trim();
        registro.Estado = datos.Estado;

        if (datos.CodRol == 0)
            dbContext.Roles.Add(registro);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqlException sql
                  && (sql.Number == 2601 || sql.Number == 2627))
        {
            // La restricción UNIQUE protege también ante guardados simultáneos.
            dbContext.Entry(registro).State = EntityState.Detached;

            logger.LogWarning(
                "No se guardó el rol porque su nombre está duplicado.");

            return ResultadoGuardarRol.NombreDuplicado;
        }

        logger.LogInformation(
            "Rol {Codigo}: {Operacion} realizada.",
            registro.CodRol,
            datos.CodRol == 0 ? "creación" : "actualización");

        return ResultadoGuardarRol.Guardado;
    }
}