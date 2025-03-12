using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JRC_Abogados.Server.Models.audits
{
    public class DocumentoAudit
    {
        [Key]
        public int Id { get; set; }
        public int DocumentoId { get; set; }
        public string Nombre { get; set; }
        public string? Descripcion { get; set; }
        public string Path { get; set; }
        public DateTime FechaInicio { get; set; }
        public int ExpedienteId { get; set; }
        [ForeignKey("ExpedienteId")]
        public virtual Expediente Expediente { get; set; }
        public int ClienteId { get; set; }
        [ForeignKey("ClienteId")]
        public virtual Cliente Cliente { get; set; }
        public int EmpleadoId { get; set; }
        [ForeignKey("EmpleadoId")]
        public virtual Empleado? Empleado { get; set; }
        public DateTime FechaAccion { get; set; }
        public string TipoAccion { get; set; }
        public int EmpleadoAccionId { get; set; }
        public virtual Empleado? EmpleadoAccion { get; set; }
        public string DetallesAccion { get; set; }
    }
}
