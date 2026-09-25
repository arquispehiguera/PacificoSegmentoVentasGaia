using FluentValidation;
using PacificoSegmentoVentasGaia.Application.Dtos;

namespace PacificoSegmentoVentasGaia.Application.Validators;

public class SegmentoVentaQueryValidator : AbstractValidator<SegmentoVentaQueryDto>
{
    public SegmentoVentaQueryValidator()
    {
        RuleFor(x => x.Celular)
            .NotEmpty().WithMessage("Celular es requerido.")
            .Matches(@"^9\d{8}$").WithMessage("Celular debe ser un número móvil peruano de 9 dígitos que empiece con 9.");
    }
}
