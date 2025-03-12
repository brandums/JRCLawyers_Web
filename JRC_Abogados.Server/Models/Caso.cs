using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JRC_Abogados.Server.Models
{
    public class Caso
    {
        [Key]
        public int Id { get; set; }
        public int TipoCasoId { get; set; }
        [ForeignKey("TipoCasoId")]
        public virtual TipoCaso? TipoCaso { get; set; }
        public int JuzgadoId { get; set; }
        [ForeignKey("JuzgadoId")]
        public virtual Juzgado? Juzgado { get; set; }
        public int UbicacionId { get; set; }
        [ForeignKey("UbicacionId")]
        public virtual Ubicacion? Ubicacion { get; set; }
        public string Descripcion { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaTermino { get; set; }
        public int EstadoId { get; set; }
        [ForeignKey("EstadoId")]
        public virtual Estado? Estado { get; set; }
        public int ClienteId { get; set; }
        [ForeignKey("ClienteId")]
        public virtual Cliente? Cliente { get; set; }
        public int EmpleadoId { get; set; }
        [ForeignKey("EmpleadoId")]
        public virtual Empleado? Empleado { get; set; }
    }
}
