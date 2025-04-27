namespace Servicio2.Events
{
    public class SolicitudCreadaEvent
    {
        public int SolicitudId { get; set; }
        public int EmpleadoId { get; set; }
        public string Folio { get; set; }=string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public SolicitudCreadaEvent()
        {
            
        }
    }
}
