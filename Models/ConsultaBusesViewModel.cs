using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TransMetroUMG.Application;

namespace TransMetroUMG.Models;

public sealed class ConsultaBusesViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una municipalidad válida.")] public int? Municipalidad { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una estación válida.")] public int? Estacion { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una línea válida.")] public int? Linea { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un bus válido.")] public int? Bus { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un piloto válido.")] public int? Piloto { get; set; }
    public string? Estado { get; set; }
    [Range(1, 1000000, ErrorMessage = "La página solicitada no es válida.")] public int Pagina { get; set; } = 1;
    [BindNever] public CatalogosConsultaBuses Catalogos { get; set; } = new([], [], [], [], []);
    [BindNever] public ResultadoConsultaBuses Resultado { get; set; } = new([], 0, 1);
    [BindNever] public DateTime? HoraReferencia { get; set; }
    [BindNever] public bool ErrorCarga { get; set; }
}
