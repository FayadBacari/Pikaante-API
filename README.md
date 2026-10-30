# Pikaante API (Hot Takes)

API sécurisée de l'application **Hot Takes** de Piiquante : des avis sur des sauces piquantes.
Écrite en C# avec ASP.NET Core 10 (LTS), ASP.NET Core Identity, Entity Framework Core et SQLite.

Le front-end Angular n'est pas dans ce dépôt.

## Prérequis

- [SDK .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0)
- L'outil Entity Framework Core :

  ```bash
  dotnet tool install --global dotnet-ef
  ```

- Node.js, seulement pour lancer le front-end

## Lancer l'API

```bash
# 1. Cloner le dépôt
git clone https://github.com/FayadBacari/Pikaante-API.git
cd Pikaante-API/Pikaante-Backend

# 2. Restaurer les dépendances
dotnet restore

# 3. Créer la base de données SQLite (Pikaante.db), tables d'Identity comprises
dotnet ef database update

# 4. Lancer l'API
dotnet run
```

L'API écoute sur **http://localhost:3000**, en HTTP, sans redirection vers HTTPS.

## Lancer le front-end

Dans un autre terminal, depuis le dossier `Pikaante-Frontend` :

```bash
npm install
npm start
```

L'application s'ouvre sur http://localhost:4200.

## Tester l'API

Le fichier [`Pikaante-Backend/Pikaante-Backend.http`](Pikaante-Backend/Pikaante-Backend.http) contient toutes les requêtes, dans l'ordre. Elles se lancent depuis Rider avec le bouton ▶ :

- inscription ;
- connexion ;
- renouvellement du jeton ;
- création, lecture, modification et suppression d'une sauce ;
- likes ;
- cas d'erreur.

## Routes

| Verbe | Route | Jeton | Rôle |
|---|---|---|---|
| POST | `/api/auth/register` | non | Créer un compte (Identity) |
| POST | `/api/auth/login` | non | Se connecter et obtenir les jetons (Identity) |
| POST | `/api/auth/refresh` | non | Renouveler les jetons (Identity) |
| GET | `/api/auth/me` | oui | Identifiant et e-mail de l'utilisateur connecté |
| GET | `/api/sauces` | oui | Lister toutes les sauces |
| GET | `/api/sauces/{id}` | oui | Lire une sauce |
| POST | `/api/sauces` | oui | Créer une sauce, avec son image (`multipart/form-data`) |
| PUT | `/api/sauces/{id}` | oui | Modifier sa sauce, en JSON ou avec une nouvelle image |
| DELETE | `/api/sauces/{id}` | oui | Supprimer sa sauce et son image |
| POST | `/api/sauces/{id}/like` | oui | Liker (`1`), annuler (`0`) ou disliker (`-1`) |

En cas d'erreur, les routes des sauces répondent `{ "message": "..." }`, sans aucun détail technique.

## Sécurité

- Les mots de passe sont hachés par Identity.
- Règles des comptes :
  - au moins 10 caractères, avec une majuscule, une minuscule, un chiffre et un caractère spécial ;
  - une seule inscription par adresse e-mail ;
  - le compte est bloqué 5 minutes après 5 échecs de connexion.
- Toutes les routes des sauces et `/api/auth/me` exigent un jeton.
- L'identité de l'utilisateur est toujours lue dans le jeton, jamais dans la requête.
- Seul le créateur d'une sauce peut la modifier ou la supprimer : sinon, l'API répond 403.
- Les images sont contrôlées : JPG, PNG, WEBP ou GIF, de 5 Mo maximum. Elles sont enregistrées sous un nom choisi par le serveur.
- Ne sont pas versionnés :
  - la base de données ;
  - les images envoyées (`wwwroot/images/`) ;
  - les clés de chiffrement des jetons, rangées par ASP.NET Core hors du projet.

## Organisation du code

```
Pikaante-Backend/
├── Controllers/   AuthController (/me) et SauceController (routes des sauces)
├── Data/          AppDbContext : la base de données (sauces + tables d'Identity)
├── Dtos/          Les données reçues du front (SauceDto, LikeDto)
├── Migrations/    L'historique de la base, rejoué par « dotnet ef database update »
├── Models/        Sauce et Person (le compte, hérité d'IdentityUser)
├── Repository/    ISauceRepository et SauceRepository : lire et écrire les sauces
├── Services/      Les messages d'erreur d'Identity en français
├── TestFiles/     L'image utilisée par le fichier .http
├── wwwroot/images Les images des sauces, servies sur /images/…
└── Program.cs     La configuration : services, puis middlewares dans l'ordre
```
