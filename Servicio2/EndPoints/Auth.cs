using Microsoft.EntityFrameworkCore;
using Servicio2.Models.DbModels;
using Servicio2.Utility;

namespace Servicio2.EndPoints
{
    public static class Auth
    {
        public static void AddAuthEndPoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("api/v1/").WithTags("Auth");
            group.MapPost("GenerarToken", async (Context context, int empleadoId) =>
            {    
                var empleado = await context.Empleados
                .Where(x=>x.Id == empleadoId)
                    .Include(x => x.Rol)
                    .FirstOrDefaultAsync(x => x.Id == empleadoId);
                if (empleado == null)
                    return Results.NotFound("Empleado no encontrado");
                if (empleado.Rol == null)
                    return Results.NotFound("Ocurrio un error al buscar el empleado en la base de datos");
                var token = await TokenFactory.GenerateAccessToken(empleadoId.ToString(),empleado.correo, empleado.Rol.Nombre);
                return Results.Ok(token);
            });
        }
    }
}
