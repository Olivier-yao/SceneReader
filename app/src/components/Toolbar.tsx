import { useRef } from "react";
import type { Projet } from "../state/project";
import type { EntreeIndexProjet } from "../state/storage";
import type { ModeAnalyse } from "../state/config";

interface Props {
  projet: Projet;
  listeProjets: EntreeIndexProjet[];
  onRenommer: (titre: string) => void;
  onNouveau: () => void;
  onOuvrir: (id: string) => void;
  onSupprimer: (id: string) => void;
  onSauvegarder: () => void;
  onExporter: () => void;
  onImporter: (fichier: File) => void;
  onOuvrirReglages: () => void;
  modeActif: ModeAnalyse;
}

const LIBELLE_MODE: Record<ModeAnalyse, string> = {
  manuel: "Mode manuel",
  "openai-compatible": "OpenAI-compatible",
  anthropic: "Anthropic",
  gemini: "Gemini",
};

export default function Toolbar(props: Props) {
  const inputFichier = useRef<HTMLInputElement>(null);

  return (
    <div className="barre-outils">
      <span className="titre-app">SceneReader</span>
      <input
        className="titre-projet"
        value={projetTitreAffiche(props.projet.titre)}
        onChange={(e) => props.onRenommer(e.target.value)}
        aria-label="Titre du projet"
      />
      <button className="discret" onClick={props.onNouveau} title="Nouveau projet">
        + Nouveau
      </button>
      <select
        aria-label="Ouvrir un projet"
        value=""
        onChange={(e) => {
          if (e.target.value) props.onOuvrir(e.target.value);
        }}
      >
        <option value="">Ouvrir un projet…</option>
        {props.listeProjets.map((p) => (
          <option key={p.id} value={p.id}>
            {p.titre} — {new Date(p.modifieLe).toLocaleString("fr-FR")}
          </option>
        ))}
      </select>
      {props.listeProjets.some((p) => p.id === props.projet.id) && (
        <button
          className="discret"
          title="Supprimer ce projet"
          onClick={() => {
            if (confirm(`Supprimer définitivement le projet « ${props.projet.titre} » ?`)) {
              props.onSupprimer(props.projet.id);
            }
          }}
        >
          Supprimer
        </button>
      )}
      <button onClick={props.onSauvegarder} title="Sauvegarder maintenant (auto-save toutes les 30s)">
        Sauvegarder
      </button>

      <span className="espace" />

      <span className="badge">{LIBELLE_MODE[props.modeActif]}</span>
      <button onClick={props.onOuvrirReglages}>Réglages</button>
      <button
        onClick={() => inputFichier.current?.click()}
        title="Importer un .scenereader.json"
      >
        Importer
      </button>
      <input
        ref={inputFichier}
        type="file"
        accept=".json,.scenereader.json,application/json"
        style={{ display: "none" }}
        onChange={(e) => {
          const fichier = e.target.files?.[0];
          if (fichier) props.onImporter(fichier);
          e.target.value = "";
        }}
      />
      <button className="principal" onClick={props.onExporter}>
        Exporter .scenereader.json
      </button>
    </div>
  );
}

function projetTitreAffiche(titre: string): string {
  return titre || "Sans titre";
}
