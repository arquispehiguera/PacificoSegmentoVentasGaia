namespace PacificoSegmentoVentasGaia.Application.Common;

/// <summary>Base común para los distintos envelopes de error de la API.</summary>
public abstract class ApiErrorResponseBase
{
    /// <summary>Siempre <c>false</c> en respuestas de error.</summary>
    /// <example>false</example>
    public bool Success { get; init; } = false;

    /// <summary>Mensaje descriptivo del error.</summary>
    public string Message { get; init; } = string.Empty;
}
