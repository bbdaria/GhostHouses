using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
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
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GhostHouses API",
        Version = "v1",
        Description = "REST API for GhostHouses authentication, building management, logs, users, exports, and GIS-backed building cards."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT bearer token returned by /api/Auth/verify-2fa."
    });

});

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ITwoFactorService, TwoFactorService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddHttpClient<IGisSnapshotService, ArcGisSnapshotService>();
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
var signingKey = jwtSection["SigningKey"];
if (string.IsNullOrWhiteSpace(signingKey) || signingKey.Length < 32)
{
    throw new InvalidOperationException("JWT signing key is missing or too short. Set Jwt__SigningKey in project/.env or in the deployment environment.");
}

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

var swaggerEnabled = app.Environment.IsDevelopment() ||
                     string.Equals(app.Configuration["Swagger:Enabled"], "true", StringComparison.OrdinalIgnoreCase);

if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "GhostHouses API v1");
    });
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await SeedData.InitializeAsync(app.Services);

app.Run();
