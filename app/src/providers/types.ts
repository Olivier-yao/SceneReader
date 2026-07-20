import type { Personnage, Scene } from "../types/format";

export interface OptionsAnalyse {
  apiKey: string;
  modele: string;
  /** Requis uniquement par l'adaptateur OpenAI-compatible (Groq, OpenRouter, …). */
  baseUrl?: string;
  /** Personnages déjà connus (chunks précédents / projet en cours), pour garder des id stables. */
  personnagesConnus?: Personnage[];
  /** Coupe court aux tentatives lors des tests unitaires / mode debug. */
  tentativesMax?: number;
}

export interface UsageTokens {
  entree: number;
  sortie: number;
}

export interface ResultatAnalyseIA {
  personnages: Personnage[];
  scenes: Scene[];
  usage: UsageTokens;
}

export interface AdaptateurIA {
  readonly id: "openai-compatible" | "anthropic" | "gemini";
  readonly nom: string;
  analyserHistoire(texte: string, options: OptionsAnalyse): Promise<ResultatAnalyseIA>;
}

export class ErreurQuotaEpuise extends Error {
  constructor(public fournisseur: string) {
    super(`Quota épuisé pour ${fournisseur}`);
    this.name = "ErreurQuotaEpuise";
  }
}

export class ErreurReponseInvalide extends Error {
  constructor(message: string, public brut: string) {
    super(message);
    this.name = "ErreurReponseInvalide";
  }
}
