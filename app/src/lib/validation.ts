import type { Histoire, Scene } from "../types/format";

export interface ErreurValidation {
  scene?: string;
  champ?: string;
  message: string;
}

const RE_ID = /^[a-z][a-z0-9_]*$/;

// Applique les règles d'intégrité du contrat (format/FORMAT_SCENEREADER.md §6).
// Utilisé avant export et affiché dans l'éditeur pour guider les corrections.
export function validerHistoire(histoire: Histoire): ErreurValidation[] {
  const erreurs: ErreurValidation[] = [];

  const idsPersonnages = new Set(histoire.personnages.map((p) => p.id));
  const idsVariables = new Map(histoire.variables.map((v) => [v.id, v.type]));
  const idsScenes = new Set(histoire.scenes.map((s) => s.id));

  const dup = (ids: string[], quoi: string) => {
    const vus = new Set<string>();
    for (const id of ids) {
      if (vus.has(id)) erreurs.push({ message: `Identifiant ${quoi} dupliqué : "${id}"` });
      vus.add(id);
    }
  };
  dup(histoire.personnages.map((p) => p.id), "de personnage");
  dup(histoire.variables.map((v) => v.id), "de variable");
  dup(histoire.scenes.map((s) => s.id), "de scène");

  for (const p of histoire.personnages) {
    if (!RE_ID.test(p.id)) erreurs.push({ message: `id de personnage invalide (snake_case attendu) : "${p.id}"` });
  }
  for (const v of histoire.variables) {
    if (!RE_ID.test(v.id)) erreurs.push({ message: `id de variable invalide (snake_case attendu) : "${v.id}"` });
  }
  for (const s of histoire.scenes) {
    if (!RE_ID.test(s.id)) erreurs.push({ scene: s.id, message: `id de scène invalide (snake_case attendu) : "${s.id}"` });
  }

  if (!idsScenes.has(histoire.scene_depart)) {
    erreurs.push({ message: `scene_depart "${histoire.scene_depart}" ne correspond à aucune scène existante` });
  }

  for (const scene of histoire.scenes) {
    validerScene(scene, idsPersonnages, idsVariables, idsScenes, erreurs);
  }

  return erreurs;
}

function validerScene(
  scene: Scene,
  idsPersonnages: Set<string>,
  idsVariables: Map<string, string>,
  idsScenes: Set<string>,
  erreurs: ErreurValidation[]
) {
  const dernier = scene.elements[scene.elements.length - 1];
  const finitParChoix = dernier?.type === "choix";

  if (finitParChoix && (scene.suivant || scene.fin)) {
    erreurs.push({ scene: scene.id, message: "une scène qui finit par un choix ne doit pas avoir suivant/fin" });
  }
  if (scene.suivant && scene.fin) {
    erreurs.push({ scene: scene.id, message: "une scène ne peut avoir à la fois suivant et fin" });
  }
  if (!finitParChoix && !scene.suivant && !scene.fin) {
    erreurs.push({ scene: scene.id, message: "impasse : ni choix final, ni suivant, ni fin" });
  }
  if (scene.suivant && !idsScenes.has(scene.suivant)) {
    erreurs.push({ scene: scene.id, champ: "suivant", message: `scène cible introuvable : "${scene.suivant}"` });
  }

  for (const el of scene.elements) {
    if (el.type === "suggestion_choix") {
      erreurs.push({ scene: scene.id, message: "suggestion_choix non résolue (à convertir en choix ou supprimer avant export)" });
    }
    if (el.type === "dialogue" && !idsPersonnages.has(el.personnage)) {
      erreurs.push({ scene: scene.id, champ: "dialogue", message: `personnage inconnu : "${el.personnage}"` });
    }
    if (el.type === "set") {
      verifierVariable(el.variable, el.valeur, idsVariables, scene.id, erreurs);
    }
    if (el.type === "choix") {
      if (el.options.length === 0) {
        erreurs.push({ scene: scene.id, message: "choix sans options" });
      }
      for (const opt of el.options) {
        if (!idsScenes.has(opt.vers)) {
          erreurs.push({ scene: scene.id, champ: "choix", message: `option "${opt.texte}" cible une scène introuvable : "${opt.vers}"` });
        }
        if (opt.condition) {
          verifierCondition(opt.condition, idsVariables, scene.id, erreurs);
        }
        for (const action of opt.set ?? []) {
          verifierVariable(action.variable, action.valeur, idsVariables, scene.id, erreurs);
        }
      }
    }
  }
}

function verifierVariable(
  id: string,
  valeur: unknown,
  idsVariables: Map<string, string>,
  sceneId: string,
  erreurs: ErreurValidation[]
) {
  const type = idsVariables.get(id);
  if (!type) {
    erreurs.push({ scene: sceneId, message: `variable inconnue : "${id}"` });
    return;
  }
  const typeReel = typeof valeur === "boolean" ? "bool" : typeof valeur === "number" ? "nombre" : "texte";
  if (typeReel !== type) {
    erreurs.push({ scene: sceneId, message: `valeur incompatible pour la variable "${id}" (attendu ${type}, reçu ${typeReel})` });
  }
}

const OPS_SUPPORTES = ["==", "!="];

function verifierCondition(condition: string, idsVariables: Map<string, string>, sceneId: string, erreurs: ErreurValidation[]) {
  const termes = condition.split(/&&|\|\|/).map((t) => t.trim().replace(/^\(|\)$/g, ""));
  for (const terme of termes) {
    const op = OPS_SUPPORTES.find((o) => terme.includes(o));
    if (!op) {
      erreurs.push({ scene: sceneId, message: `condition invalide (opérateur non supporté) : "${terme}"` });
      continue;
    }
    const [gauche] = terme.split(op).map((s) => s.trim());
    if (!idsVariables.has(gauche)) {
      erreurs.push({ scene: sceneId, message: `condition référence une variable inconnue : "${gauche}" dans "${condition}"` });
    }
  }
}
