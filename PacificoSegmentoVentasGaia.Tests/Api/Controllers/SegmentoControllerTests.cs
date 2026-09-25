using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PacificoSegmentoVentasGaia.Application.Common;
using PacificoSegmentoVentasGaia.Application.Dtos;
using PacificoSegmentoVentasGaia.Domain.Exceptions;

namespace PacificoSegmentoVentasGaia.Tests.Api.Controllers;

public class SegmentoControllerTests
{
    private const string CelularValido = "987654321";
    private const string Ruta = "api/segmento";

    private static HttpClient CreateAuthenticatedClient(CustomWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", TestAuthHandler.HeaderValue);
        return client;
    }

    [Fact]
    public async Task GetSegmento_NoAuth_Returns401()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"{Ruta}?celular={CelularValido}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSegmento_AuthWithInvalidCelular_Returns400WithValidationErrors()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = CreateAuthenticatedClient(factory);

        var response = await client.GetAsync($"{Ruta}?celular=123");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        Assert.NotNull(body);
        Assert.False(body!.Success);
        Assert.NotEmpty(body.Errors);
    }

    [Fact]
    public async Task GetSegmento_AuthWithMissingCelular_Returns400WithValidationErrors()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = CreateAuthenticatedClient(factory);

        var response = await client.GetAsync(Ruta);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        Assert.NotNull(body);
        Assert.False(body!.Success);
        Assert.NotEmpty(body.Errors);
    }

    [Fact]
    public async Task GetSegmento_AuthWithValidCelularAndServiceReturnsTwoItems_Returns200WithTwoItems()
    {
        using var factory = new CustomWebApplicationFactory();
        factory.SegmentoServiceFake
            .GetSegmentoVentaAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<SegmentoVentaDto> { new(), new() });
        var client = CreateAuthenticatedClient(factory);

        var response = await client.GetAsync($"{Ruta}?celular={CelularValido}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<SegmentoVentaDto>>>();
        Assert.NotNull(body);
        Assert.True(body!.Success);
        Assert.NotNull(body.Data);
        Assert.Equal(2, body.Data!.Count);
    }

    [Fact]
    public async Task GetSegmento_ServiceThrowsNotFoundException_Returns404WithFailureBody()
    {
        using var factory = new CustomWebApplicationFactory();
        factory.SegmentoServiceFake
            .GetSegmentoVentaAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new NotFoundException("No se encontró segmento de venta para el celular indicado."));
        var client = CreateAuthenticatedClient(factory);

        var response = await client.GetAsync($"{Ruta}?celular={CelularValido}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(body);
        Assert.False(body!.Success);
    }

    [Fact]
    public async Task GetSegmento_ServiceThrowsBusinessException_Returns500WithGenericMessage()
    {
        using var factory = new CustomWebApplicationFactory();
        factory.SegmentoServiceFake
            .GetSegmentoVentaAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new BusinessException("Detalle interno que no debe exponerse."));
        var client = CreateAuthenticatedClient(factory);

        var response = await client.GetAsync($"{Ruta}?celular={CelularValido}");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Api500Response>();
        Assert.NotNull(body);
        Assert.False(body!.Success);
        Assert.DoesNotContain("Detalle interno", body.Message);
    }

    [Fact]
    public async Task GetSegmento_ServiceThrowsInvalidOperationException_Returns500()
    {
        using var factory = new CustomWebApplicationFactory();
        factory.SegmentoServiceFake
            .GetSegmentoVentaAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("boom"));
        var client = CreateAuthenticatedClient(factory);

        var response = await client.GetAsync($"{Ruta}?celular={CelularValido}");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    private const string RutaResultado = "api/segmento/resultado";

    private static SegmentoGaiaResultadoRequestDto CreateValidResultadoGaiaRequest() => new()
    {
        ContactId          = "CONTACT-1",
        InteractionId      = "INTERACTION-1",
        NombreCliente      = "Juan",
        ApellidoCliente    = "Perez",
        DniCliente         = "12345678",
        Fecha              = "2026-09-25",
        Hora               = "10:00",
        Agente             = "Bot GAIA",
        Identificacion     = "OK",
        Consentimiento     = "SI",
        ValidacionPrima    = "OK",
        MedioPago          = "TC",
        Cobertura          = "OK",
        Exclusiones        = "OK",
        ProgramaBeneficios = "OK",
        Cierre             = "OK",
        ResultadoGeneral   = "EXITOSO"
    };

    [Fact]
    public async Task ResultadoGaia_NoAuth_Returns401()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(RutaResultado, CreateValidResultadoGaiaRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ResultadoGaia_AuthWithMissingContactIdAndInteractionId_Returns400WithValidationErrors()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = CreateAuthenticatedClient(factory);
        var request = CreateValidResultadoGaiaRequest();
        request.ContactId = null;
        request.InteractionId = null;

        var response = await client.PostAsJsonAsync(RutaResultado, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        Assert.NotNull(body);
        Assert.False(body!.Success);
        Assert.NotEmpty(body.Errors);
    }

    [Fact]
    public async Task ResultadoGaia_AuthWithValidRequestAndServiceReturnsId_Returns201WithId()
    {
        using var factory = new CustomWebApplicationFactory();
        factory.SegmentoServiceFake
            .RegistrarResultadoGaiaAsync(Arg.Any<SegmentoGaiaResultadoRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(new SegmentoGaiaResultadoResponseDto { Id = 42 });
        var client = CreateAuthenticatedClient(factory);

        var response = await client.PostAsJsonAsync(RutaResultado, CreateValidResultadoGaiaRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SegmentoGaiaResultadoResponseDto>>();
        Assert.NotNull(body);
        Assert.True(body!.Success);
        Assert.NotNull(body.Data);
        Assert.Equal(42, body.Data!.Id);
    }

    [Fact]
    public async Task ResultadoGaia_AuthWithFieldExceedingMaxLength_Returns400AndDoesNotCallService()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = CreateAuthenticatedClient(factory);
        var request = CreateValidResultadoGaiaRequest();
        request.Identificacion = new string('X', 11);

        var response = await client.PostAsJsonAsync(RutaResultado, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiValidationErrorResponse>();
        Assert.NotNull(body);
        Assert.False(body!.Success);
        Assert.NotEmpty(body.Errors);
        await factory.SegmentoServiceFake.DidNotReceive()
            .RegistrarResultadoGaiaAsync(Arg.Any<SegmentoGaiaResultadoRequestDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResultadoGaia_ServiceThrowsBusinessException_Returns500WithGenericMessage()
    {
        using var factory = new CustomWebApplicationFactory();
        factory.SegmentoServiceFake
            .RegistrarResultadoGaiaAsync(Arg.Any<SegmentoGaiaResultadoRequestDto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new BusinessException("Detalle interno que no debe exponerse."));
        var client = CreateAuthenticatedClient(factory);

        var response = await client.PostAsJsonAsync(RutaResultado, CreateValidResultadoGaiaRequest());

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Api500Response>();
        Assert.NotNull(body);
        Assert.False(body!.Success);
        Assert.DoesNotContain("Detalle interno", body.Message);
    }

    [Fact]
    public async Task ResultadoGaia_ServiceThrowsInvalidOperationException_Returns500()
    {
        using var factory = new CustomWebApplicationFactory();
        factory.SegmentoServiceFake
            .RegistrarResultadoGaiaAsync(Arg.Any<SegmentoGaiaResultadoRequestDto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("boom"));
        var client = CreateAuthenticatedClient(factory);

        var response = await client.PostAsJsonAsync(RutaResultado, CreateValidResultadoGaiaRequest());

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Api500Response>();
        Assert.NotNull(body);
        Assert.False(body!.Success);
        Assert.DoesNotContain("boom", body.Message);
    }

    /// <summary>
    /// Proves the isolation from real infrastructure documented on
    /// <see cref="CustomWebApplicationFactory"/>: no Serilog sink is configured (so the
    /// MSSqlServer sink never runs), the connection string is the local dummy value (never the
    /// real 10.151.63.162 server), and the Keycloak authority is not the real tenant.
    /// </summary>
    [Fact]
    public void GetSegmento_Configuration_IsIsolatedFromRealInfrastructure()
    {
        using var factory = new CustomWebApplicationFactory();
        var configuration = factory.Services.GetRequiredService<IConfiguration>();

        Assert.Null(configuration["Serilog:WriteTo:0:Name"]);
        var connectionString = configuration.GetConnectionString("AppConnection");
        Assert.NotNull(connectionString);
        Assert.DoesNotContain("10.151.63.162", connectionString);
        Assert.Contains("127.0.0.1", connectionString);
        Assert.NotEqual("https://auth.grupogss.com.pe/realms/covisian", configuration["Keycloak:Authority"]);
    }
}
