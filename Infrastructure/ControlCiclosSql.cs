using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Domain;

namespace TransMetroUMG.Infrastructure;

internal static class ControlCiclosSql
{
    // Toda escritura del ciclo bloquea primero esta fila dentro de una transacción.
    // Incluye las mediciones enviadas por las pantallas anteriores.
    public static Task<OperacionBus?> BloquearAsync(MunitransDbContext db, int codigo, CancellationToken ct)
        => db.OperacionesBuses.FromSqlInterpolated($"""
            SELECT COD_OPERACION_BUS, COD_ASIGNACION_BUS, COD_ESTACION_SALIDA,
                   COD_USUARIO_OPERADOR, FECHA_HORA_REGISTRO, FECHA_HORA_INICIO_ESPERA,
                   FECHA_HORA_SALIDA, CAPACIDAD_REGISTRADA, SENTIDO_INICIAL,
                   FECHA_HORA_CIERRE, COD_USUARIO_CIERRE
            FROM dbo.OPERACION_BUS WITH (UPDLOCK, HOLDLOCK)
            WHERE COD_OPERACION_BUS = {codigo}
            """).SingleOrDefaultAsync(ct);

    public static Task<DateTime> HoraAsync(MunitransDbContext db, CancellationToken ct)
        => db.Database.SqlQuery<DateTime>($"SELECT SYSDATETIME() AS [Value]").SingleAsync(ct);

    public static Task<bool> OperadorVigenteAsync(MunitransDbContext db, int operador, DateTime hora, CancellationToken ct)
        => db.Usuarios.AnyAsync(x => x.CodUsuario == operador && x.Estado == EstadoUsuario.Activo
            && (x.FechaExpiracionContrasena == null || x.FechaExpiracionContrasena > hora), ct);
}
