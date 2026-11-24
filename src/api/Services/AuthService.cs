
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ClinicaSecurityApi.Data;
using ClinicaSecurityApi.Models;
using Microsoft.IdentityModel.Tokens;

namespace ClinicaSecurityApi.Services;

public class AuthService
{
    private readonly UsuarioStore _usuarioStore;
    private readonly IConfiguration _configuration;

    public AuthService(
        UsuarioStore usuarioStore,
        IConfiguration configuration)
    {
        _usuarioStore = usuarioStore;
        _configuration = configuration;
    }

    public LoginResponse? IniciarSesion(LoginRequest request)
    {
        // Buscar usuario.
        var usuario = _usuarioStore.BuscarPorNombreUsuario(
            request.NombreUsuario);

        // Verificar credenciales.
        if (usuario is null ||
            !_usuarioStore.VerificarContrasena(
                usuario,
                request.Contrasena))
        {
            return null;
        }

        // Obtener configuración JWT.
        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "No se configuró Jwt:Key.");

        var jwtIssuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "No se configuró Jwt:Issuer.");

        var jwtAudience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "No se configuró Jwt:Audience.");

        var expirationMinutes =
            _configuration.GetValue<int>("Jwt:ExpirationMinutes");

        if (expirationMinutes <= 0)
        {
            throw new InvalidOperationException(
                "La duración del token JWT debe ser mayor que cero.");
        }

        // Preparar clave de firma.
        var key = new SymmetricSecurityKey(
            Convert.FromBase64String(jwtKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        // Información que contendrá el token.
        var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                usuario.Id.ToString()),

            new(
                ClaimTypes.Name,
                usuario.NombreUsuario),

            new(
                ClaimTypes.Role,
                usuario.Rol)
        };

        // Identificador del paciente asignado al clínico.
        if (usuario.PacienteId.HasValue)
        {
            claims.Add(
                new Claim(
                    "paciente_id",
                    usuario.PacienteId.Value.ToString()));
        }

        // Fecha de expiración en UTC.
        var expiration = DateTime.UtcNow.AddMinutes(
            expirationMinutes);

        // Crear token JWT.
        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiration,
            signingCredentials: credentials);

        var tokenString =
            new JwtSecurityTokenHandler().WriteToken(token);

        return new LoginResponse
        {
            Token = tokenString,
            ExpiraEnUtc = expiration,
            TipoToken = "Bearer"
        };
    }
}
