using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Servicio2.Models.DbModels;
using Servicio2.Utility;

namespace Servicio2.EndPoints
{
    public static class Auth
    {
        public static void AddAuthEndPoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("api/v1/auth").WithTags("Auth");
            group.MapPost("login", async (Context context,[FromBody] AuthLogin authLogin) =>
            {
                var empleado = await context.Empleados
                .Where(x => x.NumeroEmpleado == authLogin.numeroEmpleado && x.correo == authLogin. correo)
                    .Include(x => x.Rol)
                    .FirstOrDefaultAsync();
                if (empleado == null)
                    return Results.NotFound("Empleado no encontrado");
                if (empleado.Rol == null)
                    return Results.NotFound("Ocurrio un error al buscar el empleado en la base de datos");
                var token = await TokenFactory.GenerateAccessToken(empleado.Id.ToString(), empleado.correo, empleado.Rol.Nombre);
                return Results.Ok(token);
            });
            group.MapPost("GenerarYEnviarTokenEmpleado", async (Context context, int empleadoId,
                    IConfiguration configuration, IHttpClientFactory _httpClientFactory) =>
            {
                var empleado = await context.Empleados
                .Where(x => x.Id == empleadoId)
                    .Include(x => x.Rol)
                    .FirstOrDefaultAsync(x => x.Id == empleadoId);
                if (empleado == null)
                    return Results.NotFound("Empleado no encontrado");
                if (empleado.Rol == null)
                    return Results.NotFound("Ocurrio un error al buscar el empleado en la base de datos");
                var token = await TokenFactory.GenerateAccessToken(empleadoId.ToString(), empleado.correo, empleado.Rol.Nombre);
                var payload = new
                {
                    token,
                    empleado.correo,
                    nombre = empleado.Nombres + " " + empleado.Apellidos,
                    numeroEmpleado = empleado.NumeroEmpleado,
                    cargo = empleado.Cargo
                };
                var httpclient = _httpClientFactory.CreateClient();
                var hebHookURL = configuration.GetSection("WebHookMakeIA:CorreoToken").Value;

                var response = await httpclient.PostAsJsonAsync(hebHookURL, payload);
                if (!response.IsSuccessStatusCode)
                {
                    return Results.BadRequest($"Error al llamar al webhook: {response.StatusCode}");
                }
                return Results.Ok(payload);
            });
        }
        public class AuthLogin
        {
            public string? numeroEmpleado { get; set; }
            public string? correo { get; set; }

        }
    }
}
