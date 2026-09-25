namespace PacificoSegmentoVentasGaia.Application.Common;

/// <summary>Envelope para respuestas exitosas de la API.</summary>
/// <typeparam name="T">Tipo del payload de datos.</typeparam>
public class ApiResponse<T>
{
    /// <summary>Siempre <c>true</c> en respuestas exitosas.</summary>
    /// <example>true</example>
    public bool Success { get; init; } = true;

    /// <summary>Mensaje descriptivo del resultado.</summary>
    /// <example>Segmento encontrado.</example>
    public string Message { get; init; } = string.Empty;

    /// <summary>Payload de la respuesta.</summary>
    public T? Data { get; init; }

    /// <summary>Crea una respuesta exitosa.</summary>
    public static ApiResponse<T> Ok(T data, string message = "Operación exitosa") => new()
    {
        Message = message,
        Data    = data
    };
}
