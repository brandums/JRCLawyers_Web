using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JRC_Abogados.Server.Models.audits
{
    public class ClienteAudit
    {
        [Key]
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public string Telefono { get; set; }
        public string CorreoElectronico { get; set; }
        public int UbicacionId { get; set; }
        [ForeignKey("UbicacionId")]
        public virtual Ubicacion? Ubicacion { get; set; }
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
