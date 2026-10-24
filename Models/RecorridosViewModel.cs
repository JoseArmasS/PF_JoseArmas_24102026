using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;

namespace TransMetroUMG.Models;

public sealed class RecorridoFormulario
{
    [Range(0, int.MaxValue)]
    public int CodLineaEstacion { get; set; }

    [Required(ErrorMessage = "Selecciona una línea.")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una línea válida.")]
    public int? CodLinea { get; set; }

    [Required(ErrorMessage = "Selecciona una estación.")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una estación válida.")]
    public int? CodEstacion { get; set; }

    [Required(ErrorMessage = "Ingresa el orden de la estación.")]
    [Range(1, int.MaxValue, ErrorMessage = "El orden debe ser mayor que cero.")]
    [Display(Name = "Orden de la estación")]
    public int? OrdenEstacion { get; set; }

    [Range(typeof(decimal), "0.001", "99999.999", ErrorMessage = "Ingresa una distancia mayor que cero y de hasta 99999.999 km.")]
    public decimal? DistanciaSiguienteKm { get; set; }
}

public sealed class RecorridosViewModel
{
    public RecorridoFormulario Formulario { get; set; } = new();

    [BindNever]
    public IReadOnlyList<LineaEstacion> Registros { get; set; } = [];

    [BindNever]
    public IReadOnlyList<OpcionCatalogo> Lineas { get; set; } = [];

    [BindNever]
    public IReadOnlyList<OpcionCatalogo> Estaciones { get; set; } = [];

    [BindNever]
    public bool ErrorCarga { get; set; }

    [BindNever]
    public IReadOnlyDictionary<int, int> EstacionesSiguientes { get; set; } = new Dictionary<int, int>();
}
