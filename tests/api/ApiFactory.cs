using ClinicaSecurityApi.Data;
using ClinicaSecurityApi.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace ClinicaSecurityApi.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    internal string ClaveClinico { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
    internal string ClaveAdmin { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
    internal ConcurrentQueue<string> Eventos { get; } = new();
    internal const string DetalleInterno = "MARCADOR_INTERNO_SQL_FICTICIO";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging => logging.AddProvider(new RegistroDePrueba(Eventos)));
        var testSettings = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = Convert.ToBase64String(
                System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
            ["Jwt:Issuer"] = "ClinicaSecurityApi",
            ["Jwt:Audience"] = "ClinicaSecurityApiClients",
            ["Jwt:ExpirationMinutes"] = "30",
            ["Logging:EventLog:LogLevel:Default"] = "None",
            ["DemoUsers:Clinico:Password"] = ClaveClinico,
            ["DemoUsers:Administrador:Password"] = ClaveAdmin
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

    private sealed class RegistroDePrueba(ConcurrentQueue<string> eventos) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new Captura(eventos);
        public void Dispose() { }

        private sealed class Captura(ConcurrentQueue<string> eventos) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                Exception? exception, Func<TState, Exception?, string> formatter)
            {
                eventos.Enqueue(formatter(state, exception));
                if (exception is not null)
                {
                    eventos.Enqueue(exception.ToString());
                }
            }
        }
    }

    private sealed class PacientesDePrueba : IPacienteStore
    {
        public Task<Paciente?> BuscarPorIdAsync(int id, CancellationToken cancellationToken)
        {
            Paciente? paciente = id switch
            {
                1001 => new() { Id = id, Nombre = "Paciente Demo Uno", Edad = 35, MotivoConsulta = "Consulta general ficticia" },
                1002 => new() { Id = id, Nombre = "Paciente Demo Dos", Edad = 42, MotivoConsulta = "Revisión médica ficticia" },
                1003 => throw new InvalidOperationException(DetalleInterno),
                _ => null
            };
            return Task.FromResult(paciente);
        }
    }
}
