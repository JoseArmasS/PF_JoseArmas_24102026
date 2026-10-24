using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;

namespace TransMetroUMG.Models;

public sealed class ParqueoFormulario
{
    [Range(0, int.MaxValue)]
    public int CodParqueo { get; set; }

    [Required(ErrorMessage = "Selecciona una estación.")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una estación válida.")]
    public int? CodEstacion { get; set; }

    [Required(ErrorMessage = "Ingresa el nombre del parqueo.")]
    [StringLength(150, ErrorMessage = "El nombre admite hasta 150 caracteres.")]
    public string NombreParqueo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa la dirección.")]
    [StringLength(250, ErrorMessage = "La dirección admite hasta 250 caracteres.")]
    public string DireccionParqueo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa la capacidad total.")]
    [Range(1, int.MaxValue, ErrorMessage = "La capacidad debe ser mayor que cero.")]
    public int? CapacidadTotal { get; set; }

    [Required(ErrorMessage = "Selecciona un estado.")]
    [EnumDataType(typeof(EstadoParqueo), ErrorMessage = "Selecciona un estado válido.")]
    public EstadoParqueo? Estado { get; set; } = EstadoParqueo.Activo;
}

public sealed class ParqueosViewModel
{
    public ParqueoFormulario Formulario { get; set; } = new();
    [BindNever]
    public IReadOnlyList<Parqueo> Registros { get; set; } = [];
    [BindNever]
    public IReadOnlyList<EstacionParqueoOpcion> Estaciones { get; set; } = [];
    [BindNever]
    public bool ErrorCarga { get; set; }
}