using FluentValidation.TestHelper;
using PacificoSegmentoVentasGaia.Application.Dtos;
using PacificoSegmentoVentasGaia.Application.Validators;

namespace PacificoSegmentoVentasGaia.Tests.Application.Validators;

public class SegmentoVentaQueryValidatorTests
{
    private readonly SegmentoVentaQueryValidator _validator = new();

    [Fact]
    public void Validate_CelularValid_DoesNotHaveValidationError()
    {
        var model = new SegmentoVentaQueryDto { Celular = "987654321" };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.Celular);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("98765432")]
    [InlineData("9876543210")]
    [InlineData("887654321")]
    [InlineData("98765432a")]
    [InlineData("+51987654321")]
    [InlineData(" 987654321")]
    public void Validate_CelularInvalid_HasValidationError(string? celular)
    {
        var model = new SegmentoVentaQueryDto { Celular = celular };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Celular);
    }
}
