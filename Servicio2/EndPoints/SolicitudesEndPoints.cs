using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Servicio2.Models.DbModels;
using Servicio2.Models.enums;
using Servicio2.Events;
using Servicio2.Utility;
using static System.Runtime.InteropServices.JavaScript.JSType;
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
                int idEmpleado = await context.Empleados.Where(x => x.NumeroEmpleado == numeroEmpleado).Select(x => x.Id).FirstOrDefaultAsync();
                if (idEmpleado <= 0)
                    return Results.BadRequest("No se encontro el empleado");
                var solicitud = await context.solicitud.Where(x => x.Folio == folio && x.EmpleadoId == idEmpleado).Include(x => x.Empleado).FirstOrDefaultAsync();
                if (solicitud == null)
                    return Results.BadRequest("No se encontro esta solicitud ");

                return Results.Ok(solicitud);
            });

            app.MapPut("ActualizarEstatus", async (Context context, [FromBody] string folio, string numeroEmpleado,
                EstatusSolicitud estatus, IConfiguration configuration, IHttpClientFactory _httpClientFactory) =>
            {
                var result = await context.solicitud.Where(x => x.Folio == folio).ExecuteUpdateAsync(s => s.SetProperty(x => x.Estatus, estatus));
                if (result >= 1)
                {
                

                    var hebHookURL = configuration.GetSection("WebHookMakeIA:CalendarioURLWH").Value;
                    var empleado = await context.Empleados.Where(x => x.NumeroEmpleado == numeroEmpleado).Select(x => new { x.Nombres, x.Apellidos, x.correo, x.JefeId }).FirstOrDefaultAsync();
                    if (empleado == null)
                        return Results.BadRequest("No se encontro el empleado");
                    var solicitud = await context.solicitud.Where(x => x.Folio == folio).Include(x => x.Empleado).FirstOrDefaultAsync();
                    if (solicitud == null)
                        return Results.BadRequest("No se encontro la solicitud");
                    var payload = new SolicitudEstatus()
                    {
                        Folio = folio,
                        Tipo = estatus.ToString(),
                        NombresEmpleado = empleado.Nombres + " " + empleado.Apellidos,
                        NumeroEmpleado = numeroEmpleado,
                        EstatusSolicitud = estatus,
                        CorreoInteresado = empleado.correo,
                        SolicitudAceptada = new DatosSolicitudAceptada()
                        {
                            FechaFin = solicitud.FechaFin,
                            FechaInicio = solicitud.FechaInicio,
                        }
                    };
                    if (estatus == EstatusSolicitud.Aprobada)
                    {
                        var correosDepartamentos = await context.Empleados.Where(x => x.JefeId == empleado.JefeId).Select(x =>new { x.correo,x.Nombres,x.Apellidos}).ToListAsync();
                       //var empleadoInteresadoCorreo= correosDepartamentos.Where(x=>x.correo == empleado.correo).FirstOrDefault();
                       // if(empleadoInteresadoCorreo != null)
                       // {
                       //     correosDepartamentos.Remove(empleadoInteresadoCorreo);
                       // }
                
                        payload.CorreosInteresados = correosDepartamentos.Select(x => x.correo).ToArray();
                        payload.SolicitudAceptada.InteresadosDepartamento = correosDepartamentos
                            .Select(x => new Attendees
                            {
                                address = x.correo,
                                name = $"{x.Nombres} {x.Apellidos}"
                            }).ToList();
                    }
                    var httpclient = _httpClientFactory.CreateClient();
                    var response = await httpclient.PostAsJsonAsync(hebHookURL, payload);
                    if (!response.IsSuccessStatusCode)
                    {
                        // Manejar errores aquí
                        throw new Exception($"Error al llamar al webhook: {response.StatusCode}");
                    }
                    return Results.Ok(payload);
                }
                return Results.BadRequest("No se encontro la solicitud");
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
    public class SolicitudEstatus
    {
        public EstatusSolicitud EstatusSolicitud { get; set; } = EstatusSolicitud.Pendiente;
        public string NombresEmpleado { get; set; } = string.Empty;
        public string NumeroEmpleado { get; set; } = string.Empty;
        public string Folio { get; set; } = string.Empty;
        public string CorreoInteresado { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
       public Array CorreosInteresados { get; set; } = Array.Empty<string>();
        public DatosSolicitudAceptada? SolicitudAceptada { get; set; } = new DatosSolicitudAceptada();
    }
    public class DatosSolicitudAceptada
    {
        public List<Attendees>? InteresadosDepartamento { get; set; } = new List<Attendees>();
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
    }

    public class Attendees
    {
        public string? address { get; set; }
        public string? name { get; set; }
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
            this.FechaInicio = DateTime.Now.AddDays(20);
            this.FechaFin = FechaInicio.AddDays(20);
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
