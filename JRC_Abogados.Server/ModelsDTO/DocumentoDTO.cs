namespace JRC_Abogados.Server.ModelsDTO
{
    public class DocumentoDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public int ExpedienteId { get; set; }
        public int EmpleadoId { get; set; }
    }
}
