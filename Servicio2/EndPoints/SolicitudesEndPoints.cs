using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Servicio2.Models.DbModels;
using Servicio2.Models.enums;
using MassTransit.SqlTransport.Topology;
using Servicio2.Events;
using Servicio2.Utility;

namespace Servicio2.EndPoints
{
    public static class SolicitudesEndPoints
    {
        public static void AddSolicitudesEndPoints(this IEndpointRouteBuilder app)
        {
            app.MapGroup("api/v1/").WithTags("Solicitudes");
            app.MapGet("GetSolicitudes", async (Context context) =>
            {
                return Results.Ok(await context.solicitud.ToListAsync());
            });
            app.MapPost("CrearSolicitud", async (Context context, [FromBody] SolicitudDTO solicitudDto, IPublishEndpoint publish) =>
            {
                var solicitud = solicitudDto.ToEntity();
                context.solicitud.Add(solicitud);
                await context.SaveChangesAsync();
                solicitud.Folio = FolioHelper.GenerarFolio(solicitud.EmpleadoId);
                await context.SaveChangesAsync();
                await publish.Publish(new SolicitudCreadaEvent
                {
                    SolicitudId = solicitud.Id,
                    EmpleadoId = solicitud.EmpleadoId,
                    FechaInicio = solicitud.FechaInicio,
                    FechaFin = solicitud.FechaFin,
                    Tipo = solicitud.Tipo.ToString(),
                    Folio = solicitud.Folio,
                });
                return Results.Ok("Solicitud Creada con Folio: " + solicitud.Folio);
            });
        }

    }
    public class SolicitudDTO
    {

        public int Id { get; set; }
        public int EmpleadoId { get; set; }

        public DateTime FechaSolicitud { get; set; }
        public DateTime FechaInicio { get; set; }

        public DateTime FechaFin { get; set; }
        public EstatusSolicitud Estatus { get; set; } = EstatusSolicitud.Pendiente;

        public TipoSolicitud Tipo { get; set; }

        public string? Comentarios { get; set; }
        public SolicitudDTO()
        {
            
        }
        public Solicitud ToEntity()
        {
            this.FechaSolicitud = DateTime.Now;
            this.FechaInicio = DateTime.Now.AddDays(10);
            this.FechaFin = FechaInicio.AddDays(10);
            return new Solicitud()
            {
                Id = this.Id,
                EmpleadoId = this.EmpleadoId,
                FechaSolicitud = this.FechaSolicitud,
                FechaInicio = this.FechaInicio,
                FechaFin = this.FechaFin,
                Estatus = this.Estatus,
                Tipo = this.Tipo,
                Comentarios = this.Comentarios
            };
        }
    }
}
