using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;

namespace TransMetroUMG.Infrastructure;

public sealed class ConsultaBusesService(MunitransDbContext db) : IConsultaBusesService
{
    public async Task<CatalogosConsultaBuses> ObtenerCatalogosAsync(CancellationToken ct)
    {
        var estaciones = await db.Estaciones.AsNoTracking().OrderBy(x => x.NombreEstacion).ThenBy(x => x.CodEstacion)
            .Select(x => new OpcionCatalogo(x.CodEstacion, x.NombreEstacion + " (#" + x.CodEstacion + ")")).ToListAsync(ct);
        var lineas = await db.Lineas.AsNoTracking().OrderBy(x => x.NombreLinea).ThenBy(x => x.CodLinea)
            .Select(x => new OpcionCatalogo(x.CodLinea, x.NombreLinea + " (#" + x.CodLinea + ")")).ToListAsync(ct);
        var buses = await db.Buses.AsNoTracking().OrderBy(x => x.NumeroBus).ThenBy(x => x.CodBus)
            .Select(x => new OpcionCatalogo(x.CodBus, x.NumeroBus + " · " + x.NumeroPlaca)).ToListAsync(ct);
        var pilotos = await db.Pilotos.AsNoTracking().OrderBy(x => x.ApellidosPiloto).ThenBy(x => x.NombresPiloto).ThenBy(x => x.CodPiloto)
            .Select(x => new OpcionCatalogo(x.CodPiloto, x.NombresPiloto + " " + x.ApellidosPiloto + " · " + x.NumeroLicencia)).ToListAsync(ct);
        var municipalidades = await db.Municipalidades.AsNoTracking().OrderBy(x => x.NombreMunicipalidad).ThenBy(x => x.CodMunicipalidad)
            .Select(x => new OpcionCatalogo(x.CodMunicipalidad, x.NombreMunicipalidad + " (#" + x.CodMunicipalidad + ")")).ToListAsync(ct);
        return new(estaciones, lineas, buses, pilotos, municipalidades);
    }

    private IQueryable<BusEnOperacionConsulta> Consulta()
        => db.Database.SqlQuery<BusEnOperacionConsulta>($"""
            SELECT o.COD_OPERACION_BUS AS CodigoOperacion,
                   b.COD_BUS AS CodigoBus, b.NUMERO_BUS AS Bus, b.NUMERO_PLACA AS Placa,
                   b.COD_MUNICIPALIDAD AS CodigoMunicipalidad,
                   l.COD_LINEA AS CodigoLinea, l.NOMBRE_LINEA AS Linea,
                   p.COD_PILOTO AS CodigoPiloto,
                   p.NOMBRES_PILOTO + ' ' + p.APELLIDOS_PILOTO AS Piloto,
                   o.CAPACIDAD_REGISTRADA AS CapacidadMaxima,
                   parada.COD_ESTACION AS CodigoEstacion, e.NOMBRE_ESTACION AS Estacion,
                   CASE WHEN parada.FECHA_HORA_SALIDA IS NOT NULL THEN {EstadosConsultaBuses.EnTrayecto}
                        WHEN parada.COD_ESTACION IS NOT NULL THEN {EstadosConsultaBuses.EnEstacion}
                        ELSE {EstadosConsultaBuses.SinMovimientos} END AS Estado,
                   ocupacion.CANTIDAD_PASAJEROS AS Pasajeros,
                   ocupacion.FECHA_HORA_REGISTRO AS UltimaMedicion,
                   em.NOMBRE_ESTACION AS EstacionMedicion,
                   o.FECHA_HORA_REGISTRO AS InicioOperacion
            FROM dbo.OPERACION_BUS AS o
            INNER JOIN dbo.ASIGNACION_BUS AS a ON a.COD_ASIGNACION_BUS = o.COD_ASIGNACION_BUS
            INNER JOIN dbo.BUS AS b ON b.COD_BUS = a.COD_BUS
            INNER JOIN dbo.LINEA AS l ON l.COD_LINEA = a.COD_LINEA
            INNER JOIN dbo.PILOTO AS p ON p.COD_PILOTO = a.COD_PILOTO
            OUTER APPLY (
                SELECT TOP (1) visita.COD_ESTACION, visita.FECHA_HORA_SALIDA
                FROM dbo.OPERACION_BUS_PARADA AS visita
                WHERE visita.COD_OPERACION_BUS = o.COD_OPERACION_BUS AND visita.FECHA_HORA_LLEGADA IS NOT NULL
                ORDER BY visita.SECUENCIA DESC
            ) AS parada
            LEFT JOIN dbo.ESTACION AS e ON e.COD_ESTACION = parada.COD_ESTACION
            OUTER APPLY (
                SELECT TOP (1) r.CANTIDAD_PASAJEROS, r.FECHA_HORA_REGISTRO, r.COD_ESTACION
                FROM dbo.REGISTRO_OCUPACION AS r
                WHERE r.COD_OPERACION_BUS = o.COD_OPERACION_BUS
                ORDER BY r.FECHA_HORA_REGISTRO DESC, r.COD_REGISTRO_OCUPACION DESC
            ) AS ocupacion
            LEFT JOIN dbo.ESTACION AS em ON em.COD_ESTACION = ocupacion.COD_ESTACION
            WHERE o.SENTIDO_INICIAL IS NOT NULL AND o.FECHA_HORA_CIERRE IS NULL
            """);

    public async Task<ResultadoConsultaBuses> ConsultarAsync(FiltroConsultaBuses filtro, int pagina, CancellationToken ct)
    {
        if (pagina <= 0 || pagina > 1000000 || filtro.Estacion is <= 0 || filtro.Linea is <= 0
            || filtro.Bus is <= 0 || filtro.Piloto is <= 0 || filtro.Municipalidad is <= 0
            || filtro.Estado is not null && !EstadosConsultaBuses.Valores.Contains(filtro.Estado))
            throw new ArgumentException("Los filtros de la consulta no son válidos.");

        var consulta = Consulta();
        if (filtro.Municipalidad.HasValue) consulta = consulta.Where(x => x.CodigoMunicipalidad == filtro.Municipalidad.Value);
        if (filtro.Estacion.HasValue) consulta = consulta.Where(x => x.CodigoEstacion == filtro.Estacion.Value);
        if (filtro.Linea.HasValue) consulta = consulta.Where(x => x.CodigoLinea == filtro.Linea.Value);
        if (filtro.Bus.HasValue) consulta = consulta.Where(x => x.CodigoBus == filtro.Bus.Value);
        if (filtro.Piloto.HasValue) consulta = consulta.Where(x => x.CodigoPiloto == filtro.Piloto.Value);
        if (filtro.Estado is not null) consulta = consulta.Where(x => x.Estado == filtro.Estado);
        var total = await consulta.CountAsync(ct);
        pagina = Math.Min(pagina, (int)Math.Max(1, (total + (long)ResultadoConsultaBuses.TamanoPagina - 1) / ResultadoConsultaBuses.TamanoPagina));
        var filas = await consulta.OrderBy(x => x.Bus).ThenBy(x => x.CodigoOperacion)
            .Skip((pagina - 1) * ResultadoConsultaBuses.TamanoPagina).Take(ResultadoConsultaBuses.TamanoPagina).ToListAsync(ct);
        return new(filas, total, pagina);
    }
}
