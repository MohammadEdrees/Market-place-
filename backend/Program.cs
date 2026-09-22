using MarketWorkplace.Api.Data;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Controllers + JSON casing that matches the Angular models (camelCase).
builder.Services.AddControllers();

// Sample data store; swap for EF Core / a real database when you are ready.
builder.Services.AddSingleton<InMemoryStore>();

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

// Swagger / OpenAPI documentation (UI at /swagger, document at /swagger/v1/swagger.json).
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Market Workplace API",
        Version = "v1",
        Description = "Dashboard metrics and product catalogue endpoints consumed by the Angular dashboard.",
        Contact = new OpenApiContact { Name = "Market Workplace" },
    });

    // Feed <summary>/<param> comments from the compiled XML docs into the operation descriptions.
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
app.MapControllers();

app.Run();
