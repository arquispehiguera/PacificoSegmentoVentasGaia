using Dapper;
using Microsoft.Extensions.DependencyInjection;
using PacificoSegmentoVentasGaia.Domain.Entities;
using PacificoSegmentoVentasGaia.Domain.Interfaces;
using PacificoSegmentoVentasGaia.Infraestructure.Data;
using Polly;
using System.Data;
using System.Data.Common;

namespace PacificoSegmentoVentasGaia.Infraestructure.Repositories
{
    public class SegmentoRepository: ISegmentoRepository
    {
        private readonly DapperContext _context;
        private readonly ResiliencePipeline _pipeline;

        public SegmentoRepository(
            DapperContext context,
            [FromKeyedServices("db")] ResiliencePipeline pipeline)
        {
            _context = context;
            _pipeline = pipeline;
        }
        public async Task<IEnumerable<Segmento>> GetSegmentoVentaAsync(string telefono, string outboundProcessIds, CancellationToken ct = default)
        {
            const string storedProcedure = "SppGss_App_PacificoGaiaSegmentoVenta";
            var parameters = new
            {
                Telefono = telefono,
                OutboundProcessIds = outboundProcessIds
            };
            return await _pipeline.ExecuteAsync(async token =>
            {
                using var connection = _context.CreateConnection();
                var result = await connection.QueryAsync<Segmento>(
                    storedProcedure,
                    parameters,
                    commandType: CommandType.StoredProcedure);
                return result;
            }, ct);
        }

        public async Task<RegistroResultadoGaia> RegistrarResultadoAsync(SegmentoGaia segmentoGaia, CancellationToken ct = default)
        {
            // Todo el flujo corre dentro del pipeline de resiliencia ("db"), que solo reintenta
            // errores transitorios de SQL Server (ver Infraestructure.DependencyInjection). El
            // resultado GAIA siempre se persiste (no se lanza ninguna excepción de negocio si no
            // hay gestión que actualizar), así que el pipeline solo reintenta errores de
            // SqlException/TimeoutException, nunca un resultado de negocio, y nunca reintenta
            // sobre cambios ya confirmados porque el Commit() solo se ejecuta una vez, al final.
            return await _pipeline.ExecuteAsync(async token =>
            {
                using var connection = _context.CreateConnection();
                if (connection is DbConnection dbConnection)
                {
                    await dbConnection.OpenAsync(token);
                }
                else
                {
                    connection.Open();
                }

                // using sin Commit() explícito hace rollback automático al hacer Dispose, así que
                // basta con dejar que una SqlException se propague sin llegar al Commit() para
                // descartar los cambios de la transacción.
                using var transaction = connection.BeginTransaction();

                const string insertSql = """
                    INSERT INTO dbo.GSS_SegmentoGaia
                        (ContactId, InteractionId, NombreCliente, ApellidoCliente, DniCliente, Fecha, Hora, Agente,
                         Identificacion, Consentimiento, ValidacionPrima, MedioPago, Cobertura, Exclusiones,
                         ProgramaBeneficios, Cierre, ResultadoGeneral)
                    OUTPUT INSERTED.Id
                    VALUES
                        (@ContactId, @InteractionId, @NombreCliente, @ApellidoCliente, @DniCliente, @Fecha, @Hora, @Agente,
                         @Identificacion, @Consentimiento, @ValidacionPrima, @MedioPago, @Cobertura, @Exclusiones,
                         @ProgramaBeneficios, @Cierre, @ResultadoGeneral);
                    """;
                var id = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                    insertSql,
                    segmentoGaia,
                    transaction,
                    cancellationToken: token));

                const string updateSql = """
                    UPDATE dbo.GSS_Gestiones
                    SET IntegracionGaia = 1
                    WHERE ContactId = @ContactId AND LastInteractionId = @InteractionId;
                    """;
                var filasAfectadas = await connection.ExecuteAsync(new CommandDefinition(
                    updateSql,
                    new { segmentoGaia.ContactId, segmentoGaia.InteractionId },
                    transaction,
                    cancellationToken: token));

                // El resultado GAIA debe persistirse siempre, exista o no una gestión que
                // actualizar: revertir el INSERT ya confirmado causaría problemas más adelante
                // (reportes/trazabilidad del bot). Por eso no se lanza excepción ni se revierte
                // la transacción cuando la gestión no aparece; solo se informa con
                // GestionActualizada = false para que la capa de aplicación lo registre.
                transaction.Commit();
                return new RegistroResultadoGaia(id, GestionActualizada: filasAfectadas > 0);
            }, ct);
        }
    }
}
