import type { Personnage, Scene } from "../types/format";
import { ErreurReponseInvalide } from "../providers/types";

interface ReponseBrute {
  personnages?: Personnage[];
  scenes?: Scene[];
}

// Extrait un objet JSON d'une réponse de modèle qui peut être entourée de
// texte libre ou de balises ```json (malgré la consigne). Lève une erreur
// explicite sinon, pour déclencher une nouvelle tentative avec consigne renforcée.
export function extraireJSON(brut: string): unknown {
  let candidat = brut.trim();
  const blocMarkdown = candidat.match(/```(?:json)?\s*([\s\S]*?)```/i);
  if (blocMarkdown) {
    candidat = blocMarkdown[1].trim();
  } else {
    const debut = candidat.indexOf("{");
    const fin = candidat.lastIndexOf("}");
    if (debut !== -1 && fin !== -1 && fin > debut) {
      candidat = candidat.slice(debut, fin + 1);
    }
  }
  try {
    return JSON.parse(candidat);
  } catch (e) {
    throw new ErreurReponseInvalide(`JSON illisible : ${(e as Error).message}`, brut);
  }
}

let compteurScene = 0;
let compteurPersonnageAnonyme = 0;

export function analyserReponseIA(brut: string): { personnages: Personnage[]; scenes: Scene[] } {
  const objet = extraireJSON(brut) as ReponseBrute;

  if (!objet || typeof objet !== "object" || !Array.isArray(objet.scenes)) {
    throw new ErreurReponseInvalide("champ \"scenes\" manquant ou invalide", brut);
  }

  const personnages: Personnage[] = (objet.personnages ?? []).map((p) => ({
    id: p.id || `personnage_${++compteurPersonnageAnonyme}`,
    nom: p.nom || "Sans nom",
    description: p.description,
  }));

  const scenes: Scene[] = objet.scenes.map((s) => ({
    id: s.id || `scene_${String(++compteurScene).padStart(2, "0")}`,
    titre: s.titre,
    elements: Array.isArray(s.elements) ? s.elements : [],
    suivant: s.suivant,
    fin: s.fin,
  }));

  return { personnages, scenes };
}

// Relie les scènes qui n'ont ni suivant ni fin ni choix final, dans l'ordre
// où elles arrivent (l'IA ne renvoie que narration/dialogue/suggestion_choix,
// jamais de vrai choix ni de suivant explicite entre chunks). La dernière
// scène du lot est marquée fin:true par défaut — l'auteur corrige ensuite
// à la main dans l'éditeur (embranchements réels, vraies fins, etc.).
export function chainerScenes(scenes: Scene[]): Scene[] {
  return scenes.map((scene, i) => {
    const dernierElement = scene.elements[scene.elements.length - 1];
    const finitDejaParChoix = dernierElement?.type === "choix";
    if (finitDejaParChoix || scene.suivant || scene.fin) return scene;

    const suivante = scenes[i + 1];
    if (suivante) return { ...scene, suivant: suivante.id };
    return { ...scene, fin: true };
  });
}
