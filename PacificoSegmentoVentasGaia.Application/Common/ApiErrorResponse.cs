namespace PacificoSegmentoVentasGaia.Application.Common;

/// <summary>Envelope para respuestas de error generales (404).</summary>
public class ApiErrorResponse : ApiErrorResponseBase
{
    /// <summary>Crea una respuesta de error.</summary>
    public static ApiErrorResponse Fail(string message) => new() { Message = message };
}
