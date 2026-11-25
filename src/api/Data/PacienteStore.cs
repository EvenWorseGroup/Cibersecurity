using System.Data;
using ClinicaSecurityApi.Models;
using Microsoft.Data.SqlClient;

namespace ClinicaSecurityApi.Data;

public interface IPacienteStore
{
    Task<Paciente?> BuscarPorIdAsync(int id, CancellationToken cancellationToken);
}

public sealed class PacienteStore(IConfiguration configuration) : IPacienteStore
{
    public async Task<Paciente?> BuscarPorIdAsync(int id, CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("Clinica");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Falta ConnectionStrings:Clinica.");
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Nombre, Edad, MotivoConsulta
            FROM dbo.Pacientes
            WHERE Id = @Id;
            """;
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

        await using var reader = await command.ExecuteReaderAsync(
            CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new Paciente
        {
            Id = reader.GetInt32(0),
            Nombre = reader.GetString(1),
            Edad = reader.GetInt32(2),
            MotivoConsulta = reader.GetString(3)
        };
    }
}
