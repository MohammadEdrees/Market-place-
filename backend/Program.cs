using MarketWorkplace.Api.Data;

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

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("Frontend");
app.MapControllers();

app.Run();
