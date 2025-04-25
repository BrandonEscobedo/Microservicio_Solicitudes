using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using Servicio2.EndPoints;
using Servicio2.Models.DbModels;
using MassTransit;
using Servicio2.Consumer;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMassTransit(config =>{
    config.SetKebabCaseEndpointNameFormatter();
    config.AddConsumer<EmpleadoCreadoConsumer>();
    config.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", h =>
        {
            h.Username("guest"); 
            h.Password("guest");
        });
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var dbConnection = builder.Configuration.GetConnectionString("mysql")!;
var serverVersion = new MySqlServerVersion(new Version(9, 3, 0));
builder.Services.AddDbContext<Context>(options=>options.UseMySql(dbConnection, serverVersion));
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.AddServicio2EndPoints();
app.Run();
