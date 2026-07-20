using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace SceneReader.UI
{
    /// <summary>Journal des répliques déjà lues, affiché/masqué par la touche H
    /// (cahier des charges §2.2 "Historique").</summary>
    public class HistoriqueControleur : MonoBehaviour
    {
        public GameObject panneau;
        public Text texteContenu;
        public ScrollRect scrollRect;

        private readonly List<string> lignes = new List<string>();

        private void Awake()
        {
            if (panneau != null) panneau.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.H))
            {
                Basculer();
            }
        }

        public void AjouterLigne(string nomPersonnageOuNull, string texte)
        {
            string ligne = string.IsNullOrEmpty(nomPersonnageOuNull) ? texte : $"{nomPersonnageOuNull} : {texte}";
            lignes.Add(ligne);
            RafraichirTexte();
        }

        public void Basculer()
        {
            if (panneau == null) return;
            bool visible = !panneau.activeSelf;
            panneau.SetActive(visible);
            if (visible) RafraichirTexte();
        }

        private void RafraichirTexte()
        {
            if (texteContenu == null) return;
            var sb = new StringBuilder();
            foreach (var ligne in lignes) sb.AppendLine(ligne).AppendLine();
            texteContenu.text = sb.ToString();
            if (scrollRect != null) Canvas.ForceUpdateCanvases();
            if (scrollRect != null) scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
