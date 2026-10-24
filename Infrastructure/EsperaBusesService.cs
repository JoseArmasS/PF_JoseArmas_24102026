using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;

namespace TransMetroUMG.Infrastructure;

public sealed class EsperaBusesService(MunitransDbContext db) : IEsperaBusesService
{
    public async Task<IReadOnlyDictionary<long, EstadoEsperaBus>> ConsultarAsync(
        IReadOnlyList<CicloBusConsulta> ciclos, DateTime ahora, CancellationToken ct)
    {
        var visitas = ciclos.Where(x => !x.EnTrayecto && x.Siguiente is not null
            && !x.Operacion.FechaHoraCierre.HasValue && x.Actual?.Datos.FechaHoraLlegada is not null).ToList();
        if (visitas.Count == 0) return new Dictionary<long, EstadoEsperaBus>();
        string[] claves = [EsperaBus.ClaveEspera, EsperaBus.ClaveAdicional, EsperaBus.ClaveMinimo];
        var parametros = await db.ParametrosOperacion.AsNoTracking().Where(x => claves.Contains(x.Clave))
            .ToDictionaryAsync(x => x.Clave, x => x.Valor, ct);
        parametros.TryGetValue(EsperaBus.ClaveEspera, out var espera);
        parametros.TryGetValue(EsperaBus.ClaveAdicional, out var adicional);
        parametros.TryGetValue(EsperaBus.ClaveMinimo, out var minimo);
        if (!EsperaBus.ParametrosValidos(espera, adicional, minimo))
            return visitas.ToDictionary(x => x.Actual!.Datos.CodOperacionBusParada,
                _ => new EstadoEsperaBus("ConfiguracionIncompleta", "Configura ESPERA_BUS, ESPERA_ADICIONAL_BUS y PORCENTAJE_OCUPACION_MINIMA_BUS con valores válidos.", 0));

        var minutos = (int)espera!.Value;
        var codigos = visitas.Select(x => x.Actual!.Datos.CodOperacionBusParada).ToArray();
        // Una consulta para todas las visitas: no reutilizar mediciones de otra estación o de una visita anterior.
        var mediciones = await db.Database.SqlQuery<MedicionCorteEspera>($"""
            SELECT p.COD_OPERACION_BUS_PARADA AS Visita, ultima.CANTIDAD_PASAJEROS AS Pasajeros
            FROM dbo.OPERACION_BUS_PARADA AS p
            OUTER APPLY (
                SELECT TOP (1) r.CANTIDAD_PASAJEROS
                FROM dbo.REGISTRO_OCUPACION AS r
                WHERE r.COD_OPERACION_BUS = p.COD_OPERACION_BUS AND r.COD_ESTACION = p.COD_ESTACION
                    AND r.FECHA_HORA_REGISTRO >= p.FECHA_HORA_LLEGADA
                    AND r.FECHA_HORA_REGISTRO <= DATEADD(MINUTE, {minutos}, p.FECHA_HORA_LLEGADA)
                    AND r.FECHA_HORA_REGISTRO <= {ahora}
                ORDER BY r.FECHA_HORA_REGISTRO DESC, r.COD_REGISTRO_OCUPACION DESC
            ) AS ultima
            WHERE p.FECHA_HORA_LLEGADA IS NOT NULL AND p.FECHA_HORA_SALIDA IS NULL
            """).Where(x => codigos.Contains(x.Visita)).ToDictionaryAsync(x => x.Visita, x => x.Pasajeros, ct);
        return visitas.ToDictionary(x => x.Actual!.Datos.CodOperacionBusParada, x =>
            EsperaBus.Calcular(x.Actual!.Datos.FechaHoraLlegada!.Value, ahora, x.Operacion.CapacidadRegistrada,
                mediciones.GetValueOrDefault(x.Actual.Datos.CodOperacionBusParada), minutos, (int)adicional!.Value, minimo!.Value));
    }
}

public sealed class MedicionCorteEspera
{
    public long Visita { get; set; }
    public int? Pasajeros { get; set; }
}
