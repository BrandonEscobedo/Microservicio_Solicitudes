using Microsoft.EntityFrameworkCore;
using Servicio2.EndPoints;
using Servicio2.Models.DbModels;
using MassTransit;
using Servicio2.Consumer;
using Servicio2.Utility;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});
var key = Encoding.ASCII.GetBytes("CLAVE_SE@#GURA_AUTH_BR&$AND$$ON_%");
builder.Services.AddAuthorization();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; 
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});


builder.Services.AddAutoMapper(typeof(MapperProfile));
builder.Services.AddMassTransit(config =>
{
    config.SetKebabCaseEndpointNameFormatter();
    config.AddConsumer<EmpleadoCreadoConsumer>();
    config.AddConsumer<SolicitudCreadaConsumer>();
    config.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host((builder.Configuration.GetSection("rabbitMQ:URL").Value!), h =>
        {
            h.Username(builder.Configuration.GetSection("rabbitMQ:UserName").Value!);
            h.Password(builder.Configuration.GetSection("rabbitMQ:password").Value!);

        });
        cfg.ConfigureEndpoints(context);
    });
});
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var dbConnection = builder.Configuration.GetConnectionString("mysql")!;
var serverVersion = new MySqlServerVersion(new Version(9, 3, 0));
builder.Services.AddDbContext<Context>(options => options.UseMySql(dbConnection, serverVersion));
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseAuthentication();
app.UseAuthorization();
app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.AddSolicitudesEndPoints();
app.AddEmpleadosEndPoints();
app.AddAuthEndPoints();
app.Run();
