
using ClinicaSecurityApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSecurityApi.Controllers;

[ApiController]
[Route("api/pacientes")]
[Authorize]
public class PacientesController : ControllerBase
{
    private readonly PacienteStore _pacienteStore;

    public PacientesController(PacienteStore pacienteStore)
    {
        _pacienteStore = pacienteStore;
    }

    [HttpGet("{id:int}")]
    public IActionResult ObtenerPaciente(int id)
    {
        // Validar el identificador.
        if (id <= 0)
        {
            return BadRequest(new
            {
                mensaje = "El identificador del paciente no es válido."
            });
        }

        // Administrador: puede consultar cualquier paciente.
        if (User.IsInRole("Administrador"))
        {
            var pacienteAdministrador =
                _pacienteStore.BuscarPorId(id);

            if (pacienteAdministrador is null)
            {
                return NotFound(new
                {
                    mensaje = "No se encontró el paciente."
                });
            }

            return Ok(pacienteAdministrador);
        }

        // Clínico: solamente puede consultar su paciente asignado.
        if (User.IsInRole("Clinico"))
        {
            var pacienteAsignado =
                User.FindFirst("paciente_id")?.Value;

            if (!int.TryParse(pacienteAsignado, out var pacienteId) ||
                pacienteId != id)
            {
                // No revelar si el expediente ajeno existe.
                return NotFound(new
                {
                    mensaje = "No se encontró el paciente."
                });
            }

            var pacienteClinico = _pacienteStore.BuscarPorId(id);

            if (pacienteClinico is null)
            {
                return NotFound(new
                {
                    mensaje = "No se encontró el paciente."
                });
            }

            return Ok(pacienteClinico);
        }

        // Cualquier otro rol no tiene permisos.
        return Forbid();
    }
}
