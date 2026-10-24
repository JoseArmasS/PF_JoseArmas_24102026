namespace TransMetroUMG.Domain;

public sealed record EstadoEsperaBus(string Estado, string Mensaje, double SegundosRestantes);

public static class EsperaBus
{
    public const string ClaveEspera = "ESPERA_BUS";
    public const string ClaveAdicional = "ESPERA_ADICIONAL_BUS";
    public const string ClaveMinimo = "PORCENTAJE_OCUPACION_MINIMA_BUS";

    public static bool ParametrosValidos(decimal? espera, decimal? adicional, decimal? minimo)
        => espera is >= 0 and <= int.MaxValue && decimal.Truncate(espera.Value) == espera
        && adicional is >= 0 and <= int.MaxValue && decimal.Truncate(adicional.Value) == adicional
        && minimo is >= 0 and <= 100;

    // La ocupación recibida corresponde al cierre de la espera inicial, nunca a una medición posterior.
    public static EstadoEsperaBus Calcular(DateTime llegada, DateTime ahora, int capacidad,
        int? pasajerosAlCorte, int espera, int adicional, decimal minimo)
    {
        if (!ParametrosValidos(espera, adicional, minimo) || capacidad <= 0
            || pasajerosAlCorte is < 0 || pasajerosAlCorte > capacidad)
            return new("ConfiguracionIncompleta", "Revisa los parámetros de espera y la capacidad del bus.", 0);
        DateTime corte;
        DateTime finAdicional;
        try { corte = llegada.AddMinutes(espera); finAdicional = corte.AddMinutes(adicional); }
        catch (ArgumentOutOfRangeException)
        { return new("ConfiguracionIncompleta", "Los tiempos configurados exceden el rango de fechas permitido.", 0); }
        if (ahora < corte)
            return new("Inicial", "Espera inicial", (corte - ahora).TotalSeconds);
        if (!pasajerosAlCorte.HasValue)
            return new("SinMedicion", "Sin medición de esta visita al finalizar la espera inicial. No se puede evaluar la extensión; confirma la salida manualmente cuando corresponda.", 0);
        if (pasajerosAlCorte.Value * 100m / capacidad < minimo && ahora < finAdicional)
            return new("Adicional", "Espera adicional única por ocupación inferior al mínimo", (finAdicional - ahora).TotalSeconds);
        return new("Listo", "Tiempo cumplido. Aviso de salida: confirma la salida física del bus.", 0);
    }
}
