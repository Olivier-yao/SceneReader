import { adaptateurOpenAICompatible } from "./openaiCompatible";
import { adaptateurAnthropic } from "./anthropic";
import { adaptateurGemini } from "./gemini";
import type { AdaptateurIA } from "./types";

export type { AdaptateurIA, OptionsAnalyse, ResultatAnalyseIA, UsageTokens } from "./types";
export { ErreurQuotaEpuise, ErreurReponseInvalide } from "./types";

export const ADAPTATEURS: Record<AdaptateurIA["id"], AdaptateurIA> = {
  "openai-compatible": adaptateurOpenAICompatible,
  anthropic: adaptateurAnthropic,
  gemini: adaptateurGemini,
};

export function obtenirAdaptateur(id: AdaptateurIA["id"]): AdaptateurIA {
  return ADAPTATEURS[id];
}

// Fournisseurs préconfigurés pour l'adaptateur OpenAI-compatible : n'importe
// quel autre point d'accès compatible /chat/completions peut être saisi
// librement dans les réglages (URL de base personnalisée).
export const FOURNISSEURS_OPENAI_COMPATIBLE = [
  { id: "groq", nom: "Groq", baseUrl: "https://api.groq.com/openai/v1", modeleParDefaut: "llama-3.3-70b-versatile" },
  { id: "openrouter", nom: "OpenRouter", baseUrl: "https://openrouter.ai/api/v1", modeleParDefaut: "meta-llama/llama-3.3-70b-instruct:free" },
  { id: "personnalise", nom: "Autre (URL personnalisée)", baseUrl: "", modeleParDefaut: "" },
] as const;
