using Microsoft.EntityFrameworkCore;
using TicTacToe.Api.Infrastructure;
using TicTacToe.Api.Services;
using TicTacToe.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GameExceptionHandler>();

builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("GameDb")));

builder.Services.AddScoped<GameService>();

const string AngularDevClient = "angular-dev-client";
builder.Services.AddCors(options => options.AddPolicy(AngularDevClient, policy => policy
    .WithOrigins("http://localhost:4200")
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

// Applies any pending EF Core migration on startup, which creates the TicTacToe database, its
// tables and the seeded scoreboard row on a machine that has never run the app before.
// Set ConnectionStrings__GameDb and run backend/db/schema.sql instead if the database is managed
// by a DBA rather than by the app.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<GameDbContext>().Database.Migrate();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(AngularDevClient);
app.MapControllers();

app.Run();

/// <summary>Exposed so the integration tests can host the API in memory.</summary>
public partial class Program;
