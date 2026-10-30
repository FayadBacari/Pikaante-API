namespace Pikaante_Backend.Dtos;

// Les seuls champs qu'un utilisateur a le droit de remplir.
public record SauceDto(string? Name, string? Manufacturer, string? Description, string? MainPepper, int Heat);
