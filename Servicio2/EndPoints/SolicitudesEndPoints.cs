using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Servicio2.Models.DbModels;
using Servicio2.Models.enums;
using Servicio2.Events;
using Servicio2.Utility;
using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Servicio2.Dtos;
using Microsoft.AspNetCore.Authorization;
using MassTransit.Initializers;
using System.Security.Claims;
namespace Servicio2.EndPoints
{
    public static class SolicitudesEndPoints
    {
        public static void AddSolicitudesEndPoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("api/v1/solicitudes").WithTags("Solicitudes");
            group.MapGet("GetSolicitudes",[Authorize(Roles ="RH")] async (Context context, IMapper mapper) =>
            {
                var solicitudes = await context.solicitud.Include(x => x.Empleado)
                .ThenInclude(x => x.Rol).Include(x => x.Empleado.Jefe).ToListAsync();
                if (solicitudes == null)
                    return Results.BadRequest("No se encontraron solicitudes");
                var solicitudesDTo = mapper.Map<List<SolicitudResponse>>(solicitudes);
                return Results.Ok(solicitudesDTo);
            });
            group.MapGet("GetSolicitudesDeJefePendientes", [Authorize(Roles = "Gerente")] async (Context context, IMapper mapper, ClaimsPrincipal claims) =>
            {
                int userId = int.Parse(claims.FindFirstValue(ClaimTypes.NameIdentifier)!);
                if (userId <= 0)
                    return Results.BadRequest("No se encontro el empleado");
                var solicitudes = await context.solicitud.Where(x => x.JefeAsignadoId == userId && x.Estatus == EstatusSolicitud.Pendiente)
                .Include(x => x.Empleado)
                .ThenInclude(x => x.Rol).Include(x => x.Empleado.Jefe).ToListAsync();

                if (solicitudes == null)
                    return Results.BadRequest("No se encontraron solicitudes");
                var solicitudesDTo = mapper.Map<List<SolicitudResponse>>(solicitudes);
                return Results.Ok(solicitudesDTo);
            });
            group.MapGet("GetSolicitudesTipo", [Authorize(Roles = "RH")] async (Context context, IMapper mapper, string estatus) =>
            {
                if (Enum.TryParse<EstatusSolicitud>(estatus, out var estatusEnum))
                {
                    var solicitudes = await context.solicitud.Where(x => x.Estatus == estatusEnum).ToListAsync();

                    if (solicitudes == null)
                        return Results.BadRequest("No se encontraron solicitudes");
                    var solicitudesDTo = mapper.Map<List<SolicitudResponse>>(solicitudes);
                    return Results.Ok(solicitudesDTo);
                }
                return Results.BadRequest("Este tipo de estatus de solicitud no existe, por favor ingrese uno valido.");

            });
            group.MapGet("ObtenerSolicitudesPorFolio", async (Context context, [FromQuery] string folio,IMapper mapper) =>
            {
                var solicitud = await context.solicitud.Where(x => x.Folio == folio).Include(x => x.Empleado).FirstOrDefaultAsync();
                if (solicitud == null)
                    return Results.BadRequest("No se encontro la solicitud");
                var solicitudDto = mapper.Map<SolicitudResponse>(solicitud);
                return Results.Ok(solicitudDto);
            });
            group.MapGet("ObtenerEstatusSolicitud", [Authorize] async ([FromQuery] string folio, Context context) =>
            {
                var response = await context.HistorialEstatus.Where(x => x.Folio == folio).Include(x => x.Solicitud).FirstOrDefaultAsync();
                if (response == null)
                    return Results.BadRequest("No se encontro la solicitud");
                return Results.Ok(response);
            });
            group.MapGet("ObtenerSolicitud", [Authorize] async (Context context, [FromQuery] string folio, string numeroEmpleado) =>
            {
                int idEmpleado = await context.Empleados.Where(x => x.NumeroEmpleado == numeroEmpleado).Select(x => x.Id).FirstOrDefaultAsync();
                if (idEmpleado <= 0)
                    return Results.BadRequest("No se encontro el empleado");
                var solicitud = await context.solicitud.Where(x => x.Folio == folio && x.EmpleadoId == idEmpleado).Include(x => x.Empleado).FirstOrDefaultAsync();
                if (solicitud == null)
                    return Results.BadRequest("No se encontro esta solicitud ");

                return Results.Ok(solicitud);
            });
            group.MapPut("ActualizarEstatus",  async (CambioEstatus cambioEstatus, Context context,
                IConfiguration configuration, IHttpClientFactory _httpClientFactory) =>
            {
                var solicitud = await context.solicitud.Where(x => x.Folio == cambioEstatus.folio).Include(x => x.Empleado).FirstOrDefaultAsync();
                if (solicitud == null)
                    return Results.BadRequest("No se encontro la solicitud");
                await context.HistorialEstatus.AddAsync(new HistorialEstatus
                {
                    Folio = cambioEstatus.folio,
                    EstatusNuevo = cambioEstatus.estatus,
                    EstatusAnterior = solicitud.Estatus,
                    FechaCambio = DateTime.Now,
                    Comentario = cambioEstatus.comentarios,
                    SolicitudId = solicitud.Id
                });

                await context.SaveChangesAsync();
                var result = await context.solicitud.Where(x => x.Folio == cambioEstatus.folio).ExecuteUpdateAsync(s => s.SetProperty(x => x.Estatus, cambioEstatus.estatus));

                if (result >= 1)
                {
                    var empleado = await context.Empleados.Where(x => x.NumeroEmpleado == cambioEstatus.numeroEmpleado).Select(x => new { x.Nombres, x.Apellidos, x.correo, x.JefeId }).FirstOrDefaultAsync();
                    if (empleado == null)
                        return Results.BadRequest("No se encontro el empleado");
                    if (solicitud == null)
                        return Results.BadRequest("No se encontro la solicitud");
                    var datosjefe = await context.Empleados.Where(x => x.Id == empleado.JefeId).Select(x => new { x.Nombres, x.Apellidos, x.correo, x.Departamento, x.Cargo }).FirstOrDefaultAsync();
                    if (datosjefe == null)
                        return Results.BadRequest("No se encontro el jefe");
                    var payload = new SolicitudEstatus()
                    {
                        Folio = cambioEstatus.folio,
                        Tipo = solicitud.Tipo.ToString(),
                        NombresEmpleado = empleado.Nombres + " " + empleado.Apellidos,
                        NumeroEmpleado = cambioEstatus.numeroEmpleado,
                        EstatusSolicitud = cambioEstatus.estatus.ToString(),
                        CorreoInteresado = empleado.correo,
                        DatosJefe = new DatosJefe
                        {
                            Cargo = datosjefe.Cargo ?? string.Empty,
                            Departamento = datosjefe.Departamento ?? string.Empty,
                            Nombres = datosjefe.Nombres + " " + datosjefe.Apellidos
                        },
                        DatosEstatusSolicitud = new DatosEstatusSolicitud()
                        {
                            FechaFin = solicitud.FechaFin,
                            FechaInicio = solicitud.FechaInicio,
                            MotivoEstatus = cambioEstatus.comentarios,
                        }
                    };
                    if (cambioEstatus.estatus == EstatusSolicitud.Liberada)
                    {
                        var correosDepartamentos = await context.Empleados.Where(x => x.JefeId == empleado.JefeId).Select(x => new { x.correo, x.Nombres, x.Apellidos }).ToListAsync();
                        var datosEmpleado = correosDepartamentos.Where(x => x.correo == empleado.correo).FirstOrDefault();
                        if (datosEmpleado != null)
                        {
                            correosDepartamentos.Remove(datosEmpleado);
                        }
                        payload.DatosEstatusSolicitud.InteresadosDepartamento = correosDepartamentos
                            .Select(x => new Attendees
                            {
                                address = x.correo,
                                name = $"{x.Nombres} {x.Apellidos}"
                            }).ToList();
                    }
                    else if (cambioEstatus.estatus == EstatusSolicitud.Aprobada)
                    {
                        var datosRH = await context.Empleados.Where(x => x.IdRol == 4).Select(x => new { x.correo, x.Nombres, x.Apellidos }).ToListAsync();
                        if (datosRH != null)
                        {
                            foreach (var item in datosRH)
                            {
                                payload.DatosEstatusSolicitud.InteresadosDepartamento.Add(new Attendees
                                {
                                    address = item.correo,
                                    name = $"{item.Nombres} {item.Apellidos}"
                                });
                            }
                        }
                    }

                    var httpclient = _httpClientFactory.CreateClient();
                    var hebHookURL = configuration.GetSection("WebHookMakeIA:CalendarioURLWH").Value;

                    var response = await httpclient.PostAsJsonAsync(hebHookURL, payload);
                    if (!response.IsSuccessStatusCode)
                    {
                        return Results.BadRequest($"Error al llamar al webhook: {response.StatusCode}");
                    }
                    return Results.Ok(payload);
                }
                return Results.BadRequest("No se encontro la solicitud");
            });
            group.MapPost("CrearSolicitudMCP", [Authorize] async (Context context, [FromBody] SolicitudModel solicitudDto, IPublishEndpoint publish, ClaimsPrincipal claims) =>
            {
                try
                {
                    int userId = int.Parse(claims.FindFirstValue(ClaimTypes.NameIdentifier)!);
                    var solicitudesEmpleado = await context.solicitud.AnyAsync(x => x.EmpleadoId == userId && x.Estatus == EstatusSolicitud.Pendiente);
                    if (solicitudesEmpleado)
                    {
                        return Results.BadRequest(new ResponseApi<bool>
                        {
                            Data = false,
                            Exito = false,
                            Mensaje = "Ya existe una solicitud pendiente para este empleado"
                        });
                    }
                    var solicitudResponseEntity = solicitudDto.ToEntity();

                    if (solicitudResponseEntity.Exito == false)
                    {

                        return Results.BadRequest(solicitudResponseEntity.Mensaje);
                    }
                    solicitudResponseEntity.Data.FechaSolicitud = DateTime.Now;
                    solicitudResponseEntity.Data.EmpleadoId = userId;
                    var JefeId = await context.Empleados
                        .Where(x => x.Id == userId)
                        .Select(x => x.JefeId)
                        .FirstOrDefaultAsync();

                    if (!JefeId.HasValue || JefeId.Value <= 0)
                    {
                        return Results.BadRequest(new ResponseApi<bool>
                        {
                            Data = false,
                            Exito = false,
                            Mensaje = "No se encontro el jefe"
                        });
                    }
                    Solicitud solicitud = solicitudResponseEntity.Data;
                    solicitud.JefeAsignadoId = (int)JefeId;
                    context.solicitud.Add(solicitud);
                    await context.SaveChangesAsync();
                    solicitud.Folio = FolioHelper.GenerarFolio(solicitud.EmpleadoId);
                    await context.SaveChangesAsync();
                    await context.HistorialEstatus.AddAsync(new HistorialEstatus
                    {
                        Folio = solicitud.Folio,
                        EstatusNuevo = solicitud.Estatus,
                        EstatusAnterior = solicitud.Estatus,
                        FechaCambio = DateTime.Now,
                        SolicitudId = solicitud.Id
                    });
                    await context.SaveChangesAsync();
                    await publish.Publish(new SolicitudCreadaEvent
                    {
                        SolicitudId = solicitud.Id,
                        EmpleadoId = solicitud.EmpleadoId,
                        FechaInicio = solicitud.FechaInicio,
                        FechaFin = solicitud.FechaFin,
                        Tipo = solicitud.Tipo.ToString(),
                        Folio = solicitud.Folio,

                        FechaSolicitud = solicitud.FechaSolicitud,
                    });
                    var response = new ResponseApi<bool>()
                    {
                        Data = true,
                        Exito = true,
                        Mensaje = "Solicitud creada correctamente con el folio: " + solicitud.Folio
                    };
                    return Results.Ok(response);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new ResponseApi<bool>
                    {
                        Mensaje = ex.Message,
                        Exito = false,
                        Data = false,
                    });
                }
            });
            group.MapPost("CrearSolicitud", async (Context context, [FromBody] SolicitudDTO solicitudDto, IPublishEndpoint publish) =>
            {
                try
                {
                    var solicitudesEmpleado = await context.solicitud.AnyAsync(x => x.EmpleadoId == solicitudDto.EmpleadoId && x.Estatus == EstatusSolicitud.Pendiente);
                    if (solicitudesEmpleado)
                    {
                        return Results.BadRequest("Ya existe una solicitud pendiente para este empleado");
                    }
                    var solicitudResponseEntity = solicitudDto.ToEntity();
                    if (solicitudResponseEntity.Exito == false)
                    {
                        return Results.BadRequest(solicitudResponseEntity.Mensaje);
                    }
                    solicitudDto.EmpleadoId = 10;
                    var JefeId = await context.Empleados
                        .Where(x => x.Id == solicitudDto.EmpleadoId)
                        .Select(x => x.JefeId)
                        .FirstOrDefaultAsync();

                    if (!JefeId.HasValue || JefeId.Value <= 0)
                    {
                        return Results.BadRequest("No se encontro el jefe");
                    }
                    Solicitud solicitud = solicitudResponseEntity.Data;
                    solicitud.JefeAsignadoId = (int)JefeId;
                    context.solicitud.Add(solicitud);
                    await context.SaveChangesAsync();
                    solicitud.Folio = FolioHelper.GenerarFolio(solicitud.EmpleadoId);
                    await context.SaveChangesAsync();
                    await context.HistorialEstatus.AddAsync(new HistorialEstatus
                    {
                        Folio = solicitud.Folio,
                        EstatusNuevo = solicitud.Estatus,
                        EstatusAnterior = solicitud.Estatus,
                        FechaCambio = DateTime.Now,
                        SolicitudId = solicitud.Id
                    });
                    await context.SaveChangesAsync();
                    await publish.Publish(new SolicitudCreadaEvent
                    {
                        SolicitudId = solicitud.Id,
                        EmpleadoId = solicitud.EmpleadoId,
                        FechaInicio = solicitud.FechaInicio,
                        FechaFin = solicitud.FechaFin,
                        Tipo = solicitud.Tipo.ToString(),
                        Folio = solicitud.Folio,

                        FechaSolicitud = solicitud.FechaSolicitud,
                    });
                    var response = new ResponseApi<bool>()
                    {
                        Data = true,
                        Exito = true,
                        Mensaje = "Solicitud creada con el folio: " + solicitud.Folio
                    };
                    return Results.Ok(response);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new ResponseApi<bool>
                    {
                        Mensaje = ex.Message,
                        Exito = false,
                        Data = false,
                    });
                }
            });
            group.MapGet("ObtenerAusenciasEmpleado", [Authorize(Roles = "RH,Gerente")] async (Context context, [FromQuery] string numeroEmpleado, IMapper mapper) =>
            {
                var idEmpleado = await context.Empleados.Where(x => x.NumeroEmpleado == numeroEmpleado).Select(x => x.Id).FirstOrDefaultAsync();
                if (idEmpleado <= 0)
                    return Results.BadRequest("No se encontro ningun empleado con este numero de empleado");
                var solicitudes = await context.solicitud.Where(x => x.EmpleadoId == idEmpleado && x.Estatus == EstatusSolicitud.Liberada)
                    .Select(x => new SolicitudDTO
                    {
                        EmpleadoId = x.EmpleadoId,
                        FechaInicio = x.FechaInicio,
                        FechaFin = x.FechaFin,
                        Tipo = x.Tipo.ToString(),
                        Comentarios = x.Comentarios
                    })
                    .ToListAsync();
                var solicitudesDto = mapper.Map<SolicitudDTO[]>(solicitudes);
                return Results.Ok(solicitudesDto);
            });
        }
    }
    #region modelos y dtos
    public class CambioEstatus
    {
        public string folio { get; set; }

        public string numeroEmpleado { get; set; }
        public string comentarios { get; set; }
        public EstatusSolicitud estatus { get; set; } = EstatusSolicitud.Pendiente;
        public CambioEstatus()
        {
            folio = string.Empty;
            numeroEmpleado = string.Empty;
            comentarios = string.Empty;
        }
    }
    public class SolicitudEstatus
    {
        public string EstatusSolicitud { get; set; } = string.Empty;
        public string NombresEmpleado { get; set; } = string.Empty;
        public string NumeroEmpleado { get; set; } = string.Empty;
        public string Folio { get; set; } = string.Empty;
        public string CorreoInteresado { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public Array CorreosInteresados { get; set; } = Array.Empty<string>();
        public DatosJefe DatosJefe { get; set; } = new DatosJefe();
        public DatosEstatusSolicitud? DatosEstatusSolicitud { get; set; } = new DatosEstatusSolicitud();
    }
    public class DatosJefe
    {
        public string Nombres { get; set; } = string.Empty;
        public string Cargo { get; set; } = string.Empty;
        public string Departamento { get; set; } = string.Empty;
    }
    public class DatosEstatusSolicitud
    {
        public List<Attendees> InteresadosDepartamento { get; set; } = new List<Attendees>();
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string MotivoEstatus { get; set; } = string.Empty;
    }

    public class Attendees
    {
        public string? address { get; set; }
        public string? name { get; set; }
    }
    public class SolicitudModel
    {
        [Required]
        public DateTime FechaInicio { get; set; }
        [Required]
        public DateTime FechaFin { get; set; }
        [Required]
        public string Tipo { get; set; } = string.Empty;
        public string? Comentarios { get; set; }
        public SolicitudModel toDto(Solicitud solicitud)
        {
            this.FechaInicio = solicitud.FechaInicio;
            this.FechaFin = solicitud.FechaFin;
            this.Tipo = solicitud.Tipo.ToString().ToLower();
            this.Comentarios = solicitud.Comentarios;
            return this;
        }
        public ResponseApi<Solicitud> ToEntity()
        {
            if (Enum.TryParse<TipoSolicitud>(this.Tipo.ToLower(), out var estatusEnum))
            {
                return new ResponseApi<Solicitud>()
                {
                    Data = new Solicitud()
                    {
                        FechaInicio = this.FechaInicio,
                        FechaFin = this.FechaFin,
                        Estatus = EstatusSolicitud.Pendiente,
                        Tipo = estatusEnum,
                        Comentarios = this.Comentarios,
                    },
                    Exito = true,
                    Mensaje = null
                };
            }
            else
            {
                return new ResponseApi<Solicitud>()
                {
                    Data = null!,
                    Exito = false,
                    Mensaje = "Este tipo de solicitud no existe."
                };
            }
        }
    }
    public class SolicitudDTO
    {
        public int EmpleadoId { get; set; }
        [Required]
        public DateTime FechaSolicitud { get; set; }
        [Required]
        public DateTime FechaInicio { get; set; }
        [Required]
        public DateTime FechaFin { get; set; }
        [Required]
        public string Tipo { get; set; } = string.Empty;
        public string? Comentarios { get; set; }
        public SolicitudDTO()
        {

        }
        public SolicitudDTO toDto(Solicitud solicitud)
        {
            this.EmpleadoId = solicitud.EmpleadoId;
            this.FechaSolicitud = solicitud.FechaSolicitud;
            this.FechaInicio = solicitud.FechaInicio;
            this.FechaFin = solicitud.FechaFin;
            this.Tipo = solicitud.Tipo.ToString().ToLower();
            this.Comentarios = solicitud.Comentarios;
            return this;
        }
        public ResponseApi<Solicitud> ToEntity()
        {
            this.FechaSolicitud = DateTime.Now;
            if (Enum.TryParse<TipoSolicitud>(this.Tipo.ToLower(), out var estatusEnum))
            {
                return new ResponseApi<Solicitud>()
                {
                    Data = new Solicitud()
                    {
                        EmpleadoId = this.EmpleadoId,
                        FechaSolicitud = this.FechaSolicitud,
                        FechaInicio = this.FechaInicio,
                        FechaFin = this.FechaFin,
                        Estatus = EstatusSolicitud.Pendiente,
                        Tipo = estatusEnum,
                        Comentarios = this.Comentarios,
                    },
                    Exito = true,
                    Mensaje = null
                };
            }
            else
            {
                return new ResponseApi<Solicitud>()
                {
                    Data = null!,
                    Exito = false,
                    Mensaje = "Este tipo de solicitud no existe."
                };
            }
        }

    }
    public class ResponseApi<T>
    {
        public string? Mensaje { get; set; } = string.Empty;
        public T Data { get; set; } = default!;
        public bool Exito { get; set; } = false;
    }
    #endregion
}
