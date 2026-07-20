// Types TypeScript miroir de format/FORMAT_SCENEREADER.md (contrat v1.0).
// Toute modification ici doit être répercutée dans ce document.

export const FORMAT_VERSION = "1.0";

export type TypeVariable = "bool" | "nombre" | "texte";
export type ValeurVariable = boolean | number | string;

export interface Personnage {
  id: string;
  nom: string;
  description?: string;
  couleur?: string;
  portrait?: string;
}

export interface Variable {
  id: string;
  type: TypeVariable;
  defaut: ValeurVariable;
}

export interface ElementNarration {
  type: "narration";
  texte: string;
}

export interface ElementDialogue {
  type: "dialogue";
  personnage: string;
  texte: string;
  emotion?: string;
}

export interface ElementSet {
  type: "set";
  variable: string;
  valeur: ValeurVariable;
}

export interface OptionChoix {
  texte: string;
  vers: string;
  condition?: string;
  set?: { variable: string; valeur: ValeurVariable }[];
}

export interface ElementChoix {
  type: "choix";
  question?: string;
  options: OptionChoix[];
}

// Élément transitoire produit par l'IA : jamais présent dans un export final.
export interface ElementSuggestionChoix {
  type: "suggestion_choix";
  note: string;
}

export type Element =
  | ElementNarration
  | ElementDialogue
  | ElementSet
  | ElementChoix
  | ElementSuggestionChoix;

export interface Scene {
  id: string;
  titre?: string;
  decor?: string;
  elements: Element[];
  suivant?: string;
  fin?: boolean;
}

export interface Histoire {
  version: string;
  titre: string;
  auteur?: string;
  personnages: Personnage[];
  variables: Variable[];
  scenes: Scene[];
  scene_depart: string;
}

export function nouvelleHistoireVide(titre = "Nouvelle histoire"): Histoire {
  return {
    version: FORMAT_VERSION,
    titre,
    auteur: "",
    personnages: [],
    variables: [],
    scenes: [
      {
        id: "scene_01",
        titre: "Début",
        elements: [],
        fin: true,
      },
    ],
    scene_depart: "scene_01",
  };
}
