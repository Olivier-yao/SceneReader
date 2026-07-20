import type { Personnage } from "../types/format";
import { decouperEnMorceaux } from "../lib/chunking";
import { avecBackoff, Erreur429 } from "../lib/retry";
import { construirePromptAnalyse, renforcerConsigne, type PromptAnalyse } from "../lib/prompt";
import { analyserReponseIA, chainerScenes } from "../lib/parseReponseIA";
import { ErreurReponseInvalide } from "./types";
import type { OptionsAnalyse, ResultatAnalyseIA, UsageTokens } from "./types";

export type AppelModele = (
  prompt: PromptAnalyse,
  options: OptionsAnalyse
) => Promise<{ texte: string; usage: UsageTokens }>;

// Logique commune aux trois adaptateurs : découpage en morceaux, appel du
// modèle avec backoff sur 429, re-tentative avec consigne renforcée si la
// réponse n'est pas un JSON valide, fusion des personnages/scènes des
// différents morceaux, puis enchaînement des scènes sans transition explicite.
export async function analyserAvecAppel(
  texte: string,
  options: OptionsAnalyse,
  appelerModele: AppelModele
): Promise<ResultatAnalyseIA> {
  const morceaux = decouperEnMorceaux(texte);
  const personnagesAccumules: Personnage[] = [...(options.personnagesConnus ?? [])];
  const scenesAccumulees: ResultatAnalyseIA["scenes"] = [];
  const usageTotal: UsageTokens = { entree: 0, sortie: 0 };

  for (const morceau of morceaux) {
    const promptInitial = construirePromptAnalyse(morceau.texte, personnagesAccumules, morceau.index, morceau.total);

    const { personnages, scenes, usage } = await avecBackoff(
      () => appelerUneFois(promptInitial, options, appelerModele),
      { tentativesMax: options.tentativesMax ?? 4 }
    );

    usageTotal.entree += usage.entree;
    usageTotal.sortie += usage.sortie;

    for (const p of personnages) {
      const existeDeja = personnagesAccumules.some(
        (existant) => existant.id === p.id || existant.nom.toLowerCase() === p.nom.toLowerCase()
      );
      if (!existeDeja) personnagesAccumules.push(p);
    }
    scenesAccumulees.push(...scenes);
  }

  return {
    personnages: personnagesAccumules,
    scenes: chainerScenes(scenesAccumulees),
    usage: usageTotal,
  };
}

async function appelerUneFois(
  prompt: PromptAnalyse,
  options: OptionsAnalyse,
  appelerModele: AppelModele
): Promise<{ personnages: Personnage[]; scenes: ResultatAnalyseIA["scenes"]; usage: UsageTokens }> {
  const { texte, usage } = await appelerModele(prompt, options);
  try {
    const { personnages, scenes } = analyserReponseIA(texte);
    return { personnages, scenes, usage };
  } catch (e) {
    if (!(e instanceof ErreurReponseInvalide)) throw e;
    // Une seule re-tentative avec consigne renforcée (cahier des charges §"Contraintes techniques").
    const promptRenforce = renforcerConsigne(prompt, e.message);
    const seconde = await appelerModele(promptRenforce, options);
    const { personnages, scenes } = analyserReponseIA(seconde.texte);
    return {
      personnages,
      scenes,
      usage: { entree: usage.entree + seconde.usage.entree, sortie: usage.sortie + seconde.usage.sortie },
    };
  }
}

export function gererStatutHTTP(statut: number, texteErreur: string, fournisseur: string): never {
  if (statut === 429) throw new Erreur429(`${fournisseur} : quota/limite atteint (429)`);
  throw new Error(`${fournisseur} a répondu ${statut} : ${texteErreur}`);
}
