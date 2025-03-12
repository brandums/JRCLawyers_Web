using JRC_Abogados.Server.Models;
using JRC_Abogados.Server.Models.audits;
using Microsoft.EntityFrameworkCore;

namespace JRC_Abogados.Server.DataBaseContext
{
    public class DBaseContext : DbContext
    {
        public DbSet<Empleado> Empleado { get; set; }
        public DbSet<Caso> Caso { get; set; }
        public DbSet<Cita> Cita { get; set; }
        public DbSet<Cliente> Cliente { get; set; }
        public DbSet<Documento> Documento { get; set; }
        public DbSet<Expediente> Expediente { get; set; }
        public DbSet<Recordatorio> Recordatorio { get; set; }
        public DbSet<Ubicacion> Ubicacion { get; set; }
        public DbSet<Rol> Rol { get; set; }
        public DbSet<TipoCaso> TipoCaso { get; set; }
        public DbSet<TipoExpediente> TipoExpediente { get; set; }
        public DbSet<TipoDocumento> TipoDocumento { get; set; }
        public DbSet<Juzgado> Juzgado { get; set; }
        public DbSet<Estado> Estado { get; set; }
        public DbSet<ClienteAudit> ClienteAudit { get; set; }
        public DbSet<CasoAudit> CasoAudit { get; set; }
        public DbSet<CitaAudit> CitaAudit { get; set; }
        public DbSet<DocumentoAudit> DocumentoAudit { get; set; }
        public DbSet<ExpedienteAudit> ExpedienteAudit { get; set; }
        public DbSet<RecordatorioAudit> RecordatorioAudit { get; set; }
        public DbSet<EmpleadoAudit> EmpleadoAudit { get; set; }
        public DbSet<Reporte> Reporte { get; set; }

        public DBaseContext(DbContextOptions<DBaseContext> options)
        : base(options)
        { }
    }
}
