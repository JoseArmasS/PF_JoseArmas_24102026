namespace TransMetroUMG.Domain
{
    public enum EstadoRol : byte
    {
        Inactivo = 0,
        Activo = 1
    }
    public class Rol
    {
        public int CodRol { get; set; } 
        public string NombreRol { get; set; } = string.Empty;
        public string? DescripcionRol { get; set; }
        public EstadoRol Estado { get; set; } = EstadoRol.Activo;
    }


}
