
namespace ClinicaSecurityApi.Models;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiraEnUtc { get; set; }

    public string TipoToken { get; set; } = "Bearer";
}
