using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JRC_Abogados.Server.Models
{
    public class Expediente
    {
        [Key]
        public int Id { get; set; }
        public string Nombre { get; set; }
        public int TipoExpedienteId { get; set; }
        [ForeignKey("TipoExpedienteId")]
        public virtual TipoExpediente TipoExpediente { get; set; }
        public DateTime? FechaInicio { get; set; }
        public int ClienteId { get; set; }
        public int? CasoId { get; set; }
        [ForeignKey("CasoId")]
        public virtual Caso Caso { get; set; }
        [ForeignKey("ClienteId")]
        public virtual Cliente Cliente { get; set; }
        public int EmpleadoId { get; set; }
        [ForeignKey("EmpleadoId")]
        public virtual Empleado? Empleado { get; set; }
    }
}
