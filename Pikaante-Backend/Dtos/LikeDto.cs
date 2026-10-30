namespace Pikaante_Backend.Dtos;

// Ce que le front envoie pour voter : { "userId": "...", "like": 1 }
// Seul "like" est lu : le userId envoyé n'est pas fiable, le serveur prend celui du jeton.
// int? : si "like" est absent, il vaut null (et non 0, qui voudrait dire « annuler son vote »).
public record LikeDto(int? Like);
