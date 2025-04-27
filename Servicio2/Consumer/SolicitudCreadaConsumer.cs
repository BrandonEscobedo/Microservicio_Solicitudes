using MassTransit;
using Microsoft.EntityFrameworkCore;
using Servicio2.Events;
using Servicio2.Models.DbModels;

namespace Servicio2.Consumer
{
    public class SolicitudCreadaConsumer(IHttpClientFactory _httpClientFactory, Context Dbcontext,IConfiguration configuration) : IConsumer<SolicitudCreadaEvent>
    {

        public async Task Consume(ConsumeContext<SolicitudCreadaEvent> context)
        {
            var solicitud = context.Message;
            var empleado = Dbcontext.Empleados.FirstOrDefault(e => e.Id == solicitud.EmpleadoId);
            if (empleado == null)
            {
                throw new Exception($"Empleado no encontrado: {solicitud.EmpleadoId}");
            }
            var httpClient = _httpClientFactory.CreateClient();
            var datosJefe = Dbcontext.Empleados.Where(x => x.Id == empleado.JefeId).Select(e => new { e.Nombres, e.Apellidos, e.correo }).FirstOrDefault();
            if (datosJefe == null)
            {
                throw new Exception($"Jefe no encontrado para el empleado con ID: {empleado.Id}");
            }

            var payload = new SolicitudEnviada
            {
                Nombres = empleado.Nombres + " " + empleado.Apellidos,
                CorreoEmpleado = empleado.correo,
                CorreoJefe = datosJefe.correo,
                Folio = solicitud.Folio,
                Tipo = solicitud.Tipo.ToString(),
                FechaFin = solicitud.FechaFin,
                FechaInicio = solicitud.FechaInicio,
                nombresJefe = datosJefe.Nombres + " " + datosJefe.Apellidos,
                NumeroEmpleado = empleado.NumeroEmpleado,
            };
            var webhookUrl = configuration.GetSection("WebHookMakeIA:URL").Value;

            var response = await httpClient.PostAsJsonAsync(webhookUrl, payload);
            if (!response.IsSuccessStatusCode)
            {
                // Manejar errores aquí
                throw new Exception($"Error al llamar al webhook: {response.StatusCode}");
            }
            Console.WriteLine("Solicitud recibida: " + solicitud.Tipo);
        }
    }
    public class SolicitudEnviada
    {
        public string CorreoJefe { get; set; } = string.Empty;
        public string CorreoEmpleado { get; set; } = string.Empty;
        public string NumeroEmpleado { get; set; } = string.Empty;

        public string Folio { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string nombresJefe { get; set; } = string.Empty;
    }

}
