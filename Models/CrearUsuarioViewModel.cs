using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TransMetroUMG.Models;

public class CrearUsuarioViewModel
{
    [BindNever]
    public IReadOnlyList<SelectListItem> Municipalidades { get; set; } = [];

    [BindNever]
    public IReadOnlyList<SelectListItem> Roles { get; set; } = [];

    [BindNever]
    public bool ErrorCatalogos { get; set; }

    [Required(ErrorMessage = "Ingrese el nombre de usuario.")]
    [StringLength(100, ErrorMessage = "El nombre de usuario admite hasta 100 caracteres.")]
    [Display(Name = "Nombre de usuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa el nombre completo.")]
    [StringLength(200, ErrorMessage = "El nombre completo admite hasta 200 caracteres.")]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa el correo electrónico.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido.")]
    [StringLength(150, ErrorMessage = "El correo admite hasta 150 caracteres.")]
    [Display(Name = "Correo electrónico")]
    public string CorreoElectronico { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa la contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Contrasena { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma la contraseña.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Contrasena), ErrorMessage = "Las contraseñas no coinciden.")]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmarContrasena { get; set; } = string.Empty;

    [Required(ErrorMessage = "Seleccione una municipalidad.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una municipalidad válida.")]
    [Display(Name = "Municipalidad")]
    public int? CodMunicipalidad { get; set; }

    [Required(ErrorMessage = "Seleccione un rol.")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un rol válido.")]
    [Display(Name = "Rol")]
    public int? CodRol { get; set; }

    [Required(ErrorMessage = "Seleccione un estado.")]
    [Range(0, 1, ErrorMessage = "Seleccione un estado válido.")]
    [Display(Name = "Estado")]
    public byte? CodEstadoUsuario { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Fecha de expiración de contraseña")]
    public DateTime? FechaExpiracionContrasena { get; set; }
}
