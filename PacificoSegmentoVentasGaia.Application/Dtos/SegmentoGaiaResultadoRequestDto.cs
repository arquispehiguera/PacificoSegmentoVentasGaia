namespace PacificoSegmentoVentasGaia.Application.Dtos;

/// <summary>Resultado de una interacción del bot GAIA sobre un segmento de venta.</summary>
public class SegmentoGaiaResultadoRequestDto
{
    /// <summary>Identificador de contacto de la interacción.</summary>
    /// <example>CONTACT-12345</example>
    public string? ContactId { get; set; }

    /// <summary>Identificador de la interacción.</summary>
    /// <example>INTERACTION-67890</example>
    public string? InteractionId { get; set; }

    /// <summary>Nombre del cliente.</summary>
    public string? NombreCliente { get; set; }

    /// <summary>Apellido del cliente.</summary>
    public string? ApellidoCliente { get; set; }

    /// <summary>Documento de identidad del cliente.</summary>
    public string? DniCliente { get; set; }

    /// <summary>Fecha de la interacción.</summary>
    public string? Fecha { get; set; }

    /// <summary>Hora de la interacción.</summary>
    public string? Hora { get; set; }

    /// <summary>Agente que atendió la interacción.</summary>
    public string? Agente { get; set; }

    /// <summary>Resultado del paso de identificación.</summary>
    public string? Identificacion { get; set; }

    /// <summary>Resultado del paso de consentimiento.</summary>
    public string? Consentimiento { get; set; }

    /// <summary>Resultado del paso de validación de prima.</summary>
    public string? ValidacionPrima { get; set; }

    /// <summary>Resultado del paso de medio de pago.</summary>
    public string? MedioPago { get; set; }

    /// <summary>Resultado del paso de cobertura.</summary>
    public string? Cobertura { get; set; }

    /// <summary>Resultado del paso de exclusiones.</summary>
    public string? Exclusiones { get; set; }

    /// <summary>Resultado del paso de programa de beneficios.</summary>
    public string? ProgramaBeneficios { get; set; }

    /// <summary>Resultado del paso de cierre.</summary>
    public string? Cierre { get; set; }

    /// <summary>Resultado general de la interacción.</summary>
    public string? ResultadoGeneral { get; set; }
}
