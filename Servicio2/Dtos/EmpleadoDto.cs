using Servicio2.Models.DbModels;
namespace Servicio2.Dtos
{
    public class EmpleadoDto
    {
        public int Id { get; set; }
        public string NumeroEmpleado { get; set; }    
        public string Nombres { get; set; }
   
        public string Apellidos { get; set; }
    
        public string? Cargo { get; set; }

        public string? Departamento { get; set; }

        public int? IdRol { get; set; }
        public string correo { get; set; }
        public roles? Rol { get; set; }
        public int? JefeId { get; set; }
      
    }
}
