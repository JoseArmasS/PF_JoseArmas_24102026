using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TransMetroUMG.Application;

namespace TransMetroUMG.Models;

public sealed class InicioOperacionBusViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una asignación válida.")]
    public int Asignacion { get; set; }

    [Required(ErrorMessage = "Selecciona la estación de salida.")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una estación válida.")]
    [Display(Name = "Estación de salida")]
    public int? EstacionSalida { get; set; }

    [BindNever]
    public AsignacionOperacionConsulta? DatosAsignacion { get; set; }
    [BindNever]
    public IReadOnlyList<EstacionSalidaOpcion> Recorrido { get; set; } = [];
    [BindNever]
    public bool ErrorCarga { get; set; }
}
