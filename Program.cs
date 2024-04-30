using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using peliculasproyecto.Controllers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var cadena = builder.Configuration.GetConnectionString("default");

builder.Services.AddControllers();
builder.Services.AddSingleton(new UI(cadena));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configura la inyección de dependencia para IHttpClientFactory
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// Configura CORS para aceptar de todos los orígenes
app.UseCors(builder =>
{
    builder.AllowAnyOrigin()
           .AllowAnyHeader()
           .AllowAnyMethod();   
});

app.MapControllers();

app.Run();
