namespace PacificoSegmentoVentasGaia.Application.Dtos;

/// <summary>Parámetros de consulta para obtener el segmento de venta de un prospecto.</summary>
public class SegmentoVentaQueryDto
{
    /// <summary>Número de celular peruano del prospecto. Formato: 9 dígitos, empieza con 9.</summary>
    /// <example>987654321</example>
    public string? Celular { get; set; }
}
