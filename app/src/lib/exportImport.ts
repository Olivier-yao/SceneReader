import type { Histoire } from "../types/format";
import { validerHistoire } from "./validation";

export interface ResultatExport {
  ok: boolean;
  erreurs: string[];
}

// Vérifie les règles d'intégrité et déclenche le téléchargement du .scenereader.json.
// Refuse d'exporter (ok: false) si l'histoire est invalide — l'utilisateur voit
// la liste d'erreurs et corrige avant de réessayer (cahier des charges §"Critères
// de validation Phase 1" : "exporter un JSON valide").
export function exporterHistoire(histoire: Histoire): ResultatExport {
  const erreurs = validerHistoire(histoire);
  if (erreurs.length > 0) {
    return { ok: false, erreurs: erreurs.map((e) => `${e.scene ? `[${e.scene}] ` : ""}${e.message}`) };
  }

  const nomFichier = `${slugFichier(histoire.titre)}.scenereader.json`;
  const blob = new Blob([JSON.stringify(histoire, null, 2)], { type: "application/json" });
  const url = URL.createObjectURL(blob);
  const lien = document.createElement("a");
  lien.href = url;
  lien.download = nomFichier;
  document.body.appendChild(lien);
  lien.click();
  document.body.removeChild(lien);
  URL.revokeObjectURL(url);

  return { ok: true, erreurs: [] };
}

function slugFichier(titre: string): string {
  return (
    titre
      .normalize("NFD")
      .replace(/[̀-ͯ]/g, "")
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, "_")
      .replace(/^_+|_+$/g, "") || "histoire"
  );
}

export async function importerHistoire(fichier: File): Promise<Histoire> {
  const texte = await fichier.text();
  let donnees: unknown;
  try {
    donnees = JSON.parse(texte);
  } catch (e) {
    throw new Error(`Fichier JSON illisible : ${(e as Error).message}`);
  }
  if (
    typeof donnees !== "object" ||
    donnees === null ||
    !Array.isArray((donnees as Histoire).scenes) ||
    !("scene_depart" in donnees)
  ) {
    throw new Error("Ce fichier ne ressemble pas à un .scenereader.json valide (champs scenes/scene_depart manquants).");
  }
  return donnees as Histoire;
}
