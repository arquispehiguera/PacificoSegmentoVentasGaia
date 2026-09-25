namespace PacificoSegmentoVentasGaia.Application.Common;

/// <summary>Envelope para respuestas de error interno del servidor (500). No expone detalles internos.</summary>
public class Api500Response : ApiErrorResponseBase
{
    public static Api500Response Fail() => new()
    {
        Message = "Error en los servidores de Covisian. Intente nuevamente en unos minutos."
    };
}
