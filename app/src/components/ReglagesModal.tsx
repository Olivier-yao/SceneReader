import type { Config, ModeAnalyse } from "../state/config";
import { TABLE_PRIX } from "../lib/pricing";
import { FOURNISSEURS_OPENAI_COMPATIBLE } from "../providers";

interface Props {
  config: Config;
  setConfig: (fn: (c: Config) => Config) => void;
  onFermer: () => void;
}

const MODES: { id: ModeAnalyse; titre: string; description: string }[] = [
  { id: "manuel", titre: "Mode manuel (zéro API)", description: "Découpage entièrement à la main. Aucune clé requise, l'app est 100% fonctionnelle." },
  { id: "openai-compatible", titre: "Fournisseur gratuit — OpenAI-compatible", description: "Groq, OpenRouter ou tout endpoint /chat/completions compatible OpenAI." },
  { id: "anthropic", titre: "Anthropic (Claude)", description: "Meilleure qualité d'analyse. Payant (voir table de prix ci-dessous)." },
  { id: "gemini", titre: "Google Gemini", description: "Tier gratuit généreux via Google AI Studio." },
];

export default function ReglagesModal({ config, setConfig, onFermer }: Props) {
  return (
    <div className="superposition" onClick={onFermer}>
      <div className="boite-modale" onClick={(e) => e.stopPropagation()}>
        <h2>Réglages</h2>

        <div className="groupe-radio">
          {MODES.map((m) => (
            <label key={m.id} className={`option-radio ${config.modeActif === m.id ? "selectionne" : ""}`}>
              <input type="radio" checked={config.modeActif === m.id} onChange={() => setConfig((c) => ({ ...c, modeActif: m.id }))} />
              <div>
                <strong>{m.titre}</strong>
                <small>{m.description}</small>
              </div>
            </label>
          ))}
        </div>

        {config.modeActif !== "manuel" && (config.modeActif === "openai-compatible" || config.modeActif === "gemini") && !config.avertissementFournisseurGratuitVu && (
          <div className="avertissement">
            Certains fournisseurs gratuits utilisent vos textes pour entraîner leurs modèles. Vérifiez leurs conditions
            avant d'envoyer des œuvres auxquelles vous tenez.{" "}
            <button className="discret" onClick={() => setConfig((c) => ({ ...c, avertissementFournisseurGratuitVu: true }))}>
              Compris
            </button>
          </div>
        )}

        {config.modeActif === "openai-compatible" && (
          <FormulaireOpenAICompatible config={config} setConfig={setConfig} />
        )}
        {config.modeActif === "anthropic" && <FormulaireSimple config={config} setConfig={setConfig} fournisseur="anthropic" modeles={["claude-haiku-4-5", "claude-sonnet-5"]} />}
        {config.modeActif === "gemini" && <FormulaireSimple config={config} setConfig={setConfig} fournisseur="gemini" modeles={["gemini-2.0-flash", "gemini-1.5-pro"]} />}

        <details style={{ marginTop: 16 }}>
          <summary style={{ cursor: "pointer", color: "var(--texte-attenue)" }}>Table de prix (indicative, $ / 1M tokens)</summary>
          <table className="table-prix">
            <thead>
              <tr>
                <th>Modèle</th>
                <th>Entrée</th>
                <th>Sortie</th>
              </tr>
            </thead>
            <tbody>
              {Object.entries({ ...TABLE_PRIX, ...config.prixPersonnalises }).map(([modele, prix]) => (
                <tr key={modele}>
                  <td>{modele}</td>
                  <td>{prix.gratuit ? "gratuit" : prix.entree ?? "?"}</td>
                  <td>{prix.gratuit ? "gratuit" : prix.sortie ?? "?"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </details>

        <p style={{ fontSize: "0.78rem", color: "var(--texte-attenue)", marginTop: 14 }}>
          Les clés API sont stockées uniquement dans le stockage local du navigateur sur cette machine, jamais
          envoyées ailleurs qu'au fournisseur choisi.
        </p>

        <div className="ligne-boutons">
          <button className="principal" onClick={onFermer}>
            Fermer
          </button>
        </div>
      </div>
    </div>
  );
}

type ProprietesConfig = Pick<Props, "config" | "setConfig">;

function FormulaireOpenAICompatible({ config, setConfig }: ProprietesConfig) {
  const reglages = config.fournisseurs["openai-compatible"];
  return (
    <div>
      <div className="champ">
        <label>Fournisseur préconfiguré</label>
        <select
          onChange={(e) => {
            const f = FOURNISSEURS_OPENAI_COMPATIBLE.find((x) => x.id === e.target.value);
            if (!f) return;
            setConfig((c) => ({
              ...c,
              fournisseurs: { ...c.fournisseurs, "openai-compatible": { ...reglages, baseUrl: f.baseUrl, modele: f.modeleParDefaut || reglages.modele } },
            }));
          }}
        >
          {FOURNISSEURS_OPENAI_COMPATIBLE.map((f) => (
            <option key={f.id} value={f.id}>
              {f.nom}
            </option>
          ))}
        </select>
      </div>
      <div className="champ">
        <label>URL de base</label>
        <input
          value={reglages.baseUrl ?? ""}
          onChange={(e) => setConfig((c) => ({ ...c, fournisseurs: { ...c.fournisseurs, "openai-compatible": { ...reglages, baseUrl: e.target.value } } }))}
          placeholder="https://api.groq.com/openai/v1"
        />
      </div>
      <div className="champ">
        <label>Modèle</label>
        <input
          value={reglages.modele}
          onChange={(e) => setConfig((c) => ({ ...c, fournisseurs: { ...c.fournisseurs, "openai-compatible": { ...reglages, modele: e.target.value } } }))}
          placeholder="llama-3.3-70b-versatile"
        />
      </div>
      <div className="champ">
        <label>Clé API</label>
        <input
          type="password"
          value={reglages.apiKey}
          onChange={(e) => setConfig((c) => ({ ...c, fournisseurs: { ...c.fournisseurs, "openai-compatible": { ...reglages, apiKey: e.target.value } } }))}
          placeholder="gsk_… / sk-or-…"
        />
      </div>
    </div>
  );
}

function FormulaireSimple({
  config,
  setConfig,
  fournisseur,
  modeles,
}: ProprietesConfig & { fournisseur: "anthropic" | "gemini"; modeles: string[] }) {
  const reglages = config.fournisseurs[fournisseur];
  return (
    <div>
      <div className="champ">
        <label>Modèle</label>
        <select
          value={reglages.modele}
          onChange={(e) => setConfig((c) => ({ ...c, fournisseurs: { ...c.fournisseurs, [fournisseur]: { ...reglages, modele: e.target.value } } }))}
        >
          {modeles.map((m) => (
            <option key={m} value={m}>
              {m}
            </option>
          ))}
        </select>
      </div>
      <div className="champ">
        <label>Clé API</label>
        <input
          type="password"
          value={reglages.apiKey}
          onChange={(e) => setConfig((c) => ({ ...c, fournisseurs: { ...c.fournisseurs, [fournisseur]: { ...reglages, apiKey: e.target.value } } }))}
          placeholder={fournisseur === "anthropic" ? "sk-ant-…" : "AIza…"}
        />
      </div>
    </div>
  );
}
