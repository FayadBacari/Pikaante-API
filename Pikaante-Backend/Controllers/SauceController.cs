using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pikaante_Backend.Dtos;
using Pikaante_Backend.Models;
using Pikaante_Backend.Repository;

namespace Pikaante_Backend.Controllers;

[ApiController]
[Route("api/sauces")]   // le début de l'adresse de toutes les routes de cette classe
[Authorize]             // le videur, posé sur la classe : il protège toutes les routes des sauces
public class SauceController(ISauceRepository repository, IWebHostEnvironment environment) : ControllerBase
{
    // Les types d'image acceptés, avec l'extension que le serveur donnera au fichier
    private static readonly Dictionary<string, string> ImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif"
    };

    private const long MaxImageSize = 5 * 1024 * 1024;   // 5 Mo

    // GET /api/sauces : toutes les sauces
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var sauces = await repository.GetAllAsync();
        return Ok(sauces);
    }

    // GET /api/sauces/5 : une seule sauce
    // {id:int} : l'id doit être un nombre, sinon ASP.NET répond 404 tout seul
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var sauce = await repository.GetByIdAsync(id);
        if (sauce is null)
        {
            return NotFound(new { message = "Sauce introuvable." });
        }

        return Ok(sauce);
    }

    // POST /api/sauces : créer une sauce avec son image
    // Le front envoie un formulaire (multipart/form-data) avec deux champs :
    //   sauce = la sauce, écrite en texte JSON
    //   image = le fichier
    // Les noms des paramètres doivent être exactement ceux des champs du formulaire.
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] string? sauce, IFormFile? image)
    {
        // 1. Vérifier ce qui arrive : l'image, puis la sauce
        if (image is null)
        {
            return BadRequest(new { message = "L'image est obligatoire." });
        }

        var dto = ReadSauce(sauce);
        if (dto is null)
        {
            return BadRequest(new { message = "Les informations de la sauce sont manquantes ou mal formées." });
        }

        var error = ValidateSauce(dto) ?? ValidateImage(image);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        // 2. Enregistrer l'image dans wwwroot/images
        var imageUrl = await SaveImageAsync(image);

        // 3. Remplir côté serveur ce que l'utilisateur ne doit pas choisir
        var newSauce = new Sauce
        {
            UserId = CurrentUserId,   // lu dans le jeton, jamais dans la requête
            ImageUrl = imageUrl,
            Likes = 0,
            Dislikes = 0,
            UsersLiked = [],
            UsersDisliked = []
        };
        CopyFields(dto, newSauce);

        await repository.AddAsync(newSauce);

        // 201 = « créé »
        return StatusCode(StatusCodes.Status201Created, new { message = "Sauce enregistrée !" });
    }

    // L'identifiant de l'utilisateur connecté, lu dans le jeton (comme dans /api/auth/me)
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    // Le dossier où sont rangées les images : wwwroot/images
    private string ImagesFolder => Path.Combine(environment.WebRootPath, "images");

    // Transforme le texte JSON du formulaire en objet. Renvoie null si le texte est absent ou illisible.
    private static SauceDto? ReadSauce(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            // Web : les noms sont en camelCase ("mainPepper"), comme dans le JSON du front
            return JsonSerializer.Deserialize<SauceDto>(json, JsonSerializerOptions.Web);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // Vérifie les champs de la sauce. Renvoie le message d'erreur, ou null si tout va bien.
    private static string? ValidateSauce(SauceDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Manufacturer)
            || string.IsNullOrWhiteSpace(dto.Description) || string.IsNullOrWhiteSpace(dto.MainPepper))
        {
            return "Tous les champs sont obligatoires.";
        }

        // Mêmes limites que les [MaxLength] du modèle Sauce
        if (dto.Name.Length > 100 || dto.Manufacturer.Length > 100
            || dto.MainPepper.Length > 100 || dto.Description.Length > 1000)
        {
            return "Un champ est trop long (100 caractères maximum, 1000 pour la description).";
        }

        // « plus petit que 1 ou plus grand que 10 »
        if (dto.Heat is < 1 or > 10)
        {
            return "Le piquant (heat) doit être compris entre 1 et 10.";
        }

        return null;
    }

    // Vérifie le type et le poids de l'image. Renvoie le message d'erreur, ou null si tout va bien.
    private static string? ValidateImage(IFormFile image)
    {
        if (!ImageTypes.ContainsKey(image.ContentType))
        {
            return "Format d'image refusé : utilisez une image JPG, PNG, WEBP ou GIF.";
        }

        if (image.Length == 0)
        {
            return "L'image est vide.";
        }

        if (image.Length > MaxImageSize)
        {
            return "L'image est trop lourde (5 Mo maximum).";
        }

        return null;
    }

    // Copie les champs modifiables par l'utilisateur sur la sauce (déjà vérifiés par ValidateSauce)
    private static void CopyFields(SauceDto dto, Sauce sauce)
    {
        sauce.Name = dto.Name ?? "";
        sauce.Manufacturer = dto.Manufacturer ?? "";
        sauce.Description = dto.Description ?? "";
        sauce.MainPepper = dto.MainPepper ?? "";
        sauce.Heat = dto.Heat;
    }

    // Enregistre l'image sous un nom choisi par le serveur, et renvoie son adresse complète
    private async Task<string> SaveImageAsync(IFormFile image)
    {
        Directory.CreateDirectory(ImagesFolder);   // ne fait rien si le dossier existe déjà

        // Un identifiant unique + l'extension du type d'image : jamais le nom envoyé par l'utilisateur
        var fileName = $"{Guid.NewGuid()}{ImageTypes[image.ContentType]}";

        // System.IO.File, car File(...) est déjà une méthode des controllers
        await using var stream = System.IO.File.Create(Path.Combine(ImagesFolder, fileName));
        await image.CopyToAsync(stream);

        // Par exemple : http://localhost:3000/images/0b8e1c2a-….jpg
        return $"{Request.Scheme}://{Request.Host}/images/{fileName}";
    }
}
