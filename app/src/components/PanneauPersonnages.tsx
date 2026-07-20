import { useState } from "react";
import type { Histoire, TypeVariable, ValeurVariable } from "../types/format";
import { ajouterPersonnage, fusionnerPersonnages, modifierPersonnage, supprimerPersonnage } from "../state/histoireActions";
import { idUnique } from "../lib/id";

interface Props {
  histoire: Histoire;
  setHistoire: (fn: (h: Histoire) => Histoire) => void;
}

export default function PanneauPersonnages({ histoire, setHistoire }: Props) {
  const [nomNouveau, setNomNouveau] = useState("");
  const [source, setSource] = useState("");
  const [cible, setCible] = useState("");

  function ajouter() {
    if (!nomNouveau.trim()) return;
    setHistoire((h) => ajouterPersonnage(h, nomNouveau.trim()));
    setNomNouveau("");
  }

  function fusionner() {
    if (!source || !cible || source === cible) return;
    setHistoire((h) => fusionnerPersonnages(h, source, cible));
    setSource("");
    setCible("");
  }

  return (
    <div className="volet">
      <div className="volet-entete">
        <h2>3. Personnages</h2>
      </div>
      <div className="volet-corps">
        <div className="liste-personnages">
          {histoire.personnages.length === 0 && <p className="vide">Aucun personnage détecté pour l'instant.</p>}
          {histoire.personnages.map((p) => (
            <div className="carte-personnage" key={p.id}>
              <div className="carte-personnage-entete">
                <span className="pastille-couleur" style={{ background: p.couleur || "#888" }} />
                <input value={p.nom} onChange={(e) => setHistoire((h) => modifierPersonnage(h, p.id, { nom: e.target.value }))} style={{ flex: 1, fontWeight: 600 }} />
                <button className="discret" onClick={() => setHistoire((h) => supprimerPersonnage(h, p.id))} title="Supprimer">
                  ✕
                </button>
              </div>
              <code style={{ fontSize: "0.72rem", color: "var(--texte-attenue)" }}>id : {p.id}</code>
              <textarea
                placeholder="Description (note d'auteur, non affichée en jeu)"
                value={p.description ?? ""}
                onChange={(e) => setHistoire((h) => modifierPersonnage(h, p.id, { description: e.target.value }))}
                style={{ minHeight: 40 }}
              />
              <div style={{ display: "flex", gap: 6 }}>
                <input
                  type="color"
                  value={p.couleur || "#e8734a"}
                  onChange={(e) => setHistoire((h) => modifierPersonnage(h, p.id, { couleur: e.target.value }))}
                  style={{ width: 40, padding: 2 }}
                />
                <input
                  placeholder="Chemin du portrait (optionnel)"
                  value={p.portrait ?? ""}
                  onChange={(e) => setHistoire((h) => modifierPersonnage(h, p.id, { portrait: e.target.value || undefined }))}
                  style={{ flex: 1 }}
                />
              </div>
            </div>
          ))}
        </div>

        <div style={{ display: "flex", gap: 6, marginTop: 10 }}>
          <input placeholder="Nom du nouveau personnage" value={nomNouveau} onChange={(e) => setNomNouveau(e.target.value)} onKeyDown={(e) => e.key === "Enter" && ajouter()} />
          <button onClick={ajouter}>+ Ajouter</button>
        </div>

        {histoire.personnages.length > 1 && (
          <div className="zone-fusion">
            <span>Fusionner</span>
            <select value={source} onChange={(e) => setSource(e.target.value)}>
              <option value="">— personnage —</option>
              {histoire.personnages.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.nom}
                </option>
              ))}
            </select>
            <span>dans</span>
            <select value={cible} onChange={(e) => setCible(e.target.value)}>
              <option value="">— personnage —</option>
              {histoire.personnages.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.nom}
                </option>
              ))}
            </select>
            <button className="discret" onClick={fusionner} disabled={!source || !cible || source === cible}>
              Fusionner
            </button>
          </div>
        )}

        <VariablesSection histoire={histoire} setHistoire={setHistoire} />
      </div>
    </div>
  );
}

function VariablesSection({ histoire, setHistoire }: Props) {
  const [nom, setNom] = useState("");
  const [type, setType] = useState<TypeVariable>("bool");

  function ajouter() {
    if (!nom.trim()) return;
    const id = idUnique(nom.trim(), histoire.variables.map((v) => v.id));
    const defaut: ValeurVariable = type === "bool" ? false : type === "nombre" ? 0 : "";
    setHistoire((h) => ({ ...h, variables: [...h.variables, { id, type, defaut }] }));
    setNom("");
  }

  return (
    <div style={{ marginTop: 20, paddingTop: 14, borderTop: "1px solid var(--bordure)" }}>
      <h2 style={{ fontSize: "0.9rem", color: "var(--texte-attenue)", textTransform: "uppercase", margin: "0 0 8px" }}>Variables</h2>
      {histoire.variables.length === 0 && <p className="vide">Aucune variable. Ajoutez-en une pour piloter des conditions/choix.</p>}
      {histoire.variables.map((v) => (
        <div key={v.id} className="option-choix" style={{ marginBottom: 6 }}>
          <code style={{ flex: 1 }}>{v.id}</code>
          <span className="badge">{v.type}</span>
          <button
            className="discret"
            onClick={() => setHistoire((h) => ({ ...h, variables: h.variables.filter((x) => x.id !== v.id) }))}
          >
            ✕
          </button>
        </div>
      ))}
      <div style={{ display: "flex", gap: 6, marginTop: 6 }}>
        <input placeholder="nom_variable" value={nom} onChange={(e) => setNom(e.target.value)} onKeyDown={(e) => e.key === "Enter" && ajouter()} />
        <select value={type} onChange={(e) => setType(e.target.value as TypeVariable)}>
          <option value="bool">bool</option>
          <option value="nombre">nombre</option>
          <option value="texte">texte</option>
        </select>
        <button onClick={ajouter}>+</button>
      </div>
    </div>
  );
}
