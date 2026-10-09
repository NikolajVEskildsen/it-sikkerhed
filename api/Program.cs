using api.Data;
using api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddScoped<Database>();
builder.Services.AddScoped<IQueryService, QueryService>();

var app = builder.Build();

app.UseCors();
app.MapControllers();
app.MapGet("/api/health", () => "ok");

// Seed databasen ved opstart (venter på at MSSQL er klar).
var conn = app.Configuration["DB_CONNECTION"]
    ?? "Server=localhost,1433;Database=SqliDemo;User Id=sa;Password=Str0ng!Passw0rd;TrustServerCertificate=true";
DbSeeder.Seed(conn);

app.Run();
