using AutoMapper;
using PacificoSegmentoVentasGaia.Application.Mappings;

namespace PacificoSegmentoVentasGaia.Tests.Application.Mappings;

public class SegmentoMappingProfileTests
{
    [Fact]
    public void Configuration_IsValid()
    {
        var configuration = new MapperConfiguration(cfg => cfg.AddProfile<SegmentoMappingProfile>());

        configuration.AssertConfigurationIsValid();
    }
}
