using UnityEngine;

namespace SceneReader.Modele
{
    /// <summary>Miroir de "Personnage" dans format/FORMAT_SCENEREADER.md §2.</summary>
    public class Personnage
    {
        public string id;
        public string nom;
        public string description;
        public string couleurHex;
        public string portrait;

        public Color CouleurUnity()
        {
            if (!string.IsNullOrEmpty(couleurHex) && ColorUtility.TryParseHtmlString(couleurHex, out var couleur))
                return couleur;
            return Color.white;
        }
    }
}
