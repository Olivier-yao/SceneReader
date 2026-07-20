import type { Config } from "./config";
import { configParDefaut } from "./config";
import type { Projet } from "./project";

// Persistance 100 % locale (localStorage du navigateur) — aucune donnée ne
// part sur un serveur (cahier des charges : "pas de compte, pas de cloud").
const CLE_CONFIG = "scenereader:config";
const CLE_INDEX_PROJETS = "scenereader:projets";
const prefixeProjet = (id: string) => `scenereader:projet:${id}`;

export function chargerConfig(): Config {
  try {
    const brut = localStorage.getItem(CLE_CONFIG);
    if (!brut) return configParDefaut();
    return { ...configParDefaut(), ...JSON.parse(brut) };
  } catch {
    return configParDefaut();
  }
}

export function sauvegarderConfig(config: Config): void {
  localStorage.setItem(CLE_CONFIG, JSON.stringify(config));
}

export interface EntreeIndexProjet {
  id: string;
  titre: string;
  modifieLe: string;
}

function chargerIndex(): EntreeIndexProjet[] {
  try {
    const brut = localStorage.getItem(CLE_INDEX_PROJETS);
    return brut ? JSON.parse(brut) : [];
  } catch {
    return [];
  }
}

function sauvegarderIndex(index: EntreeIndexProjet[]): void {
  localStorage.setItem(CLE_INDEX_PROJETS, JSON.stringify(index));
}

export function listerProjets(): EntreeIndexProjet[] {
  return chargerIndex().sort((a, b) => b.modifieLe.localeCompare(a.modifieLe));
}

export function chargerProjet(id: string): Projet | null {
  try {
    const brut = localStorage.getItem(prefixeProjet(id));
    return brut ? JSON.parse(brut) : null;
  } catch {
    return null;
  }
}

export function sauvegarderProjet(projet: Projet): void {
  localStorage.setItem(prefixeProjet(projet.id), JSON.stringify(projet));
  const index = chargerIndex().filter((e) => e.id !== projet.id);
  index.push({ id: projet.id, titre: projet.titre, modifieLe: projet.modifieLe });
  sauvegarderIndex(index);
}

export function supprimerProjet(id: string): void {
  localStorage.removeItem(prefixeProjet(id));
  sauvegarderIndex(chargerIndex().filter((e) => e.id !== id));
}
