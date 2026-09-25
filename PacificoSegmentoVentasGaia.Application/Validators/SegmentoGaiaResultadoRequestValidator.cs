using FluentValidation;
using PacificoSegmentoVentasGaia.Application.Dtos;

namespace PacificoSegmentoVentasGaia.Application.Validators;

public class SegmentoGaiaResultadoRequestValidator : AbstractValidator<SegmentoGaiaResultadoRequestDto>
{
    public SegmentoGaiaResultadoRequestValidator()
    {
        RuleFor(x => x.ContactId)
            .NotEmpty().WithMessage("ContactId es requerido.")
            .MaximumLength(100).WithMessage("ContactId no debe exceder 100 caracteres.");

        RuleFor(x => x.InteractionId)
            .NotEmpty().WithMessage("InteractionId es requerido.")
            .MaximumLength(100).WithMessage("InteractionId no debe exceder 100 caracteres.");

        RuleFor(x => x.NombreCliente)
            .MaximumLength(150).WithMessage("NombreCliente no debe exceder 150 caracteres.");

        RuleFor(x => x.ApellidoCliente)
            .MaximumLength(150).WithMessage("ApellidoCliente no debe exceder 150 caracteres.");

        RuleFor(x => x.DniCliente)
            .MaximumLength(20).WithMessage("DniCliente no debe exceder 20 caracteres.");

        RuleFor(x => x.Fecha)
            .MaximumLength(50).WithMessage("Fecha no debe exceder 50 caracteres.");

        RuleFor(x => x.Hora)
            .MaximumLength(50).WithMessage("Hora no debe exceder 50 caracteres.");

        RuleFor(x => x.Agente)
            .MaximumLength(150).WithMessage("Agente no debe exceder 150 caracteres.");

        RuleFor(x => x.Identificacion)
            .MaximumLength(10).WithMessage("Identificacion no debe exceder 10 caracteres.");

        RuleFor(x => x.Consentimiento)
            .MaximumLength(10).WithMessage("Consentimiento no debe exceder 10 caracteres.");

        RuleFor(x => x.ValidacionPrima)
            .MaximumLength(10).WithMessage("ValidacionPrima no debe exceder 10 caracteres.");

        RuleFor(x => x.MedioPago)
            .MaximumLength(10).WithMessage("MedioPago no debe exceder 10 caracteres.");

        RuleFor(x => x.Cobertura)
            .MaximumLength(10).WithMessage("Cobertura no debe exceder 10 caracteres.");

        RuleFor(x => x.Exclusiones)
            .MaximumLength(10).WithMessage("Exclusiones no debe exceder 10 caracteres.");

        RuleFor(x => x.ProgramaBeneficios)
            .MaximumLength(10).WithMessage("ProgramaBeneficios no debe exceder 10 caracteres.");

        RuleFor(x => x.Cierre)
            .MaximumLength(10).WithMessage("Cierre no debe exceder 10 caracteres.");

        RuleFor(x => x.ResultadoGeneral)
            .MaximumLength(50).WithMessage("ResultadoGeneral no debe exceder 50 caracteres.");
    }
}
