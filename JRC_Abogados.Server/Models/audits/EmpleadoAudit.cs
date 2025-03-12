using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JRC_Abogados.Server.Models.audits
{
    public class EmpleadoAudit
    {
        [Key]
        public int Id { get; set; }
        public int EmpleadoId { get; set; }
        public string Nombre { get; set; }
        public string CorreoElectronico { get; set; }
        public string Contraseña { get; set; }
        public int RolId { get; set; }
        [ForeignKey("RolId")]
        public virtual Rol? Rol { get; set; }
        public DateTime FechaAccion { get; set; }
        public string TipoAccion { get; set; }
        public int EmpleadoAccionId { get; set; }
        public virtual Empleado? EmpleadoAccion { get; set; }
        public string DetallesAccion { get; set; }
    }
}
