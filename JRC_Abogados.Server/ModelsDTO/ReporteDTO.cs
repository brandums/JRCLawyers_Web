namespace JRC_Abogados.Server.ModelsDTO
{
    public class ReporteDTO
    {
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string TablasSeleccionadas { get; set; }
        public string? Path { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public DateTime? FechaGeneracion { get; set; }
        public int? ClienteId { get; set; }
        public int? EmpleadoId { get; set; }
        public int EmpleadoAccionId { get; set; }
    }
}
