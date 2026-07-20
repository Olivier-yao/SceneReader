import { analyserAvecAppel, gererStatutHTTP } from "./orchestrerAnalyse";
import type { AdaptateurIA, OptionsAnalyse, ResultatAnalyseIA } from "./types";

// Adaptateur "OpenAI-compatible" : couvre Groq, OpenRouter et tout fournisseur
// exposant l'API /chat/completions au format OpenAI. URL de base + modèle +
// clé sont configurables dans les réglages (cahier des charges §1.2, adaptateur prioritaire).
export const adaptateurOpenAICompatible: AdaptateurIA = {
  id: "openai-compatible",
  nom: "OpenAI-compatible (Groq, OpenRouter, …)",

  async analyserHistoire(texte: string, options: OptionsAnalyse): Promise<ResultatAnalyseIA> {
    if (!options.baseUrl) {
      throw new Error("URL de base manquante pour l'adaptateur OpenAI-compatible (réglages > fournisseur).");
    }
    const baseUrl = options.baseUrl.replace(/\/+$/, "");

    return analyserAvecAppel(texte, options, async (prompt, opts) => {
      const reponse = await fetch(`${baseUrl}/chat/completions`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${opts.apiKey}`,
        },
        body: JSON.stringify({
          model: opts.modele,
          messages: [
            { role: "system", content: prompt.systeme },
            { role: "user", content: prompt.utilisateur },
          ],
          temperature: 0.3,
          response_format: { type: "json_object" },
        }),
      });

      if (!reponse.ok) {
        gererStatutHTTP(reponse.status, await reponse.text(), "OpenAI-compatible");
      }

      const donnees = await reponse.json();
      const texteReponse: string = donnees.choices?.[0]?.message?.content ?? "";
      const usage = {
        entree: donnees.usage?.prompt_tokens ?? 0,
        sortie: donnees.usage?.completion_tokens ?? 0,
      };
      return { texte: texteReponse, usage };
    });
  },
};
