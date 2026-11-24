
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ClinicaSecurityApi.Tests;

public class ApiIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> ObtenerTokenAsync(
        string usuario,
        string contrasena)
    {
        var response = await _client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                nombreUsuario = usuario,
                contrasena = contrasena
            });

        response.EnsureSuccessStatusCode();

        var resultado =
            await response.Content.ReadFromJsonAsync<LoginTokenResponse>();

        Assert.NotNull(resultado);
        Assert.False(string.IsNullOrWhiteSpace(resultado.Token));

        return resultado.Token;
    }

    private async Task<HttpResponseMessage> ConsultarConTokenAsync(
        string ruta,
        string token)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            ruta);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            Console.WriteLine(
                "DETALLE JWT: " +
                string.Join(
                    ", ",
                    response.Headers.WwwAuthenticate.Select(
                        header => header.ToString())));
        }

        return response;
    }

    [Fact]
    public async Task Health_DebeResponder200()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task Login_ConCredencialesCorrectas_DebeResponder200()
    {
        var response = await _client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                nombreUsuario = "clinico.demo",
                contrasena = "ClinicoTest2026!"
            });

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var resultado =
            await response.Content.ReadFromJsonAsync<LoginTokenResponse>();

        Assert.NotNull(resultado);
        Assert.False(string.IsNullOrWhiteSpace(resultado.Token));
        Assert.Equal("Bearer", resultado.TipoToken);
    }

    [Fact]
    public async Task Login_ConContrasenaIncorrecta_DebeResponder401()
    {
        var response = await _client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                nombreUsuario = "clinico.demo",
                contrasena = "ContrasenaIncorrecta2026!"
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Login_SinDatosObligatorios_DebeResponder400()
    {
        var response = await _client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                nombreUsuario = "",
                contrasena = ""
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Paciente_SinToken_DebeResponder401()
    {
        var response = await _client.GetAsync(
            "/api/pacientes/1001");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Clinico_ConsultaSuPaciente_DebeResponder200()
    {
        var token = await ObtenerTokenAsync(
            "clinico.demo",
            "ClinicoTest2026!");

        var response = await ConsultarConTokenAsync(
            "/api/pacientes/1001",
            token);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task Clinico_ConsultaPacienteAjeno_DebeResponder404()
    {
        var token = await ObtenerTokenAsync(
            "clinico.demo",
            "ClinicoTest2026!");

        var response = await ConsultarConTokenAsync(
            "/api/pacientes/1002",
            token);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task Administrador_ConsultaPaciente_DebeResponder200()
    {
        var token = await ObtenerTokenAsync(
            "admin.demo",
            "AdminTest2026!");

        var response = await ConsultarConTokenAsync(
            "/api/pacientes/1002",
            token);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task Auditoria_SinToken_DebeResponder401()
    {
        var response = await _client.GetAsync(
            "/api/admin/auditoria");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Clinico_ConsultaAuditoria_DebeResponder403()
    {
        var token = await ObtenerTokenAsync(
            "clinico.demo",
            "ClinicoTest2026!");

        var response = await ConsultarConTokenAsync(
            "/api/admin/auditoria",
            token);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Administrador_ConsultaAuditoria_DebeResponder200()
    {
        var token = await ObtenerTokenAsync(
            "admin.demo",
            "AdminTest2026!");

        var response = await ConsultarConTokenAsync(
            "/api/admin/auditoria",
            token);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task Paciente_ConIdentificadorCero_DebeResponder400()
    {
        var token = await ObtenerTokenAsync(
            "clinico.demo",
            "ClinicoTest2026!");

        var response = await ConsultarConTokenAsync(
            "/api/pacientes/0",
            token);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Health_DebeDevolverIdDeCorrelacion()
    {
        var response = await _client.GetAsync("/health");

        Assert.True(
            response.Headers.TryGetValues(
                "X-Correlation-ID",
                out var valores));

        Assert.True(
            Guid.TryParse(valores!.First(), out _));
    }

    private sealed class LoginTokenResponse
    {
        public string Token { get; set; } = string.Empty;

        public string TipoToken { get; set; } = string.Empty;
    }
}
