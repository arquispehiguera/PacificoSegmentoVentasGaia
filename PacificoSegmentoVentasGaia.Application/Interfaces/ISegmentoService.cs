using PacificoSegmentoVentasGaia.Application.Dtos;

namespace PacificoSegmentoVentasGaia.Application.Interfaces;

public interface ISegmentoService
{
    Task<IReadOnlyList<SegmentoVentaDto>> GetSegmentoVentaAsync(string celular, CancellationToken ct = default);

    /// <summary>
    /// Registra el resultado de una interacción GAIA y, si existe, marca la gestión relacionada
    /// como integrada. Pueden existir varios resultados para el mismo ContactId + InteractionId.
    /// El resultado GAIA siempre se persiste: si no existe una gestión con ese ContactId +
    /// InteractionId, no se lanza ninguna excepción y se registra una advertencia en el log.
    /// </summary>
    Task<SegmentoGaiaResultadoResponseDto> RegistrarResultadoGaiaAsync(SegmentoGaiaResultadoRequestDto request, CancellationToken ct = default);
}
