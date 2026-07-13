---
date: 2026-07-13
tags: [documentation, git, github]
statut: validé
projet: Traduction par IA (C:\sessions_claude_code\Protos\Traduction par IA)
---

# Publier un projet sur GitHub — de zéro jusqu'au push

Guide pas à pas, réutilisable pour tout nouveau projet : création du dépôt Git local,
premier commit, création du dépôt distant sur GitHub, liaison et premier push.
Environnement : Windows / PowerShell, Git for Windows installé.

## Vue d'ensemble

```
[Étape 0]  identité git        →  qui signe les commits
[Étape 1]  git init            →  créer le dépôt local (.git/)
[Étape 2]  .gitignore          →  exclure build, secrets, fichiers locaux
[Étape 3]  git add / commit    →  enregistrer un instantané en local
[Étape 4]  branche main        →  nom de branche attendu par GitHub
[Étape 5]  dépôt GitHub vide   →  créer la destination (site web)
[Étape 6]  git remote add      →  relier local ↔ GitHub
[Étape 7]  git push            →  publier (authentification navigateur)
```

Les étapes 0 à 4 sont **100 % locales et réversibles** ; seul le `push` (étape 7) publie.

## Étape 0 — Définir son identité Git

Git a besoin d'un nom et d'un email pour signer les commits. Deux portées possibles :

| Portée | Commande | Fichier | S'applique à |
|--------|----------|---------|--------------|
| Locale (ce dépôt seul) | `git config --local ...` | `.git/config` du projet | ce seul projet |
| Globale (toute la machine) | `git config --global ...` | `~/.gitconfig` | tous les projets |

La portée **locale** exige que le dépôt existe déjà (`git init` d'abord — voir étape 1).
Utiliser l'email associé au compte GitHub pour que les commits soient bien attribués.

```powershell
# après git init, dans le dossier du projet :
git config --local user.name  "Prénom Nom"
git config --local user.email "email-du-compte-github"

# vérifier :
git config --local --list
```

> La config locale **prime** sur la globale. Pratique pour isoler un email perso
> (GitHub) d'un email pro utilisé ailleurs.

## Étape 1 — Initialiser le dépôt local

```powershell
cd "C:\chemin\du\projet"
git init
```

Crée le dossier caché `.git/` : le projet devient un dépôt Git, sans historique ni remote.

*Vérifier :* `git status` fonctionne et liste les fichiers en « untracked ».

## Étape 2 — Mettre en place le `.gitignore`

Avant tout `git add`, s'assurer qu'un `.gitignore` exclut ce qui ne doit jamais être
versionné : sorties de build (`bin/`, `obj/`), fichiers d'IDE (`.vs/`), secrets
(`.env`, `secrets.json`), fichiers d'OS (`Thumbs.db`, `*.lnk`).

Le `.gitignore` de ce projet sert de base réutilisable .NET / Python (voir le fichier
à la racine).

*Vérifier qu'un chemin est bien ignoré :*
```powershell
git check-ignore src/MonProjet/bin/Debug/app.exe   # renvoie le chemin si ignoré
```

## Étape 3 — Préparer et committer

```powershell
git add .          # met tous les fichiers non ignorés en zone de préparation
git status         # CONTRÔLE : ni bin/, ni obj/, ni secrets dans la liste verte
git commit -m "Initial commit"
```

*Vérifier :* `git log --oneline` affiche le commit ; `git log -1 --format="%an <%ae>"`
confirme l'auteur.

## Étape 4 — Nommer la branche `main`

```powershell
git branch -M main
```

Selon la version de Git, la branche initiale peut s'appeler `master` ; GitHub attend
`main` par défaut. À faire **après** le premier commit.

*Vérifier :* `git branch` affiche `* main`.

## Étape 5 — Créer le dépôt (vide) sur GitHub

Sur github.com :

1. **New repository** (bouton **+** en haut à droite).
2. **Repository name** : nom du projet (ex. `traduction-via-openai-api`).
3. Visibilité : **Private** recommandé pour du code non public.
4. **Ne rien cocher** (ni README, ni .gitignore, ni licence) : le dépôt doit rester
   **vide**, sinon le premier push entre en conflit avec le contenu généré.
5. **Create repository**, puis copier l'URL HTTPS affichée :
   `https://github.com/<utilisateur>/<repo>.git`

## Étape 6 — Relier le dépôt local à GitHub

```powershell
git remote add origin https://github.com/<utilisateur>/<repo>.git
git remote -v      # vérifier : l'URL github.com apparaît en fetch et en push
```

`origin` est le nom conventionnel du dépôt distant principal.

## Étape 7 — Premier push (avec authentification)

GitHub n'accepte **plus le mot de passe de compte** pour les opérations Git.
L'authentification passe par le **Git Credential Manager** (GCM, livré avec Git for
Windows), qui ouvre une fenêtre de navigateur au premier push.

```powershell
# une fois pour toutes (crée ~/.gitconfig) :
git config --global credential.helper manager

# publier :
git push -u origin main
```

- Une fenêtre de navigateur s'ouvre → se connecter **avec le bon compte GitHub** et
  autoriser. Les identifiants sont ensuite mémorisés (plus de saisie aux push suivants).
- `-u` mémorise le lien `main → origin/main` : les prochains envois se feront avec un
  simple `git push`.

*Vérifier :* la sortie affiche `* [new branch] main -> main` ; recharger la page GitHub —
les fichiers du projet apparaissent ; `git status -sb` affiche `## main...origin/main`.

### Cas particulier — un autre compte GitHub est mémorisé sur la machine

Symptôme : le push échoue avec `remote: Repository not found` **sans** ouvrir de fenêtre
de navigateur. Cause probable : GCM détient déjà une session pour un **autre** compte
GitHub, qui n'a pas accès au dépôt (GitHub renvoie alors 404 pour masquer l'existence
des dépôts privés).

Lister les comptes connus de GCM, puis déconnecter celui qui n'est pas le bon :

```powershell
git credential-manager github list
git credential-manager github logout <ancien-compte>
```

Relancer ensuite `git push -u origin main` : la fenêtre d'authentification s'ouvre et
permet de se connecter avec le bon compte.

## Cycle de travail quotidien (après la mise en place)

```powershell
git add .                        # préparer les modifications
git commit -m "Message clair"    # enregistrer en local
git push                         # publier (l'upstream est déjà mémorisé)
```

## Récapitulatif des vérifications

| Après l'étape | Commande | Attendu |
|---------------|----------|---------|
| 0 | `git config --local --list` | `user.name` / `user.email` |
| 1 | `git status` | liste des fichiers untracked |
| 2 | `git check-ignore <chemin>` | chemin renvoyé si ignoré |
| 3 | `git log --oneline` | 1 commit |
| 4 | `git branch` | `* main` |
| 6 | `git remote -v` | URL github.com ×2 |
| 7 | `git status -sb` | `## main...origin/main` |
