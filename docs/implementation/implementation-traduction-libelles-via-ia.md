---
date: 2026-06-29
tags: [implementation]
statut: terminé
spec_technique: docs/spec-technique/spec-technique-traduction-libelles-via-ia.md
projet: Traduction par IA (C:\sessions_claude_code\Protos\Traduction par IA)
---

# Implémentation — Traduction de libellés via une IA (OpenAI)

## 1. Référence

- Spec technique : [spec-technique-traduction-libelles-via-ia.md](../spec-technique/spec-technique-traduction-libelles-via-ia.md)
- Spec fonctionnelle : [spec-fonctionnelle-traduction-libelles-via-ia.md](../spec-fonctionnelle/spec-fonctionnelle-traduction-libelles-via-ia.md)

Périmètre implémenté : prototype WinForms .NET 8 traduisant 5 libellés FR vers 4 langues
(anglais, espagnol, chinois simplifié, japonais) via l'API OpenAI Chat Completions, **un appel par
cellule** (20 appels), avec log des échanges, gestion d'erreur par cellule et export CSV. Toute la
logique traduisible est isolée dans un assembly testable et vérifiée par un harnais exécuté.

## 2. Fichiers touchés

Tous créés sous `src/` :

| Fichier | Rôle |
|---------|------|
| `TraductionIA.sln` | Solution regroupant les 3 projets. |
| `TraductionIA.Core/LangueCible.cs` | Enum des 4 langues cibles. |
| `TraductionIA.Core/LabelRow.cs` | Ligne du tableau (6 colonnes) + jeu de démo figé + `SetTranslation`. |
| `TraductionIA.Core/AppConfig.cs` | Porte clé API, endpoint, modèle ; constantes (`FromEnvironment` conservé). |
| `TraductionIA.Core/UserSecretsReader.cs` | Lit une clé dans un `secrets.json` (System.Text.Json), sans dépendance. |
| `TraductionIA.App/SecretResolver.cs` | Résout la clé : user-secrets .NET d'abord, puis `OPENAI_API_KEY`. |
| `TraductionIA.Core/OutputCleaner.cs` | Nettoyage du terme (trim + guillemets droits/typographiques/CJK). |
| `TraductionIA.Core/OpenAiTranslator.cs` | `TranslationResult` + DTO + `TranslateAsync` (HttpClient + System.Text.Json) + construction du prompt. |
| `TraductionIA.Core/CsvExporter.cs` | Export CSV `;` / UTF-8 BOM, échappement des champs. |
| `TraductionIA.App/Program.cs` | Point d'entrée WinForms. |
| `TraductionIA.App/MainForm.cs` | UI : grille 6 colonnes, boutons Traduire/Exporter, log, police CJK, orchestration des 20 appels. |
| `TraductionIA.Tests/FakeHandler.cs` | `HttpMessageHandler` simulé (réponses fabriquées, sans réseau). |
| `TraductionIA.Tests/Program.cs` | Harnais de vérification (19 contrôles), code retour 0/1. |
| `*.csproj` (×3) | Core `net8.0`, App `net8.0-windows` (WinForms), Tests `net8.0` (console). |

## 3. Choix d'implémentation

- **Découpage en 3 projets** (Core / App / Tests) : isole la logique d'échange de l'UI, ce qui la
  rend testable sans fenêtre WinForms (conforme à l'esprit §5/§14 de la spec technique).
- **`System.Text.Json`** intégré (aucune dépendance externe), avec `UnsafeRelaxedJsonEscaping` pour
  que les accents et le CJK restent lisibles dans le JSON tracé (utile pour la doc explicative).
- **`HttpClient` injecté** dans `OpenAiTranslator` → permet de brancher un handler simulé en test ;
  instance unique partagée côté App, `Timeout` 30 s.
- **Sécurité** : la clé n'est portée que par l'en-tête `Authorization` du `HttpRequestMessage` ;
  le champ `RawRequest` tracé ne contient que le corps JSON (vérifié par test).
- **Clé API via user-secrets .NET** : `SecretResolver` (App) lit la clé dans les user-secrets
  (`%APPDATA%\Microsoft\UserSecrets\traductionia-openai-proto\secrets.json`) en priorité, puis à
  défaut `OPENAI_API_KEY`. Lecture **sans paquet NuGet** (`UserSecretsReader` dans Core, via
  System.Text.Json) car `Microsoft.Extensions.Configuration.UserSecrets` est absent du cache. Le
  `<UserSecretsId>` du csproj et `SecretResolver.UserSecretsId` doivent rester synchronisés.
  Réserve de sécurité : les user-secrets ne sont pas chiffrés (dev) ; chiffrement au repos →
  Windows Credential Manager (DPAPI).
- **Police `Microsoft YaHei UI`** sur la grille pour le rendu CJK (font-linking Windows en complément).
- **`BuildSystemPrompt` / `NomLangue` publics et statiques** pour les tester unitairement.

## 4. Emplacement de la clé API (garantie anti-fuite Git)

**La clé API n'est jamais stockée dans le code ni dans le dépôt.** Elle est résolue à l'exécution
par `SecretResolver.Resolve()` ([SecretResolver.cs](../../src/TraductionIA.App/SecretResolver.cs)),
selon deux sources — toutes deux **hors du dépôt Git** :

| Priorité | Source | Emplacement réel | Défini par |
|----------|--------|------------------|------------|
| 1 | User-secrets .NET | `%APPDATA%\Microsoft\UserSecrets\traductionia-openai-proto\secrets.json` (profil Windows) | `dotnet user-secrets set "OpenAI:ApiKey" "sk-..."` |
| 2 (repli) | Variable d'environnement | `OPENAI_API_KEY` (session / système Windows) | `setx OPENAI_API_KEY "sk-..."` |

Si aucune des deux n'est trouvée, la traduction est refusée avec un message d'aide (RG8) — voir
`MainForm.cs` (`HasApiKey`).

**Pourquoi c'est sûr pour GitHub :**

- **Le `secrets.json` vit dans `%APPDATA%`**, en dehors de l'arborescence du projet : il ne peut donc
  pas être ajouté à un commit par mégarde.
- **La variable d'environnement** n'est écrite dans aucun fichier du projet.
- **Filet de sécurité `.gitignore`** : les motifs `.env`, `.env.*`, `secrets.json`, `appsettings.*.local.json`,
  `*.pfx`, `*.p12` sont ignorés. Même un `secrets.json` déposé par erreur à la racine ne serait pas versionné.
- **Aucune clé réelle dans les sources** : les seules chaînes `sk-...` présentes sont des valeurs de
  test factices (`sk-test-fake`, `sk-xyz-123` dans `TraductionIA.Tests`) et des exemples de documentation.
- **En transit** : la clé n'est portée que par l'en-tête `Authorization: Bearer` du `HttpRequestMessage` ;
  le corps JSON tracé (`RawRequest`) ne la contient jamais (vérifié par test — cf. §6).

**Synchronisation à maintenir** : `<UserSecretsId>` dans `TraductionIA.App.csproj` et
`SecretResolver.UserSecretsId` doivent rester identiques (`traductionia-openai-proto`), sinon la clé
écrite par la CLI ne serait pas relue.

> Réserve de sécurité : les user-secrets ne sont **pas chiffrés** (usage dev). Pour un chiffrement au
> repos, passer par le Windows Credential Manager (DPAPI).

## 5. Écarts vs spec technique

- **Plateforme .NET 8 au lieu de .NET Framework 4.8** : contrainte levée d'un commun accord (la 4.8
  n'est pas compilable dans l'environnement courant). Les deux specs ont été amendées en conséquence
  (plateforme, `System.Text.Json`, suppression du forçage TLS).
- **Harnais de test console au lieu de xUnit** : `xunit` absent du cache NuGet et accès réseau
  incertain ; un harnais console à zéro dépendance garantit une vérification hors-ligne reproductible.
  Mêmes cas de test que prévu (succès, 401, 429, JSON invalide, réponse vide).
- **`OutputCleaner` extrait** comme classe dédiée (la spec le mentionnait comme étape de §7) pour la
  testabilité.

## 6. Vérification

- **Build** : `dotnet build TraductionIA.sln` → **succès, 0 avertissement, 0 erreur** pour les 3
  projets, y compris l'app WinForms (`net8.0-windows`).
- **Tests** : `dotnet run --project TraductionIA.Tests` → **23 PASS / 0 FAIL**. Couvre :
  - succès FR→EN et FR→ZH (CJK préservé) ;
  - corps de requête contenant modèle + langue + terme ; en-tête `Bearer` présent ; clé absente du JSON tracé ;
  - erreurs 401, 429, JSON illisible, réponse vide → `Success=false` (RG6) ;
  - nettoyage des guillemets (droits + typographiques) ;
  - prompt ZH = « chinois simplifié », prompt JA = « japonais » (RG5) ;
  - CSV : en-tête 6 colonnes + CJK, échappement du `;`, BOM UTF-8 + relecture intacte ;
  - user-secrets : clé présente lue, clé absente/valeur vide/fichier absent → `null`.
- **Round-trip user-secrets CLI** : `dotnet user-secrets set "OpenAI:ApiKey" "..."` (valeur factice)
  → `secrets.json` écrit au chemin attendu au format `{ "OpenAI:ApiKey": "..." }`, lu par le code,
  puis effacé (`dotnet user-secrets clear`). Câblage `UserSecretsId` validé.

Commandes :
```
cd "src"
dotnet build TraductionIA.sln -c Debug
dotnet run --project TraductionIA.Tests -c Debug
```

## 7. Reste à faire / limites

- **Non vérifié ici** (impossible dans l'environnement) :
  - l'**aller-retour réseau réel** vers l'API OpenAI (pas de `OPENAI_API_KEY` ; l'échange est prouvé
    via handler simulé, pas via un appel live) ;
  - le **rendu visuel** de la fenêtre WinForms et l'affichage effectif des glyphes CJK dans la grille.
- **À faire par l'utilisateur pour un essai réel** : définir la clé via user-secrets depuis
  `src/TraductionIA.App` (`dotnet user-secrets set "OpenAI:ApiKey" "sk-..."`) — ou à défaut
  `setx OPENAI_API_KEY "sk-..."` puis rouvrir un terminal —, lancer `dotnet run --project TraductionIA.App`,
  cliquer « Traduire », vérifier la grille et l'export CSV (ouverture Excel FR).
- **Pistes hors périmètre** (proto) : parallélisme des appels, cache, reprise sur incident, import de
  données externes.
- Pont testeur : le harnais console pourra être converti en projet xUnit/MSTest une fois l'accès NuGet
  disponible, sans changer la logique testée.
