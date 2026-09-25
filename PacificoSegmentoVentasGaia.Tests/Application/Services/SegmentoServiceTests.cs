using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using PacificoSegmentoVentasGaia.Application.Dtos;
using PacificoSegmentoVentasGaia.Application.Mappings;
using PacificoSegmentoVentasGaia.Application.Options;
using PacificoSegmentoVentasGaia.Application.Services;
using PacificoSegmentoVentasGaia.Domain.Entities;
using PacificoSegmentoVentasGaia.Domain.Exceptions;
using PacificoSegmentoVentasGaia.Domain.Interfaces;

namespace PacificoSegmentoVentasGaia.Tests.Application.Services;

public class SegmentoServiceTests
{
    private const string CelularValido = "987654321";

    private static IMapper CreateMapper()
    {
        var configuration = new MapperConfiguration(cfg => cfg.AddProfile<SegmentoMappingProfile>());
        return configuration.CreateMapper();
    }

    private static SegmentoService CreateService(
        ISegmentoRepository repository,
        List<string> campanias)
    {
        var mapper = CreateMapper();
        var options = Options.Create(new SegmentoVentaConfig { Campanias = campanias });
        return new SegmentoService(
            repository,
            mapper,
            options,
            NullLogger<SegmentoService>.Instance);
    }

    private static SegmentoService CreateServiceForGaia(ISegmentoRepository repository)
        => CreateService(repository, ["PS_EC_STR"]);

    [Fact]
    public async Task GetSegmentoVentaAsync_JoinsCampaniasTrimmedSkippingBlankEntries_PassesExpectedOutboundProcessIds()
    {
        var repository = Substitute.For<ISegmentoRepository>();
        repository.GetSegmentoVentaAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<Segmento> { new() });
        var campanias = new List<string> { " PS_EC_VIDA_BCP ", "", "   ", "PS_EC_STR" };
        var service = CreateService(repository, campanias);

        await service.GetSegmentoVentaAsync(CelularValido);

        await repository.Received(1)
            .GetSegmentoVentaAsync(Arg.Any<string>(), "PS_EC_VIDA_BCP,PS_EC_STR", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSegmentoVentaAsync_PassesCelularUnchanged()
    {
        var repository = Substitute.For<ISegmentoRepository>();
        repository.GetSegmentoVentaAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<Segmento> { new() });
        var service = CreateService(repository, ["PS_EC_STR"]);

        await service.GetSegmentoVentaAsync(CelularValido);

        await repository.Received(1)
            .GetSegmentoVentaAsync(CelularValido, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSegmentoVentaAsync_EmptyCampanias_ThrowsBusinessExceptionAndRepositoryIsNeverCalled()
    {
        var repository = Substitute.For<ISegmentoRepository>();
        var service = CreateService(repository, []);

        await Assert.ThrowsAsync<BusinessException>(() => service.GetSegmentoVentaAsync(CelularValido));

        await repository.DidNotReceive()
            .GetSegmentoVentaAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSegmentoVentaAsync_AllBlankCampanias_ThrowsBusinessExceptionAndRepositoryIsNeverCalled()
    {
        var repository = Substitute.For<ISegmentoRepository>();
        var service = CreateService(repository, ["", "   ", "\t"]);

        await Assert.ThrowsAsync<BusinessException>(() => service.GetSegmentoVentaAsync(CelularValido));

        await repository.DidNotReceive()
            .GetSegmentoVentaAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSegmentoVentaAsync_RepositoryReturnsEmpty_ThrowsNotFoundExceptionWithoutPhoneNumberInMessage()
    {
        var repository = Substitute.For<ISegmentoRepository>();
        repository.GetSegmentoVentaAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        var service = CreateService(repository, ["PS_EC_STR"]);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => service.GetSegmentoVentaAsync(CelularValido));

        Assert.DoesNotContain(CelularValido, exception.Message);
    }

    [Fact]
    public async Task GetSegmentoVentaAsync_OneRow_ReturnsSingleMappedItem()
    {
        var segmento = new Segmento { ContactId = "CONTACT-1", DNI = "12345678" };
        var repository = Substitute.For<ISegmentoRepository>();
        repository.GetSegmentoVentaAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<Segmento> { segmento });
        var service = CreateService(repository, ["PS_EC_STR"]);

        var result = await service.GetSegmentoVentaAsync(CelularValido);

        Assert.Single(result);
        Assert.Equal("CONTACT-1", result[0].ContactId);
        Assert.Equal("12345678", result[0].DNI);
    }

    [Fact]
    public async Task GetSegmentoVentaAsync_SeveralRows_ReturnsSameCount()
    {
        var rows = new List<Segmento> { new(), new(), new() };
        var repository = Substitute.For<ISegmentoRepository>();
        repository.GetSegmentoVentaAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(rows);
        var service = CreateService(repository, ["PS_EC_STR"]);

        var result = await service.GetSegmentoVentaAsync(CelularValido);

        Assert.Equal(rows.Count, result.Count);
    }

    [Fact]
    public async Task GetSegmentoVentaAsync_ForwardsCancellationTokenToRepository()
    {
        var repository = Substitute.For<ISegmentoRepository>();
        repository.GetSegmentoVentaAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<Segmento> { new() });
        var service = CreateService(repository, ["PS_EC_STR"]);
        using var cts = new CancellationTokenSource();

        await service.GetSegmentoVentaAsync(CelularValido, cts.Token);

        await repository.Received(1)
            .GetSegmentoVentaAsync(Arg.Any<string>(), Arg.Any<string>(), cts.Token);
    }

    private static SegmentoGaiaResultadoRequestDto CreateGaiaRequest() => new()
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
    public async Task RegistrarResultadoGaiaAsync_MapsRequestFieldsAndCallsRepository_ReturnsId()
    {
        var repository = Substitute.For<ISegmentoRepository>();
        repository.RegistrarResultadoAsync(Arg.Any<SegmentoGaia>(), Arg.Any<CancellationToken>())
            .Returns(new RegistroResultadoGaia(42, GestionActualizada: true));
        var service = CreateServiceForGaia(repository);
        var request = CreateGaiaRequest();

        var result = await service.RegistrarResultadoGaiaAsync(request);

        Assert.Equal(42, result.Id);
        await repository.Received(1).RegistrarResultadoAsync(
            Arg.Is<SegmentoGaia>(s =>
                s.ContactId == request.ContactId &&
                s.InteractionId == request.InteractionId &&
                s.NombreCliente == request.NombreCliente &&
                s.ApellidoCliente == request.ApellidoCliente &&
                s.DniCliente == request.DniCliente &&
                s.ResultadoGeneral == request.ResultadoGeneral),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegistrarResultadoGaiaAsync_GestionActualizada_ReturnsId()
    {
        var repository = Substitute.For<ISegmentoRepository>();
        repository.RegistrarResultadoAsync(Arg.Any<SegmentoGaia>(), Arg.Any<CancellationToken>())
            .Returns(new RegistroResultadoGaia(7, GestionActualizada: true));
        var service = CreateServiceForGaia(repository);

        var result = await service.RegistrarResultadoGaiaAsync(CreateGaiaRequest());

        Assert.Equal(7, result.Id);
    }

    [Fact]
    public async Task RegistrarResultadoGaiaAsync_GestionNoEncontrada_ReturnsIdWithoutThrowing()
    {
        var repository = Substitute.For<ISegmentoRepository>();
        repository.RegistrarResultadoAsync(Arg.Any<SegmentoGaia>(), Arg.Any<CancellationToken>())
            .Returns(new RegistroResultadoGaia(7, GestionActualizada: false));
        var service = CreateServiceForGaia(repository);

        var result = await service.RegistrarResultadoGaiaAsync(CreateGaiaRequest());

        Assert.Equal(7, result.Id);
    }

    [Fact]
    public async Task RegistrarResultadoGaiaAsync_ForwardsCancellationTokenToRepository()
    {
        var repository = Substitute.For<ISegmentoRepository>();
        repository.RegistrarResultadoAsync(Arg.Any<SegmentoGaia>(), Arg.Any<CancellationToken>())
            .Returns(new RegistroResultadoGaia(1, GestionActualizada: true));
        var service = CreateServiceForGaia(repository);
        using var cts = new CancellationTokenSource();

        await service.RegistrarResultadoGaiaAsync(CreateGaiaRequest(), cts.Token);

        await repository.Received(1).RegistrarResultadoAsync(Arg.Any<SegmentoGaia>(), cts.Token);
    }
}
