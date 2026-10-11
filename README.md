# Traduction de libellés via l'API OpenAI

Prototype pédagogique en C# (.NET 8, Windows Forms) qui montre **pas à pas comment un programme
dialogue avec une IA** : comment les données sont envoyées, comment la consigne est formulée et
comment le résultat est récupéré.

Cas concret : un tableau de libellés métier en français est traduit automatiquement en **anglais,
espagnol, chinois simplifié et japonais** via l'API OpenAI Chat Completions (`gpt-4o-mini`).

## Ce que fait l'application

- Affiche un jeu de démonstration de 5 libellés (`Client`, `Commande`, `Inventaire`,
  `Planification`, `Histogramme`) dans une grille.
- Au clic sur **Traduire**, envoie **une requête par cellule** (5 libellés × 4 langues = 20 appels).
- Journalise, pour chaque échange, le **JSON envoyé** et le **JSON reçu** — c'est le cœur de la
  démonstration.
- Isole les erreurs : une cellule en échec affiche `[ERREUR]`, les autres continuent.
- Affiche un récapitulatif (traduites / erreurs) puis permet d'**exporter en CSV** (UTF-8, `;`,
  caractères chinois et japonais préservés).

## Anatomie d'un échange

Requête envoyée à `POST https://api.openai.com/v1/chat/completions` :

```json
{
  "model": "gpt-4o-mini",
  "temperature": 0,
  "messages": [
    { "role": "system", "content": "Tu es un traducteur. Traduis le terme fourni en anglais. Réponds UNIQUEMENT par le terme traduit, sans guillemets, sans ponctuation finale, sans explication." },
    { "role": "user", "content": "Client" }
  ]
}
```

Le terme traduit est extrait de `choices[0].message.content` dans la réponse, puis nettoyé
(espaces, guillemets éventuels). Un exemple complet de requête et de réponse réelles est disponible
dans [docs/spec-technique/OpenAI -JSON appel-reponse.json](docs/spec-technique/OpenAI%20-JSON%20appel-reponse.json).

## Prérequis

- Windows (l'interface est en Windows Forms)
- [SDK .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0)
- Une clé API OpenAI ([platform.openai.com](https://platform.openai.com/api-keys)) — le coût d'une
  exécution complète (20 appels `gpt-4o-mini`) est négligeable

## Configurer la clé API

La clé n'est **jamais** stockée dans le code ni dans le dépôt. Elle est lue au lancement, dans cet
ordre :

1. **User-secrets .NET** (recommandé) — stockés dans votre profil Windows, hors du dépôt :

   ```bash
   cd src/TraductionIA.App
   dotnet user-secrets set "OpenAI:ApiKey" "sk-..."
   ```

2. **Variable d'environnement** `OPENAI_API_KEY` (repli) — rouvrir le terminal après :

   ```bash
   setx OPENAI_API_KEY "sk-..."
   ```

Si aucune clé n'est trouvée, l'application l'indique et n'effectue aucun appel.

> Les user-secrets sont stockés en clair dans `%APPDATA%\Microsoft\UserSecrets\` : c'est adapté au
> développement, pas à un déploiement.

## Lancer l'application

```bash
cd src
dotnet run --project TraductionIA.App
```

## Lancer les tests

Les tests n'appellent pas l'API : les réponses OpenAI sont simulées (succès, erreurs 401 et 429,
JSON invalide, réponse vide), aucune clé n'est nécessaire.

```bash
cd src
dotnet run --project TraductionIA.Tests
```

Le programme affiche le détail de chaque contrôle et se termine avec le code 0 si tout passe.

## Structure du projet

```
src/
├── TraductionIA.sln
├── TraductionIA.Core/     Logique testable : appel OpenAI, nettoyage, export CSV, lecture des secrets
├── TraductionIA.App/      Interface Windows Forms et résolution de la clé API
└── TraductionIA.Tests/    Harnais de tests console, sans dépendance externe
docs/
├── draft/                 Expression de besoin initiale
├── spec-fonctionnelle/    Règles de gestion, cas d'usage, critères d'acceptation
├── spec-technique/        Architecture, contrat HTTP OpenAI, flux, gestion d'erreurs
└── implementation/        Choix d'implémentation, vérifications, guide de publication GitHub
```

## Documentation

- [Spécification fonctionnelle](docs/spec-fonctionnelle/spec-fonctionnelle-traduction-libelles-via-ia.md)
- [Spécification technique](docs/spec-technique/spec-technique-traduction-libelles-via-ia.md)
- [Notes d'implémentation](docs/implementation/implementation-traduction-libelles-via-ia.md)
- [Publier un projet sur GitHub, de zéro jusqu'au push](docs/implementation/publication-github-depuis-zero.md)

## Limites

C'est un prototype pédagogique : la clarté prime sur l'exhaustivité.

- Jeu de données figé dans le code, pas d'import.
- Appels séquentiels, sans cache ni reprise sur incident.
- Un terme isolé, sans contexte, peut être ambigu (« Inventaire » par exemple), surtout en chinois
  et en japonais.

## Licence

[MIT](LICENSE)
