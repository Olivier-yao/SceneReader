import type { ModeAnalyse } from "../state/config";

interface Props {
  texte: string;
  onChange: (texte: string) => void;
  nbMots: number;
  onAnalyser: () => void;
  analyseEnCours: boolean;
  erreur: string | null;
  dernierCout: string | null;
  modeActif: ModeAnalyse;
}

export default function PanneauTexteSource(props: Props) {
  return (
    <div className="volet">
      <div className="volet-entete">
        <h2>1. Texte source</h2>
        <span className="espace" />
        <button className="principal" onClick={props.onAnalyser} disabled={props.analyseEnCours}>
          {props.analyseEnCours ? "Analyse en cours…" : props.modeActif === "manuel" ? "Mode manuel" : "Analyser"}
        </button>
      </div>
      <div className="volet-corps">
        <textarea
          className="texte-source"
          placeholder="Collez ici votre histoire, écrite comme une nouvelle ou un roman — pas de balise ni de syntaxe spéciale requise…"
          value={props.texte}
          onChange={(e) => props.onChange(e.target.value)}
        />
        {props.erreur && <div className="erreurs-validation" style={{ margin: "10px 0 0" }}>{props.erreur}</div>}
        {props.dernierCout && !props.erreur && (
          <div className="avertissement" style={{ marginTop: 10 }}>
            Dernière analyse : {props.dernierCout}
          </div>
        )}
      </div>
      <div className="stat-ligne">
        <span>{props.nbMots.toLocaleString("fr-FR")} mots</span>
        <span>{props.texte.length.toLocaleString("fr-FR")} caractères</span>
      </div>
    </div>
  );
}
