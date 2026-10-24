using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;

namespace TransMetroUMG.Models;

public sealed class LineaFormulario
{
    [Range(0, int.MaxValue)]
    public int CodLinea { get; set; }

    [Required(ErrorMessage = "Selecciona una municipalidad.")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una municipalidad válida.")]
    public int? CodMunicipalidad { get; set; }

    [Required(ErrorMessage = "Ingresa el nombre de la línea.")]
    [StringLength(100, ErrorMessage = "El nombre admite hasta 100 caracteres.")]
    [Display(Name = "Nombre de la línea")]
    public string NombreLinea { get; set; } = string.Empty;

    [StringLength(250, ErrorMessage = "La descripción admite hasta 250 caracteres.")]
    [Display(Name = "Descripción")]
    public string? DescripcionLinea { get; set; }

    [Required(ErrorMessage = "Selecciona un estado.")]
    [EnumDataType(typeof(EstadoLinea), ErrorMessage = "Selecciona un estado válido.")]
    public EstadoLinea? Estado { get; set; } = EstadoLinea.Activo;
}

public sealed class LineasViewModel
{
    public LineaFormulario Formulario { get; set; } = new();

    [BindNever]
    public IReadOnlyList<Linea> Registros { get; set; } = [];

    [BindNever]
    public IReadOnlyList<OpcionCatalogo> Municipalidades { get; set; } = [];

    [BindNever]
    public bool ErrorCarga { get; set; }
}
