using System.Net.WebSockets;
using Microsoft.EntityFrameworkCore;
using PostItter_RESTfulAPI;
using PostItter_RESTfulAPI.DatabaseContext;

var builder = WebApplication.CreateBuilder(args);

// Configura i servizi
builder.Services.AddControllers(options =>
{
    options.ReturnHttpNotAcceptable = true;
});
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 37))
    ));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configura CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin();
        builder.AllowAnyMethod();
        builder.AllowAnyHeader();
    });
});

// Configura Kestrel
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Listen(System.Net.IPAddress.Any, 5265);
});

var app = builder.Build();

// Configura il pipeline di richieste HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1"));
}

app.UseCors();

app.UseWebSockets();

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/ws/chat", out var remainingPath))
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            string chatId = remainingPath.ToString().Trim('/');
            WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
            await WebSocketHandler.HandleWebSocket(context, webSocket, chatId);
        }
        else
        {
            context.Response.StatusCode = 400;
        }
    }
    else
    {
        await next();
    }
});



// Usa routing
app.UseRouting();

// app.UseHttpsRedirection();

// Mappa i controller
app.MapControllers();

app.Run();