using MassTransit;
using Servicio2.Models.DbModels;

namespace Servicio2.Consumer
{
    public class EmpleadoCreadoConsumer : IConsumer<Empleados>
    {
        public Task Consume(ConsumeContext<Empleados> context)
        {
            var evento = context.Message;

            Console.WriteLine($"Empleado recibido: {evento.Nombres} {evento.Apellidos}");

            return Task.CompletedTask;
        }
    }
}
