# Objectif principal
Comprendre les mécanismes d'interaction entre un programme C# et un agent IA.
* comment les données sont envoyées
* comment les taitements attendus sont spécifiés à 'IA
* comment les données sont récupérées en retour
 Au delà du code, l'objectif est surtout ici de produire un document markdown qui explique pas à pas les mécanismes d'échange impliqués.

# Exemple appliqué 
## Introduction
Suivre ce scénarios pour iilustrer l'obkectif principal
## Contexte 
Créer un programme en C# 4.8. ce programme contient un tableau de données :
- Code   
- Libellé français
- Libellé anglais
- Libellé espagnol

| Code | libellé français | Libellé anglais | libellé espagnol |
|------|------------------|-----------------|------------------|
| A01  | Client           | <empty>         |<empty>           |
| A02  | Commande         | <empty>         |<empty>           |
| A03  | Inventaire       | <empty>         |<empty>           |
| A04  | Planification    | <empty>         |<empty>           |
| A05  | Histrogramme     | <empty>         |<empty>           |
|------|------------------|-----------------|------------------|

## Objectif
Comment traduire en renseignant les colonnes "Libellé anglais" et "Libellé espagnol" en faisant appel à l'IA.
Je dispose d'un compte avec API chez OpenAI qui peut être utilisé.
Privilégier des moteurs peu couteux.

# étapes
1. Dans un premier temps produire un document de spécification fonctionnelle qui reprend cette demande
2. Produire un document de spécification techniques qui détaillera bien pas à pas le process qui sera déroulé
3. coder l'exemple appliqué
