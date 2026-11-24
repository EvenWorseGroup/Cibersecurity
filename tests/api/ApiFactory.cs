
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ClinicaSecurityApi.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var testKeyBytes =
            System.Text.Encoding.UTF8.GetBytes(
                "01234567890123456789012345678901");

        var testSettings = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = Convert.ToBase64String(testKeyBytes),

            ["Jwt:Issuer"] = "ClinicaSecurityApi",
            ["Jwt:Audience"] = "ClinicaSecurityApiClients",
            ["Jwt:ExpirationMinutes"] = "30",

            ["DemoUsers:Clinico:Password"] = "ClinicoTest2026!",
            ["DemoUsers:Administrador:Password"] = "AdminTest2026!"
        };

        builder.ConfigureAppConfiguration((context, configuration) =>
        {
            configuration.Sources.Clear();
            configuration.AddInMemoryCollection(testSettings);
        });

        // Sobrescribir la clave de validación JWT
        // exclusivamente dentro del entorno de pruebas.
        builder.ConfigureTestServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.TokenValidationParameters
                        .ValidateIssuerSigningKey = true;

                    options.TokenValidationParameters.IssuerSigningKey =
                        new SymmetricSecurityKey(testKeyBytes);

                    options.TokenValidationParameters.ValidateIssuer = true;
                    options.TokenValidationParameters.ValidIssuer =
                        "ClinicaSecurityApi";

                    options.TokenValidationParameters.ValidateAudience = true;
                    options.TokenValidationParameters.ValidAudience =
                        "ClinicaSecurityApiClients";
                });
        });
    }
}
