import { analyserAvecAppel, gererStatutHTTP } from "./orchestrerAnalyse";
import type { AdaptateurIA, OptionsAnalyse, ResultatAnalyseIA } from "./types";

// Adaptateur Google Gemini (API AI Studio, tier gratuit généreux — cahier des charges §1.2).
export const adaptateurGemini: AdaptateurIA = {
  id: "gemini",
  nom: "Google Gemini",

  async analyserHistoire(texte: string, options: OptionsAnalyse): Promise<ResultatAnalyseIA> {
    return analyserAvecAppel(texte, options, async (prompt, opts) => {
      const url = `https://generativelanguage.googleapis.com/v1beta/models/${opts.modele}:generateContent?key=${opts.apiKey}`;
      const reponse = await fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          systemInstruction: { parts: [{ text: prompt.systeme }] },
          contents: [{ role: "user", parts: [{ text: prompt.utilisateur }] }],
          generationConfig: {
            temperature: 0.3,
            responseMimeType: "application/json",
          },
        }),
      });

      if (!reponse.ok) {
        gererStatutHTTP(reponse.status, await reponse.text(), "Gemini");
      }

      const donnees = await reponse.json();
      const texteReponse: string =
        donnees.candidates?.[0]?.content?.parts?.map((p: { text?: string }) => p.text ?? "").join("") ?? "";
      const usage = {
        entree: donnees.usageMetadata?.promptTokenCount ?? 0,
        sortie: donnees.usageMetadata?.candidatesTokenCount ?? 0,
      };
      return { texte: texteReponse, usage };
    });
  },
};
