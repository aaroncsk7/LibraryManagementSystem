using LibraryManagementSystem.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Library Management System API",
        Version = "v1",
        Description = "A simple CRUD API for managing books, members, and loans."
    });
});

// SQLite database — the file lives at the path in appsettings.json,
// so no external database server is required to run this project.
var connectionString = builder.Configuration.GetConnectionString("LibraryDb") ?? "Data Source=library.db";
builder.Services.AddDbContext<LibraryContext>(options =>
    options.UseSqlite(connectionString));

// Allow any origin during local/demo use so the API can be called from a
// separately hosted frontend if one is added later.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// Ensure the database exists and has seed data every time the app starts.
// This keeps the project runnable with a single `dotnet run` — no separate
// migration step is required for anyone cloning the repo.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryContext>();
    db.Database.EnsureCreated();
    SeedData.Initialize(db);
}

// Swagger is enabled in all environments (not just Development) since this
// is a small demo/teaching project meant to be cloned and run locally.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Library Management System API v1");
});

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Serves wwwroot/index.html at "/" and the rest of wwwroot (styles.css, app.js)
// as static files — this is the simple frontend for the API.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();
app.MapControllers();

app.Run();
