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
// CONFIGURACIÃ“N JWT
// ==========================================

// ==========================================
// REGISTRO DE SERVICIOS
// ==========================================

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<
    IPasswordHasher<Usuario>,
    PasswordHasher<Usuario>>();

builder.Services.AddSingleton<UsuarioStore>();

builder.Services.AddSingleton<IPacienteStore, PacienteStore>();

builder.Services.AddSingleton<AuditoriaStore>();

builder.Services.AddScoped<AuthService>();

// ==========================================
// AUTENTICACIÃ“N JWT
// ==========================================

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "No se configurÃ³ Jwt:Key.");
        var jwtIssuer = builder.Configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "No se configurÃ³ Jwt:Issuer.");
        var jwtAudience = builder.Configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "No se configurÃ³ Jwt:Audience.");
        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(jwtKey);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                "La clave JWT no tiene un formato Base64 vÃ¡lido.");
        }
        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException(
                "La clave JWT debe tener al menos 32 bytes.");
        }
        var signingKey = new SymmetricSecurityKey(keyBytes);

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
// AUTORIZACIÃ“N
// ==========================================

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .ValidateOnStart();
builder.Services.AddAuthorization();

// ==========================================
// CONSTRUCCIÃ“N DE LA APLICACIÃ“N
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
public partial class Program { }
