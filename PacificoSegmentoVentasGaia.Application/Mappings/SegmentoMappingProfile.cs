using AutoMapper;
using PacificoSegmentoVentasGaia.Application.Dtos;
using PacificoSegmentoVentasGaia.Domain.Entities;

namespace PacificoSegmentoVentasGaia.Application.Mappings;

public class SegmentoMappingProfile : Profile
{
    public SegmentoMappingProfile()
    {
        CreateMap<Segmento, SegmentoVentaDto>();
        CreateMap<SegmentoGaiaResultadoRequestDto, SegmentoGaia>();
    }
}
