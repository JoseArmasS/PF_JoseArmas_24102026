using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;

namespace TransMetroUMG.Infrastructure;

public sealed class OcupacionEstacionesService(MunitransDbContext dbContext,
    ILogger<OcupacionEstacionesService> logger) : IOcupacionEstacionesService
{
    public async Task<IReadOnlyList<EstacionOcupacionConsulta>> ConsultarEstacionesAsync(CancellationToken cancellationToken)
        => await (from estacion in dbContext.Estaciones.AsNoTracking()
                  join municipalidad in dbContext.Municipalidades on estacion.CodMunicipalidad equals municipalidad.CodMunicipalidad
                  orderby municipalidad.NombreMunicipalidad, estacion.NombreEstacion, estacion.CodEstacion
                  select new EstacionOcupacionConsulta { Codigo = estacion.CodEstacion, Nombre = estacion.NombreEstacion,
                      Municipalidad = municipalidad.NombreMunicipalidad, Capacidad = estacion.CapacidadPasajeros })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MedicionEstacionConsulta>> ConsultarHistorialAsync(
        int estacion, int pagina, CancellationToken cancellationToken)
    {
        if (estacion <= 0 || pagina is < 1 or > 1000000) throw new ArgumentOutOfRangeException(nameof(pagina));
        // Un registro adicional permite detectar la siguiente página sin cargar todo el historial.
        return await dbContext.Database.SqlQuery<MedicionEstacionConsulta>($"""
            SELECT r.COD_REGISTRO_OCUPACION_ESTACION AS Codigo, r.FECHA_HORA_REGISTRO AS Fecha,
                   r.CANTIDAD_PASAJEROS AS Pasajeros, r.CAPACIDAD_REGISTRADA AS Capacidad,
                   u.NOMBRE_COMPLETO AS Operador
            FROM dbo.REGISTRO_OCUPACION_ESTACION AS r
            INNER JOIN dbo.USUARIO AS u ON u.COD_USUARIO = r.COD_USUARIO_OPERADOR
            WHERE r.COD_ESTACION = {estacion}
            """).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Codigo)
            .Skip((pagina - 1) * 20).Take(21).ToListAsync(cancellationToken);
    }

    public async Task<ResultadoRegistrarOcupacion> RegistrarAsync(
        int estacion, int pasajeros, int operador, CancellationToken cancellationToken)
    {
        if (estacion <= 0 || pasajeros < 0 || operador <= 0) throw new ArgumentException("Datos de medición inválidos.");
        // INSERT SELECT toma la capacidad directamente de la estación en la misma instrucción.
        // Ni la identidad del operador ni la capacidad se aceptan desde el formulario.
        var filas = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO dbo.REGISTRO_OCUPACION_ESTACION
                (COD_ESTACION, COD_USUARIO_OPERADOR, CANTIDAD_PASAJEROS, CAPACIDAD_REGISTRADA)
            SELECT e.COD_ESTACION, u.COD_USUARIO, {pasajeros}, e.CAPACIDAD_PASAJEROS
            FROM dbo.ESTACION AS e
            INNER JOIN dbo.USUARIO AS u ON u.COD_USUARIO = {operador}
            WHERE e.COD_ESTACION = {estacion} AND e.CAPACIDAD_PASAJEROS > 0
                AND u.COD_ESTADO_USUARIO = {(byte)EstadoUsuario.Activo}
                AND (u.FECHA_EXPIRACION_CONTRASENA IS NULL OR u.FECHA_EXPIRACION_CONTRASENA > SYSDATETIME())
            """, cancellationToken);
        if (filas > 0)
        {
            logger.LogInformation("Ocupación registrada en estación {Estacion} por usuario {Operador}.", estacion, operador);
            return ResultadoRegistrarOcupacion.Guardado;
        }
        var datos = await dbContext.Estaciones.AsNoTracking().SingleOrDefaultAsync(x => x.CodEstacion == estacion, cancellationToken);
        return datos is null ? ResultadoRegistrarOcupacion.EstacionNoEncontrada
            : datos.CapacidadPasajeros is null or <= 0 ? ResultadoRegistrarOcupacion.SinCapacidad
            : ResultadoRegistrarOcupacion.OperadorNoValido;
    }
}
