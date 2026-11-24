
using ClinicaSecurityApi.Models;

namespace ClinicaSecurityApi.Data;

public class PacienteStore
{
    private readonly Dictionary<int, Paciente> _pacientes;

    public PacienteStore()
    {
        _pacientes = new Dictionary<int, Paciente>
        {
            {
                1001,
                new Paciente
                {
                    Id = 1001,
                    Nombre = "Paciente Demo Uno",
                    Edad = 35,
                    MotivoConsulta = "Consulta general ficticia"
                }
            },
            {
                1002,
                new Paciente
                {
                    Id = 1002,
                    Nombre = "Paciente Demo Dos",
                    Edad = 42,
                    MotivoConsulta = "Revisión médica ficticia"
                }
            }
        };
    }

    public Paciente? BuscarPorId(int id)
    {
        _pacientes.TryGetValue(id, out var paciente);

        return paciente;
    }
}
