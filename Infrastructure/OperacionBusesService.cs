using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;

namespace TransMetroUMG.Infrastructure;

public sealed class OperacionBusesService(MunitransDbContext dbContext) : IOperacionBusesService
{
    public async Task<IReadOnlyList<AsignacionOperacionConsulta>> ConsultarAsync(
        DateOnly? fecha, CancellationToken cancellationToken, int? codigoLinea = null)
    {
       
        var consulta = dbContext.Database.SqlQuery<AsignacionOperacionConsulta>($"""
            SELECT a.COD_ASIGNACION_BUS AS Codigo, a.FECHA_ASIGNACION AS Fecha,
                   m.NOMBRE_MUNICIPALIDAD AS Municipalidad, b.NUMERO_BUS AS NumeroBus,
                   b.NUMERO_PLACA AS Placa, b.CAPACIDAD_PASAJEROS AS Capacidad,
                   b.COD_ESTADO_BUS AS EstadoBus,
                   p.NOMBRES_PILOTO + ' ' + p.APELLIDOS_PILOTO AS Piloto,
                   l.NOMBRE_LINEA AS Linea, l.COD_LINEA AS CodigoLinea
            FROM dbo.ASIGNACION_BUS AS a
            INNER JOIN dbo.BUS AS b ON b.COD_BUS = a.COD_BUS
            INNER JOIN dbo.MUNICIPALIDAD AS m ON m.COD_MUNICIPALIDAD = b.COD_MUNICIPALIDAD
            INNER JOIN dbo.PILOTO AS p ON p.COD_PILOTO = a.COD_PILOTO
            INNER JOIN dbo.LINEA AS l ON l.COD_LINEA = a.COD_LINEA
            """);

        if (fecha.HasValue)
            consulta = consulta.Where(x => x.Fecha == fecha.Value);
        if (codigoLinea.HasValue)
            consulta = consulta.Where(x => x.CodigoLinea == codigoLinea.Value);

        return await consulta.OrderByDescending(x => x.Fecha)
            .ThenBy(x => x.NumeroBus).ThenBy(x => x.Codigo)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OpcionCatalogo>> ObtenerLineasAsync(CancellationToken cancellationToken)
        => await (from linea in dbContext.Lineas.AsNoTracking()
                  join municipalidad in dbContext.Municipalidades on linea.CodMunicipalidad equals municipalidad.CodMunicipalidad
                  orderby municipalidad.NombreMunicipalidad, linea.NombreLinea, linea.CodLinea
                  select new OpcionCatalogo(linea.CodLinea, municipalidad.NombreMunicipalidad + " / " + linea.NombreLinea))
            .ToListAsync(cancellationToken);
}
