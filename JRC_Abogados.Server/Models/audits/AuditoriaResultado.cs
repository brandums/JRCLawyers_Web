using JRC_Abogados.Server.ModelsDTO;

namespace JRC_Abogados.Server.Models.audits
{
    public class AuditoriaResultado
    {
        public ReporteDTO Reporte { get; set; }
        public List<CasoAudit> CasosAuditados { get; set; }
        public List<ClienteAudit> ClientesAuditados { get; set; }
        public List<CitaAudit> CitasAuditadas { get; set; }
        public List<RecordatorioAudit> RecordatoriosAuditados { get; set; }
        public List<ExpedienteAudit> ExpedientesAuditados { get; set; }
        public List<DocumentoAudit> DocumentosAuditados { get; set; }
    }
}
