import { analyserAvecAppel, gererStatutHTTP } from "./orchestrerAnalyse";
import type { AdaptateurIA, OptionsAnalyse, ResultatAnalyseIA } from "./types";

// Adaptateur Anthropic (API Claude native). Modèle par défaut : Haiku,
// option Sonnet pour les histoires complexes (cahier des charges §1.2).
//
// Note CORS : l'API Anthropic est appelée directement depuis le navigateur.
// Cela nécessite l'en-tête "anthropic-dangerous-direct-browser-access" et
// expose la clé API dans le code client — acceptable ici car l'app tourne en
// local sur la machine de l'utilisateur (pas de déploiement public), comme
// précisé au cahier des charges. Voir app/README.md pour le détail.
export const adaptateurAnthropic: AdaptateurIA = {
  id: "anthropic",
  nom: "Anthropic (Claude)",

  async analyserHistoire(texte: string, options: OptionsAnalyse): Promise<ResultatAnalyseIA> {
    return analyserAvecAppel(texte, options, async (prompt, opts) => {
      const reponse = await fetch("https://api.anthropic.com/v1/messages", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "x-api-key": opts.apiKey,
          "anthropic-version": "2023-06-01",
          "anthropic-dangerous-direct-browser-access": "true",
        },
        body: JSON.stringify({
          model: opts.modele,
          max_tokens: 8192,
          system: prompt.systeme,
          messages: [{ role: "user", content: prompt.utilisateur }],
        }),
      });

      if (!reponse.ok) {
        gererStatutHTTP(reponse.status, await reponse.text(), "Anthropic");
      }

      const donnees = await reponse.json();
      const texteReponse: string = donnees.content?.map((bloc: { text?: string }) => bloc.text ?? "").join("") ?? "";
      const usage = {
        entree: donnees.usage?.input_tokens ?? 0,
        sortie: donnees.usage?.output_tokens ?? 0,
      };
      return { texte: texteReponse, usage };
    });
  },
};
