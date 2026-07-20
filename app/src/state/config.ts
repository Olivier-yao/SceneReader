import type { AdaptateurIA } from "../providers/types";
import type { PrixModele } from "../lib/pricing";

export type ModeAnalyse = "manuel" | AdaptateurIA["id"];

export interface ReglagesFournisseur {
  apiKey: string;
  modele: string;
  baseUrl?: string;
}

export interface Config {
  modeActif: ModeAnalyse;
  fournisseurs: Record<AdaptateurIA["id"], ReglagesFournisseur>;
  prixPersonnalises: Record<string, PrixModele>;
  avertissementFournisseurGratuitVu: boolean;
}

export function configParDefaut(): Config {
  return {
    modeActif: "manuel",
    fournisseurs: {
      "openai-compatible": { apiKey: "", modele: "llama-3.3-70b-versatile", baseUrl: "https://api.groq.com/openai/v1" },
      anthropic: { apiKey: "", modele: "claude-haiku-4-5" },
      gemini: { apiKey: "", modele: "gemini-2.0-flash" },
    },
    prixPersonnalises: {},
    avertissementFournisseurGratuitVu: false,
  };
}
