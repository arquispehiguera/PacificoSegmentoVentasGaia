namespace PacificoSegmentoVentasGaia.Domain.Exceptions;

/// <summary>
/// Excepción de validación de negocio. Hoy el 400 de request malformado lo resuelve
/// FluentValidation vía <c>InvalidModelStateResponseFactory</c> (ver Program.cs) sin pasar
/// por acá — esta clase queda para validaciones de negocio explícitas que se lancen a mano
/// desde código de Application/Domain (ej. reglas cruzadas entre campos que FluentValidation
/// no puede expresar declarativamente). Se mapea a 400 en <c>ExceptionHandlingMiddleware</c>.
/// </summary>
public class ValidationException : BusinessException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("Se produjo uno o más errores de validación.")
    {
        Errors = errors ?? new Dictionary<string, string[]>();
    }

    public ValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }
}
