using System.Security.Claims;
using ClinicaSecurityApi.Data;
using ClinicaSecurityApi.Models;
using ClinicaSecurityApi.Services;
using ClinicaSecurityApi.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// CONFIGURACIÓN JWT
// ==========================================

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "No se configuró Jwt:Key.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "No se configuró Jwt:Issuer.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "No se configuró Jwt:Audience.");

byte[] keyBytes;

try
{
    keyBytes = Convert.FromBase64String(jwtKey);
}
catch (FormatException)
{
    throw new InvalidOperationException(
        "La clave JWT no tiene un formato Base64 válido.");
}

if (keyBytes.Length < 32)
{
    throw new InvalidOperationException(
        "La clave JWT debe tener al menos 32 bytes.");
}

var signingKey = new SymmetricSecurityKey(keyBytes);

// ==========================================
// REGISTRO DE SERVICIOS
// ==========================================

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<
    IPasswordHasher<Usuario>,
    PasswordHasher<Usuario>>();

builder.Services.AddSingleton<UsuarioStore>();

builder.Services.AddSingleton<PacienteStore>();

builder.Services.AddSingleton<AuditoriaStore>();

builder.Services.AddScoped<AuthService>();

// ==========================================
// AUTENTICACIÓN JWT
// ==========================================

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,

                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateLifetime = true,
                RequireExpirationTime = true,

                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role,

                ClockSkew = TimeSpan.FromSeconds(30)
            };
    });

// ==========================================
// AUTORIZACIÓN
// ==========================================

builder.Services.AddAuthorization();

// ==========================================
// CONSTRUCCIÓN DE LA APLICACIÓN
// ==========================================

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();

app.UseMiddleware<AuditoriaMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();