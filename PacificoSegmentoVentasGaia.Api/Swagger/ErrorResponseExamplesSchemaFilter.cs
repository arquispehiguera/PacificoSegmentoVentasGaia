using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using PacificoSegmentoVentasGaia.Application.Common;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PacificoSegmentoVentasGaia.Api.Swagger;

/// <summary>
/// Asigna un ejemplo de "message" distinto por tipo de respuesta de error.
/// Necesario porque ApiErrorResponseBase centraliza la propiedad Message y el XML doc
/// de una propiedad heredada no admite un &lt;example&gt; distinto por tipo derivado.
/// </summary>
public class ErrorResponseExamplesSchemaFilter : ISchemaFilter
{
    private static readonly Dictionary<Type, string> MessageExamples = new()
    {
        [typeof(ApiErrorResponse)]           = "Recurso no encontrado.",
        [typeof(Api401Response)]             = "Token JWT ausente o inválido.",
        [typeof(Api500Response)]             = "Error en los servidores de Covisian. Intente nuevamente en unos minutos.",
        [typeof(ApiValidationErrorResponse)] = "Datos de entrada inválidos.",
    };

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (!MessageExamples.TryGetValue(context.Type, out var example))
            return;

        if (schema.Properties.TryGetValue("message", out var messageSchema))
            messageSchema.Example = new OpenApiString(example);
    }
}
