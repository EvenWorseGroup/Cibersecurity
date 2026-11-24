
using ClinicaSecurityApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSecurityApi.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Administrador")]
public class AdminController : ControllerBase
{
    private readonly AuditoriaStore _auditoriaStore;

    public AdminController(AuditoriaStore auditoriaStore)
    {
        _auditoriaStore = auditoriaStore;
    }

    [HttpGet("auditoria")]
    public IActionResult ObtenerAuditoria()
    {
        var registros = _auditoriaStore.ObtenerTodos();

        return Ok(registros);
    }
}
