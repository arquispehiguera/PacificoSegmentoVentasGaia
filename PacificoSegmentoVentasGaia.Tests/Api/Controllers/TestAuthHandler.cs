using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PacificoSegmentoVentasGaia.Tests.Api.Controllers;

/// <summary>
/// Authentication handler used only by integration tests. Authenticates a request only
/// when the "Authorization" header carries the exact value <see cref="HeaderValue"/>;
/// otherwise it returns <see cref="AuthenticateResult.NoResult"/> so the normal
/// ASP.NET Core pipeline challenges the request (401), without ever contacting Keycloak.
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";
    public const string HeaderValue = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var headerValue) || headerValue != HeaderValue)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new[] { new Claim(ClaimTypes.Name, "integration-test-user") };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
