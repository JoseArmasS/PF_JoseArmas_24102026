namespace TransMetroUMG.Domain;

public static class OcupacionEstacion
{
    public const decimal UmbralAlertaPorcentaje = 150m;

    public static decimal CalcularPorcentaje(int pasajeros, int capacidad)
    {
        if (pasajeros < 0 || capacidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacidad), "El conteo y la capacidad no son válidos.");
        return pasajeros * 100m / capacidad;
    }

    public static bool RequiereApoyo(int pasajeros, int capacidad)
        => CalcularPorcentaje(pasajeros, capacidad) >= UmbralAlertaPorcentaje;
}
