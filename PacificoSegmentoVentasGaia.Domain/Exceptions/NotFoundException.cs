namespace PacificoSegmentoVentasGaia.Domain.Exceptions;

/// <summary>
/// Excepción para recursos no encontrados (404). Ningún endpoint actual la lanza —
/// la API hoy solo expone un POST sin operaciones de lectura por id. Queda reservada
/// para cuando se agreguen endpoints de consulta. Se mapea a 404 en
/// <c>ExceptionHandlingMiddleware</c>.
/// </summary>
public class NotFoundException : BusinessException
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found.")
    {
    }

    public NotFoundException(string message) : base(message)
    {
    }
}
