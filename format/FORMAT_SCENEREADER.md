# Format `.scenereader.json` — spécification v1.0

Ce document est **le contrat** entre les trois composants de SceneReader :
l'app d'édition (Phase 1), le player Unity (Phase 2) et l'éditeur de
branches (Phase 3). Tout changement de format doit être documenté ici, avec
une entrée dans le [Journal des versions](#journal-des-versions).

Un fichier `.scenereader.json` est un unique document JSON (UTF-8, sans BOM)
qui décrit une histoire interactive complète : personnages, variables,
scènes, dialogues, narration et embranchements.

## 1. Squelette du document

```json
{
  "version": "1.0",
  "titre": "Titre de l'histoire",
  "auteur": "Nom de l'auteur",
  "personnages": [ /* Personnage[] */ ],
  "variables": [ /* Variable[] */ ],
  "scenes": [ /* Scene[] */ ],
  "scene_depart": "scene_01"
}
```

| Champ           | Type       | Obligatoire | Description |
|------------------|-----------|:-----------:|--------------|
| `version`        | string     | oui | Version du format, actuellement `"1.0"`. Le player doit refuser (ou avertir) si la version majeure n'est pas supportée. |
| `titre`          | string     | oui | Titre affiché dans le menu du player. |
| `auteur`         | string     | non | Nom affiché dans le menu ("d'après une histoire de…"). |
| `personnages`    | Personnage[] | oui (peut être vide) | Tous les personnages détectés ou créés. |
| `variables`      | Variable[] | oui (peut être vide) | Variables d'état utilisées par les conditions et les actions `set`. |
| `scenes`         | Scene[]    | oui | Liste des scènes. L'ordre dans le tableau n'a pas de valeur narrative (la navigation se fait via `id`/`suivant`/`vers`) mais il est utilisé comme ordre d'affichage par défaut dans l'éditeur. |
| `scene_depart`   | string     | oui | `id` de la scène par laquelle l'histoire commence. Doit exister dans `scenes`. |

## 2. Personnage

```json
{
  "id": "amara",
  "nom": "Amara",
  "description": "Une jeune femme déterminée, cheveux courts.",
  "couleur": "#E8734A",
  "portrait": "portraits/amara.png"
}
```

| Champ | Type | Obligatoire | Description |
|---|---|:---:|---|
| `id` | string | oui | Identifiant unique, `snake_case`, stable dans le temps (les répliques y font référence). Ne jamais réutiliser un `id` pour un autre personnage après export. |
| `nom` | string | oui | Nom affiché à l'écran. |
| `description` | string | non | Note d'auteur / résumé utilisé par l'IA pour la cohérence, non affichée en jeu par défaut. |
| `couleur` | string | non | Couleur hexadécimale (`#RRGGBB`) utilisée pour le nom du personnage dans la boîte de dialogue. Valeur par défaut si absente : couleur neutre du thème du player. |
| `portrait` | string | non | Chemin relatif (depuis `Assets/StreamingAssets/Histoires/<histoire>/`) vers l'image de portrait. Absent = pas de portrait affiché. |

Un personnage spécial optionnel `id: "narrateur"` peut être utilisé si l'auteur
veut personnaliser explicitement la voix off, mais ce n'est **pas requis** :
les éléments `narration` (section 4.1) n'ont pas de personnage.

## 3. Variable

```json
{ "id": "a_menti", "type": "bool", "defaut": false }
```

| Champ | Type | Obligatoire | Description |
|---|---|:---:|---|
| `id` | string | oui | Identifiant unique, `snake_case`. |
| `type` | `"bool"` \| `"nombre"` \| `"texte"` | oui | Type de la variable. |
| `defaut` | bool \| number \| string | oui | Valeur initiale à la création d'une nouvelle partie. Doit être cohérente avec `type`. |

Toutes les variables utilisées dans une condition ou une action `set` doivent
être déclarées dans `variables`. Un fichier qui référence une variable non
déclarée est **invalide**.

## 4. Scène

```json
{
  "id": "scene_01",
  "titre": "La porte",
  "decor": "decors/maison_nuit.png",
  "elements": [ /* Element[] */ ],
  "suivant": "scene_02"
}
```

| Champ | Type | Obligatoire | Description |
|---|---|:---:|---|
| `id` | string | oui | Identifiant unique, `snake_case`, stable (cible de `suivant`/`vers`). |
| `titre` | string | non | Titre interne (affiché dans l'éditeur / la vue graphe Phase 3, pas forcément en jeu). |
| `decor` | string | non | Chemin relatif vers l'image de fond. Absent = décor par défaut du player. |
| `elements` | Element[] | oui (peut être vide) | Séquence ordonnée jouée du début à la fin de la scène. |
| `suivant` | string | non* | `id` de la scène suivante, jouée automatiquement une fois `elements` épuisés. |
| `fin` | boolean | non* | `true` si cette scène est une fin de l'histoire (voir règle de terminaison ci-dessous). |

**Règle de terminaison** — une scène doit se terminer d'une (et une seule) des
trois façons :
1. Son dernier élément est de type `choix` (la navigation part des options du choix, `suivant` et `fin` sont alors ignorés/absents) ; ou
2. Elle possède un champ `suivant` (et pas de `fin: true`) ; ou
3. Elle possède `fin: true` (et pas de `suivant`) — fin de l'histoire, le player affiche l'écran de fin / retour au menu.

Une scène qui n'a ni `choix` en dernier élément, ni `suivant`, ni `fin: true`
est une **impasse** et doit être signalée comme erreur par l'éditeur (voir
Phase 3, détection des impasses).

### 4.1 Éléments de scène

Le tableau `elements` contient une séquence d'objets, chacun avec un champ
`type` discriminant :

#### `narration`
```json
{ "type": "narration", "texte": "La pluie tombait depuis des heures." }
```
Texte affiché sans nom de personnage (voix du narrateur).

#### `dialogue`
```json
{ "type": "dialogue", "personnage": "amara", "texte": "Tu es sûr de vouloir entrer ?", "emotion": "inquiete" }
```
| Champ | Obligatoire | Description |
|---|:---:|---|
| `personnage` | oui | `id` référencant `personnages[].id`. |
| `texte` | oui | Réplique. |
| `emotion` | non | Étiquette libre (ex. `"inquiete"`, `"colere"`) — réservée à un usage futur (variante de portrait). Ignorée si le player ne la supporte pas. |

#### `set`
```json
{ "type": "set", "variable": "a_menti", "valeur": true }
```
Affecte immédiatement `valeur` à la variable `variable` (doit exister dans
`variables`, `valeur` doit être du bon type). N'affiche rien à l'écran.

#### `choix`
```json
{
  "type": "choix",
  "question": "Que faire ?",
  "options": [
    { "texte": "Ouvrir la porte", "vers": "scene_02" },
    { "texte": "Faire demi-tour", "vers": "scene_03", "condition": "a_menti == false" },
    { "texte": "Mentir encore", "vers": "scene_04", "condition": "a_menti == true", "set": [{ "variable": "a_menti", "valeur": true }] }
  ]
}
```
| Champ | Obligatoire | Description |
|---|:---:|---|
| `question` | non | Texte affiché au-dessus des options (absent = pas de question, juste les boutons). |
| `options` | oui, ≥ 1 | Liste des options. |

Chaque option :

| Champ | Obligatoire | Description |
|---|:---:|---|
| `texte` | oui | Libellé du bouton. |
| `vers` | oui | `id` de la scène ciblée. |
| `condition` | non | Expression booléenne (section 5). Absente = option toujours visible. Une option dont la condition est fausse est masquée par le player, pas seulement désactivée. |
| `set` | non | Liste d'actions `{variable, valeur}` appliquées **au moment où l'option est choisie**, avant de charger `vers`. |

Un `choix` doit obligatoirement être le **dernier élément** de `elements`
lorsqu'il est présent.

#### Élément suggéré (sortie IA uniquement, jamais dans un export final)
Pendant l'analyse, l'IA peut proposer des points de choix potentiels sous la
forme d'une **suggestion**, distincte d'un vrai `choix` :
```json
{ "type": "suggestion_choix", "note": "Ici, Amara pourrait refuser d'entrer." }
```
Ces éléments sont affichés dans l'éditeur comme pistes à valider par
l'auteur ; ils **doivent être convertis en `choix` réel ou supprimés** avant
export — un fichier exporté ne doit jamais contenir de `suggestion_choix`
(l'export refuse / avertit si c'est le cas).

## 5. Expressions de condition

Grammaire (EBNF simplifié) :

```
expression   := terme (("&&" | "||") terme)*
terme        := variable_id comparateur valeur | "(" expression ")"
comparateur  := "==" | "!="
variable_id  := identifiant déclaré dans variables[].id
valeur       := "true" | "false" | nombre | "'texte entre quotes simples'"
```

- Pas de priorité d'opérateurs au-delà des parenthèses explicites : évaluer
  strictement de gauche à droite.
- Les comparaisons `==`/`!=` doivent respecter le `type` déclaré de la
  variable (comparer un `bool` à un `nombre` est une erreur de validation).
- Exemples valides : `a_menti == false`, `points >= 3` *(← non supporté en
  v1.0, voir Extensions futures)*, `a_menti == true && rencontre_garde == false`.
- En v1.0 seuls `==`, `!=`, `&&`, `||` sont supportés (pas de `<`, `>`, `<=`,
  `>=` — cf. cahier des charges §1.1 "Le format pivot"). Un moteur qui
  rencontre un opérateur non supporté doit lever une erreur explicite plutôt
  que d'ignorer silencieusement la condition.

## 6. Contraintes d'intégrité (un fichier `.scenereader.json` valide doit respecter)

1. `scene_depart` référence un `id` existant dans `scenes`.
2. Tous les `id` de `personnages`, `variables` et `scenes` sont uniques dans
   leur propre liste.
3. Tout `personnage` référencé par un élément `dialogue` existe dans
   `personnages`.
4. Toute `variable` référencée par un `set` ou une `condition` existe dans
   `variables`, avec une `valeur`/comparaison du bon `type`.
5. Tout `vers` (dans une option de `choix`) et tout `suivant` référencent un
   `id` de scène existant.
6. Chaque scène respecte la règle de terminaison (§4, "Règle de
   terminaison").
7. Aucun élément `suggestion_choix` n'est présent dans un fichier exporté.
8. Les identifiants (`id`) sont en `snake_case` : lettres minuscules, chiffres,
   underscores, doivent commencer par une lettre.

L'app Phase 1 doit valider ces règles avant d'autoriser l'export et lister
les erreurs trouvées de façon actionnable (quelle scène, quel champ).

## 7. Conventions de nommage

- `id` de scène : `scene_01`, `scene_02`, … ou libellé court `scene_porte_entree`
  si renommé manuellement — peu importe tant que c'est stable et unique.
- `id` de personnage / variable : `snake_case` dérivé du nom (`amara`,
  `a_menti`, `rencontre_garde`).
- Chemins d'images (`decor`, `portrait`) : toujours relatifs, jamais absolus,
  jamais d'URL distante (tout le contenu reste local, cf. cahier des
  charges).

## 8. Exemple minimal complet

Voir [`exemples/histoire_demo.scenereader.json`](../exemples/histoire_demo.scenereader.json)
pour une histoire jouable de bout en bout (plusieurs scènes, un personnage,
une variable, un embranchement conditionnel, une fin).

## 9. Extensions futures (hors v1.0, ne pas implémenter en Phase 1/2)

Réservé pour discussion, **non contractuel** :
- Opérateurs numériques `<`, `>`, `<=`, `>=` sur variables de type `nombre`.
- Élément `son` / `musique` (déclenchement d'un fichier audio).
- Élément `attente` (pause chronométrée sans interaction).
- Variantes de portrait par `emotion`.
Toute implémentation de ces points nécessite d'abord une mise à jour de ce
document et un bump de `version` (`"1.1"`), en respectant la compatibilité
ascendante (un player v1.0 doit pouvoir ignorer proprement les champs
inconnus plutôt que planter).

## Journal des versions

- **1.0** (initiale) — première spécification du format pivot, correspond au
  cahier des charges SceneReader Phase 1.
