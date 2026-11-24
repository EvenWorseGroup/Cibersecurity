
using System.ComponentModel.DataAnnotations;

namespace ClinicaSecurityApi.Models;

public class LoginRequest
{
    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [StringLength(50, MinimumLength = 3)]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 8)]
    public string Contrasena { get; set; } = string.Empty;
}
