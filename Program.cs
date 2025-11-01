using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;
using V1_2025_07;
using V1_2025_07.Services;

var builder = WebApplication.CreateBuilder(args);

// Add DB context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("monsterconnection")));

// Add Email sender service
builder.Services.AddScoped<EmailSender>();

builder.Services.AddHealthChecks()
    .AddCheck("api", () => HealthCheckResult.Healthy("API running")) // API health
    .AddSqlServer(builder.Configuration.GetConnectionString("monsterconnection"), name: "mssql"); // MSSQL health


// Add controllers
builder.Services.AddControllers();

// --- CORS: allow frontend.localhost:3000, your production frontend, and allow credentials ---
var corsPolicyName = "FrontendCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicyName, policy =>
    {
        policy.WithOrigins(
                "https://frontend.multiplayers.in",
                "http://localhost:3000",
                "https://localhost:3000",
                "https://localhost:7127",
                "http://localhost:5173",
                "https://localhost:5173"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// --- JWT Authentication ---
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new Exception("JWT secret not set");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

// --- Swagger/OpenAPI ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Multiplayer Tournament API",
        Version = "v1",
        Description = "API documentation for the Multiplayer Tournament platform."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement{
        {
            new OpenApiSecurityScheme{
                Reference=new OpenApiReference{
                    Id="Bearer",
                    Type=ReferenceType.SecurityScheme
                }
            },new List<string>()
        }
    });
});

var app = builder.Build();

// --- CORS must be before authentication/authorization ---
app.UseCors(corsPolicyName);

app.UseAuthentication();
app.UseAuthorization();

// --- Swagger only in dev/prod ---
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Multiplayer Tournament API v1");
        options.RoutePrefix = "swagger";
    });
}

app.MapControllers();

app.MapHealthChecks("/", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new {
                Component = e.Key,
                Status = e.Value.Status.ToString()
            }),
            serverTime = DateTime.UtcNow
        });
        await context.Response.WriteAsync(result);
    }
});

app.Run();
