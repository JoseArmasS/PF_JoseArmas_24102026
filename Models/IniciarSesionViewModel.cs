using System.ComponentModel.DataAnnotations;

namespace TransMetroUMG.Models;

public sealed class IniciarSesionViewModel
{
    [Required(ErrorMessage = "Ingresa tu usuario.")]
    [StringLength(100)]
    [Display(Name = "Usuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Contrasena { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
