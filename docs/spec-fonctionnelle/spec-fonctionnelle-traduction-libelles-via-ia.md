---
date: 2026-06-29
tags: [spec, fonctionnel]
statut: brouillon
source: docs/draft/draft-spec-fonctionnelle.md
---

# Traduction de libellés via une IA (OpenAI) — programme C# de démonstration

## 1. Objectif & contexte

Comprendre et **documenter pas à pas les mécanismes d'échange entre un programme C# et un agent IA** :
comment les données sont envoyées, comment le traitement attendu est spécifié à l'IA, comment les
résultats sont récupérés.

Le livrable phare est un **document Markdown explicatif**. Un programme C# de démonstration sert de
support concret : il traduit automatiquement des libellés métier du français vers l'anglais,
l'espagnol, le chinois (simplifié) et le japonais en appelant l'API OpenAI.

Le contexte est **pédagogique** (prototype) : la clarté du propos prime sur l'exhaustivité
fonctionnelle ou l'optimisation.

## 2. Problème / besoin

Un tableau de libellés contient une colonne « Libellé français » renseignée, mais les colonnes
« Libellé anglais », « Libellé espagnol », « Libellé chinois » et « Libellé japonais » sont vides.
Les remplir manuellement est fastidieux et peu reproductible. On veut les compléter automatiquement
via l'IA, tout en **rendant visible et compréhensible le mécanisme d'appel** pour qu'il puisse être
réutilisé dans d'autres contextes.

## 3. Périmètre

### Dans le périmètre
- Document Markdown expliquant l'anatomie complète d'un échange programme C# ↔ API OpenAI.
- Application **Windows Forms** (.NET 8 / `net8.0-windows`) affichant le tableau dans une grille
  (`DataGridView`) avec un bouton « Traduire ».
- Jeu de données de démonstration **figé dans le code** (les 5 lignes du draft).
- Traduction FR → EN, FR → ES, FR → ZH (chinois simplifié) et FR → JA via l'API OpenAI,
  **un appel par cellule** (20 appels au total).
- Export du tableau complété en **fichier CSV**.
- Lecture de la clé API depuis les **user-secrets .NET** (Secret Manager) en priorité, avec repli
  sur la **variable d'environnement** `OPENAI_API_KEY`.
- Gestion d'erreur par cellule : une cellule en échec est marquée, le traitement continue.

### Hors périmètre
- Import de données depuis une source externe (CSV/Excel/base) en entrée.
- Langues autres que l'anglais, l'espagnol, le chinois (simplifié) et le japonais.
- Variante chinois traditionnel (seul le chinois simplifié est couvert).
- Persistance/édition du tableau au-delà de l'export CSV.
- Authentification, gestion multi-utilisateur, déploiement.
- Optimisation des coûts au-delà du choix d'un moteur peu coûteux (`gpt-4o-mini`).
- Mécanismes avancés (cache, file d'attente, parallélisme, reprise sur incident).

## 4. Acteurs & rôles

| Acteur | Rôle |
|--------|------|
| Utilisateur (développeur/lecteur) | Lance l'application, déclenche la traduction, lit le document explicatif. |
| Programme C# | Construit les requêtes, appelle l'API, récupère et affiche les résultats. |
| API OpenAI | Reçoit les requêtes, renvoie les traductions. |

## 5. Règles de gestion

- **RG1** — Seules les cellules « Libellé anglais », « Libellé espagnol », « Libellé chinois » et
  « Libellé japonais » sont calculées ; le code et le libellé français sont des données d'entrée
  non modifiées.
- **RG2** — Une traduction est demandée par **un appel distinct par cellule** : une paire
  (ligne, langue cible) = une requête.
- **RG3** — Le moteur utilisé est un modèle **peu coûteux** : `gpt-4o-mini`, pour les quatre langues
  cibles (y compris chinois et japonais).
- **RG4** — La consigne envoyée à l'IA (message *system*) impose de **ne renvoyer que le terme
  traduit**, sans guillemets, ponctuation parasite ni explication.
- **RG5** — La langue cible est précisée explicitement dans chaque requête (anglais, espagnol,
  chinois simplifié ou japonais).
- **RG6** — Si un appel échoue (réseau, quota, clé absente/invalide, réponse vide), la cellule
  concernée est marquée `[ERREUR]` et le traitement **continue** sur les autres cellules.
- **RG7** — En fin de traitement, un **récapitulatif** indique le nombre de cellules traduites et le
  nombre d'erreurs (sur les 20 cellules attendues).
- **RG8** — La clé API n'est **jamais** écrite dans le code source ni dans un fichier versionné ;
  elle est lue d'abord dans les **user-secrets .NET** (`secrets.json` du profil, hors dépôt), puis à
  défaut dans la variable d'environnement `OPENAI_API_KEY`.
- **RG9** — Si aucune clé n'est trouvée (ni user-secrets ni `OPENAI_API_KEY`), l'application le signale clairement et n'effectue aucun
  appel.

## 6. Cas d'usage / parcours

**Nominal**
1. L'utilisateur lance l'application ; la grille affiche les 5 lignes (Code, FR, EN/ES/ZH/JA vides).
2. Il clique sur « Traduire ».
3. Pour chaque ligne et chaque langue cible, le programme construit une requête et appelle l'API.
4. Chaque cellule se remplit au fur et à mesure avec la traduction reçue.
5. Un récapitulatif s'affiche (ex. « 20 cellules traduites, 0 erreur »).
6. L'utilisateur exporte le résultat en CSV.

**Alternatif — clé absente (RG9)**
- Au lancement de la traduction, si aucune clé n'est trouvée (user-secrets puis `OPENAI_API_KEY`), un message l'indique et aucun
  appel n'est lancé.

**Cas d'erreur — appel en échec (RG6)**
- Un appel échoue : la cellule affiche `[ERREUR]`, les autres cellules continuent d'être traitées,
  l'erreur est comptabilisée dans le récapitulatif.

## 7. Critères d'acceptation

- [ ] Le programme compile et s'exécute en .NET 8 (Windows Forms, `net8.0-windows`).
- [ ] La grille affiche au lancement les 5 lignes du draft avec EN, ES, ZH et JA vides.
- [ ] Un clic sur « Traduire » remplit les colonnes EN, ES, ZH et JA pour chaque ligne.
- [ ] Chaque cellule est obtenue par un appel API distinct (20 appels pour 5 lignes × 4 langues).
- [ ] Le modèle appelé est `gpt-4o-mini` pour les quatre langues.
- [ ] Les colonnes chinois et japonais affichent des caractères CJK correctement encodés (UTF-8).
- [ ] Les traductions ne contiennent ni guillemets ni texte explicatif parasite.
- [ ] La clé API est lue depuis les user-secrets .NET (à défaut `OPENAI_API_KEY`) et n'apparaît nulle part dans le code ni le dépôt.
- [ ] Si la clé est absente, l'application le signale et n'appelle pas l'API.
- [ ] Une cellule dont l'appel échoue affiche `[ERREUR]` sans interrompre les autres.
- [ ] Un récapitulatif (traduites / erreurs sur 20 cellules) est présenté en fin de traitement.
- [ ] Le tableau complété est exportable en fichier CSV, caractères CJK lisibles à la réouverture.
- [ ] Le document Markdown explique pas à pas un échange complet : endpoint, en-têtes,
      rôles system/user, prompt, corps JSON envoyé, réponse JSON reçue, extraction du résultat,
      avec des exemples réels de requête et de réponse.

## 8. Données & états

**Ligne du tableau**

| Champ | Type | Contrainte | État |
|-------|------|-----------|------|
| Code | texte | Donnée d'entrée, non modifié | Fixe |
| Libellé français | texte | Donnée d'entrée, non vide | Fixe |
| Libellé anglais | texte | Calculé | Vide → Traduit / `[ERREUR]` |
| Libellé espagnol | texte | Calculé | Vide → Traduit / `[ERREUR]` |
| Libellé chinois | texte | Calculé (chinois simplifié) | Vide → Traduit / `[ERREUR]` |
| Libellé japonais | texte | Calculé | Vide → Traduit / `[ERREUR]` |

Jeu de démonstration figé : `A01 Client`, `A02 Commande`, `A03 Inventaire`,
`A04 Planification`, `A05 Histogramme`.

## 9. Contraintes

- **Technique** : C# / .NET 8 (`net8.0-windows`), Windows Forms ; appel HTTPS à l'API OpenAI
  (endpoint Chat Completions) ; réponses et requêtes au format JSON.
- **Coût** : privilégier `gpt-4o-mini` ; volumétrie de démonstration (20 appels) à coût négligeable.
- **Internationalisation / encodage** : affichage et export doivent gérer les caractères CJK
  (chinois, japonais) ; UTF-8 de bout en bout, police d'affichage de la grille compatible CJK.
- **Sécurité** : clé API hors du code et hors du dépôt (RG8).
- **Performance** : non critique (prototype) ; le traitement séquentiel des 10 appels est acceptable.

## 10. Dépendances & risques

- **Dépendance** : compte et clé API OpenAI valides et provisionnés.
- **Dépendance** : accès réseau sortant HTTPS vers l'API OpenAI.
- **Risque** : qualité de traduction d'un terme isolé hors contexte (ex. « Inventaire ») — acceptable
  pour un prototype, mais à signaler dans la documentation. Accru pour le chinois et le japonais,
  où un terme métier isolé peut être ambigu sans contexte.
- **Risque** : mauvais rendu des caractères CJK (carrés/points d'interrogation) si l'encodage n'est
  pas UTF-8 partout ou si la police de la grille ne couvre pas le CJK — à vérifier en recette.
- **Risque** : évolution du format de l'API OpenAI — figer la version d'endpoint utilisée dans la doc.
- **Note** : la sérialisation JSON utilise `System.Text.Json`, intégré à .NET 8 (aucune dépendance
  externe ni accès NuGet requis).
