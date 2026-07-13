---
date: 2026-06-29
tags: [spec, technique]
statut: brouillon
spec_fonctionnelle: docs/spec-fonctionnelle/spec-fonctionnelle-traduction-libelles-via-ia.md
source: docs/draft/draft-spec-fonctionnelle.md
---

# Traduction de libellés via une IA (OpenAI) — spécification technique

## 1. Contexte & référence

Spec fonctionnelle source : [spec-fonctionnelle-traduction-libelles-via-ia.md](../spec-fonctionnelle/spec-fonctionnelle-traduction-libelles-via-ia.md).

Objectif : prototype Windows Forms (.NET 8 / `net8.0-windows`) qui complète les colonnes « Libellé anglais »,
« Libellé espagnol », « Libellé chinois » (simplifié) et « Libellé japonais » d'un tableau de libellés
en appelant l'API OpenAI, **un appel par cellule** (5 lignes × 4 langues = 20 appels).
Le but transverse est pédagogique : rendre **visible et documentable** chaque échange HTTP avec l'IA.

Périmètre technique couvert : architecture du prototype, appel HTTP/JSON à l'API Chat Completions,
modèle d'exécution UI, traçage des échanges, gestion d'erreur, export CSV.

## 2. Vue d'ensemble de la solution

```
┌─────────────────────────────────────────────────────────┐
│  MainForm (UI WinForms)                                   │
│   - DataGridView (Code, FR, EN, ES, ZH, JA)               │
│   - Bouton « Traduire »  - Bouton « Exporter CSV »        │
│   - Zone de log (JSON envoyé / reçu)                      │
└───────────────┬──────────────────────────┬───────────────┘
                │ async/await               │
                ▼                           ▼
   ┌───────────────────────────┐   ┌──────────────────┐
   │ OpenAiTranslator (service)│   │  CsvExporter     │
   │  TranslateAsync(terme,    │   │  Export(rows)    │
   │     langueCible)          │   └──────────────────┘
   │  → HttpClient + JSON      │
   └───────────┬───────────────┘
               │ HTTPS POST /v1/chat/completions
               ▼
        ┌──────────────┐
        │  API OpenAI  │
        └──────────────┘
```

Flux : pour chacune des 5 lignes et chacune des 4 langues cibles, `MainForm` appelle `OpenAiTranslator.TranslateAsync`,
qui construit le corps JSON, fait le POST HTTPS, lit la réponse JSON, en extrait le terme traduit et le
renvoie. La cellule est mise à jour et l'échange (requête + réponse brutes) est journalisé.

## 3. Choix techniques & justifications

| Choix | Décision | Alternative écartée | Pourquoi |
|-------|----------|--------------------|----------|
| Plateforme | .NET 8, WinForms (`net8.0-windows`) | .NET Framework 4.8 | Compilable et exécutable directement (SDK présent) ; JSON et TLS intégrés ; WinForms supporté nativement. |
| Appel HTTP | `System.Net.Http.HttpClient` (instance unique réutilisée) | `WebRequest`/`WebClient` | API moderne, claire, support natif async. |
| JSON | `System.Text.Json` (intégré .NET 8) | `Newtonsoft.Json` (NuGet) | Intégré au runtime : aucune dépendance externe ni accès NuGet ; suffisant et lisible pour la pédagogie. |
| Modèle d'exécution | `async/await` **séquentiel** | Synchrone / parallèle | UI non gelée, et un échange à la fois = mécanisme lisible (RG2). |
| Structure | Service `OpenAiTranslator` isolé + `MainForm` | Tout dans le Form | Sépare le mécanisme d'échange de l'UI ; testable et documentable. |
| Traçage | Journalisation du JSON envoyé/reçu | Résultat seul | Rend l'échange visible et alimente la doc avec des exemples réels. |
| Modèle IA | `gpt-4o-mini` pour les 4 langues | modèle dédié au CJK | Peu coûteux et suffisant, y compris pour le chinois et le japonais (RG3) ; mécanisme uniforme. |
| TLS | Défaut du runtime .NET 8 (TLS 1.2/1.3 négocié) | forçage manuel | .NET 8 négocie un protocole moderne par défaut ; aucun réglage nécessaire. |
| Encodage | UTF-8 de bout en bout (HTTP, UI, CSV) | défaut ANSI/Latin-1 | Indispensable pour afficher et exporter sans perte les caractères chinois et japonais. |
| Police UI | Police couvrant le CJK pour le `DataGridView` (ex. « Microsoft YaHei » / « Yu Gothic », ou `Segoe UI` + fallback système) | police par défaut du Form | Évite les caractères « tofu » (carrés) sur les colonnes ZH/JA. |

## 4. Modèle de données

Pas de base de données. Données en mémoire.

**`LabelRow`** (ligne du tableau, liée au `DataGridView`) :

| Champ | Type | Obligatoire | Notes |
|-------|------|-------------|-------|
| `Code` | `string` | oui | Donnée d'entrée, non modifiée. |
| `LibelleFr` | `string` | oui | Donnée d'entrée (source de traduction). |
| `LibelleEn` | `string` | non | Calculé. Vide → traduction ou `[ERREUR]`. |
| `LibelleEs` | `string` | non | Calculé. Vide → traduction ou `[ERREUR]`. |
| `LibelleZh` | `string` | non | Calculé (chinois simplifié). Vide → traduction ou `[ERREUR]`. |
| `LibelleJa` | `string` | non | Calculé (japonais). Vide → traduction ou `[ERREUR]`. |

Jeu de démonstration figé (initialisé en dur) : `A01 Client`, `A02 Commande`, `A03 Inventaire`,
`A04 Planification`, `A05 Histogramme`.

**Enum `LangueCible`** : `Anglais`, `Espagnol`, `Chinois`, `Japonais` (porte le nom de langue envoyé
dans le prompt ; pour le chinois, la consigne précise « chinois simplifié »).

**DTO de (dé)sérialisation** (mappés sur le contrat OpenAI, voir §6) :
`ChatRequest`, `ChatMessage`, `ChatResponse`, `Choice`, `ResponseMessage`.

## 5. Composants & modules

| Composant | Responsabilité | Dépend de |
|-----------|----------------|-----------|
| `MainForm` | UI : grille, boutons, zone de log, orchestration séquentielle des 20 appels, mise à jour des cellules, récapitulatif. | `OpenAiTranslator`, `CsvExporter` |
| `OpenAiTranslator` | Construire la requête, appeler l'API, extraire le terme traduit, exposer le JSON brut envoyé/reçu via un événement/log. | `HttpClient`, `System.Text.Json` |
| `CsvExporter` | Sérialiser les `LabelRow` (6 colonnes) en CSV (séparateur `;`, UTF-8 BOM pour Excel FR et rendu correct du CJK). | — |
| `AppConfig` | Porter clé API, endpoint et nom de modèle. | — |
| `UserSecretsReader` (Core) | Lire une clé dans un `secrets.json` (System.Text.Json), sans dépendance. | `System.Text.Json` |
| `SecretResolver` (App) | Résoudre la clé : user-secrets .NET d'abord, puis `OPENAI_API_KEY`. | `UserSecretsReader`, `Environment` |

## 6. Interfaces & contrats

### 6.1 Service `OpenAiTranslator`

```csharp
public class TranslationResult
{
    public bool Success { get; set; }
    public string Text { get; set; }        // terme traduit, nettoyé
    public string Error { get; set; }        // message si échec
    public string RawRequest { get; set; }   // JSON envoyé (traçage)
    public string RawResponse { get; set; }  // JSON reçu (traçage)
}

public Task<TranslationResult> TranslateAsync(string termeFr, LangueCible cible);
```

`TranslateAsync` ne lève pas d'exception métier : tout échec est porté par `Success = false`
et `Error` (cf. RG6). `RawRequest`/`RawResponse` sont toujours renseignés quand l'échange a eu lieu.

### 6.2 Contrat HTTP OpenAI (Chat Completions)

- **Endpoint** : `POST https://api.openai.com/v1/chat/completions`
- **En-têtes** : `Authorization: Bearer <OPENAI_API_KEY>`, `Content-Type: application/json`
- **Corps envoyé** (exemple, FR→EN) :

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

- **Réponse reçue** (extrait pertinent) :

```json
{
  "choices": [
    { "message": { "role": "assistant", "content": "Customer" } }
  ]
}
```

- **Consignes *system* par langue** (le message *user* reste le libellé français brut) :
  - Anglais → « …Traduis le terme fourni en anglais. … »
  - Espagnol → « …Traduis le terme fourni en espagnol. … »
  - Chinois → « …Traduis le terme fourni en chinois simplifié. … »
  - Japonais → « …Traduis le terme fourni en japonais. … »

  Exemple FR→ZH : *system* « …Traduis le terme fourni en chinois simplifié. Réponds UNIQUEMENT par le
  terme traduit… », *user* « Client » → réponse attendue `客户`.

- **Extraction** : `response.choices[0].message.content`, puis nettoyage (`Trim`, retrait
  guillemets résiduels) avant affichage en cellule.

### 6.3 Interface utilisateur

- `DataGridView` : 6 colonnes (Code, FR en lecture seule ; EN, ES, ZH, JA en lecture seule, remplies par le programme). Police compatible CJK pour le rendu des colonnes chinois/japonais.
- Bouton **Traduire** : désactivé pendant le traitement, déclenche la boucle async.
- Bouton **Exporter CSV** : ouvre un `SaveFileDialog`, écrit le fichier.
- Zone **Log** (`TextBox` multiligne) : empile, par échange, le JSON envoyé puis le JSON reçu.
- `Label`/barre de statut : récapitulatif final (RG7).

## 7. Algorithmes & règles techniques

- **Traduit RG1/RG2** — Boucle : `foreach row { foreach cible in {Anglais, Espagnol, Chinois, Japonais} { await TranslateAsync(row.LibelleFr, cible) } }`. 5 × 4 = 20 appels distincts.
- **Traduit RG3** — `model = "gpt-4o-mini"` pour les 4 langues ; `temperature = 0` pour des traductions déterministes.
- **Traduit RG4/RG5** — Le message *system* impose « terme seul » et nomme explicitement la langue
  cible (anglais, espagnol, chinois simplifié ou japonais) ; le message *user* contient le libellé
  français brut.
- **Traduit RG6** — Try/catch autour de chaque appel (HTTP, timeout, JSON, réponse vide → `Success=false`). La cellule reçoit `[ERREUR]`, la boucle continue.
- **Traduit RG7** — Compteurs `traduites` / `erreurs` incrémentés, affichés en fin de boucle.
- **Traduit RG8/RG9** — `SecretResolver` lit la clé dans les user-secrets .NET (`secrets.json` du
  profil, via `UserSecretsReader`) puis, à défaut, dans `OPENAI_API_KEY` ; si aucune n'est trouvée, la traduction est refusée
  avec un message, aucun appel n'est émis.
- **Nettoyage de sortie** — `content.Trim()` puis retrait des guillemets encadrants éventuels.

## 8. Flux & séquences

**Nominal (par cellule)**
1. `MainForm` désactive « Traduire », initialise compteurs.
2. Pour chaque (ligne, langue) : construit `ChatRequest` → `OpenAiTranslator.TranslateAsync`.
3. Le service POST le JSON, attend la réponse, extrait/nettoie le terme.
4. `MainForm` écrit le terme dans la cellule, journalise `RawRequest`/`RawResponse`.
5. Après les 20 appels : affiche le récapitulatif, réactive « Traduire ».

**Erreur (RG6)**
- À l'étape 3, en cas d'exception ou de réponse vide : `TranslationResult.Success=false` ;
  `MainForm` met `[ERREUR]` dans la cellule, incrémente `erreurs`, journalise l'échec, **poursuit**.

**Clé absente (RG9)**
- Au clic « Traduire », `SecretResolver` ne trouve la clé ni dans les user-secrets ni dans l'environnement → message d'avertissement, sortie immédiate
  sans appel.

## 9. Sécurité & droits

- Clé API lue depuis les **user-secrets .NET** (à défaut `OPENAI_API_KEY`) (RG8) ; jamais en dur,
  jamais journalisée (l'en-tête `Authorization` est exclu du log, seul le corps JSON est tracé).
  Note : les user-secrets ne sont **pas chiffrés** (JSON en clair dans le profil, hors dépôt) — usage
  développement ; pour un chiffrement au repos, viser le Windows Credential Manager (DPAPI).
- Pas de gestion de droits multi-utilisateur (hors périmètre).
- Communications en HTTPS (TLS 1.2/1.3 négocié par défaut par .NET 8).
- Données non sensibles (libellés métier génériques).

## 10. Performance & dimensionnement

- Volumétrie : 20 appels par exécution — négligeable.
- Séquentiel volontaire (lisibilité pédagogique) ; latence totale = somme des 20 latences (~quelques
  secondes à une dizaine de secondes), acceptable pour un prototype.
- `HttpClient` en **instance unique réutilisée** (évite l'épuisement de sockets), `Timeout` explicite
  (ex. 30 s) par appel.

## 11. Migration / reprise de données

*Non applicable* — aucune donnée existante, jeu figé en mémoire.

## 12. Intégrations

- Unique intégration : API OpenAI Chat Completions (REST/JSON sur HTTPS).
- Gestion de panne d'interface : couverte par la stratégie d'erreur par cellule (§7, RG6) ;
  timeout par appel pour éviter un blocage indéfini.

## 13. Gestion des erreurs & robustesse

| Cas | Traitement |
|-----|-----------|
| Clé absente | Message, aucun appel (RG9). |
| Erreur réseau / timeout | `Success=false`, cellule `[ERREUR]`, on continue (RG6). |
| HTTP non-2xx (401, 429, 5xx) | Lire le statut + corps, `Error` renseigné, cellule `[ERREUR]`. |
| JSON illisible / `choices` vide | `Success=false`, cellule `[ERREUR]`. |
| Réponse vide après nettoyage | Traité comme erreur. |

Observabilité : zone de log UI (JSON envoyé/reçu + erreurs). Pas de framework de log externe (proto).

## 14. Testabilité

- `OpenAiTranslator` isolé derrière une signature simple (`TranslateAsync`) → testable en injectant un
  `HttpClient` muni d'un `HttpMessageHandler` simulé (réponses OK / 401 / 429 / JSON invalide / vide).
- `CsvExporter` testable purement (entrée `LabelRow[]` → chaîne CSV attendue).
- Le nettoyage de sortie (trim, guillemets) testable unitairement.
- Points d'injection : `HttpClient` et `AppConfig` passés au constructeur du service.

## 15. Risques techniques & dépendances

- **Dépendance** : aucune dépendance externe — `System.Text.Json` est intégré à .NET 8.
- **Dépendance** : clé OpenAI valide + accès réseau HTTPS sortant.
- **Note** : TLS géré par défaut par .NET 8 (1.2/1.3) ; aucun risque de handshake à mitiger.
- **Risque** : qualité de traduction d'un terme isolé hors contexte (ex. « Inventaire ») ; à signaler
  dans la documentation, `temperature = 0` limite la variabilité. Accru pour le chinois et le japonais.
- **Risque** : rendu CJK incorrect (caractères « tofu », mojibake) si l'encodage n'est pas UTF-8 de
  bout en bout ou si la police du `DataGridView` ne couvre pas le CJK ; mitigé en forçant UTF-8
  (HTTP/UI/CSV avec BOM) et une police compatible CJK — à valider en recette.
- **Risque** : évolution du contrat OpenAI ; la version d'endpoint (`/v1/chat/completions`) est figée
  dans le code et la doc.
