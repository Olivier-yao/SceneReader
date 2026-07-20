import { useEffect, useMemo, useRef, useState } from "react";
import type { Config } from "./state/config";
import {
  chargerConfig,
  sauvegarderConfig,
  chargerProjet,
  sauvegarderProjet,
  listerProjets,
  supprimerProjet,
  type EntreeIndexProjet,
} from "./state/storage";
import { nouveauProjet, type Projet } from "./state/project";
import type { Histoire } from "./types/format";
import { obtenirAdaptateur, ErreurQuotaEpuise, ErreurReponseInvalide } from "./providers";
import { Erreur429 } from "./lib/retry";
import { estimerCoutUSD, formaterCout, TABLE_PRIX } from "./lib/pricing";
import { validerHistoire } from "./lib/validation";
import { exporterHistoire, importerHistoire } from "./lib/exportImport";
import { appliquerResultatAnalyse } from "./state/histoireActions";
import Toolbar from "./components/Toolbar";
import PanneauTexteSource from "./components/PanneauTexteSource";
import PanneauDecoupage from "./components/PanneauDecoupage";
import PanneauPersonnages from "./components/PanneauPersonnages";
import ReglagesModal from "./components/ReglagesModal";

const CLE_DERNIER_PROJET = "scenereader:dernier_projet";

export default function App() {
  const [config, setConfig] = useState<Config>(() => chargerConfig());
  const [projet, setProjetInterne] = useState<Projet>(() => {
    const dernierId = localStorage.getItem(CLE_DERNIER_PROJET);
    const existant = dernierId ? chargerProjet(dernierId) : null;
    return existant ?? nouveauProjet();
  });
  const [listeProjets, setListeProjets] = useState<EntreeIndexProjet[]>(() => listerProjets());
  const [reglagesOuverts, setReglagesOuverts] = useState(false);
  const [analyseEnCours, setAnalyseEnCours] = useState(false);
  const [erreurAnalyse, setErreurAnalyse] = useState<string | null>(null);
  const [dernierCout, setDernierCout] = useState<string | null>(null);
  const [erreursExport, setErreursExport] = useState<string[]>([]);
  const sale = useRef(false);

  useEffect(() => {
    sauvegarderConfig(config);
  }, [config]);

  useEffect(() => {
    localStorage.setItem(CLE_DERNIER_PROJET, projet.id);
    sale.current = true;
  }, [projet]);

  // Auto-save toutes les 30 s (cahier des charges §1.3).
  useEffect(() => {
    const intervalle = setInterval(() => {
      if (sale.current) {
        const misAJour = { ...projet, modifieLe: new Date().toISOString() };
        sauvegarderProjet(misAJour);
        setListeProjets(listerProjets());
        sale.current = false;
      }
    }, 30000);
    return () => clearInterval(intervalle);
  }, [projet]);

  function setProjet(prochain: Projet | ((p: Projet) => Projet)) {
    setProjetInterne(prochain);
  }

  function sauvegarderMaintenant() {
    const misAJour = { ...projet, modifieLe: new Date().toISOString() };
    sauvegarderProjet(misAJour);
    setProjetInterne(misAJour);
    setListeProjets(listerProjets());
    sale.current = false;
  }

  function setHistoire(prochain: Histoire | ((h: Histoire) => Histoire)) {
    setProjetInterne((p) => ({
      ...p,
      histoire: typeof prochain === "function" ? (prochain as (h: Histoire) => Histoire)(p.histoire) : prochain,
    }));
  }

  function setTexteSource(texte: string) {
    setProjetInterne((p) => ({ ...p, texteSource: texte }));
  }

  function renommerProjet(titre: string) {
    setProjetInterne((p) => ({ ...p, titre, histoire: { ...p.histoire, titre } }));
  }

  const erreursLive = useMemo(() => validerHistoire(projet.histoire), [projet.histoire]);

  async function lancerAnalyse() {
    if (config.modeActif === "manuel") {
      setErreurAnalyse(
        "Mode manuel actif : découpez le texte à la main dans le panneau « Découpage » (aucune clé API requise). Changez de mode dans les réglages pour utiliser l'IA."
      );
      return;
    }
    const reglages = config.fournisseurs[config.modeActif];
    if (!reglages.apiKey) {
      setErreurAnalyse("Aucune clé API configurée pour ce fournisseur. Ouvrez les réglages, ou passez en mode manuel.");
      return;
    }
    if (!projet.texteSource.trim()) {
      setErreurAnalyse("Collez d'abord du texte dans le panneau « Texte source ».");
      return;
    }
    setErreurAnalyse(null);
    setAnalyseEnCours(true);
    try {
      const adaptateur = obtenirAdaptateur(config.modeActif);
      const resultat = await adaptateur.analyserHistoire(projet.texteSource, {
        apiKey: reglages.apiKey,
        modele: reglages.modele,
        baseUrl: reglages.baseUrl,
        personnagesConnus: projet.histoire.personnages,
      });
      setHistoire((h) => appliquerResultatAnalyse(h, resultat));
      const prix = { ...TABLE_PRIX, ...config.prixPersonnalises };
      const estGratuit = prix[reglages.modele]?.gratuit ?? false;
      const cout = estimerCoutUSD(reglages.modele, resultat.usage.entree, resultat.usage.sortie);
      setDernierCout(`${formaterCout(cout, estGratuit)} · ${resultat.usage.entree + resultat.usage.sortie} tokens`);
    } catch (e) {
      if (e instanceof Erreur429) {
        setErreurAnalyse(
          "Quota épuisé pour ce fournisseur. Attendez quelques instants, changez de fournisseur dans les réglages, ou passez en mode manuel."
        );
      } else if (e instanceof ErreurReponseInvalide) {
        setErreurAnalyse(`Le modèle n'a pas renvoyé un JSON exploitable après plusieurs tentatives : ${e.message}`);
      } else if (e instanceof ErreurQuotaEpuise) {
        setErreurAnalyse(e.message);
      } else {
        setErreurAnalyse(`Erreur pendant l'analyse : ${(e as Error).message}`);
      }
    } finally {
      setAnalyseEnCours(false);
    }
  }

  function gererExport() {
    const resultat = exporterHistoire(projet.histoire);
    setErreursExport(resultat.erreurs);
  }

  async function gererImport(fichier: File) {
    try {
      const histoire = await importerHistoire(fichier);
      setHistoire(histoire);
      setErreursExport([]);
      setErreurAnalyse(null);
    } catch (e) {
      setErreurAnalyse((e as Error).message);
    }
  }

  function nouveauProjetHandler() {
    sauvegarderMaintenant();
    setProjetInterne(nouveauProjet());
    setDernierCout(null);
    setErreurAnalyse(null);
    setErreursExport([]);
  }

  function ouvrirProjetHandler(id: string) {
    sauvegarderMaintenant();
    const p = chargerProjet(id);
    if (p) {
      setProjetInterne(p);
      setDernierCout(null);
      setErreurAnalyse(null);
      setErreursExport([]);
    }
  }

  function supprimerProjetHandler(id: string) {
    supprimerProjet(id);
    setListeProjets(listerProjets());
    if (id === projet.id) setProjetInterne(nouveauProjet());
  }

  const nbMots = projet.texteSource.trim() ? projet.texteSource.trim().split(/\s+/).length : 0;

  return (
    <div className="app">
      <Toolbar
        projet={projet}
        listeProjets={listeProjets}
        onRenommer={renommerProjet}
        onNouveau={nouveauProjetHandler}
        onOuvrir={ouvrirProjetHandler}
        onSupprimer={supprimerProjetHandler}
        onSauvegarder={sauvegarderMaintenant}
        onExporter={gererExport}
        onImporter={gererImport}
        onOuvrirReglages={() => setReglagesOuverts(true)}
        modeActif={config.modeActif}
      />

      {erreursExport.length > 0 && (
        <div className="erreurs-validation">
          <strong>Export impossible — {erreursExport.length} erreur(s) à corriger :</strong>
          <ul>
            {erreursExport.map((e, i) => (
              <li key={i}>{e}</li>
            ))}
          </ul>
        </div>
      )}

      <div className="volets">
        <PanneauTexteSource
          texte={projet.texteSource}
          onChange={setTexteSource}
          nbMots={nbMots}
          onAnalyser={lancerAnalyse}
          analyseEnCours={analyseEnCours}
          erreur={erreurAnalyse}
          dernierCout={dernierCout}
          modeActif={config.modeActif}
        />
        <PanneauDecoupage histoire={projet.histoire} setHistoire={setHistoire} erreurs={erreursLive} />
        <PanneauPersonnages histoire={projet.histoire} setHistoire={setHistoire} />
      </div>

      {reglagesOuverts && (
        <ReglagesModal config={config} setConfig={setConfig} onFermer={() => setReglagesOuverts(false)} />
      )}
    </div>
  );
}
