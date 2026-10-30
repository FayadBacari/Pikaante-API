namespace Pikaante_Backend.Dtos;

// Les seuls champs qu'un utilisateur a le droit de remplir.
// Le reste (id, userId, imageUrl, likes…) est décidé par le serveur, jamais par le front.
// « record » : une classe courte, faite pour transporter des données.
public record SauceDto(string? Name, string? Manufacturer, string? Description, string? MainPepper, int Heat);
