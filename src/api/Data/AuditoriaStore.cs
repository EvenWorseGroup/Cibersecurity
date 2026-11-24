
using System.Collections.Concurrent;
using ClinicaSecurityApi.Models;

namespace ClinicaSecurityApi.Data;

public class AuditoriaStore
{
    private readonly ConcurrentQueue<RegistroAuditoria> _registros = new();

    private int _siguienteId = 0;

    public AuditoriaStore()
    {
        // Registros ficticios para las pruebas iniciales.
        Agregar(new RegistroAuditoria
        {
            FechaUtc = DateTime.UtcNow.AddMinutes(-15),
            Usuario = "clinico.demo",
            Accion = "Consulta de paciente",
            Ruta = "/api/pacientes/1001",
            CodigoHttp = 200,
            IdCorrelacion = Guid.NewGuid().ToString()
        });

        Agregar(new RegistroAuditoria
        {
            FechaUtc = DateTime.UtcNow.AddMinutes(-10),
            Usuario = "admin.demo",
            Accion = "Inicio de sesión",
            Ruta = "/auth/login",
            CodigoHttp = 200,
            IdCorrelacion = Guid.NewGuid().ToString()
        });

        Agregar(new RegistroAuditoria
        {
            FechaUtc = DateTime.UtcNow.AddMinutes(-5),
            Usuario = "clinico.demo",
            Accion = "Acceso denegado",
            Ruta = "/api/pacientes/1002",
            CodigoHttp = 404,
            IdCorrelacion = Guid.NewGuid().ToString()
        });
    }

    public void Agregar(RegistroAuditoria registro)
    {
        registro.Id = Interlocked.Increment(ref _siguienteId);

        _registros.Enqueue(registro);
    }

    public IReadOnlyCollection<RegistroAuditoria> ObtenerTodos()
    {
        return _registros.ToArray();
    }
}

