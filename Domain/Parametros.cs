namespace TransMetroUMG.Domain
{
    public class Parametros
    {
        public int CodParametroOperacion { get; set; }
        public string Clave { get; set; } = string.Empty;
        public decimal? Valor { get; set; }
        public string Descripcion { get; set; } = string.Empty;

    }
}
