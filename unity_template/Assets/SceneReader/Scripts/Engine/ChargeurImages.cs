using System.IO;
using UnityEngine;

namespace SceneReader.Engine
{
    /// <summary>
    /// Charge au runtime une image déposée par l'auteur à côté de son
    /// .scenereader.json (cahier des charges §2.1 : "L'utilisateur dépose
    /// MonHistoire.scenereader.json (et les images éventuelles) dans
    /// Assets/StreamingAssets/Histoires/"). Les chemins `decor`/`portrait` du
    /// format pivot sont donc résolus depuis ce dossier Histoires/, ex.
    /// "decors/maison_nuit.png" → Assets/StreamingAssets/Histoires/decors/maison_nuit.png.
    /// </summary>
    public static class ChargeurImages
    {
        public static Sprite ChargerSprite(string cheminRelatif)
        {
            if (string.IsNullOrEmpty(cheminRelatif)) return null;

            string chemin = Path.Combine(ChargeurHistoires.DossierHistoires, cheminRelatif);
            if (!File.Exists(chemin))
            {
                Debug.LogWarning($"Image introuvable, décor/portrait par défaut utilisé : {chemin}");
                return null;
            }

            byte[] octets = File.ReadAllBytes(chemin);
            var texture = new Texture2D(2, 2);
            if (!ImageConversion.LoadImage(texture, octets))
            {
                Debug.LogWarning($"Image illisible : {chemin}");
                return null;
            }
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
    }
}
