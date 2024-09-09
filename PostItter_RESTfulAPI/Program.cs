using Microsoft.EntityFrameworkCore;
using PostItter_RESTfulAPI;
using PostItter_RESTfulAPI.DatabaseContext;
using Microsoft.AspNetCore.SignalR;
using PostItter_RESTfulAPI.Entity;

var builder = WebApplication.CreateBuilder(args);

// Configura i servizi
builder.Services.AddControllers(options =>
{
    options.ReturnHttpNotAcceptable = true;
});
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 37)),
        mysqlOptions => mysqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5, // Numero massimo di tentativi
            maxRetryDelay: TimeSpan.FromSeconds(1), // Ritardo massimo tra i tentativi
            errorNumbersToAdd: null // Puoi specificare gli errori da considerare per i tentativi
        )
    ));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

// Configura CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policyBuilder =>
    {
        policyBuilder.WithOrigins("http://localhost:4200")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Listen(System.Net.IPAddress.Any, 5265);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PostItter RESTful API v1"));
}


app.UseRouting();

app.UseCors();

app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/chathub");

app.Run();