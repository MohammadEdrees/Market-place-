using System.Text;
using MarketWorkplace.Api.Auth;
using MarketWorkplace.Api.Data;
using MarketWorkplace.Api.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Controllers + JSON casing that matches the Angular models (camelCase).
builder.Services.AddControllers();

// Sample data store; swap for EF Core / a real database when you are ready.
builder.Services.AddSingleton<InMemoryStore>();

// --- JWT authentication ----------------------------------------------------
// Tokens are minted by TokenService (POST /api/auth/login) and validated here.
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSection["Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is missing from configuration.");
var jwtIssuer = jwtSection["Issuer"] ?? "MarketWorkplace.Api";
var jwtAudience = jwtSection["Audience"] ?? "MarketWorkplace.Client";

builder.Services.AddSingleton<TokenService>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // TokenService writes short claim names (sub/name/role) — don't remap them on the way in.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "name",
            RoleClaimType = "role",
        };
    });
builder.Services.AddAuthorization();

// Allow the Angular dev server to call the API directly.
builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(
            "http://localhost:4200",
            "https://localhost:4200",
            "http://localhost:4201",
            "https://localhost:4201")
        .AllowAnyHeader()
        .AllowAnyMethod()));

// Swagger / OpenAPI documentation served at /swagger (see appsettings.json -> Swagger:Enabled).
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Market Workplace API",
        Version = "v1",
        Description = "Dashboard metrics and product catalogue endpoints consumed by the Angular dashboard. "
            + "Sign in via `POST /api/auth/login` and click **Authorize** to call the protected operations.",
        Contact = new OpenApiContact { Name = "Market Workplace" },
    });

    // Exposes the padlock ("Authorize") so the UI can send `Authorization: Bearer <token>`.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT returned by `POST /api/auth/login`. Swagger UI prefixes it with `Bearer `.",
    });

    // Global requirement: every operation expects a token unless marked [AllowAnonymous].
    // OpenAPI.NET 2.x keys requirements by scheme *reference*, not by the scheme instance.
    options.AddSecurityRequirement(doc =>
        new OpenApiSecurityRequirement
        {
            { new OpenApiSecuritySchemeReference("Bearer", doc), new List<string>() },
        });
    options.OperationFilter<AllowAnonymousOperationFilter>();

    // Pull <summary> comments out of the compiled XML docs so operations are self-describing.
    var xmlPath = Path.Combine(AppContext.BaseDirectory, "MarketWorkplace.Api.xml");
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Kill switch for environments that should not expose the docs: "Swagger": { "Enabled": false }
if (app.Configuration.GetValue("Swagger:Enabled", true))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Market Workplace API v1");
        options.DocumentTitle = "Market Workplace API";
        options.EnableTryItOutByDefault();
    });
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
