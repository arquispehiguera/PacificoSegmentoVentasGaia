using FluentValidation.TestHelper;
using PacificoSegmentoVentasGaia.Application.Dtos;
using PacificoSegmentoVentasGaia.Application.Validators;

namespace PacificoSegmentoVentasGaia.Tests.Application.Validators;

public class SegmentoGaiaResultadoRequestValidatorTests
{
    private readonly SegmentoGaiaResultadoRequestValidator _validator = new();

    private static SegmentoGaiaResultadoRequestDto CreateValidRequest() => new()
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
    public void Validate_ValidRequest_DoesNotHaveAnyValidationError()
    {
        var model = CreateValidRequest();

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingContactId_HasValidationErrorForContactId(string? contactId)
    {
        var model = CreateValidRequest();
        model.ContactId = contactId;

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.ContactId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingInteractionId_HasValidationErrorForInteractionId(string? interactionId)
    {
        var model = CreateValidRequest();
        model.InteractionId = interactionId;

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.InteractionId);
    }

    [Theory]
    [InlineData(nameof(SegmentoGaiaResultadoRequestDto.ContactId), 101)]
    [InlineData(nameof(SegmentoGaiaResultadoRequestDto.InteractionId), 101)]
    [InlineData(nameof(SegmentoGaiaResultadoRequestDto.NombreCliente), 151)]
    [InlineData(nameof(SegmentoGaiaResultadoRequestDto.ApellidoCliente), 151)]
    [InlineData(nameof(SegmentoGaiaResultadoRequestDto.DniCliente), 21)]
    [InlineData(nameof(SegmentoGaiaResultadoRequestDto.Fecha), 51)]
    [InlineData(nameof(SegmentoGaiaResultadoRequestDto.Hora), 51)]
    [InlineData(nameof(SegmentoGaiaResultadoRequestDto.Agente), 151)]
    [InlineData(nameof(SegmentoGaiaResultadoRequestDto.Identificacion), 11)]
    [InlineData(nameof(SegmentoGaiaResultadoRequestDto.Consentimiento), 11)]
    [InlineData(nameof(SegmentoGaiaResultadoRequestDto.ValidacionPrima), 11)]
    [InlineData(nameof(SegmentoGaiaResultadoRequestDto.MedioPago), 11)]
    [InlineData(nameof(SegmentoGaiaResultadoRequestDto.Cobertura), 11)]
    [InlineData(nameof(SegmentoGaiaResultadoRequestDto.Exclusiones), 11)]
    [InlineData(nameof(SegmentoGaiaResultadoRequestDto.ProgramaBeneficios), 11)]
    [InlineData(nameof(SegmentoGaiaResultadoRequestDto.Cierre), 11)]
    [InlineData(nameof(SegmentoGaiaResultadoRequestDto.ResultadoGeneral), 51)]
    public void Validate_FieldExceedsMaxLength_HasValidationErrorForThatField(string propertyName, int length)
    {
        var model = CreateValidRequest();
        var overLongValue = new string('x', length);
        var property = typeof(SegmentoGaiaResultadoRequestDto).GetProperty(propertyName)!;
        property.SetValue(model, overLongValue);

        var result = _validator.TestValidate(model);

        Assert.True(result.Errors.Exists(e => e.PropertyName == propertyName));
    }
}
