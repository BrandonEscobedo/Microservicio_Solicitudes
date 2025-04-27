using Microsoft.AspNetCore.Mvc;
using Servicio2.Models.DbModels;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Servicio2.EndPoints
{
    public static class Servicio2EndPoints
    {
        public static void AddEmpleadosEndPoints(this IEndpointRouteBuilder app)
        {
          var group= app.MapGroup("api/v1/").WithTags("Empleados");
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
