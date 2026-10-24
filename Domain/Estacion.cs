
namespace TransMetroUMG.Domain
{
    public enum EstadoEstacion : byte
    {
        Inactivo = 0,
        Activo = 1
    }

    public class Estacion
    {
        public int CodEstacion { get; set; }

        public int CodMunicipalidad { get; set; }

        public string NombreEstacion { get; set; } = string.Empty;

        public string DireccionEstacion { get; set; } = string.Empty;

        public decimal? Latitud { get; set; }

        public decimal? Longitud { get; set; }

        public int? CapacidadPasajeros { get; set; }

        public EstadoEstacion Estado { get; set; } = EstadoEstacion.Activo;
    }
}
