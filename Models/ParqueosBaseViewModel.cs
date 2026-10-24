using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TransMetroUMG.Application;

namespace TransMetroUMG.Models;

public sealed class ParqueoBaseFormulario
{
    [Required(ErrorMessage = "Selecciona un bus.")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un bus válido.")]
    public int? CodBus { get; set; }

    [Required(ErrorMessage = "Selecciona un parqueo base.")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un parqueo válido.")]
    public int? CodParqueoBase { get; set; }
}

public sealed class ParqueosBaseViewModel
{
    public ParqueoBaseFormulario Formulario { get; set; } = new();
    [BindNever]
    public IReadOnlyList<BusParqueoBaseConsulta> Buses { get; set; } = [];
    [BindNever]
    public IReadOnlyList<OpcionCatalogo> Parqueos { get; set; } = [];
    [BindNever]
    public bool ErrorCarga { get; set; }
}
