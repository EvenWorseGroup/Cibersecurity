using ClinicaSecurityApi.Models;

namespace ClinicaSecurityApi.Data;

public class AuditoriaStore(ILogger<AuditoriaStore> logger)
{
    private const int Capacidad = 1000;
    private readonly Queue<RegistroAuditoria> _registros = new();
    private readonly object _sync = new();
    private int _siguienteId;

    public void Agregar(RegistroAuditoria registro)
    {
        lock (_sync)
        {
            registro.Id = ++_siguienteId;
            if (_registros.Count == Capacidad)
            {
                _registros.Dequeue();
            }

            _registros.Enqueue(registro);
        }

        logger.LogInformation(
            "Auditoria HTTP: Usuario={Usuario}, Accion={Accion}, Ruta={Ruta}, " +
            "CodigoHttp={CodigoHttp}, Resultado={Resultado}, IdCorrelacion={IdCorrelacion}",
            registro.Usuario, registro.Accion, registro.Ruta, registro.CodigoHttp,
            registro.Resultado, registro.IdCorrelacion);
    }

    public IReadOnlyCollection<RegistroAuditoria> ObtenerTodos()
    {
        lock (_sync)
        {
            return _registros.ToArray();
        }
    }
}
