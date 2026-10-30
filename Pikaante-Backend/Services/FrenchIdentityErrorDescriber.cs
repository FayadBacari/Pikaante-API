using Microsoft.AspNetCore.Identity;

namespace Pikaante_Backend.Services;

// Identity écrit ses messages d'erreur en anglais, et le front les affiche tels quels.
// Cette classe remplace les messages que l'utilisateur peut voir par leur version française.
public class FrenchIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => new()
        { Code = nameof(DefaultError), Description = "Une erreur inconnue est survenue." };

    public override IdentityError ConcurrencyFailure() => new()
        { Code = nameof(ConcurrencyFailure), Description = "Le compte a été modifié entre-temps : réessayez." };

    public override IdentityError InvalidEmail(string? email) => new()
        { Code = nameof(InvalidEmail), Description = $"L'adresse e-mail « {email} » n'est pas valide." };

    public override IdentityError InvalidUserName(string? userName) => new()
        { Code = nameof(InvalidUserName), Description = $"L'adresse e-mail « {userName} » n'est pas valide." };

    // Identity utilise l'e-mail comme nom d'utilisateur : les deux erreurs arrivent ensemble,
    // avec la même phrase (le front n'affiche qu'une fois une phrase en double).
    public override IdentityError DuplicateEmail(string email) => new()
        { Code = nameof(DuplicateEmail), Description = $"L'adresse e-mail « {email} » est déjà utilisée." };

    public override IdentityError DuplicateUserName(string userName) => new()
        { Code = nameof(DuplicateUserName), Description = $"L'adresse e-mail « {userName} » est déjà utilisée." };

    public override IdentityError PasswordTooShort(int length) => new()
        { Code = nameof(PasswordTooShort), Description = $"Le mot de passe doit contenir au moins {length} caractères." };

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new()
        { Code = nameof(PasswordRequiresUniqueChars), Description = $"Le mot de passe doit contenir au moins {uniqueChars} caractères différents." };

    public override IdentityError PasswordRequiresNonAlphanumeric() => new()
        { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Le mot de passe doit contenir au moins un caractère spécial (par exemple ! ou #)." };

    public override IdentityError PasswordRequiresDigit() => new()
        { Code = nameof(PasswordRequiresDigit), Description = "Le mot de passe doit contenir au moins un chiffre." };

    public override IdentityError PasswordRequiresLower() => new()
        { Code = nameof(PasswordRequiresLower), Description = "Le mot de passe doit contenir au moins une minuscule." };

    public override IdentityError PasswordRequiresUpper() => new()
        { Code = nameof(PasswordRequiresUpper), Description = "Le mot de passe doit contenir au moins une majuscule." };
}
