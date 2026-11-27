
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ClinicaSecurityApi.Tests;

public class ApiIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    private readonly ApiFactory _factory;

    public ApiIntegrationTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
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

        return await _client.SendAsync(request);
    }

    [Fact]
    public async Task Health_DebeResponder200()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("healthy", body.GetProperty("status").GetString());
        Assert.Equal("ClinicaSecurityApi", body.GetProperty("service").GetString());
    }

    [Fact]
    public async Task Login_ConCredencialesCorrectas_DebeResponder200()
    {
        var response = await _client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                nombreUsuario = "clinico.demo",
                contrasena = _factory.ClaveClinico
            });

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var resultado =
            await response.Content.ReadFromJsonAsync<LoginTokenResponse>();

        Assert.NotNull(resultado);
        Assert.False(string.IsNullOrWhiteSpace(resultado.Token));
        Assert.Equal("Bearer", resultado.TipoToken);
        Assert.True(resultado.ExpiraEnUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_ConContrasenaIncorrecta_DebeResponder401()
    {
        var response = await _client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                nombreUsuario = "clinico.demo",
                contrasena = Guid.NewGuid().ToString("N")
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("mensaje", Assert.Single(body.EnumerateObject()).Name);
        Assert.Equal("Usuario o contraseña incorrectos.", body.GetProperty("mensaje").GetString());
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
            _factory.ClaveClinico);

        var response = await ConsultarConTokenAsync(
            "/api/pacientes/1001",
            token);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1001, body.GetProperty("id").GetInt32());
        Assert.Equal("Paciente Demo Uno", body.GetProperty("nombre").GetString());
        Assert.Equal(35, body.GetProperty("edad").GetInt32());
        Assert.Equal("Consulta general ficticia", body.GetProperty("motivoConsulta").GetString());
    }

    [Fact]
    public async Task Clinico_ConsultaPacienteAjeno_DebeResponder404()
    {
        var token = await ObtenerTokenAsync(
            "clinico.demo",
            _factory.ClaveClinico);

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
            _factory.ClaveAdmin);

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
            _factory.ClaveClinico);

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
            _factory.ClaveAdmin);

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
            _factory.ClaveClinico);

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

    [Theory]
    [InlineData("/api/pacientes/1001")]
    [InlineData("/api/admin/auditoria")]
    public async Task RutaProtegida_ConTokenInvalido_DebeResponder401(string ruta)
    {
        using var response = await ConsultarConTokenAsync(ruta, "jwt-malformado-de-prueba");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var challenge = Assert.Single(response.Headers.WwwAuthenticate);
        Assert.Equal("Bearer", challenge.Scheme);
        Assert.Null(challenge.Parameter);
        Assert.Empty(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Administrador_ConsultaPacienteInexistente_DebeResponder404Generico()
    {
        var admin = await ObtenerTokenAsync("admin.demo", _factory.ClaveAdmin);
        var clinico = await ObtenerTokenAsync("clinico.demo", _factory.ClaveClinico);
        using var inexistente = await ConsultarConTokenAsync("/api/pacientes/9999", admin);
        using var ajeno = await ConsultarConTokenAsync("/api/pacientes/1002", clinico);
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, ajeno.StatusCode);
        Assert.Equal(await inexistente.Content.ReadAsStringAsync(), await ajeno.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task RutaProtegida_RechazaTokenEmitidoConOtraClave()
    {
        using var otroHost = new ApiFactory();
        using var clienteOtroHost = otroHost.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        using var login = await clienteOtroHost.PostAsJsonAsync("/auth/login", new
        {
            nombreUsuario = "admin.demo",
            contrasena = otroHost.ClaveAdmin
        });
        login.EnsureSuccessStatusCode();
        var resultado = await login.Content.ReadFromJsonAsync<LoginTokenResponse>();
        Assert.NotNull(resultado);

        using var respuesta = await ConsultarConTokenAsync("/api/admin/auditoria", resultado.Token);
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Null(Assert.Single(respuesta.Headers.WwwAuthenticate).Parameter);
        Assert.Empty(await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task FalloDeLectura_Devuelve500GenericoYRegistraEventoSeguro()
    {
        var token = await ObtenerTokenAsync("admin.demo", _factory.ClaveAdmin);
        using var response = await ConsultarConTokenAsync("/api/pacientes/1003", token);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("mensaje", Assert.Single(body.EnumerateObject()).Name);
        Assert.Equal("Error interno del servidor.", body.GetProperty("mensaje").GetString());
        var correlacion = Assert.Single(response.Headers.GetValues("X-Correlation-ID"));
        Assert.Contains(_factory.Eventos, evento =>
            evento.Contains("CodigoHttp=500") && evento.Contains(correlacion));

        Assert.False(_factory.Eventos.Any(evento =>
            evento.Contains(ApiFactory.DetalleInterno) ||
            evento.Contains(_factory.ClaveAdmin) ||
            evento.Contains(token)), "El registro contiene detalles reservados.");
    }

    [Fact]
    public async Task Auditoria_RegistraLoginUnaVezYNormalizaRutaDelExpediente()
    {
        using var login = await _client.PostAsJsonAsync("/AUTH/LOGIN/", new
        {
            nombreUsuario = "ADMIN.DEMO",
            contrasena = _factory.ClaveAdmin
        });
        login.EnsureSuccessStatusCode();
        var resultado = await login.Content.ReadFromJsonAsync<LoginTokenResponse>();
        Assert.NotNull(resultado);
        var correlacionLogin = Assert.Single(login.Headers.GetValues("X-Correlation-ID"));
        using var consulta = await ConsultarConTokenAsync("/api/pacientes/1001", resultado.Token);
        consulta.EnsureSuccessStatusCode();
        var correlacionConsulta = Assert.Single(consulta.Headers.GetValues("X-Correlation-ID"));
        using var auditoria = await ConsultarConTokenAsync("/api/admin/auditoria", resultado.Token);
        auditoria.EnsureSuccessStatusCode();
        var body = await auditoria.Content.ReadFromJsonAsync<JsonElement>();
        var eventos = body.EnumerateArray().ToArray();
        var eventoLogin = Assert.Single(eventos, evento =>
            evento.GetProperty("idCorrelacion").GetString() == correlacionLogin);
        Assert.Equal("admin.demo", eventoLogin.GetProperty("usuario").GetString());
        Assert.Equal("/auth/login", eventoLogin.GetProperty("ruta").GetString());
        Assert.Single(_factory.Eventos, evento =>
            evento.Contains("Auditoria HTTP") && evento.Contains(correlacionLogin));
        var eventoConsulta = Assert.Single(eventos, evento =>
            evento.GetProperty("idCorrelacion").GetString() == correlacionConsulta);
        Assert.Equal("/api/pacientes/{id}", eventoConsulta.GetProperty("ruta").GetString());
        Assert.DoesNotContain("Paciente Demo Uno", body.GetRawText());
        Assert.DoesNotContain("Consulta general ficticia", body.GetRawText());
    }

    private sealed class LoginTokenResponse
    {
        public string Token { get; set; } = string.Empty;

        public string TipoToken { get; set; } = string.Empty;
        public DateTime ExpiraEnUtc { get; set; }
    }
}
