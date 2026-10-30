using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pikaante_Backend.Models;
using Pikaante_Backend.Data;
using Pikaante_Backend.Repository;

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


// Ajout de AppDbdContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Quand un controller demande un ISauceRepository, ASP.NET lui donne un SauceRepository
builder.Services.AddScoped<ISauceRepository, SauceRepository>();


// Compte hériter de la class IdentityUser
builder.Services.AddIdentityApiEndpoints<Person>(options =>
    {
        // Mot de passe : 10 caractères minimum
        options.Password.RequiredLength = 10;

        // Compte : un seul compte par adresse e-mail
        options.User.RequireUniqueEmail = true;

        // Blocage : après 5 mauvais mots de passe, le compte est bloqué 5 minutes
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddEntityFrameworkStores<AppDbContext>();

// Active-les controllers (AuthController…)
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Données illisibles (JSON mal formé, mauvais type…) : 400 + { "message": "..." }, sans détail technique
        options.InvalidModelStateResponseFactory = _ =>
            new BadRequestObjectResult(new { message = "Les données envoyées sont invalides." });
    });

// Enregistre la vérification des droits, utilisée par app.UseAuthorization()
builder.Services.AddAuthorization();

var app = builder.Build();

// Middleware d'ajout d'image 
app.UseExceptionHandler(errorApp =>
{
    // Ce bloc ne s'exécute que lorsqu'une erreur n'a été rattrapée nulle part
    errorApp.Run(async context =>
    {
        // Statut 500 = « erreur du serveur »
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        // Réponse au format des spécifications { "message" "..." }, sans détail technique.
        // Le détail de l'erreur reste affiché dans la console de Rider, pour toi.
        await context.Response.WriteAsJsonAsync(new { message = "Une erreur inattendue est survenue. Réessayez plus tard." });
    });
});
app.UseStaticFiles();


app.UseCors(myAllowSpecificOrigins);
app.UseAuthentication();   // Lit le jeton 
app.UseAuthorization();    // Vérifie les droits


// Les routes Register, Login, Refresh
app.MapGroup("/api/auth").MapIdentityApi<Person>();
app.MapControllers();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();
