using Microsoft.EntityFrameworkCore;
using Npgsql;
using api.Data;
using api.Services;

var builder = WebApplication.CreateBuilder(args);

var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "timetable";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "timetable";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";
var port = Environment.GetEnvironmentVariable("PORT") ?? Environment.GetEnvironmentVariable("API_PORT");
var apiPort = int.TryParse(port, out var configuredPort) && configuredPort is > 0 and <= 65535
    ? configuredPort
    : 5000;

var connectionString = new NpgsqlConnectionStringBuilder
{
    Host = dbHost,
    Port = int.Parse(dbPort),
    Database = dbName,
    Username = dbUser,
    Password = dbPassword,
    SslMode = string.Equals(
        Environment.GetEnvironmentVariable("DB_SSL_MODE"),
        "require",
        StringComparison.OrdinalIgnoreCase)
        ? SslMode.Require
        : SslMode.Prefer
}.ConnectionString;

builder.Services.AddDbContext<TimetableDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<LectureService>();

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(apiPort);
});

var app = builder.Build();

app.UseCors("AllowAll");
app.MapControllers();

app.Run();
