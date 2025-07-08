using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using V1_2025_07;
using V1_2025_07.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("monsterconnection")));

builder.Services.AddScoped<EmailSender>();

builder.Services.AddControllers();

// CORS configuration
var corsPolicyName = "FrontendProd";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: corsPolicyName, policy =>
    {
        policy
            .WithOrigins("https://frontend.multiplayers.in")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Only if you use cookies/auth, otherwise remove
    });
});

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Multiplayer Tournament API",
        Version = "v1",
        Description = "API documentation for the Multiplayer Tournament platform."
    });
});

var app = builder.Build();

// Use CORS for all requests
app.UseCors(corsPolicyName);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Multiplayer Tournament API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
