import type { Personnage } from "../types/format";

// Prompt commun à tous les adaptateurs (cahier des charges §1.2).
// La sortie demandée est un objet JSON strict : { personnages: Personnage[], scenes: Scene[] }
// Les scenes peuvent contenir des éléments `suggestion_choix` (points de choix
// potentiels, à valider par l'auteur) mais jamais de vrai `choix` inventé de toutes pièces.

export const SCHEMA_JSON_ATTENDU = `{
  "personnages": [
    { "id": "snake_case", "nom": "string", "description": "string court" }
  ],
  "scenes": [
    {
      "id": "scene_NN",
      "titre": "string court",
      "elements": [
        { "type": "narration", "texte": "string" },
        { "type": "dialogue", "personnage": "id_personnage", "texte": "string" },
        { "type": "suggestion_choix", "note": "description d'un embranchement possible à cet endroit" }
      ],
      "suivant": "id_de_la_scene_suivante_ou_absent"
    }
  ]
}`;

export interface PromptAnalyse {
  systeme: string;
  utilisateur: string;
}

export function construirePromptAnalyse(
  texte: string,
  personnagesConnus: Personnage[],
  numeroMorceau: number,
  totalMorceaux: number
): PromptAnalyse {
  const contextePersonnages =
    personnagesConnus.length > 0
      ? `Personnages déjà identifiés dans les morceaux précédents (réutilise leurs "id" exacts, n'en recrée pas de nouveaux pour la même personne) :\n${personnagesConnus
          .map((p) => `- ${p.id} : ${p.nom}${p.description ? " — " + p.description : ""}`)
          .join("\n")}`
      : "Aucun personnage identifié pour l'instant : c'est le premier morceau.";

  const contexteMorceau =
    totalMorceaux > 1
      ? `Ce texte est le morceau ${numeroMorceau + 1}/${totalMorceaux} d'une histoire plus longue, découpée pour tenir dans le contexte. Les scènes doivent s'enchaîner logiquement avec ce qui précède (ne recommence pas la numérotation à scene_01 sauf si c'est vraiment le premier morceau).`
      : "";

  const systeme = `Tu es un assistant qui découpe de la prose française en structure de jeu narratif (visual novel). Tu réponds UNIQUEMENT avec un objet JSON strict conforme au schéma demandé, sans texte avant ni après, sans balises markdown \`\`\`.

Tâches :
1. Découpe la prose en scènes : un changement de lieu ou de temps marque une nouvelle scène.
2. Sépare narration et dialogues ; attribue chaque réplique au bon personnage.
3. Détecte les personnages (nom + courte description tirée du texte). Fusionne les désignations différentes d'une même personne (ex. "Amara" et "la jeune femme").
4. Repère 2 à 5 points de choix potentiels dans l'ensemble du texte (moments où l'histoire pourrait bifurquer) et marque-les comme éléments "suggestion_choix" — ce sont des suggestions à valider par l'auteur, N'INSERE JAMAIS de vrai "choix" avec des options inventées.
5. Ne modifie jamais le sens du texte original : reformule le minimum, préserve les répliques telles quelles.

Schéma JSON attendu :
${SCHEMA_JSON_ATTENDU}`;

  const utilisateur = `${contextePersonnages}
${contexteMorceau}

Texte à analyser :
"""
${texte}
"""`;

  return { systeme, utilisateur };
}

// Utilisé en cas de réponse JSON invalide : on renforce la consigne plutôt que d'abandonner.
export function renforcerConsigne(promptOriginal: PromptAnalyse, erreurPrecedente: string): PromptAnalyse {
  return {
    systeme: promptOriginal.systeme,
    utilisateur: `${promptOriginal.utilisateur}

IMPORTANT : ta réponse précédente n'était pas un JSON valide (erreur : ${erreurPrecedente}). Réponds cette fois avec UNIQUEMENT l'objet JSON, rien d'autre, pas de \`\`\`json, pas de commentaire.`,
  };
}
