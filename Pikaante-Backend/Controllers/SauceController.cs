using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pikaante_Backend.Repository;

namespace Pikaante_Backend.Controllers;

[ApiController]
[Route("api/sauces")]   // le début de l'adresse de toutes les routes de cette classe
[Authorize]             // le videur, posé sur la classe : il protège toutes les routes des sauces
public class SauceController(ISauceRepository repository) : ControllerBase
{
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
}
