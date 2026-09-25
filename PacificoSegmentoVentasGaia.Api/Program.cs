using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PacificoSegmentoVentasGaia.Api.Swagger;
using PacificoSegmentoVentasGaia.Api.Middleware;
using PacificoSegmentoVentasGaia.Application;
using PacificoSegmentoVentasGaia.Application.Common;
using PacificoSegmentoVentasGaia.Application.Options;
using PacificoSegmentoVentasGaia.Infraestructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

//Log.Logger = new LoggerConfiguration()
//    .ReadFrom.Configuration(builder.Configuration)
//    .Enrich.FromLogContext()
//    .Enrich.WithProperty("Application", "PacificoSegmentoVentasGaia")
//    .CreateLogger();
//builder.Host.UseSerilog();

try
{
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddControllers();

    builder.Services.Configure<ApiBehaviorOptions>(options =>
    {
        options.InvalidModelStateResponseFactory = ctx =>
        {
            var errors = ctx.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .SelectMany(e => e.Value!.Errors.Select(err => $"{e.Key}: {err.ErrorMessage}"))
                .ToList();

            return new BadRequestObjectResult(ApiValidationErrorResponse.Fail("Errores de validación.", errors));
        };
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "PacificoSegmentoVentasGaia Segmento Venta API",
            Version = "v1",
            Description = """
                API para la consulta del segmento de venta de seguros Pacífico.

                **Autenticación:** JWT Bearer. Incluir el token en el header Authorization: Bearer {token}.


                **Códigos de respuesta:**
                - 200 – Segmento de venta encontrado
                - 201 – Resultado GAIA registrado
                - 400 – Datos de entrada inválidos
                - 401 – Token ausente o inválido
                - 404 – Segmento de venta no encontrado
                - 500 – Error en los servidores de Covisian
                """,
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "Covisian – Desarrollo Peru",
                Email = "webperu@covisian.com",
                Url = new Uri("https://covisian.com")
            }
        });

        c.EnableAnnotations();
        c.SchemaFilter<ErrorResponseExamplesSchemaFilter>();

        var apiXml = Path.Combine(AppContext.BaseDirectory, "PacificoSegmentoVentasGaia.Api.xml");
        var appXml = Path.Combine(AppContext.BaseDirectory, "PacificoSegmentoVentasGaia.Application.xml");
        if (File.Exists(apiXml)) c.IncludeXmlComments(apiXml, includeControllerXmlComments: true);
        if (File.Exists(appXml)) c.IncludeXmlComments(appXml);

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Bearer emitido por Keycloak. Formato: Bearer {token}",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id   = "Bearer"
                    }
                },
                new List<string>()
            }
        });
    });

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
       .AddJwtBearer(options =>
       {
           var keycloakConfig = builder.Configuration.GetSection("Keycloak").Get<KeycloakConfig>();
           if (keycloakConfig == null)
           {
               throw new InvalidOperationException("Keycloak configuration is missing");
           }
           options.Authority = keycloakConfig.Authority;
           options.Audience = keycloakConfig.Audience;
           options.RequireHttpsMetadata = true;
           options.MetadataAddress = keycloakConfig.CertsUrl;
           options.BackchannelHttpHandler = new HttpClientHandler()
           {
               ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
           };
           options.TokenValidationParameters = new TokenValidationParameters
           {
               ValidateIssuer = true,
               ValidIssuer = keycloakConfig.Authority,
               ValidateAudience = true,
               ValidAudience = keycloakConfig.Audience,
               ValidateLifetime = true
           };
           options.Events = new JwtBearerEvents
           {
               OnChallenge = async ctx =>
               {
                   ctx.HandleResponse();
                   ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                   ctx.Response.ContentType = "application/json";
                   var body = System.Text.Json.JsonSerializer.Serialize(
                       Api401Response.Fail("Token JWT ausente o inválido."),
                       new System.Text.Json.JsonSerializerOptions
                       {
                           PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                       });
                   await ctx.Response.WriteAsync(body);
               }
           };
       });


    builder.Services.AddAuthorization();

    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.DocumentTitle = "Pacifico Segmento Ventas Gaia API";
        c.RoutePrefix = "swagger/documentation";
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pacifico Segmento Ventas Gaia API v1");
        c.DefaultModelsExpandDepth(-1);
        c.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Model);
        c.DisplayRequestDuration();
        c.EnableFilter();
        c.EnableDeepLinking();
    });

    app.UseMiddleware<LogEnrichmentMiddleware>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>Partial class exposed so <c>WebApplicationFactory&lt;Program&gt;</c> can bootstrap the app for integration tests.</summary>
public partial class Program;