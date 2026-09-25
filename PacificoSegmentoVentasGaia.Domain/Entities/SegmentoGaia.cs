namespace PacificoSegmentoVentasGaia.Domain.Entities
{
    /// <summary>Resultado de una interacción del bot GAIA sobre un segmento de venta, pendiente de persistir en dbo.GSS_SegmentoGaia.</summary>
    public class SegmentoGaia
    {
        public string? ContactId { get; set; }
        public string? InteractionId { get; set; }
        public string? NombreCliente { get; set; }
        public string? ApellidoCliente { get; set; }
        public string? DniCliente { get; set; }
        public string? Fecha { get; set; }
        public string? Hora { get; set; }
        public string? Agente { get; set; }
        public string? Identificacion { get; set; }
        public string? Consentimiento { get; set; }
        public string? ValidacionPrima { get; set; }
        public string? MedioPago { get; set; }
        public string? Cobertura { get; set; }
        public string? Exclusiones { get; set; }
        public string? ProgramaBeneficios { get; set; }
        public string? Cierre { get; set; }
        public string? ResultadoGeneral { get; set; }
    }
}
