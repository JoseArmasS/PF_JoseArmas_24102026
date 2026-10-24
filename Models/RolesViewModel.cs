using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TransMetroUMG.Domain;

namespace TransMetroUMG.Models;
public sealed class RolFormulario
{
    [Range(0, int.MaxValue)]
    public int CodRol { get; set; }

    [Required(ErrorMessage = "Nombre del rol.")]
    [StringLength(100, ErrorMessage = "limite de 100 caracteres.")]
    [Display(Name = "Nombre del rol")]
    public string NombreRol { get; set; } = string.Empty;

    [StringLength(250, ErrorMessage = "limite de 250 caracteres.")]
    [Display(Name = "Descripción")]
    public string? DescripcionRol { get; set; }

    [Required(ErrorMessage = "Selecciona")]
    [EnumDataType(typeof(EstadoRol), ErrorMessage = "Selecciona")]
    public EstadoRol? Estado { get; set; } = EstadoRol.Activo;
}

public sealed class RolesViewModel
{
    public RolFormulario Formulario { get; set; } = new();

    [BindNever]
    public IReadOnlyList<Rol> Registros { get; set; } = [];

    [BindNever]
    public bool ErrorCarga { get; set; }
}