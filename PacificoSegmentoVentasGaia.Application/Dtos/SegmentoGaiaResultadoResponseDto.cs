namespace PacificoSegmentoVentasGaia.Application.Dtos;

/// <summary>Datos devueltos al registrar el resultado de una interacción GAIA.</summary>
public class SegmentoGaiaResultadoResponseDto
{
    /// <summary>Identificador del registro creado en `GSS_SegmentoGaia`.</summary>
    /// <example>123</example>
    public int Id { get; set; }
}
