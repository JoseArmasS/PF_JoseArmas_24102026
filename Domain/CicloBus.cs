namespace TransMetroUMG.Domain;

public enum AccionCiclo { Llegada, Salida, Cierre }

public static class CicloBus
{
    public const short OrdenCreciente = 1;
    public const short OrdenDecreciente = -1;

    /// <summary>Recorre ambos extremos y vuelve al origen. La lista debe estar ordenada por la línea.</summary>
    public static IReadOnlyList<int> GenerarRecorrido(IReadOnlyList<int> estaciones, int origen, short sentido)
    {
        if (estaciones.Count < 2 || estaciones.Distinct().Count() != estaciones.Count)
            throw new ArgumentException("El recorrido necesita al menos dos estaciones diferentes y sin duplicados.");
        var posicion = Array.IndexOf(estaciones.ToArray(), origen);
        if (posicion < 0 || sentido is not (OrdenCreciente or OrdenDecreciente)
            || posicion == 0 && sentido != OrdenCreciente
            || posicion == estaciones.Count - 1 && sentido != OrdenDecreciente)
            throw new ArgumentException("La estación o el sentido inicial no son válidos para esta línea.");

        var visitas = new List<int> { origen };
        for (var paso = 0; paso < 2 * (estaciones.Count - 1); paso++)
        {
            if (posicion == 0) sentido = OrdenCreciente;
            if (posicion == estaciones.Count - 1) sentido = OrdenDecreciente;
            posicion += sentido;
            visitas.Add(estaciones[posicion]);
        }
        return visitas;
    }

    public static ParadaOperacionBus? VisitaActual(IEnumerable<ParadaOperacionBus> paradas)
        => paradas.Where(x => x.FechaHoraLlegada.HasValue).MaxBy(x => x.Secuencia);

    public static bool PuedeMedir(OperacionBus operacion, IEnumerable<ParadaOperacionBus> paradas,
        int estacion, long? visita = null)
    {
        var actual = VisitaActual(paradas);
        return operacion.SentidoInicial.HasValue && !operacion.FechaHoraCierre.HasValue
            && actual is not null && actual.CodEstacion == estacion && !actual.FechaHoraSalida.HasValue
            && (!visita.HasValue || actual.CodOperacionBusParada == visita.Value);
    }

    public static bool PuedeConfirmar(OperacionBus operacion, IReadOnlyList<ParadaOperacionBus> paradas,
        long visita, int estacion, AccionCiclo accion)
    {
        if (!operacion.SentidoInicial.HasValue || operacion.FechaHoraCierre.HasValue) return false;
        var actual = VisitaActual(paradas);
        if (actual is null) return false;
        var siguiente = paradas.SingleOrDefault(x => x.Secuencia == actual.Secuencia + 1);
        return accion switch
        {
            AccionCiclo.Llegada => actual.FechaHoraSalida.HasValue && siguiente is not null
                && !siguiente.FechaHoraLlegada.HasValue && siguiente.CodOperacionBusParada == visita
                && siguiente.CodEstacion == estacion,
            AccionCiclo.Salida => !actual.FechaHoraSalida.HasValue && siguiente is not null
                && actual.CodOperacionBusParada == visita && actual.CodEstacion == estacion,
            AccionCiclo.Cierre => siguiente is null && !actual.FechaHoraSalida.HasValue
                && actual.CodOperacionBusParada == visita && actual.CodEstacion == estacion
                && actual.CodEstacion == operacion.CodEstacionSalida && actual.Secuencia > 0
                && paradas.All(x => x.FechaHoraLlegada.HasValue
                    && (x.Secuencia == actual.Secuencia || x.FechaHoraSalida.HasValue)),
            _ => false
        };
    }
}
