namespace PacificoSegmentoVentasGaia.Domain.Entities
{
    /// <summary>
    /// Resultado de persistir un registro de <c>dbo.GSS_SegmentoGaia</c>: el identificador
    /// generado y si además se encontró y actualizó la gestión relacionada en
    /// <c>dbo.GSS_Gestiones</c>.
    /// </summary>
    /// <param name="Id">Identificador del registro creado en <c>dbo.GSS_SegmentoGaia</c>.</param>
    /// <param name="GestionActualizada">
    /// <c>true</c> si existía una gestión con el ContactId + LastInteractionId indicados y quedó
    /// marcada como integrada; <c>false</c> si no se encontró ninguna (el registro igual se
    /// persiste).
    /// </param>
    public record RegistroResultadoGaia(int Id, bool GestionActualizada);
}
