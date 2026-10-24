using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;

namespace TransMetroUMG.Infrastructure;

public sealed class RegistroOperacionBusService(MunitransDbContext dbContext,
    ILogger<RegistroOperacionBusService> logger) : IRegistroOperacionBusService
{
    public async Task<IReadOnlyList<EstacionOperacionOpcion>> ObtenerRecorridosAsync(int[] asignaciones, CancellationToken cancellationToken)
        => await (from asignacion in dbContext.AsignacionesBuses.AsNoTracking()
                  join recorrido in dbContext.LineasEstaciones on asignacion.CodLinea equals recorrido.CodLinea
                  join estacion in dbContext.Estaciones on recorrido.CodEstacion equals estacion.CodEstacion
                  where asignaciones.Contains(asignacion.CodAsignacionBus)
                  orderby recorrido.OrdenEstacion
                  select new EstacionOperacionOpcion(asignacion.CodAsignacionBus, estacion.CodEstacion,
                      estacion.NombreEstacion, recorrido.OrdenEstacion)).ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<OperacionBusConsulta>> ConsultarAsync(CancellationToken cancellationToken)
        => await Consulta().OrderByDescending(x => x.Registro).ThenByDescending(x => x.Codigo)
            .Take(100).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OperacionBusConsulta>> ConsultarEnEstacionAsync(int estacion, CancellationToken cancellationToken)
        => await Consulta().Where(x => dbContext.OperacionesBuses.Any(o => o.CodOperacionBus == x.Codigo
                && o.SentidoInicial != null && o.FechaHoraCierre == null)
            && dbContext.ParadasOperacionesBuses.Any(p => p.CodOperacionBus == x.Codigo
                && p.CodEstacion == estacion && p.FechaHoraLlegada != null && p.FechaHoraSalida == null))
            .OrderByDescending(x => x.Registro).ThenByDescending(x => x.Codigo).ToListAsync(cancellationToken);

    private IQueryable<OperacionBusConsulta> Consulta()
        => dbContext.Database.SqlQuery<OperacionBusConsulta>($"""
            SELECT o.COD_OPERACION_BUS AS Codigo, o.COD_ASIGNACION_BUS AS Asignacion,
                   b.NUMERO_BUS AS Bus, l.NOMBRE_LINEA AS Linea,
                   e.NOMBRE_ESTACION AS EstacionSalida, u.NOMBRE_COMPLETO AS Operador,
                   o.FECHA_HORA_REGISTRO AS Registro, o.FECHA_HORA_INICIO_ESPERA AS InicioEspera,
                   o.FECHA_HORA_SALIDA AS Salida, o.CAPACIDAD_REGISTRADA AS Capacidad, o.SENTIDO_INICIAL AS SentidoInicial,
                   ultima.CANTIDAD_PASAJEROS AS UltimosPasajeros,
                   ultima.FECHA_HORA_REGISTRO AS UltimaMedicion, ultima.NOMBRE_ESTACION AS UltimaEstacion
            FROM dbo.OPERACION_BUS AS o
            INNER JOIN dbo.ASIGNACION_BUS AS a ON a.COD_ASIGNACION_BUS = o.COD_ASIGNACION_BUS
            INNER JOIN dbo.BUS AS b ON b.COD_BUS = a.COD_BUS
            INNER JOIN dbo.LINEA AS l ON l.COD_LINEA = a.COD_LINEA
            INNER JOIN dbo.ESTACION AS e ON e.COD_ESTACION = o.COD_ESTACION_SALIDA
            INNER JOIN dbo.USUARIO AS u ON u.COD_USUARIO = o.COD_USUARIO_OPERADOR
            OUTER APPLY (
                SELECT TOP (1) r.CANTIDAD_PASAJEROS, r.FECHA_HORA_REGISTRO, em.NOMBRE_ESTACION
                FROM dbo.REGISTRO_OCUPACION AS r
                INNER JOIN dbo.ESTACION AS em ON em.COD_ESTACION = r.COD_ESTACION
                WHERE r.COD_OPERACION_BUS = o.COD_OPERACION_BUS
                ORDER BY r.FECHA_HORA_REGISTRO DESC, r.COD_REGISTRO_OCUPACION DESC
            ) AS ultima
            """);

    public async Task<IReadOnlyList<EstacionSalidaOpcion>> ObtenerRecorridoAsync(int asignacion, CancellationToken cancellationToken)
        => await (from registro in dbContext.AsignacionesBuses.AsNoTracking()
                  join recorrido in dbContext.LineasEstaciones on registro.CodLinea equals recorrido.CodLinea
                  join estacion in dbContext.Estaciones on recorrido.CodEstacion equals estacion.CodEstacion
                  where registro.CodAsignacionBus == asignacion
                  orderby recorrido.OrdenEstacion
                  select new EstacionSalidaOpcion(estacion.CodEstacion, estacion.NombreEstacion, recorrido.OrdenEstacion))
            .ToListAsync(cancellationToken);

    public async Task<ResultadoIniciarOperacion> IniciarAsync(int asignacion, int estacion, int operador, CancellationToken cancellationToken)
    {
        if (asignacion <= 0 || estacion <= 0 || operador <= 0)
            throw new ArgumentException("Los datos de la operación no son válidos.");
        // La capacidad se obtiene del bus y el operador de la sesión; nunca del formulario.
        // El inicio de la operación marca el inicio de espera en la estación de salida.
        var filas = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO dbo.OPERACION_BUS
                (COD_ASIGNACION_BUS, COD_ESTACION_SALIDA, COD_USUARIO_OPERADOR,
                 FECHA_HORA_REGISTRO, FECHA_HORA_INICIO_ESPERA, CAPACIDAD_REGISTRADA)
            SELECT a.COD_ASIGNACION_BUS, {estacion}, u.COD_USUARIO,
                   SYSDATETIME(), SYSDATETIME(), b.CAPACIDAD_PASAJEROS
            FROM dbo.ASIGNACION_BUS AS a
            INNER JOIN dbo.BUS AS b ON b.COD_BUS = a.COD_BUS
            INNER JOIN dbo.USUARIO AS u ON u.COD_USUARIO = {operador}
            WHERE a.COD_ASIGNACION_BUS = {asignacion} AND b.CAPACIDAD_PASAJEROS > 0
                AND u.COD_ESTADO_USUARIO = {(byte)EstadoUsuario.Activo}
                AND (u.FECHA_EXPIRACION_CONTRASENA IS NULL OR u.FECHA_EXPIRACION_CONTRASENA > SYSDATETIME())
                AND EXISTS (SELECT 1 FROM dbo.LINEA_ESTACION AS le
                    WHERE le.COD_LINEA = a.COD_LINEA AND le.COD_ESTACION = {estacion})
            """, cancellationToken);
        if (filas > 0)
        {
            logger.LogInformation("Operación iniciada para asignación {Asignacion}, estación {Estacion}, usuario {Operador}.", asignacion, estacion, operador);
            return ResultadoIniciarOperacion.Guardado;
        }
        if (!await dbContext.AsignacionesBuses.AnyAsync(x => x.CodAsignacionBus == asignacion, cancellationToken))
            return ResultadoIniciarOperacion.AsignacionNoValida;
        if (!(await ObtenerRecorridoAsync(asignacion, cancellationToken)).Any(x => x.Codigo == estacion))
            return ResultadoIniciarOperacion.EstacionFueraDeLinea;
        return ResultadoIniciarOperacion.OperadorNoValido;
    }
}
