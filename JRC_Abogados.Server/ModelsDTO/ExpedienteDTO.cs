namespace JRC_Abogados.Server.ModelsDTO
{
    public class ExpedienteDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public int TipoExpedienteId { get; set; }
        public int ClienteId { get; set; }
        public int? CasoId { get; set; }
        public int EmpleadoId { get; set; }

    }
}
