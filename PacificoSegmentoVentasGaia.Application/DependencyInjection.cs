using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PacificoSegmentoVentasGaia.Application.Interfaces;
using PacificoSegmentoVentasGaia.Application.Mappings;
using PacificoSegmentoVentasGaia.Application.Services;
using PacificoSegmentoVentasGaia.Application.Validators;

namespace PacificoSegmentoVentasGaia.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(SegmentoMappingProfile).Assembly);
        services.AddValidatorsFromAssemblyContaining<SegmentoVentaQueryValidator>();
        services.AddScoped<ISegmentoService, SegmentoService>();
        return services;
    }
}
