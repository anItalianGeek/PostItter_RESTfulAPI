using Microsoft.EntityFrameworkCore;
using PostItter_RESTfulAPI.DatabaseContext;

var builder = WebApplication.CreateBuilder(args);

// Configura i servizi
builder.Services.AddControllers(options =>
{
    options.ReturnHttpNotAcceptable = true;
});
builder.Services.AddDbContext<ApplicationDbContext>(option =>
    option.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"), 
        new MySqlServerVersion(new Version(8, 0, 37))
    ));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// TODO add aws connections

// Configura CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
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

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.Redirect("/swagger/index.html");
        return;
    }

    await next();
});

// Usa routing
app.UseRouting();

app.UseHttpsRedirection();
app.UseCors();

// Mappa i controller
app.MapControllers();

app.Run();