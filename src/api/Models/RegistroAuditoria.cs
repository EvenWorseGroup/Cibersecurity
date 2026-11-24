
namespace ClinicaSecurityApi.Models;

public class RegistroAuditoria
{
    public int Id { get; set; }

    public DateTime FechaUtc { get; set; }

    public string Usuario { get; set; } = string.Empty;

    public string Accion { get; set; } = string.Empty;

    public string Ruta { get; set; } = string.Empty;

    public int CodigoHttp { get; set; }

    public string Resultado { get; set; } = string.Empty;

    public string IdCorrelacion { get; set; } = string.Empty;
}
