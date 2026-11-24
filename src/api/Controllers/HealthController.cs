
using Microsoft.AspNetCore.Mvc;

namespace ClinicaSecurityApi.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            status = "healthy",
            service = "ClinicaSecurityApi"
        });
    }
}
