namespace PacificoSegmentoVentasGaia.Application.Common;

/// <summary>Envelope para respuestas de acceso no autorizado (401).</summary>
public class Api401Response : ApiErrorResponseBase
{
    public static Api401Response Fail(string message) => new() { Message = message };
}
