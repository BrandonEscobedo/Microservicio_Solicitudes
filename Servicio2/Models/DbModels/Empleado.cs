using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace Servicio2.Models.DbModels
{
    [Table("empleados")]

    public class Empleados
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("numero_empleado")]
        public string NumeroEmpleado { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("nombres")]
        public string Nombres { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("apellidos")]
        public string Apellidos { get; set; }

        [MaxLength(100)]
        [Column("cargo")]
        public string? Cargo { get; set; }

        [MaxLength(100)]
        [Column("departamento")]
        public string? Departamento { get; set; }

        [ForeignKey("Rol")]
        [Column("id_rol")]
        public int? IdRol { get; set; }

        public string correo { get; set; }
        public roles? Rol { get; set; }

        [ForeignKey("Jefe")]
        [Column("jefe_id")]
        public int? JefeId { get; set; }
        [JsonIgnore]
        public Empleados? Jefe { get; set; }
        [JsonIgnore]

        public ICollection<Empleados>? Subordinados { get; set; }
        public Empleados()
        {

        }
    }
}

