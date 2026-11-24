
using ClinicaSecurityApi.Data;
using ClinicaSecurityApi.Models;
using ClinicaSecurityApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSecurityApi.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly AuditoriaStore _auditoriaStore;

    public AuthController(
        AuthService authService,
        AuditoriaStore auditoriaStore)
    {
        _authService = authService;
        _auditoriaStore = auditoriaStore;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var resultado = _authService.IniciarSesion(request);

        if (resultado is null)
        {
            return Unauthorized(new
            {
                mensaje = "Usuario o contraseña incorrectos."
            });
        }

        // Registrar el inicio de sesión con el usuario autenticado.
        var correlationId =
            HttpContext.Response.Headers["X-Correlation-ID"].ToString();

        _auditoriaStore.Agregar(new RegistroAuditoria
        {
            FechaUtc = DateTime.UtcNow,
            Usuario = request.NombreUsuario,
            Accion = "POST /auth/login",
            Ruta = "/auth/login",
            CodigoHttp = 200,
            Resultado = "Exito",
            IdCorrelacion = correlationId
        });

        return Ok(resultado);
    }
}
