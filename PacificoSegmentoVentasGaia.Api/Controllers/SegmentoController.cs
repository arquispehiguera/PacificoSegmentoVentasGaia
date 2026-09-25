using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PacificoSegmentoVentasGaia.Application.Common;
using PacificoSegmentoVentasGaia.Application.Dtos;
using PacificoSegmentoVentasGaia.Application.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace PacificoSegmentoVentasGaia.Api.Controllers;

/// <summary>Operaciones de consulta de segmento de venta.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class SegmentoController : ControllerBase
{
    private readonly ISegmentoService _segmentoService;

    public SegmentoController(ISegmentoService segmentoService)
    {
        _segmentoService = segmentoService;
    }

    /// <summary>Obtiene el segmento de venta de un prospecto a partir de su celular.</summary>
    /// <remarks>
    /// Consulta el segmento de venta asociado al número de celular en las campañas
    /// configuradas en `SegmentoVenta:Campanias`.
    /// </remarks>
    /// <param name="query">Parámetros de consulta. Requiere `celular`.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>La lista de registros de segmento de venta encontrados para el celular.</returns>
    [HttpGet]
    [SwaggerOperation(
        Summary     = "Obtener segmento de venta",
        Description = "Consulta el segmento de venta asociado a un número de celular.",
        OperationId = "GetSegmentoVenta",
        Tags        = new[] { "Segmento" }
    )]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SegmentoVentaDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiValidationErrorResponse),                   StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Api401Response),                               StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse),                             StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Api500Response),                               StatusCodes.Status500InternalServerError)]
    [SwaggerResponse(StatusCodes.Status200OK,                  "Segmento de venta encontrado.",                                           typeof(ApiResponse<IReadOnlyList<SegmentoVentaDto>>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest,          "Datos de entrada inválidos. Revisar el campo `errors` para el detalle.",   typeof(ApiValidationErrorResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized,        "Token JWT ausente o inválido.",                                           typeof(Api401Response))]
    [SwaggerResponse(StatusCodes.Status404NotFound,            "No se encontró segmento de venta para el celular indicado.",              typeof(ApiErrorResponse))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Error en los servidores de Covisian. Intente nuevamente en unos minutos.", typeof(Api500Response))]
    public async Task<IActionResult> GetSegmentoVentaAsync([FromQuery] SegmentoVentaQueryDto query, CancellationToken ct)
    {
        var segmentos = await _segmentoService.GetSegmentoVentaAsync(query.Celular!, ct);
        return Ok(ApiResponse<IReadOnlyList<SegmentoVentaDto>>.Ok(segmentos, "Segmento encontrado."));
    }

    /// <summary>Registra el resultado de una interacción del bot GAIA.</summary>
    /// <remarks>
    /// Persiste el resultado en `GSS_SegmentoGaia` y, si existe, marca la gestión relacionada en
    /// `GSS_Gestiones` (ContactId + LastInteractionId) como integrada con GAIA. El resultado se
    /// persiste siempre, incluso si no existe una gestión asociada; en ese caso se registra una
    /// advertencia en el log.
    /// </remarks>
    /// <param name="request">Resultado de la interacción GAIA.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>El identificador del registro creado y si la gestión relacionada fue actualizada.</returns>
    [HttpPost("resultado")]
    [SwaggerOperation(
        Summary     = "Registrar resultado de interacción GAIA",
        Description = "Persiste el resultado de una interacción GAIA y, si existe, marca la gestión relacionada como integrada.",
        OperationId = "RegistrarResultadoGaia",
        Tags        = new[] { "Segmento" }
    )]
    [ProducesResponseType(typeof(ApiResponse<SegmentoGaiaResultadoResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiValidationErrorResponse),                   StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Api401Response),                               StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Api500Response),                               StatusCodes.Status500InternalServerError)]
    [SwaggerResponse(StatusCodes.Status201Created,              "Resultado GAIA registrado.",                                                typeof(ApiResponse<SegmentoGaiaResultadoResponseDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest,           "Datos de entrada inválidos. Revisar el campo `errors` para el detalle.",     typeof(ApiValidationErrorResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized,         "Token JWT ausente o inválido.",                                             typeof(Api401Response))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError,  "Error en los servidores de Covisian. Intente nuevamente en unos minutos.",   typeof(Api500Response))]
    public async Task<IActionResult> RegistrarResultadoGaiaAsync([FromBody] SegmentoGaiaResultadoRequestDto request, CancellationToken ct)
    {
        var resultado = await _segmentoService.RegistrarResultadoGaiaAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<SegmentoGaiaResultadoResponseDto>.Ok(resultado, "Resultado GAIA registrado."));
    }
}
