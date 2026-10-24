using System.Data;
using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;

namespace TransMetroUMG.Infrastructure;

public sealed class CiclosBusesService(MunitransDbContext db, IOperacionBusesService asignaciones,
    ILogger<CiclosBusesService> logger) : ICiclosBusesService
{
    public async Task<IReadOnlyList<OpcionCatalogo>> ObtenerLineasAsync(int estacion, CancellationToken ct)
        => await db.Lineas.AsNoTracking()
            .Where(x => db.LineasEstaciones.Any(r => r.CodLinea == x.CodLinea && r.CodEstacion == estacion))
            .OrderBy(x => x.NombreLinea).ThenBy(x => x.CodLinea)
            .Select(x => new OpcionCatalogo(x.CodLinea, x.NombreLinea)).ToListAsync(ct);

    public async Task<IReadOnlyList<AsignacionOperacionConsulta>> ObtenerAsignacionesAsync(int estacion, int linea, CancellationToken ct)
        => await db.LineasEstaciones.AnyAsync(x => x.CodLinea == linea && x.CodEstacion == estacion, ct)
            ? await asignaciones.ConsultarAsync(null, ct, linea) : [];

    public async Task<IReadOnlyList<EstacionSalidaOpcion>> ObtenerRecorridoAsync(int linea, CancellationToken ct)
        => await (from recorrido in db.LineasEstaciones.AsNoTracking()
                  join estacion in db.Estaciones on recorrido.CodEstacion equals estacion.CodEstacion
                  where recorrido.CodLinea == linea
                  orderby recorrido.OrdenEstacion
                  select new EstacionSalidaOpcion(estacion.CodEstacion, estacion.NombreEstacion, recorrido.OrdenEstacion))
            .ToListAsync(ct);

    private IQueryable<CicloBusConsulta> Consulta()
        => from operacion in db.OperacionesBuses.AsNoTracking()
           join asignacion in db.AsignacionesBuses on operacion.CodAsignacionBus equals asignacion.CodAsignacionBus
           join bus in db.Buses on asignacion.CodBus equals bus.CodBus
           join piloto in db.Pilotos on asignacion.CodPiloto equals piloto.CodPiloto
           join linea in db.Lineas on asignacion.CodLinea equals linea.CodLinea
           where operacion.SentidoInicial != null
           select new CicloBusConsulta { Operacion = operacion, Bus = bus.NumeroBus + " · " + bus.NumeroPlaca,
               Piloto = piloto.NombresPiloto + " " + piloto.ApellidosPiloto, Linea = linea.NombreLinea };

    private async Task CargarParadasAsync(IReadOnlyList<CicloBusConsulta> ciclos, CancellationToken ct)
    {
        var codigos = ciclos.Select(x => x.Operacion.CodOperacionBus).ToArray();
        if (codigos.Length == 0) return;
        var paradas = await (from parada in db.ParadasOperacionesBuses.AsNoTracking()
                            join estacion in db.Estaciones on parada.CodEstacion equals estacion.CodEstacion
                            join llegada in db.Usuarios on parada.CodUsuarioLlegada equals llegada.CodUsuario into llegadas
                            from llegada in llegadas.DefaultIfEmpty()
                            join salida in db.Usuarios on parada.CodUsuarioSalida equals salida.CodUsuario into salidas
                            from salida in salidas.DefaultIfEmpty()
                            where codigos.Contains(parada.CodOperacionBus)
                            orderby parada.Secuencia
                            select new ParadaCicloConsulta(parada, estacion.NombreEstacion,
                                llegada == null ? null : llegada.NombreCompleto,
                                salida == null ? null : salida.NombreCompleto)).ToListAsync(ct);
        var agrupadas = paradas.ToLookup(x => x.Datos.CodOperacionBus);
        foreach (var ciclo in ciclos) ciclo.Paradas = agrupadas[ciclo.Operacion.CodOperacionBus].ToList();
    }

    public async Task<IReadOnlyList<CicloBusConsulta>> ConsultarActivosAsync(int estacion, CancellationToken ct)
    {
        // No limitar los ciclos activos a los últimos 100 registros históricos.
        var ciclos = await Consulta().Where(x => x.Operacion.FechaHoraCierre == null
            && db.ParadasOperacionesBuses.Any(p => p.CodOperacionBus == x.Operacion.CodOperacionBus && p.CodEstacion == estacion))
            .OrderBy(x => x.Operacion.FechaHoraRegistro).ToListAsync(ct);
        await CargarParadasAsync(ciclos, ct);
        return ciclos.Where(x => x.VisitaAtencion?.Datos.CodEstacion == estacion).ToList();
    }

    public async Task<CicloBusConsulta?> ObtenerAsync(int operacion, CancellationToken ct)
    {
        var ciclo = await Consulta().SingleOrDefaultAsync(x => x.Operacion.CodOperacionBus == operacion, ct);
        if (ciclo is not null) await CargarParadasAsync([ciclo], ct);
        return ciclo;
    }

    public async Task<ResultadoCiclo> IniciarAsync(int estacion, int linea, int asignacion, short sentido, int operador, CancellationToken ct)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        // Mismo bloqueo que la edición de asignaciones para no cambiar de bus o línea al iniciar.
        var registro = await db.AsignacionesBuses.FromSqlInterpolated($"""
            SELECT COD_ASIGNACION_BUS, FECHA_ASIGNACION, COD_BUS, COD_PILOTO, COD_LINEA
            FROM dbo.ASIGNACION_BUS WITH (UPDLOCK, HOLDLOCK) WHERE COD_ASIGNACION_BUS = {asignacion}
            """).SingleOrDefaultAsync(ct);
        if (registro is null || registro.CodLinea != linea)
            return new(false, "La asignación no corresponde a la línea seleccionada.");
        var recorrido = await ObtenerRecorridoAsync(linea, ct);
        IReadOnlyList<int> visitas;
        try { visitas = CicloBus.GenerarRecorrido(recorrido.Select(x => x.Codigo).ToArray(), estacion, sentido); }
        catch (ArgumentException exception) { return new(false, exception.Message); }

        var hora = await ControlCiclosSql.HoraAsync(db, ct);
        if (!await ControlCiclosSql.OperadorVigenteAsync(db, operador, hora, ct))
            return new(false, "Tu cuenta no permite iniciar la operación. Inicia sesión nuevamente.");
        var capacidad = await db.Buses.Where(x => x.CodBus == registro.CodBus).Select(x => x.CapacidadPasajeros).SingleAsync(ct);
        if (capacidad <= 0) return new(false, "El bus no tiene una capacidad válida.");
        var operacion = new OperacionBus { CodAsignacionBus = asignacion, CodEstacionSalida = estacion,
            CodUsuarioOperador = operador, FechaHoraRegistro = hora, FechaHoraInicioEspera = hora,
            CapacidadRegistrada = capacidad, SentidoInicial = sentido };
        db.OperacionesBuses.Add(operacion);
        await db.SaveChangesAsync(ct);
        db.ParadasOperacionesBuses.AddRange(visitas.Select((codigo, secuencia) => new ParadaOperacionBus
        {
            CodOperacionBus = operacion.CodOperacionBus, Secuencia = secuencia, CodEstacion = codigo,
            FechaHoraLlegada = secuencia == 0 ? hora : null, CodUsuarioLlegada = secuencia == 0 ? operador : null
        }));
        await db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
        logger.LogInformation("Ciclo {Ciclo} iniciado por usuario {Operador} en estación {Estacion}.", operacion.CodOperacionBus, operador, estacion);
        return new(true, "Ciclo iniciado. El bus está en espera en la estación seleccionada.", operacion.CodOperacionBus);
    }

    public async Task<ResultadoCiclo> ConfirmarAsync(int operacion, long visita, int estacion, AccionCiclo accion, int operador, CancellationToken ct)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync(ct);
        var registro = await ControlCiclosSql.BloquearAsync(db, operacion, ct);
        if (registro is null) return new(false, "La operación no existe.");
        var paradas = await db.ParadasOperacionesBuses.Where(x => x.CodOperacionBus == operacion).OrderBy(x => x.Secuencia).ToListAsync(ct);
        if (!CicloBus.PuedeConfirmar(registro, paradas, visita, estacion, accion))
            return new(false, "El movimiento ya fue registrado o no corresponde a la siguiente acción. Recarga la pantalla.");
        var hora = await ControlCiclosSql.HoraAsync(db, ct);
        if (!await ControlCiclosSql.OperadorVigenteAsync(db, operador, hora, ct))
            return new(false, "Tu cuenta no permite registrar el movimiento. Inicia sesión nuevamente.");
        var parada = paradas.Single(x => x.CodOperacionBusParada == visita);
        // Proteger también contra cambios hacia atrás en el reloj del servidor.
        var ultimaFecha = paradas.SelectMany(x => new[] { x.FechaHoraLlegada, x.FechaHoraSalida }).Max();
        if (ultimaFecha > hora) return new(false, "La hora del servidor es anterior al último movimiento. Intenta nuevamente.");
        switch (accion)
        {
            case AccionCiclo.Llegada: parada.FechaHoraLlegada = hora; parada.CodUsuarioLlegada = operador; break;
            case AccionCiclo.Salida:
                parada.FechaHoraSalida = hora; parada.CodUsuarioSalida = operador;
                if (parada.Secuencia == 0) registro.FechaHoraSalida = hora;
                break;
            case AccionCiclo.Cierre: registro.FechaHoraCierre = hora; registro.CodUsuarioCierre = operador; break;
        }
        await db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
        logger.LogInformation("Ciclo {Ciclo}: {Accion}, visita {Visita}, estación {Estacion}, usuario {Operador}.", operacion, accion, visita, estacion, operador);
        return new(true, accion switch { AccionCiclo.Llegada => "Llegada registrada.", AccionCiclo.Salida => "Salida registrada.", _ => "Ciclo cerrado correctamente." }, operacion);
    }

    public async Task<ResultadoCiclo> MedirAsync(int operacion, long visita, int estacion, int pasajeros, int operador, CancellationToken ct)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync(ct);
        var registro = await ControlCiclosSql.BloquearAsync(db, operacion, ct);
        if (registro is null) return new(false, "La operación no existe.");
        var paradas = await db.ParadasOperacionesBuses.Where(x => x.CodOperacionBus == operacion).ToListAsync(ct);
        if (!CicloBus.PuedeMedir(registro, paradas, estacion, visita))
            return new(false, "El bus debe estar en esta visita, con llegada registrada y sin salida ni cierre.");
        if (pasajeros < 0 || pasajeros > registro.CapacidadRegistrada)
            return new(false, "Se requieren más buses si el conteo supera la capacidad. Ingresa un conteo entre cero y la capacidad registrada.");
        var hora = await ControlCiclosSql.HoraAsync(db, ct);
        if (!await ControlCiclosSql.OperadorVigenteAsync(db, operador, hora, ct))
            return new(false, "Tu cuenta no permite registrar la medición.");
        if (hora < CicloBus.VisitaActual(paradas)!.FechaHoraLlegada)
            return new(false, "La hora del servidor es anterior a la llegada del bus.");
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO dbo.REGISTRO_OCUPACION
                (COD_OPERACION_BUS, COD_ESTACION, COD_USUARIO_OPERADOR, FECHA_HORA_REGISTRO, CANTIDAD_PASAJEROS)
            VALUES ({operacion}, {estacion}, {operador}, {hora}, {pasajeros})
            """, ct);
        await transaccion.CommitAsync(ct);
        logger.LogInformation("Medición registrada para ciclo {Ciclo}, visita {Visita}, usuario {Operador}.", operacion, visita, operador);
        return new(true, "Ocupación registrada.", operacion);
    }
}
