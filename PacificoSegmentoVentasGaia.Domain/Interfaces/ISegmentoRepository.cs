using PacificoSegmentoVentasGaia.Domain.Entities;

namespace PacificoSegmentoVentasGaia.Domain.Interfaces
{
    public interface ISegmentoRepository
    {
        Task<IEnumerable<Segmento>> GetSegmentoVentaAsync(string telefono, string outboundProcessIds, CancellationToken ct = default);

        /// <summary>
        /// Registra el resultado de una interacción GAIA en <c>dbo.GSS_SegmentoGaia</c> y, si
        /// existe, marca como integrada la gestión relacionada en <c>dbo.GSS_Gestiones</c>, en
        /// una única transacción. Pueden existir varios resultados para el mismo ContactId +
        /// InteractionId. El resultado GAIA siempre se persiste: si no existe una gestión con ese
        /// ContactId + LastInteractionId, la transacción se confirma igual y
        /// <see cref="RegistroResultadoGaia.GestionActualizada"/> queda en
        /// <c>false</c>.
        /// </summary>
        /// <returns>El identificador del registro creado y si la gestión fue actualizada.</returns>
        Task<RegistroResultadoGaia> RegistrarResultadoAsync(SegmentoGaia segmentoGaia, CancellationToken ct = default);
    }
}
