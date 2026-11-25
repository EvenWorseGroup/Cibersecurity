using ClinicaSecurityApi.Data;
using ClinicaSecurityApi.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ClinicaSecurityApi.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var testSettings = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = Convert.ToBase64String(
                System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
            ["Jwt:Issuer"] = "ClinicaSecurityApi",
            ["Jwt:Audience"] = "ClinicaSecurityApiClients",
            ["Jwt:ExpirationMinutes"] = "30",
            ["Logging:EventLog:LogLevel:Default"] = "None",
            ["DemoUsers:Clinico:Password"] = "ClinicoTest2026!",
            ["DemoUsers:Administrador:Password"] = "AdminTest2026!"
        };

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.Sources.Clear();
            configuration.AddInMemoryCollection(testSettings);
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPacienteStore>();
            services.AddSingleton<IPacienteStore>(new PacientesDePrueba());
        });
    }

    private sealed class PacientesDePrueba : IPacienteStore
    {
        public Task<Paciente?> BuscarPorIdAsync(int id, CancellationToken cancellationToken)
        {
            Paciente? paciente = id switch
            {
                1001 => new() { Id = id, Nombre = "Paciente Demo Uno", Edad = 35, MotivoConsulta = "Consulta general ficticia" },
                1002 => new() { Id = id, Nombre = "Paciente Demo Dos", Edad = 42, MotivoConsulta = "Revisión médica ficticia" },
                _ => null
            };
            return Task.FromResult(paciente);
        }
    }
}
