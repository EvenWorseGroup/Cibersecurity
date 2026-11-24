
using System.Collections.Concurrent;
using ClinicaSecurityApi.Models;
using Microsoft.AspNetCore.Identity;

namespace ClinicaSecurityApi.Data;

public class UsuarioStore
{
    private readonly ConcurrentDictionary<string, Usuario> _usuarios;
    private readonly IPasswordHasher<Usuario> _passwordHasher;

    public UsuarioStore(
        IConfiguration configuration,
        IPasswordHasher<Usuario> passwordHasher)
    {
        _passwordHasher = passwordHasher;

        _usuarios = new ConcurrentDictionary<string, Usuario>(
            StringComparer.OrdinalIgnoreCase);

        InicializarUsuarios(configuration);
    }

    private void InicializarUsuarios(IConfiguration configuration)
    {
        CrearUsuario(
            id: 1,
            nombreUsuario: "clinico.demo",
            contrasena: configuration["DemoUsers:Clinico:Password"],
            rol: "Clinico",
            pacienteId: 1001);

        CrearUsuario(
            id: 2,
            nombreUsuario: "admin.demo",
            contrasena: configuration["DemoUsers:Administrador:Password"],
            rol: "Administrador",
            pacienteId: null);
    }

    private void CrearUsuario(
        int id,
        string nombreUsuario,
        string? contrasena,
        string rol,
        int? pacienteId)
    {
        if (string.IsNullOrWhiteSpace(contrasena))
        {
            throw new InvalidOperationException(
                $"No se configuró la contraseña de prueba para {nombreUsuario}.");
        }

        var usuario = new Usuario
        {
            Id = id,
            NombreUsuario = nombreUsuario,
            Rol = rol,
            PacienteId = pacienteId
        };

        // Nunca almacenar contraseñas en texto plano.
        usuario.PasswordHash = _passwordHasher.HashPassword(
            usuario,
            contrasena);

        if (!_usuarios.TryAdd(nombreUsuario, usuario))
        {
            throw new InvalidOperationException(
                $"No se pudo registrar el usuario {nombreUsuario}.");
        }
    }

    public Usuario? BuscarPorNombreUsuario(string nombreUsuario)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario))
        {
            return null;
        }

        _usuarios.TryGetValue(nombreUsuario, out var usuario);

        return usuario;
    }

    public bool VerificarContrasena(
        Usuario usuario,
        string contrasena)
    {
        if (usuario is null || string.IsNullOrEmpty(contrasena))
        {
            return false;
        }

        var resultado = _passwordHasher.VerifyHashedPassword(
            usuario,
            usuario.PasswordHash,
            contrasena);

        return resultado != PasswordVerificationResult.Failed;
    }
}
