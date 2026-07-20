// Backoff exponentiel pour les erreurs 429 (rate limit), cf. cahier des charges §1.2
// "Gestion des quotas : sur erreur 429, attendre et réessayer avec backoff".

export class Erreur429 extends Error {
  constructor(message = "Limite de requêtes atteinte (429)") {
    super(message);
    this.name = "Erreur429";
  }
}

export interface OptionsBackoff {
  tentativesMax?: number;
  delaiBaseMs?: number;
}

export async function avecBackoff<T>(fn: () => Promise<T>, options: OptionsBackoff = {}): Promise<T> {
  const { tentativesMax = 4, delaiBaseMs = 1000 } = options;
  let derniereErreur: unknown;

  for (let tentative = 0; tentative < tentativesMax; tentative++) {
    try {
      return await fn();
    } catch (erreur) {
      derniereErreur = erreur;
      if (!(erreur instanceof Erreur429)) throw erreur;
      if (tentative === tentativesMax - 1) break;
      const delai = delaiBaseMs * 2 ** tentative + Math.random() * 250;
      await new Promise((resolve) => setTimeout(resolve, delai));
    }
  }
  throw derniereErreur;
}
