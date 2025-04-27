using Servicio2.Models.enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Servicio2.Models.DbModels
{
    [Table("solicitudes")]
    public class Solicitud
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("empleado_id")]
        public int EmpleadoId { get; set; }

        [Required]
        [Column("folio")]
        public string Folio { get; set; }=string.Empty;

        [ForeignKey("EmpleadoId")]
        public Empleados Empleado { get; set; } = null!;

        [Required]
        [Column("fecha_solicitud", TypeName = "date")]
        public DateTime FechaSolicitud { get; set; }

        [Required]
        [Column("fecha_inicio", TypeName = "date")]
        public DateTime FechaInicio { get; set; }

        [Required]
        [Column("fecha_fin", TypeName = "date")]
        public DateTime FechaFin { get; set; }

        [Column("estatus")]
        [Required]
        [EnumDataType(typeof(EstatusSolicitud))]
        public EstatusSolicitud Estatus { get; set; } = EstatusSolicitud.Pendiente;

        [Column("tipo")]
        [Required]
        [EnumDataType(typeof(TipoSolicitud))]
        public TipoSolicitud Tipo { get; set; }

        [Column("comentarios")]
        public string? Comentarios { get; set; }
    }
}
