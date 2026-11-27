
using System.Security.Claims;
using System.Text.RegularExpressions;
using ClinicaSecurityApi.Data;
using ClinicaSecurityApi.Models;
using Microsoft.AspNetCore.Routing;

namespace ClinicaSecurityApi.Middleware;

public class AuditoriaMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditoriaMiddleware> _logger;

    public AuditoriaMiddleware(
        RequestDelegate next,
        ILogger<AuditoriaMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        AuditoriaStore auditoriaStore)
    {
        var fechaUtc = DateTime.UtcNow;

        // Conservar un GUID válido o generar uno nuevo.
        var correlationHeader =
            context.Request.Headers["X-Correlation-ID"].ToString();

        var correlationId = Guid.TryParse(
            correlationHeader,
            out var incomingCorrelationId)
                ? incomingCorrelationId.ToString("D")
                : Guid.NewGuid().ToString("D");

        context.Response.Headers["X-Correlation-ID"] =
            correlationId;

        // Identificar al usuario autenticado.
        var usuario = context.User.FindFirst(
            ClaimTypes.Name)?.Value ?? "anonimo";

        // Obtener y normalizar la ruta.
        var routeEndpoint = context.GetEndpoint() as RouteEndpoint;
        var rutaOriginal = routeEndpoint?.RoutePattern.RawText;
        var ruta = NormalizarRuta(rutaOriginal);

        var accion = $"{context.Request.Method} {ruta}";

        var codigoHttp = 500;

        try
        {
            await _next(context);

            codigoHttp = context.Response.StatusCode;
        }
        catch (Exception error) when (!context.Response.HasStarted &&
                                      !context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogError(
                "Fallo HTTP: Tipo={Tipo}, IdCorrelacion={IdCorrelacion}",
                error.GetType().Name, correlationId);
            context.Response.Clear();
            context.Response.Headers["X-Correlation-ID"] = correlationId;
            context.Response.StatusCode = codigoHttp;
            await context.Response.WriteAsJsonAsync(new { mensaje = "Error interno del servidor." });
        }
        finally
        {
            // El login exitoso se registra desde AuthController,
            // donde ya conocemos el usuario autenticado.
            var esLoginExitoso =
                ruta.Equals(
                    "/auth/login",
                    StringComparison.OrdinalIgnoreCase)
                && context.Request.Method == "POST"
                && codigoHttp == 200;

            if (!esLoginExitoso)
            {
                var resultado = ObtenerResultado(codigoHttp);

                var registro = new RegistroAuditoria
                {
                    FechaUtc = fechaUtc,
                    Usuario = usuario,
                    Accion = accion,
                    Ruta = ruta,
                    CodigoHttp = codigoHttp,
                    Resultado = resultado,
                    IdCorrelacion = correlationId
                };

                auditoriaStore.Agregar(registro);
            }
        }
    }

    private static string ObtenerResultado(int codigoHttp)
    {
        if (codigoHttp >= 200 && codigoHttp < 400)
        {
            return "Exito";
        }

        if (codigoHttp == 401 || codigoHttp == 403)
        {
            return "Denegado";
        }

        if (codigoHttp >= 500)
        {
            return "Error";
        }

        return "SolicitudRechazada";
    }

    private static string NormalizarRuta(string? rutaOriginal)
    {
        if (string.IsNullOrWhiteSpace(rutaOriginal))
        {
            return "/ruta-no-encontrada";
        }

        var rutaNormalizada = Regex.Replace(
            rutaOriginal,
            @"\{([^{}:]+):[^{}]+\}",
            "{$1}");

        return rutaNormalizada.StartsWith("/")
            ? rutaNormalizada
            : "/" + rutaNormalizada;
    }
}
