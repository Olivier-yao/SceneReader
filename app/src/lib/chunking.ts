// Découpe un texte long en morceaux avec recouvrement, pour rester sous la
// fenêtre de contexte des modèles (cahier des charges §1.2).
// Découpage sur des frontières de paragraphes pour ne jamais couper une phrase en deux.

export interface Morceau {
  texte: string;
  index: number;
  total: number;
}

const TAILLE_MAX_CARACTERES = 12000; // ~ 3000 tokens, marge confortable pour tous les modèles ciblés
const RECOUVREMENT_CARACTERES = 800;

export function decouperEnMorceaux(
  texte: string,
  tailleMax = TAILLE_MAX_CARACTERES,
  recouvrement = RECOUVREMENT_CARACTERES
): Morceau[] {
  const paragraphes = texte.split(/\n{2,}/);
  const morceauxTexte: string[] = [];
  let courant = "";

  for (const paragraphe of paragraphes) {
    const candidat = courant ? `${courant}\n\n${paragraphe}` : paragraphe;
    if (candidat.length > tailleMax && courant) {
      morceauxTexte.push(courant);
      // recouvrement : on repart avec la fin du morceau précédent pour donner du contexte
      const queue = courant.slice(Math.max(0, courant.length - recouvrement));
      courant = `${queue}\n\n${paragraphe}`;
    } else {
      courant = candidat;
    }
  }
  if (courant) morceauxTexte.push(courant);
  if (morceauxTexte.length === 0) morceauxTexte.push(texte);

  return morceauxTexte.map((t, i) => ({ texte: t, index: i, total: morceauxTexte.length }));
}
