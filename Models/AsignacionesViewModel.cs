using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;

namespace TransMetroUMG.Models;

public sealed class AsignacionFormulario
{
    [Range(0, int.MaxValue)]
    public int CodAsignacionBus { get; set; }

    [Required(ErrorMessage = "Ingrese la fecha de asignación.")]
    [DataType(DataType.Date)]
    public DateOnly? FechaAsignacion { get; set; }

    [Required(ErrorMessage = "Seleccione un bus.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un bus válido.")]
    public int? CodBus { get; set; }

    [Required(ErrorMessage = "Seleccione un piloto.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un piloto válido.")]
    public int? CodPiloto { get; set; }

    [Required(ErrorMessage = "Seleccione una línea.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una línea válida.")]
    public int? CodLinea { get; set; }
}

public sealed class AsignacionesViewModel
{
    public AsignacionFormulario Formulario { get; set; } = new();

    [BindNever]
    public IReadOnlyList<AsignacionBus> Registros { get; set; } = [];
    [BindNever]
    public IReadOnlyList<OpcionCatalogo> Buses { get; set; } = [];
    [BindNever]
    public IReadOnlyList<OpcionCatalogo> Pilotos { get; set; } = [];
    [BindNever]
    public IReadOnlyList<OpcionCatalogo> Lineas { get; set; } = [];
    [BindNever]
    public bool ErrorCarga { get; set; }
}
