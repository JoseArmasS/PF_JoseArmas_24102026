namespace TransMetroUMG.Models;

public sealed record TiempoMedicionViewModel(DateTime? Medicion, DateTime? Referencia)
{
    public string TiempoTranscurrido
    {
        get
        {
            if (!Medicion.HasValue) return "Sin medición";
            if (!Referencia.HasValue) return "Tiempo transcurrido no disponible";
            if (Medicion.Value > Referencia.Value) return "Medición posterior a la hora de consulta";
            var tiempo = Referencia.Value - Medicion.Value;
            if (tiempo.Days > 0) return $"{tiempo.Days} d {tiempo.Hours} h {tiempo.Minutes} min";
            if (tiempo.Hours > 0) return $"{tiempo.Hours} h {tiempo.Minutes} min";
            if (tiempo.Minutes > 0) return $"{tiempo.Minutes} min {tiempo.Seconds} s";
            return $"{tiempo.Seconds} s";
        }
    }
}
