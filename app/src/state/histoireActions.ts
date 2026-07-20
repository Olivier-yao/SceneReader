import type { Element, ElementChoix, Histoire, Personnage, Scene } from "../types/format";
import { idUnique, prochainIdScene } from "../lib/id";
import type { ResultatAnalyseIA } from "../providers/types";

// Toutes les fonctions sont pures : elles reçoivent une Histoire et renvoient
// une nouvelle Histoire, jamais de mutation en place (facilite l'undo futur
// et les re-render React prévisibles).

export function appliquerResultatAnalyse(histoire: Histoire, resultat: ResultatAnalyseIA): Histoire {
  const idsPersonnagesExistants = new Set(histoire.personnages.map((p) => p.id));
  const nouveauxPersonnages = resultat.personnages.filter(
    (p) =>
      !idsPersonnagesExistants.has(p.id) &&
      !histoire.personnages.some((existant) => existant.nom.toLowerCase() === p.nom.toLowerCase())
  );

  return {
    ...histoire,
    personnages: [...histoire.personnages, ...nouveauxPersonnages],
    scenes: resultat.scenes,
    scene_depart: resultat.scenes[0]?.id ?? histoire.scene_depart,
  };
}

// --- Scènes -----------------------------------------------------------

export function ajouterScene(histoire: Histoire): Histoire {
  const id = prochainIdScene(histoire.scenes.map((s) => s.id));
  const scene: Scene = { id, titre: "Nouvelle scène", elements: [], fin: true };
  return { ...histoire, scenes: [...histoire.scenes, scene] };
}

export function supprimerScene(histoire: Histoire, sceneId: string): Histoire {
  return { ...histoire, scenes: histoire.scenes.filter((s) => s.id !== sceneId) };
}

export function modifierScene(histoire: Histoire, sceneId: string, patch: Partial<Scene>): Histoire {
  return {
    ...histoire,
    scenes: histoire.scenes.map((s) => (s.id === sceneId ? { ...s, ...patch } : s)),
  };
}

export function deplacerScene(histoire: Histoire, sceneId: string, direction: -1 | 1): Histoire {
  const index = histoire.scenes.findIndex((s) => s.id === sceneId);
  const cible = index + direction;
  if (index === -1 || cible < 0 || cible >= histoire.scenes.length) return histoire;
  const scenes = [...histoire.scenes];
  [scenes[index], scenes[cible]] = [scenes[cible], scenes[index]];
  return { ...histoire, scenes };
}

// --- Éléments -----------------------------------------------------------

function surScene(histoire: Histoire, sceneId: string, transformer: (elements: Element[]) => Element[]): Histoire {
  return {
    ...histoire,
    scenes: histoire.scenes.map((s) => (s.id === sceneId ? nettoyerTerminaison({ ...s, elements: transformer(s.elements) }) : s)),
  };
}

// Si le dernier élément devient (ou cesse d'être) un `choix`, on maintient la
// règle de terminaison automatiquement (format §4 "Règle de terminaison") au
// lieu de laisser un `fin`/`suivant` obsolète qui déclenche une erreur de
// validation invisible pour l'auteur.
function nettoyerTerminaison(scene: Scene): Scene {
  const dernier = scene.elements[scene.elements.length - 1];
  if (dernier?.type === "choix") {
    if (!scene.fin && !scene.suivant) return scene;
    const { fin: _fin, suivant: _suivant, ...reste } = scene;
    return reste as Scene;
  }
  if (!scene.fin && !scene.suivant) {
    return { ...scene, fin: true };
  }
  return scene;
}

export function ajouterElement(histoire: Histoire, sceneId: string, element: Element, apresIndex?: number): Histoire {
  return surScene(histoire, sceneId, (elements) => {
    const position = apresIndex === undefined ? elements.length : apresIndex + 1;
    const copie = [...elements];
    copie.splice(position, 0, element);
    return copie;
  });
}

export function modifierElement(histoire: Histoire, sceneId: string, index: number, element: Element): Histoire {
  return surScene(histoire, sceneId, (elements) => elements.map((e, i) => (i === index ? element : e)));
}

export function supprimerElement(histoire: Histoire, sceneId: string, index: number): Histoire {
  return surScene(histoire, sceneId, (elements) => elements.filter((_, i) => i !== index));
}

export function deplacerElement(histoire: Histoire, sceneId: string, index: number, direction: -1 | 1): Histoire {
  return surScene(histoire, sceneId, (elements) => {
    const cible = index + direction;
    if (cible < 0 || cible >= elements.length) return elements;
    const copie = [...elements];
    [copie[index], copie[cible]] = [copie[cible], copie[index]];
    return copie;
  });
}

// Fusionne l'élément à `index` avec le suivant (concatène le texte).
// Ne s'applique qu'à narration+narration ou dialogue+dialogue du même personnage.
export function fusionnerElements(histoire: Histoire, sceneId: string, index: number): Histoire {
  return surScene(histoire, sceneId, (elements) => {
    const a = elements[index];
    const b = elements[index + 1];
    if (!a || !b) return elements;
    if (a.type === "narration" && b.type === "narration") {
      const fusionne: Element = { type: "narration", texte: `${a.texte} ${b.texte}` };
      return [...elements.slice(0, index), fusionne, ...elements.slice(index + 2)];
    }
    if (a.type === "dialogue" && b.type === "dialogue" && a.personnage === b.personnage) {
      const fusionne: Element = { ...a, texte: `${a.texte} ${b.texte}` };
      return [...elements.slice(0, index), fusionne, ...elements.slice(index + 2)];
    }
    return elements;
  });
}

// Scinde un élément narration/dialogue en deux, coupé au caractère `position`.
export function scinderElement(histoire: Histoire, sceneId: string, index: number, position: number): Histoire {
  return surScene(histoire, sceneId, (elements) => {
    const el = elements[index];
    if (!el || (el.type !== "narration" && el.type !== "dialogue")) return elements;
    const texteA = el.texte.slice(0, position).trim();
    const texteB = el.texte.slice(position).trim();
    if (!texteA || !texteB) return elements;
    const a: Element = { ...el, texte: texteA };
    const b: Element = { ...el, texte: texteB };
    return [...elements.slice(0, index), a, b, ...elements.slice(index + 1)];
  });
}

export function ajouterOptionChoix(histoire: Histoire, sceneId: string, index: number): Histoire {
  return surScene(histoire, sceneId, (elements) =>
    elements.map((e, i) => {
      if (i !== index || e.type !== "choix") return e;
      const choix = e as ElementChoix;
      return {
        ...choix,
        options: [...choix.options, { texte: "Nouvelle option", vers: histoire.scenes[0]?.id ?? "" }],
      };
    })
  );
}

// --- Personnages ---------------------------------------------------------

export function ajouterPersonnage(histoire: Histoire, nom: string): Histoire {
  const id = idUnique(nom, histoire.personnages.map((p) => p.id));
  const personnage: Personnage = { id, nom };
  return { ...histoire, personnages: [...histoire.personnages, personnage] };
}

export function modifierPersonnage(histoire: Histoire, id: string, patch: Partial<Personnage>): Histoire {
  return { ...histoire, personnages: histoire.personnages.map((p) => (p.id === id ? { ...p, ...patch } : p)) };
}

export function supprimerPersonnage(histoire: Histoire, id: string): Histoire {
  return { ...histoire, personnages: histoire.personnages.filter((p) => p.id !== id) };
}

// Fusionne `idSource` dans `idCible` : toutes les répliques de idSource sont
// réattribuées à idCible, puis idSource est supprimé (cahier des charges §1.3
// "Renommer/fusionner des personnages (met à jour toutes les répliques)").
export function fusionnerPersonnages(histoire: Histoire, idSource: string, idCible: string): Histoire {
  if (idSource === idCible) return histoire;
  return {
    ...histoire,
    personnages: histoire.personnages.filter((p) => p.id !== idSource),
    scenes: histoire.scenes.map((s) => ({
      ...s,
      elements: s.elements.map((e) => (e.type === "dialogue" && e.personnage === idSource ? { ...e, personnage: idCible } : e)),
    })),
  };
}
