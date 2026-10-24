using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;

namespace TransMetroUMG.Infrastructure;

public sealed class ConsultasAsignacionesService(MunitransDbContext db) : IConsultasAsignacionesService
{
    public async Task<PaginaConsulta<PilotoBusConsulta>> ConsultarPilotosAsync(FiltroPilotosBuses filtro, int pagina, CancellationToken ct)
    {
        Validar(pagina, filtro.Municipalidad, filtro.Piloto, filtro.Bus);
        // El filtro de fecha restringe asignaciones antes del LEFT JOIN para conservar pilotos sin asignación en esa fecha.
        var asignaciones = db.AsignacionesBuses.AsNoTracking().AsQueryable();
        if (filtro.Fecha.HasValue) asignaciones = asignaciones.Where(x => x.FechaAsignacion == filtro.Fecha.Value);
        var consulta = from p in db.Pilotos.AsNoTracking()
                       join m in db.Municipalidades on p.CodMunicipalidad equals m.CodMunicipalidad
                       join a in asignaciones on p.CodPiloto equals a.CodPiloto into asignados
                       from a in asignados.DefaultIfEmpty()
                       join b in db.Buses on a.CodBus equals b.CodBus into buses
                       from b in buses.DefaultIfEmpty()
                       join l in db.Lineas on a.CodLinea equals l.CodLinea into lineas
                       from l in lineas.DefaultIfEmpty()
                       select new PilotoBusConsulta
                       {
                           CodigoPiloto = p.CodPiloto, CodigoMunicipalidad = m.CodMunicipalidad,
                           Municipalidad = m.NombreMunicipalidad, Piloto = p.NombresPiloto + " " + p.ApellidosPiloto,
                           Licencia = p.NumeroLicencia, Telefono = p.TelefonoPiloto,
                           CodigoAsignacion = a == null ? null : a.CodAsignacionBus,
                           Fecha = a == null ? null : a.FechaAsignacion,
                           CodigoBus = b == null ? null : b.CodBus, Bus = b == null ? null : b.NumeroBus,
                           Placa = b == null ? null : b.NumeroPlaca, Capacidad = b == null ? null : b.CapacidadPasajeros,
                           Linea = l == null ? null : l.NombreLinea
                       };
        if (filtro.Municipalidad.HasValue) consulta = consulta.Where(x => x.CodigoMunicipalidad == filtro.Municipalidad);
        if (filtro.Piloto.HasValue) consulta = consulta.Where(x => x.CodigoPiloto == filtro.Piloto);
        if (filtro.Bus.HasValue) consulta = consulta.Where(x => x.CodigoBus == filtro.Bus);
        return await PaginarAsync(consulta.OrderBy(x => x.Piloto).ThenBy(x => x.CodigoPiloto)
            .ThenByDescending(x => x.Fecha).ThenBy(x => x.CodigoAsignacion), pagina, ct);
    }

    public async Task<PaginaConsulta<EstacionParqueoBusConsulta>> ConsultarParqueosAsync(FiltroEstacionesParqueos filtro, int pagina, CancellationToken ct)
    {
        Validar(pagina, filtro.Municipalidad, filtro.Estacion, filtro.Parqueo, filtro.Bus);
        var consulta = from e in db.Estaciones.AsNoTracking()
                       join m in db.Municipalidades on e.CodMunicipalidad equals m.CodMunicipalidad
                       join p in db.Parqueos on e.CodEstacion equals p.CodEstacion into parqueos
                       from p in parqueos.DefaultIfEmpty()
                       join b in db.Buses on (int?)p.CodParqueo equals b.CodParqueoBase into buses
                       from b in buses.DefaultIfEmpty()
                       select new EstacionParqueoBusConsulta
                       {
                           CodigoEstacion = e.CodEstacion, CodigoMunicipalidad = m.CodMunicipalidad,
                           Municipalidad = m.NombreMunicipalidad, Estacion = e.NombreEstacion,
                           CodigoParqueo = p == null ? null : p.CodParqueo, Parqueo = p == null ? null : p.NombreParqueo,
                           CapacidadParqueo = p == null ? null : p.CapacidadTotal,
                           CodigoBus = b == null ? null : b.CodBus, Bus = b == null ? null : b.NumeroBus,
                           Placa = b == null ? null : b.NumeroPlaca, CapacidadBus = b == null ? null : b.CapacidadPasajeros
                       };
        if (filtro.Municipalidad.HasValue) consulta = consulta.Where(x => x.CodigoMunicipalidad == filtro.Municipalidad);
        if (filtro.Estacion.HasValue) consulta = consulta.Where(x => x.CodigoEstacion == filtro.Estacion);
        if (filtro.Parqueo.HasValue) consulta = consulta.Where(x => x.CodigoParqueo == filtro.Parqueo);
        if (filtro.Bus.HasValue) consulta = consulta.Where(x => x.CodigoBus == filtro.Bus);
        return await PaginarAsync(consulta.OrderBy(x => x.Estacion).ThenBy(x => x.CodigoEstacion)
            .ThenBy(x => x.Parqueo).ThenBy(x => x.CodigoParqueo).ThenBy(x => x.Bus).ThenBy(x => x.CodigoBus), pagina, ct);
    }

    private static void Validar(int pagina, params int?[] codigos)
    {
        if (pagina is < 1 or > 1000000 || codigos.Any(x => x is <= 0))
            throw new ArgumentException("Los filtros de la consulta no son válidos.");
    }

    private static async Task<PaginaConsulta<T>> PaginarAsync<T>(IQueryable<T> consulta, int pagina, CancellationToken ct)
    {
        var total = await consulta.CountAsync(ct);
        pagina = Math.Min(pagina, (int)Math.Max(1, (total + (long)PaginaConsulta<T>.Tamano - 1) / PaginaConsulta<T>.Tamano));
        var filas = await consulta.Skip((pagina - 1) * PaginaConsulta<T>.Tamano).Take(PaginaConsulta<T>.Tamano).ToListAsync(ct);
        return new(filas, total, pagina);
    }
}
