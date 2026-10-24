using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;

namespace TransMetroUMG.Infrastructure;

public sealed class OcupacionBusesService(MunitransDbContext dbContext,
    ILogger<OcupacionBusesService> logger) : IOcupacionBusesService
{
    public Task<OperacionBusConsulta?> ObtenerOperacionAsync(int operacion, CancellationToken cancellationToken)
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
            WHERE o.COD_OPERACION_BUS = {operacion}
            """).SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<MedicionBusConsulta>> ConsultarAsync(int operacion, int pagina, CancellationToken cancellationToken)
    {
        if (operacion <= 0 || pagina is < 1 or > 1000000) throw new ArgumentOutOfRangeException(nameof(pagina));
        return await dbContext.Database.SqlQuery<MedicionBusConsulta>($"""
            SELECT r.COD_REGISTRO_OCUPACION AS Codigo, r.FECHA_HORA_REGISTRO AS Fecha,
                   e.NOMBRE_ESTACION AS Estacion, r.CANTIDAD_PASAJEROS AS Pasajeros,
                   u.NOMBRE_COMPLETO AS Operador
            FROM dbo.REGISTRO_OCUPACION AS r
            INNER JOIN dbo.ESTACION AS e ON e.COD_ESTACION = r.COD_ESTACION
            INNER JOIN dbo.USUARIO AS u ON u.COD_USUARIO = r.COD_USUARIO_OPERADOR
            WHERE r.COD_OPERACION_BUS = {operacion}
            """).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Codigo)
            .Skip((pagina - 1) * 20).Take(21).ToListAsync(cancellationToken);
    }

    public async Task<ResultadoMedicionBus> RegistrarAsync(int operacion, int estacion, int pasajeros,
        int operador, CancellationToken cancellationToken)
    {
        if (operacion <= 0 || estacion <= 0 || pasajeros < 0 || operador <= 0)
            throw new ArgumentException("Los datos de la medición no son válidos.");
        await using var transaccion = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var ciclo = await ControlCiclosSql.BloquearAsync(dbContext, operacion, cancellationToken);
        if (ciclo is null) return ResultadoMedicionBus.OperacionNoValida;
        if (ciclo.SentidoInicial.HasValue)
        {
            // Los ciclos se miden desde la visita explícita para evitar reutilizar una
            // pantalla antigua cuando el bus vuelve a pasar por la misma estación.
            return ResultadoMedicionBus.VisitaNoValida;
        }
        // Validar capacidad, estación de la línea y operador en la misma instrucción
        // que inserta. No se modifican tiempos ni registros anteriores.
        var filas = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO dbo.REGISTRO_OCUPACION
                (COD_OPERACION_BUS, COD_ESTACION, COD_USUARIO_OPERADOR, CANTIDAD_PASAJEROS)
            SELECT o.COD_OPERACION_BUS, {estacion}, u.COD_USUARIO, {pasajeros}
            FROM dbo.OPERACION_BUS AS o
            INNER JOIN dbo.ASIGNACION_BUS AS a ON a.COD_ASIGNACION_BUS = o.COD_ASIGNACION_BUS
            INNER JOIN dbo.USUARIO AS u ON u.COD_USUARIO = {operador}
            WHERE o.COD_OPERACION_BUS = {operacion} AND o.CAPACIDAD_REGISTRADA > 0
                AND {pasajeros} <= o.CAPACIDAD_REGISTRADA
                AND u.COD_ESTADO_USUARIO = {(byte)EstadoUsuario.Activo}
                AND (u.FECHA_EXPIRACION_CONTRASENA IS NULL OR u.FECHA_EXPIRACION_CONTRASENA > SYSDATETIME())
                AND EXISTS (SELECT 1 FROM dbo.LINEA_ESTACION AS le
                    WHERE le.COD_LINEA = a.COD_LINEA AND le.COD_ESTACION = {estacion})
            """, cancellationToken);
        if (filas > 0)
        {
            await transaccion.CommitAsync(cancellationToken);
            logger.LogInformation("Ocupación de bus registrada: operación {Operacion}, estación {Estacion}, usuario {Operador}.", operacion, estacion, operador);
            return ResultadoMedicionBus.Guardado;
        }
        var datos = await ObtenerOperacionAsync(operacion, cancellationToken);
        if (datos is null || datos.Capacidad <= 0) return ResultadoMedicionBus.OperacionNoValida;
        if (pasajeros > datos.Capacidad) return ResultadoMedicionBus.CapacidadExcedida;
        if (!await dbContext.LineasEstaciones.AnyAsync(le => le.CodEstacion == estacion
            && dbContext.AsignacionesBuses.Any(a => a.CodAsignacionBus == datos.Asignacion && a.CodLinea == le.CodLinea), cancellationToken))
            return ResultadoMedicionBus.EstacionFueraDeLinea;
        return ResultadoMedicionBus.OperadorNoValido;
    }
}
