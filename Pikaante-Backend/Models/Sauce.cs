using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Pikaante_Backend.Models;
public class Sauce
{
    [JsonPropertyName("_id")]   // Le front lit l'identifiant sous le nom "_id."
    public int Id { get; set; }
    
    [MaxLength(36)]
    public string UserId { get; set; } = "";  

    [MaxLength(100)]
    public string Name { get; set; } = "";

    [MaxLength(100)]
    public string Manufacturer { get; set; } = "";

    [MaxLength(1000)]
    public string Description { get; set; } = "";

    [MaxLength(100)]
    public string MainPepper { get; set; } = "";

    [MaxLength(500)]
    public string ImageUrl { get; set; } = "";

    public int Heat { get; set; }
    public int Likes { get; set; }
    public int Dislikes { get; set; }

    public List<string> UsersLiked { get; set; } = new();
    public List<string> UsersDisliked { get; set; } = new();
}
