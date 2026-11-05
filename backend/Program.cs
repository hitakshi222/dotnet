using Microsoft.EntityFrameworkCore;
using api.Data;
using api.Services;

var builder = WebApplication.CreateBuilder(args);

var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "timetable";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "timetable";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";
var apiPort = int.TryParse(Environment.GetEnvironmentVariable("API_PORT"), out var configuredPort) && configuredPort is > 0 and <= 65535
    ? configuredPort
    : 5000;

var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";

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
