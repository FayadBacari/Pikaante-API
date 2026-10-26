using Microsoft.EntityFrameworkCore;
using Pikaante_Backend.Models;
using Pikaante_Backend.Data;

var myAllowSpecificOrigins = "_myAllowSpecificOrigins";
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

// Ajout de l'appel CORS. 
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: myAllowSpecificOrigins,
        policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                .AllowAnyHeader()      // AVEC QUELS en-têtes (Content-Type, Authorization…)
                .AllowAnyMethod();     // AVEC QUELLES méthodes (GET, POST, PUT, DELETE…)
        });
});

// Base de données : AppDbContext utilise SQLite, avec la chaîne "DefaultConnection"
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));


// Compte hériter de la class IdentityUser
builder.Services.AddIdentityApiEndpoints<Person>()
    .AddEntityFrameworkStores<AppDbContext>();

// Enregistre la vérification des droits, utilisée par app.UseAuthorization()
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();   // Lit le jeton 
app.UseAuthorization();    // Vérifie les droits

// Les routes Register, Login, Refresh
app.MapGroup("/api/comptes").MapIdentityApi<Person>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(myAllowSpecificOrigins);

app.Run();
