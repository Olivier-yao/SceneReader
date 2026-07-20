import type { Histoire } from "../types/format";
import { nouvelleHistoireVide } from "../types/format";

export interface Projet {
  id: string;
  titre: string;
  texteSource: string;
  histoire: Histoire;
  creeLe: string; // ISO
  modifieLe: string; // ISO
}

export function nouveauProjet(titre = "Nouvelle histoire"): Projet {
  const maintenant = new Date().toISOString();
  return {
    id: `projet_${Date.now().toString(36)}_${Math.random().toString(36).slice(2, 8)}`,
    titre,
    texteSource: "",
    histoire: nouvelleHistoireVide(titre),
    creeLe: maintenant,
    modifieLe: maintenant,
  };
}
