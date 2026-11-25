using ClinicaSecurityApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSecurityApi.Controllers;

[ApiController]
[Route("api/pacientes")]
[Authorize]
public class PacientesController(IPacienteStore pacienteStore) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPaciente(int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest(new { mensaje = "El identificador del paciente no es válido." });
        }

        if (!User.IsInRole("Administrador"))
        {
            if (!User.IsInRole("Clinico"))
            {
                return Forbid();
            }

            if (!int.TryParse(User.FindFirst("paciente_id")?.Value, out var pacienteId) ||
                pacienteId != id)
            {
                return NotFound(new { mensaje = "No se encontró el paciente." });
            }
        }

        var paciente = await pacienteStore.BuscarPorIdAsync(id, cancellationToken);
        return paciente is null
            ? NotFound(new { mensaje = "No se encontró el paciente." })
            : Ok(paciente);
    }
}
