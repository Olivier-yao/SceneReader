import type { Element, Histoire } from "../types/format";
import type { ErreurValidation } from "../lib/validation";
import {
  ajouterElement,
  ajouterOptionChoix,
  ajouterScene,
  deplacerElement,
  deplacerScene,
  fusionnerElements,
  modifierElement,
  modifierScene,
  scinderElement,
  supprimerElement,
  supprimerScene,
} from "../state/histoireActions";

interface Props {
  histoire: Histoire;
  setHistoire: (fn: (h: Histoire) => Histoire) => void;
  erreurs: ErreurValidation[];
}

export default function PanneauDecoupage({ histoire, setHistoire, erreurs }: Props) {
  const erreursParScene = (id: string) => erreurs.filter((e) => e.scene === id);
  const erreursGlobales = erreurs.filter((e) => !e.scene);

  return (
    <div className="volet">
      <div className="volet-entete">
        <h2>2. Découpage</h2>
        <span className="espace" />
        <button onClick={() => setHistoire((h) => ajouterScene(h))}>+ Scène</button>
      </div>
      <div className="volet-corps">
        {erreursGlobales.length > 0 && (
          <div className="erreurs-validation">
            <ul>
              {erreursGlobales.map((e, i) => (
                <li key={i}>{e.message}</li>
              ))}
            </ul>
          </div>
        )}
        {histoire.scenes.length === 0 && <p className="vide">Aucune scène. Collez du texte et lancez l'analyse, ou ajoutez une scène.</p>}
        {histoire.scenes.map((scene, iScene) => {
          const erreursScene = erreursParScene(scene.id);
          return (
            <div className="carte-scene" key={scene.id}>
              <div className="carte-scene-entete">
                <input
                  className="scene-titre"
                  value={scene.titre ?? ""}
                  placeholder={scene.id}
                  onChange={(e) => setHistoire((h) => modifierScene(h, scene.id, { titre: e.target.value }))}
                />
                <code style={{ fontSize: "0.75rem", color: "var(--texte-attenue)" }}>{scene.id}</code>
                <button className="discret" title="Monter" disabled={iScene === 0} onClick={() => setHistoire((h) => deplacerScene(h, scene.id, -1))}>
                  ↑
                </button>
                <button
                  className="discret"
                  title="Descendre"
                  disabled={iScene === histoire.scenes.length - 1}
                  onClick={() => setHistoire((h) => deplacerScene(h, scene.id, 1))}
                >
                  ↓
                </button>
                <button className="discret" title="Supprimer la scène" onClick={() => setHistoire((h) => supprimerScene(h, scene.id))}>
                  ✕
                </button>
              </div>
              <div className="carte-scene-corps">
                <input
                  placeholder="Chemin du décor (optionnel), ex. decors/maison_nuit.png"
                  value={scene.decor ?? ""}
                  onChange={(e) => setHistoire((h) => modifierScene(h, scene.id, { decor: e.target.value || undefined }))}
                />

                {scene.elements.map((el, iEl) => (
                  <ElementLigne
                    key={iEl}
                    histoire={histoire}
                    scene={scene}
                    element={el}
                    index={iEl}
                    estDernier={iEl === scene.elements.length - 1}
                    setHistoire={setHistoire}
                  />
                ))}

                <AjouterElementMenu onAjouter={(el) => setHistoire((h) => ajouterElement(h, scene.id, el))} />

                <TerminaisonScene histoire={histoire} scene={scene} setHistoire={setHistoire} />

                {erreursScene.length > 0 && (
                  <div className="erreurs-validation">
                    <ul>
                      {erreursScene.map((e, i) => (
                        <li key={i}>{e.message}</li>
                      ))}
                    </ul>
                  </div>
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

function TerminaisonScene({
  histoire,
  scene,
  setHistoire,
}: {
  histoire: Histoire;
  scene: Histoire["scenes"][number];
  setHistoire: (fn: (h: Histoire) => Histoire) => void;
}) {
  const finitParChoix = scene.elements[scene.elements.length - 1]?.type === "choix";
  if (finitParChoix) {
    return <p className="etiquette-type">Cette scène se termine par le choix ci-dessus.</p>;
  }
  return (
    <div className="option-choix">
      <label style={{ display: "flex", gap: 4, alignItems: "center" }}>
        <input
          type="radio"
          checked={!!scene.fin}
          onChange={() => setHistoire((h) => modifierScene(h, scene.id, { fin: true, suivant: undefined }))}
        />
        Fin de l'histoire
      </label>
      <label style={{ display: "flex", gap: 4, alignItems: "center" }}>
        <input
          type="radio"
          checked={!scene.fin}
          onChange={() => setHistoire((h) => modifierScene(h, scene.id, { fin: undefined, suivant: h.scenes.find((s) => s.id !== scene.id)?.id }))}
        />
        Scène suivante :
      </label>
      {!scene.fin && (
        <select
          value={scene.suivant ?? ""}
          onChange={(e) => setHistoire((h) => modifierScene(h, scene.id, { suivant: e.target.value || undefined }))}
        >
          <option value="">— choisir —</option>
          {histoire.scenes
            .filter((s) => s.id !== scene.id)
            .map((s) => (
              <option key={s.id} value={s.id}>
                {s.titre || s.id}
              </option>
            ))}
        </select>
      )}
    </div>
  );
}

function AjouterElementMenu({ onAjouter }: { onAjouter: (el: Element) => void }) {
  return (
    <div style={{ display: "flex", gap: 6, flexWrap: "wrap" }}>
      <button className="discret" onClick={() => onAjouter({ type: "narration", texte: "" })}>+ Narration</button>
      <button className="discret" onClick={() => onAjouter({ type: "dialogue", personnage: "", texte: "" })}>+ Dialogue</button>
      <button className="discret" onClick={() => onAjouter({ type: "set", variable: "", valeur: false })}>+ Set</button>
      <button className="discret" onClick={() => onAjouter({ type: "choix", options: [] })}>+ Choix</button>
    </div>
  );
}

function ElementLigne({
  histoire,
  scene,
  element,
  index,
  estDernier,
  setHistoire,
}: {
  histoire: Histoire;
  scene: Histoire["scenes"][number];
  element: Element;
  index: number;
  estDernier: boolean;
  setHistoire: (fn: (h: Histoire) => Histoire) => void;
}) {
  const actionsCommunes = (
    <div className="element-actions">
      <button className="discret" title="Monter" disabled={index === 0} onClick={() => setHistoire((h) => deplacerElement(h, scene.id, index, -1))}>
        ↑
      </button>
      <button className="discret" title="Descendre" disabled={estDernier} onClick={() => setHistoire((h) => deplacerElement(h, scene.id, index, 1))}>
        ↓
      </button>
      <button className="discret" title="Supprimer" onClick={() => setHistoire((h) => supprimerElement(h, scene.id, index))}>
        ✕
      </button>
    </div>
  );

  if (element.type === "narration") {
    return (
      <div className="element-ligne narration">
        <div className="element-corps">
          <span className="etiquette-type">Narration</span>
          <textarea value={element.texte} onChange={(e) => setHistoire((h) => modifierElement(h, scene.id, index, { type: "narration", texte: e.target.value }))} />
          <div style={{ display: "flex", gap: 6 }}>
            <button
              className="discret"
              onClick={() => setHistoire((h) => modifierElement(h, scene.id, index, { type: "dialogue", personnage: histoire.personnages[0]?.id ?? "", texte: element.texte }))}
            >
              → Convertir en dialogue
            </button>
            {!estDernier && (
              <button className="discret" onClick={() => setHistoire((h) => fusionnerElements(h, scene.id, index))}>
                Fusionner avec suivant
              </button>
            )}
            <button className="discret" onClick={() => setHistoire((h) => scinderElement(h, scene.id, index, Math.floor(element.texte.length / 2)))}>
              Scinder en deux
            </button>
          </div>
        </div>
        {actionsCommunes}
      </div>
    );
  }

  if (element.type === "dialogue") {
    return (
      <div className="element-ligne dialogue">
        <div className="element-corps">
          <span className="etiquette-type">Dialogue</span>
          <select
            value={element.personnage}
            onChange={(e) => setHistoire((h) => modifierElement(h, scene.id, index, { ...element, personnage: e.target.value }))}
          >
            <option value="">— personnage —</option>
            {histoire.personnages.map((p) => (
              <option key={p.id} value={p.id}>
                {p.nom}
              </option>
            ))}
          </select>
          <textarea value={element.texte} onChange={(e) => setHistoire((h) => modifierElement(h, scene.id, index, { ...element, texte: e.target.value }))} />
          <div style={{ display: "flex", gap: 6 }}>
            <button
              className="discret"
              onClick={() => setHistoire((h) => modifierElement(h, scene.id, index, { type: "narration", texte: element.texte }))}
            >
              → Convertir en narration
            </button>
            {!estDernier && (
              <button className="discret" onClick={() => setHistoire((h) => fusionnerElements(h, scene.id, index))}>
                Fusionner avec suivant
              </button>
            )}
            <button className="discret" onClick={() => setHistoire((h) => scinderElement(h, scene.id, index, Math.floor(element.texte.length / 2)))}>
              Scinder en deux
            </button>
          </div>
        </div>
        {actionsCommunes}
      </div>
    );
  }

  if (element.type === "set") {
    return (
      <div className="element-ligne set">
        <div className="element-corps">
          <span className="etiquette-type">Affectation (set)</span>
          <div style={{ display: "flex", gap: 6 }}>
            <select
              value={element.variable}
              onChange={(e) => setHistoire((h) => modifierElement(h, scene.id, index, { ...element, variable: e.target.value }))}
            >
              <option value="">— variable —</option>
              {histoire.variables.map((v) => (
                <option key={v.id} value={v.id}>
                  {v.id} ({v.type})
                </option>
              ))}
            </select>
            <ValeurVariableInput
              histoire={histoire}
              variableId={element.variable}
              valeur={element.valeur}
              onChange={(valeur) => setHistoire((h) => modifierElement(h, scene.id, index, { ...element, valeur }))}
            />
          </div>
        </div>
        {actionsCommunes}
      </div>
    );
  }

  if (element.type === "choix") {
    return (
      <div className="element-ligne choix">
        <span className="etiquette-type">Choix (dernier élément de la scène)</span>
        <input
          placeholder="Question posée au joueur (optionnel)"
          value={element.question ?? ""}
          onChange={(e) => setHistoire((h) => modifierElement(h, scene.id, index, { ...element, question: e.target.value || undefined }))}
        />
        {element.options.map((opt, iOpt) => (
          <div className="option-choix" key={iOpt}>
            <input
              type="text"
              placeholder="Texte de l'option"
              value={opt.texte}
              onChange={(e) =>
                setHistoire((h) =>
                  modifierElement(h, scene.id, index, {
                    ...element,
                    options: element.options.map((o, i) => (i === iOpt ? { ...o, texte: e.target.value } : o)),
                  })
                )
              }
            />
            <select
              value={opt.vers}
              onChange={(e) =>
                setHistoire((h) =>
                  modifierElement(h, scene.id, index, {
                    ...element,
                    options: element.options.map((o, i) => (i === iOpt ? { ...o, vers: e.target.value } : o)),
                  })
                )
              }
            >
              <option value="">— scène cible —</option>
              {histoire.scenes.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.titre || s.id}
                </option>
              ))}
            </select>
            <input
              type="text"
              placeholder="Condition (optionnel), ex. a_menti == true"
              value={opt.condition ?? ""}
              onChange={(e) =>
                setHistoire((h) =>
                  modifierElement(h, scene.id, index, {
                    ...element,
                    options: element.options.map((o, i) => (i === iOpt ? { ...o, condition: e.target.value || undefined } : o)),
                  })
                )
              }
            />
            <button
              className="discret"
              onClick={() =>
                setHistoire((h) =>
                  modifierElement(h, scene.id, index, { ...element, options: element.options.filter((_, i) => i !== iOpt) })
                )
              }
            >
              ✕
            </button>
          </div>
        ))}
        <div style={{ display: "flex", gap: 6 }}>
          <button className="discret" onClick={() => setHistoire((h) => ajouterOptionChoix(h, scene.id, index))}>
            + Option
          </button>
          {actionsCommunes}
        </div>
      </div>
    );
  }

  // suggestion_choix : proposition IA à valider par l'auteur
  return (
    <div className="element-ligne suggestion_choix">
      <div className="element-corps">
        <span className="etiquette-type">Suggestion de choix (IA) — à valider</span>
        <p style={{ margin: 0 }}>{element.note}</p>
        <div style={{ display: "flex", gap: 6 }}>
          <button
            className="discret"
            onClick={() =>
              setHistoire((h) =>
                modifierElement(h, scene.id, index, {
                  type: "choix",
                  question: element.note,
                  options: [
                    { texte: "Option A", vers: histoire.scenes[0]?.id ?? "" },
                    { texte: "Option B", vers: histoire.scenes[0]?.id ?? "" },
                  ],
                })
              )
            }
          >
            Convertir en choix
          </button>
        </div>
      </div>
      {actionsCommunes}
    </div>
  );
}

function ValeurVariableInput({
  histoire,
  variableId,
  valeur,
  onChange,
}: {
  histoire: Histoire;
  variableId: string;
  valeur: boolean | number | string;
  onChange: (v: boolean | number | string) => void;
}) {
  const variable = histoire.variables.find((v) => v.id === variableId);
  if (!variable || variable.type === "bool") {
    return (
      <select value={String(!!valeur)} onChange={(e) => onChange(e.target.value === "true")}>
        <option value="true">vrai</option>
        <option value="false">faux</option>
      </select>
    );
  }
  if (variable.type === "nombre") {
    return <input type="number" value={Number(valeur)} onChange={(e) => onChange(Number(e.target.value))} />;
  }
  return <input type="text" value={String(valeur)} onChange={(e) => onChange(e.target.value)} />;
}
