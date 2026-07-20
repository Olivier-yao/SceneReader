using System;
using System.Collections.Generic;
using System.IO;
using SceneReader.Json;
using SceneReader.Modele;
using UnityEngine;

namespace SceneReader.Engine
{
    /// <summary>
    /// Liste les histoires déposées dans Assets/StreamingAssets/Histoires
    /// (format/FORMAT_SCENEREADER.md). Implémentation par accès fichier direct,
    /// valable sur PC/Mac/Linux standalone. Une build WebGL (Phase 3) devra
    /// remplacer la lecture par UnityWebRequest, StreamingAssets n'étant pas un
    /// vrai système de fichiers dans un navigateur.
    /// </summary>
    public static class ChargeurHistoires
    {
        public class HistoireDisponible
        {
            public string cheminFichier;
            public Histoire histoire;
        }

        public static string DossierHistoires => Path.Combine(Application.streamingAssetsPath, "Histoires");

        public static List<HistoireDisponible> ListerHistoiresDisponibles()
        {
            var resultat = new List<HistoireDisponible>();
            string dossier = DossierHistoires;
            if (!Directory.Exists(dossier)) return resultat;

            foreach (var chemin in Directory.GetFiles(dossier, "*.scenereader.json"))
            {
                try
                {
                    string texte = File.ReadAllText(chemin);
                    var histoire = HistoireJsonMapper.VersHistoire(texte);
                    resultat.Add(new HistoireDisponible { cheminFichier = chemin, histoire = histoire });
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Histoire ignorée (JSON invalide) : {chemin} — {e.Message}");
                }
            }
            return resultat;
        }

        public static Histoire ChargerDepuisFichier(string cheminFichier)
        {
            return HistoireJsonMapper.VersHistoire(File.ReadAllText(cheminFichier));
        }
    }
}
