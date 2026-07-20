// Table de prix par modèle, éditable (cahier des charges §1.2 "compteur de coût").
// Prix en USD pour 1 million de tokens. `null` = gratuit (tier gratuit / quota).
// Ces prix sont indicatifs et doivent être vérifiés/ajustés par l'utilisateur
// dans les réglages : les fournisseurs changent leurs tarifs sans préavis.

export interface PrixModele {
  entree: number | null; // $ / 1M tokens en entrée
  sortie: number | null; // $ / 1M tokens en sortie
  gratuit?: boolean;
}

export const TABLE_PRIX: Record<string, PrixModele> = {
  // Anthropic
  "claude-haiku-4-5": { entree: 1, sortie: 5 },
  "claude-sonnet-5": { entree: 3, sortie: 15 },
  // Groq (tier gratuit généreux au moment de l'écriture)
  "llama-3.3-70b-versatile": { entree: 0, sortie: 0, gratuit: true },
  "llama-3.1-8b-instant": { entree: 0, sortie: 0, gratuit: true },
  // OpenRouter (varie fortement selon le modèle choisi ; exemple gratuit)
  "meta-llama/llama-3.3-70b-instruct:free": { entree: 0, sortie: 0, gratuit: true },
  // Google Gemini
  "gemini-2.0-flash": { entree: 0, sortie: 0, gratuit: true },
  "gemini-1.5-pro": { entree: 1.25, sortie: 5 },
};

export function estimerCoutUSD(modele: string, tokensEntree: number, tokensSortie: number): number | null {
  const prix = TABLE_PRIX[modele];
  if (!prix) return null; // modèle inconnu de la table : coût non estimable
  if (prix.gratuit) return 0;
  if (prix.entree == null || prix.sortie == null) return null;
  return (tokensEntree / 1_000_000) * prix.entree + (tokensSortie / 1_000_000) * prix.sortie;
}

export function formaterCout(cout: number | null, gratuit: boolean): string {
  if (gratuit || cout === 0) return "gratuit (quota)";
  if (cout === null) return "coût inconnu (modèle absent de la table de prix)";
  if (cout < 0.01) return "< 0,01 $";
  return `~${cout.toFixed(2).replace(".", ",")} $`;
}
