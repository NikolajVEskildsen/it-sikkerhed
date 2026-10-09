using api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();

var conn = app.Configuration["DB_CONNECTION"]
    ?? "Server=localhost,1433;Database=SqliDemo;User Id=sa;Password=Str0ng!Passw0rd;TrustServerCertificate=true";

var db = new Db(conn);
db.EnsureSeeded();

app.MapGet("/api/health", () => Results.Ok("ok"));

// mode = "vulnerable" | "secure"
app.MapPost("/api/{mode}/login", (string mode, Credentials c) =>
    mode switch
    {
        "vulnerable" => Results.Ok(db.VulnerableLogin(c.Username, c.Password)),
        "secure"     => Results.Ok(db.SecureLogin(c.Username, c.Password)),
        _            => Results.NotFound()
    });

app.MapGet("/api/{mode}/search", (string mode, string term) =>
    mode switch
    {
        "vulnerable" => Results.Ok(db.VulnerableSearch(term)),
        "secure"     => Results.Ok(db.SecureSearch(term)),
        _            => Results.NotFound()
    });

app.Run();

record Credentials(string Username, string Password);
