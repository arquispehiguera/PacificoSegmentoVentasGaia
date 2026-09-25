namespace PacificoSegmentoVentasGaia.Domain.Exceptions;

/// <summary>
/// Excepción lanzada cuando la autenticación falla. Hoy el 401 real lo resuelve
/// <c>JwtBearerEvents.OnChallenge</c> (ver Program.cs), que responde directo sin lanzar
/// esta excepción. Queda reservada para fallas de autenticación que se detecten a mano
/// en código propio (fuera del pipeline de JWT). Se mapea a 401 en
/// <c>ExceptionHandlingMiddleware</c>.
/// </summary>
public class AuthenticationFailedException : BusinessException
{
    public AuthenticationFailedException(string message) : base(message)
    {
    }

    public AuthenticationFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
