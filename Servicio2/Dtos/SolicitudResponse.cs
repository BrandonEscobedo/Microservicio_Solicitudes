namespace Servicio2.Dtos
{
    public class SolicitudResponse
    {

        public int Id { get; set; }

        public int EmpleadoId { get; set; }

        public string Folio { get; set; } = string.Empty;
        public EmpleadoResponse Empleado { get; set; } = new EmpleadoResponse();
        public EmpleadoResponse Jefe { get; set; }=new EmpleadoResponse();

        public DateTime FechaInicio { get; set; }

        public DateTime FechaFin { get; set; }

        public string Estatus { get; set; } = string.Empty;

        public string Tipo { get; set; } = string.Empty;

        public string? Comentarios { get; set; }
    }
}
