using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WebServer.Data;
using WebServer.Models.Users;
using WebServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ITwoFactorService, TwoFactorService>();
builder.Services.AddScoped<IExternalDataService, ExternalDataService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddHostedService<ExternalSyncWorker>();
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var configuration = builder.Configuration;
    var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? configuration["Database:Host"] ?? "localhost";
    var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? configuration["Database:Port"] ?? "5432";
    var database = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? configuration["Database:Name"] ?? "ghosthouses";
    var user = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? configuration["Database:User"] ?? "postgres";
    var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? configuration["Database:Password"] ?? "postgres";

    var connectionString = $"Host={host};Port={port};Database={database};Username={user};Password={password};";
    options.UseNpgsql(connectionString);
});

var jwtSection = builder.Configuration.GetSection("Jwt");
var signingKey = jwtSection["SigningKey"] ?? "super-secret-key-change-me";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"] ?? "ghosthouses",
            ValidAudience = jwtSection["Audience"] ?? "ghosthouses-clients",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Viewer", policy => policy.RequireRole(
        UserRole.Viewer.ToString(),
        UserRole.Editor.ToString(),
        UserRole.Admin.ToString()));
    options.AddPolicy("Editor", policy => policy.RequireRole(
        UserRole.Editor.ToString(),
        UserRole.Admin.ToString()));
    options.AddPolicy("Admin", policy => policy.RequireRole(UserRole.Admin.ToString()));
});

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
var webRoot = app.Environment.WebRootPath;
if (!string.IsNullOrWhiteSpace(webRoot) && File.Exists(Path.Combine(webRoot, "index.html")))
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.MapControllers();
if (!string.IsNullOrWhiteSpace(webRoot) && File.Exists(Path.Combine(webRoot, "index.html")))
{
    app.MapFallbackToFile("index.html");
}

await SeedData.InitializeAsync(app.Services);

app.Run();
