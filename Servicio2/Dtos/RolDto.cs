using Servicio2.Models.DbModels;
using System.ComponentModel.DataAnnotations;

namespace Servicio2.Dtos
{
    public class RolDto
    {
        public int Id { get; set; }

   
        public string Nombre { get; set; }

        public string? Descripcion { get; set; }

    }
}
