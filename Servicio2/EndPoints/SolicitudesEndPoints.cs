using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Servicio2.Models.DbModels;
using Servicio2.Models.enums;
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
            app.MapGet("ObtenerSolicitud", async (Context context, [FromQuery] string folio, string numeroEmpleado) =>
            {
                int idEmpleado = await context.Empleados.Where(x=>x.NumeroEmpleado==numeroEmpleado).Select(x=>x.Id).FirstOrDefaultAsync();
                if (idEmpleado <=0)
                    return Results.BadRequest("No se encontro el empleado");
                var solicitud = await context.solicitud.Where(x => x.Folio == folio && x.EmpleadoId == idEmpleado).Include(x=>x.Empleado).FirstOrDefaultAsync();
                if (solicitud == null)
                    return Results.BadRequest("No se encontro esta solicitud ");
             
                return Results.Ok(solicitud);
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
        public SolicitudDTO toDto(Solicitud solicitud)
        {
            this.Id = solicitud.Id;
            this.EmpleadoId = solicitud.EmpleadoId;
            this.FechaSolicitud = solicitud.FechaSolicitud;
            this.FechaInicio = solicitud.FechaInicio;
            this.FechaFin = solicitud.FechaFin;
            this.Estatus = solicitud.Estatus;
            this.Tipo = solicitud.Tipo;
            this.Comentarios = solicitud.Comentarios;
            return this;
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
