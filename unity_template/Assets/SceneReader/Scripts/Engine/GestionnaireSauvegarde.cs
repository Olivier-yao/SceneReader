using System;
using System.Collections.Generic;
using System.IO;
using SceneReader.Json;
using UnityEngine;

namespace SceneReader.Engine
{
    /// <summary>État sauvegardé d'une partie (format/FORMAT_SCENEREADER.md n'impose
    /// pas de schéma pour les sauvegardes : c'est un format interne au player).</summary>
    public class DonneesSauvegarde
    {
        public string histoireCheminFichier;
        public string histoireTitre;
        public string sceneId;
        public int indexElement;
        public Dictionary<string, object> variables;
        public string horodatage;
    }

    /// <summary>
    /// Sauvegarde/charge la partie en JSON dans Application.persistentDataPath,
    /// 3 emplacements (cahier des charges §2.2 "Sauvegarde").
    /// </summary>
    public static class GestionnaireSauvegarde
    {
        public const int NB_EMPLACEMENTS = 3;

        private static string DossierSauvegardes => Path.Combine(Application.persistentDataPath, "Sauvegardes");
        private static string CheminEmplacement(int emplacement) => Path.Combine(DossierSauvegardes, $"slot{emplacement}.json");

        public static void Sauvegarder(int emplacement, DonneesSauvegarde donnees)
        {
            Directory.CreateDirectory(DossierSauvegardes);
            var racine = new Dictionary<string, object>
            {
                ["histoireCheminFichier"] = donnees.histoireCheminFichier,
                ["histoireTitre"] = donnees.histoireTitre,
                ["sceneId"] = donnees.sceneId,
                ["indexElement"] = (double)donnees.indexElement,
                ["horodatage"] = donnees.horodatage,
                ["variables"] = new Dictionary<string, object>(donnees.variables),
            };
            File.WriteAllText(CheminEmplacement(emplacement), JsonEcrivain.Ecrire(racine));
        }

        public static DonneesSauvegarde Charger(int emplacement)
        {
            string chemin = CheminEmplacement(emplacement);
            if (!File.Exists(chemin)) return null;

            if (!(JsonAnalyseur.Analyser(File.ReadAllText(chemin)) is Dictionary<string, object> racine))
                return null;

            return new DonneesSauvegarde
            {
                histoireCheminFichier = racine.TryGetValue("histoireCheminFichier", out var f) ? f as string : null,
                histoireTitre = racine.TryGetValue("histoireTitre", out var t) ? t as string : null,
                sceneId = racine.TryGetValue("sceneId", out var s) ? s as string : null,
                indexElement = racine.TryGetValue("indexElement", out var idx) && idx is double d ? (int)d : 0,
                horodatage = racine.TryGetValue("horodatage", out var h) ? h as string : null,
                variables = racine.TryGetValue("variables", out var v) && v is Dictionary<string, object> vd
                    ? vd
                    : new Dictionary<string, object>(),
            };
        }

        public static bool EmplacementOccupe(int emplacement) => File.Exists(CheminEmplacement(emplacement));

        public static void Supprimer(int emplacement)
        {
            string chemin = CheminEmplacement(emplacement);
            if (File.Exists(chemin)) File.Delete(chemin);
        }
    }
}
