using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.NetworkInformation;
using Servicio2.Models.enums;

namespace Servicio2.Models.DbModels
{
    [Table("historial_estatus")]

    public class HistorialEstatus
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Solicitud")]
        [Column("solicitud_id")]
        public int SolicitudId { get; set; }

        [Column("estatus_anterior", TypeName = "varchar(10)")]
        public EstatusSolicitud? EstatusAnterior { get; set; }

        [Column("estatus_nuevo", TypeName = "varchar(10)")]
        public EstatusSolicitud? EstatusNuevo { get; set; }

        [Column("fecha_cambio", TypeName = "datetime")]
        public DateTime FechaCambio { get; set; } = DateTime.Now;
        [Column("folio")]
        public string Folio { get; set; } = string.Empty;
        [Column("comentario", TypeName = "varchar(255)")]
        public string? Comentario { get; set; }

        // Navegación a la tabla solicitudes
        public virtual Solicitud? Solicitud { get; set; }
    }
}
