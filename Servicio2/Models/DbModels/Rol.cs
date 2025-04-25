using System.ComponentModel.DataAnnotations;
namespace Servicio2.Models.DbModels
{
    public class roles
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Nombre { get; set; }

        public string? Descripcion { get; set; }

        public ICollection<Empleados>? Empleados { get; set; }
        public roles()
        {

        }
    }

}
