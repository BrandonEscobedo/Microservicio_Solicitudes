using Microsoft.AspNetCore.Mvc;
using Servicio2.Models.DbModels;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Servicio2.Models.enums;

namespace Servicio2.EndPoints
{
    public static class EmpleadosEndPoints
    {
        public static void AddEmpleadosEndPoints(this IEndpointRouteBuilder app)
        {
          var group= app.MapGroup("api/v1/Empleados").WithTags("Empleados");
            group.MapGet("GetEmpleados", async (Context context) =>
            {
                return Results.Ok(await context.Empleados.ToListAsync());
            });
            group.MapPost("CrearEmpleado", async (Context context, [FromBody] empleadoDTO empleadoDTo,IPublishEndpoint publish) =>
            {
                var empleado = empleadoDTo.toEntity();
                await publish.Publish(empleado);
                var response = context.Add(empleado);
                await context.SaveChangesAsync();
            });
            group.MapGet("ObtenerEmpleadoPorNumeroEmpleado", async (Context context, string NumeroEmpleado) =>
            {
                var empleado = await context.Empleados.Where(x => x.NumeroEmpleado == NumeroEmpleado)
                .Select(x =>
                new
                {
                    x.Nombres,
                    x.Apellidos,
                    x.correo,
                    x.Departamento,
                    x.NumeroEmpleado,
                    x.Cargo,
                    x.Id,
                    x.JefeId,
                }).FirstOrDefaultAsync();
                if (empleado == null)
                    return Results.BadRequest("No se encontro el empleado con este numero de empleado");
                if(empleado.JefeId==null)
                    return Results.BadRequest("El empleado no tiene jefe asignado, " +
                        "si usted es  gerente o lider directo de area, las ausencias se manejan directamente con direccion, de lo contrario consulte al area de sistemas para mas informacion.");
                var solicitudes = await context.solicitud.Where(x => x.EmpleadoId == empleado.Id && x.Estatus == EstatusSolicitud.Pendiente).FirstOrDefaultAsync();
                if (solicitudes != null)
                    return Results.BadRequest("Ya existe una solicitud pendiente para este empleado con el folio:" + solicitudes.Folio);
                return Results.Ok(empleado);
            });
            group.MapGet("ObtenerDatosEmpleado", async (Context context, string NumeroEmpleado) =>
            {
                var empleado = await context.Empleados.Where(x => x.NumeroEmpleado == NumeroEmpleado)
                .Select(x =>
                new
                {
                    x.Nombres,
                    x.Apellidos,
                    x.correo,
                    x.Departamento,
                    x.NumeroEmpleado,
                    x.Cargo,
                    x.Id,
                    x.JefeId,
                }).FirstOrDefaultAsync();
                if (empleado == null)
                    return Results.BadRequest("No se encontro el empleado con este numero de empleado");
                return Results.Ok(empleado);
            });
        }
         

    }
    public class empleadoDTO
    {  
        public string NumeroEmpleado { get; set; }

        public string Nombres { get; set; }
  
        public string Apellidos { get; set; }
        public string? Cargo { get; set; }
        public string correo { get; set; } = string.Empty;
        public string? Departamento { get; set; }

        public int? IdRol { get; set; }

        public int? JefeId { get; set; }
        public Empleados toEntity()
        {
            return new Empleados()
            {
                Apellidos = this.Apellidos,
                Cargo = this.Cargo,
                Departamento = this.Departamento,
                IdRol = this.IdRol,
                JefeId = this.JefeId,
                Nombres = this.Nombres,
                NumeroEmpleado = this.NumeroEmpleado,
                correo = this.correo
            };
        }
   
    }
}
