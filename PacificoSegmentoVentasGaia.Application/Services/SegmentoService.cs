using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PacificoSegmentoVentasGaia.Application.Dtos;
using PacificoSegmentoVentasGaia.Application.Interfaces;
using PacificoSegmentoVentasGaia.Application.Options;
using PacificoSegmentoVentasGaia.Domain.Entities;
using PacificoSegmentoVentasGaia.Domain.Exceptions;
using PacificoSegmentoVentasGaia.Domain.Interfaces;

namespace PacificoSegmentoVentasGaia.Application.Services;

public class SegmentoService : ISegmentoService
{
    private readonly ISegmentoRepository      _segmentoRepository;
    private readonly IMapper                  _mapper;
    private readonly SegmentoVentaConfig      _segmentoVentaConfig;
    private readonly ILogger<SegmentoService> _logger;

    public SegmentoService(
        ISegmentoRepository segmentoRepository,
        IMapper mapper,
        IOptions<SegmentoVentaConfig> segmentoVentaConfig,
        ILogger<SegmentoService> logger)
    {
        _segmentoRepository      = segmentoRepository;
        _mapper                  = mapper;
        _segmentoVentaConfig     = segmentoVentaConfig.Value;
        _logger                  = logger;
    }

    public async Task<IReadOnlyList<SegmentoVentaDto>> GetSegmentoVentaAsync(string celular, CancellationToken ct = default)
    {
        var outboundProcessIds = string.Join(",", (_segmentoVentaConfig.Campanias ?? [])
            .Select(c => c?.Trim())
            .Where(c => !string.IsNullOrEmpty(c)));

        if (string.IsNullOrEmpty(outboundProcessIds))
        {
            throw new BusinessException("No hay campañas configuradas para la consulta de segmento de venta (SegmentoVenta:Campanias).");
        }

        var segmentos = await _segmentoRepository.GetSegmentoVentaAsync(celular, outboundProcessIds, ct);
        var lista = segmentos.ToList();

        if (lista.Count == 0)
        {
            throw new NotFoundException("No se encontró segmento de venta para el celular indicado.");
        }

        return _mapper.Map<List<SegmentoVentaDto>>(lista);
    }

    public async Task<SegmentoGaiaResultadoResponseDto> RegistrarResultadoGaiaAsync(SegmentoGaiaResultadoRequestDto request, CancellationToken ct = default)
    {
        var segmentoGaia = _mapper.Map<SegmentoGaia>(request);
        var registro = await _segmentoRepository.RegistrarResultadoAsync(segmentoGaia, ct);

        if (!registro.GestionActualizada)
        {
            _logger.LogWarning(
                "Resultado GAIA {SegmentoGaiaId} persistido sin gestión asociada para ContactId {ContactId} e InteractionId {InteractionId}.",
                registro.Id, request.ContactId, request.InteractionId);
        }

        return new SegmentoGaiaResultadoResponseDto { Id = registro.Id };
    }
}
