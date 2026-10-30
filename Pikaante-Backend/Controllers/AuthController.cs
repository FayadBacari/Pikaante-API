using System.Security.Claims;            
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; 

namespace Pikaante_Backend.Controllers;


[ApiController]

//le début de l'adresse de toutes les routes de cette classe
[Route("api/auth")]
public class AuthController : ControllerBase
{
    [HttpGet("me")]
    [Authorize] 
    public IActionResult Me()
    {
        // NameIdentifier = l'identifiant du compte
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // l'adresse e-mail du compte
        var email = User.FindFirstValue(ClaimTypes.Email);
        return Ok(new { userId, email });
    }
}