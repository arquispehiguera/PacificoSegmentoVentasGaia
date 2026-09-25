namespace PacificoSegmentoVentasGaia.Application.Common;

/// <summary>Envelope para respuestas de error de validación (400).</summary>
public class ApiValidationErrorResponse : ApiErrorResponseBase
{
    /// <summary>Detalle de los campos que fallaron la validación.</summary>
    /// <example>["Celular: El campo es requerido.", "Correo: Formato inválido."]</example>
    public List<string> Errors { get; init; } = [];

    /// <summary>Crea una respuesta de error de validación.</summary>
    public static ApiValidationErrorResponse Fail(string message, List<string>? errors = null) => new()
    {
        Message = message,
        Errors  = errors ?? []
    };
}
