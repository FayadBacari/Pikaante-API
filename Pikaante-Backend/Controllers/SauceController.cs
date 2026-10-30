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
    
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var sauces = await repository.GetAllAsync();
        return Ok(sauces);
    }
    
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
        
        return StatusCode(StatusCodes.Status201Created, new { message = "Sauce enregistrée !" });
    }
    
    [HttpPut("{id:int}")]
    [Consumes("application/json")]
    public async Task<IActionResult> Update(int id, [FromBody] SauceDto dto)
    {
        var existing = await repository.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound(new { message = "Sauce introuvable." });
        }

        if (existing.UserId != CurrentUserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Seul le créateur de la sauce peut la modifier." });
        }

        var error = ValidateSauce(dto);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }
        
        CopyFields(dto, existing);
        await repository.UpdateAsync(existing);

        return Ok(new { message = "Sauce modifiée !" });
    }
    
    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateWithImage(int id, [FromForm] string? sauce, IFormFile? image)
    {
        var existing = await repository.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound(new { message = "Sauce introuvable." });
        }

        if (existing.UserId != CurrentUserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Seul le créateur de la sauce peut la modifier." });
        }

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
        
        var oldImageUrl = existing.ImageUrl;
        existing.ImageUrl = await SaveImageAsync(image);
        CopyFields(dto, existing);
        await repository.UpdateAsync(existing);
        
        DeleteImage(oldImageUrl);

        return Ok(new { message = "Sauce modifiée !" });
    }
    
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var sauce = await repository.GetByIdAsync(id);
        if (sauce is null)
        {
            return NotFound(new { message = "Sauce introuvable." });
        }
        
        if (sauce.UserId != CurrentUserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Seul le créateur de la sauce peut la supprimer." });
        }

        await repository.DeleteAsync(sauce);
        
        DeleteImage(sauce.ImageUrl);

        return Ok(new { message = "Sauce supprimée !" });
    }
    
    [HttpPost("{id:int}/like")]
    public async Task<IActionResult> Like(int id, [FromBody] LikeDto dto)
    {
        // Seules trois valeurs sont acceptées
        if (dto.Like is not (1 or 0 or -1))
        {
            return BadRequest(new { message = "La valeur de like doit être 1, 0 ou -1." });
        }

        var sauce = await repository.GetByIdAsync(id);
        if (sauce is null)
        {
            return NotFound(new { message = "Sauce introuvable." });
        }

        var userId = CurrentUserId;
        
        sauce.UsersLiked.RemoveAll(user => user == userId);
        sauce.UsersDisliked.RemoveAll(user => user == userId);
        
        if (dto.Like == 1)
        {
            sauce.UsersLiked.Add(userId);
        }
        else if (dto.Like == -1)
        {
            sauce.UsersDisliked.Add(userId);
        }
        
        sauce.Likes = sauce.UsersLiked.Count;
        sauce.Dislikes = sauce.UsersDisliked.Count;

        await repository.UpdateAsync(sauce);

        var message = dto.Like switch
        {
            1 => "Like ajouté !",
            -1 => "Dislike ajouté !",
            _ => "Vote annulé."
        };
        return Ok(new { message });
    }
    
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
    
    private string ImagesFolder => Path.Combine(environment.WebRootPath, "images");
    
    private static SauceDto? ReadSauce(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<SauceDto>(json, JsonSerializerOptions.Web);
        }
        catch (JsonException)
        {
            return null;
        }
    }
    
    private static string? ValidateSauce(SauceDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Manufacturer)
            || string.IsNullOrWhiteSpace(dto.Description) || string.IsNullOrWhiteSpace(dto.MainPepper))
        {
            return "Tous les champs sont obligatoires.";
        }
        
        if (dto.Name.Length > 100 || dto.Manufacturer.Length > 100
            || dto.MainPepper.Length > 100 || dto.Description.Length > 1000)
        {
            return "Un champ est trop long (100 caractères maximum, 1000 pour la description).";
        }
        
        if (dto.Heat is < 1 or > 10)
        {
            return "Le piquant (heat) doit être compris entre 1 et 10.";
        }

        return null;
    }
    
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
    
    private static void CopyFields(SauceDto dto, Sauce sauce)
    {
        sauce.Name = dto.Name ?? "";
        sauce.Manufacturer = dto.Manufacturer ?? "";
        sauce.Description = dto.Description ?? "";
        sauce.MainPepper = dto.MainPepper ?? "";
        sauce.Heat = dto.Heat;
    }
    
    private async Task<string> SaveImageAsync(IFormFile image)
    {
        Directory.CreateDirectory(ImagesFolder);   // ne fait rien si le dossier existe déjà

        // Un identifiant unique + l'extension du type d'image : jamais le nom envoyé par l'utilisateur
        var fileName = $"{Guid.NewGuid()}{ImageTypes[image.ContentType]}";
        
        await using var stream = System.IO.File.Create(Path.Combine(ImagesFolder, fileName));
        await image.CopyToAsync(stream);
        
        return $"{Request.Scheme}://{Request.Host}/images/{fileName}";
    }

    // Supprime le fichier d'une image à partir de son adresse
    private void DeleteImage(string imageUrl)
    {
        var fileName = Path.GetFileName(imageUrl);
        var path = Path.Combine(ImagesFolder, fileName);

        if (!string.IsNullOrEmpty(fileName) && System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }
    }
}
