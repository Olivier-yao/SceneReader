using System.Collections.Generic;
using System.Linq;
using SceneReader.Engine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SceneReader.UI
{
    /// <summary>
    /// Menu principal : liste les histoires trouvées dans StreamingAssets/Histoires,
    /// propose Nouvelle partie / Continuer (cahier des charges §2.2 "Menu principal").
    /// </summary>
    public class MenuPrincipalControleur : MonoBehaviour
    {
        [Header("Liste des histoires")]
        public Transform conteneurListeHistoires;
        public GameObject prefabBoutonHistoire;
        public Text texteAucuneHistoire;

        [Header("Panneau de sélection")]
        public GameObject panneauSelection;
        public Text texteTitreSelection;
        public Button boutonNouvellePartie;
        public Button boutonContinuer;
        public Text texteContinuer;

        public const string NOM_SCENE_JEU = "Jeu";

        private List<ChargeurHistoires.HistoireDisponible> histoiresDisponibles;
        private ChargeurHistoires.HistoireDisponible histoireSelectionnee;

        private void Start()
        {
            histoiresDisponibles = ChargeurHistoires.ListerHistoiresDisponibles();
            ConstruireListe();
            if (panneauSelection != null) panneauSelection.SetActive(false);

            if (boutonNouvellePartie != null) boutonNouvellePartie.onClick.AddListener(DemarrerNouvellePartie);
            if (boutonContinuer != null) boutonContinuer.onClick.AddListener(ContinuerPartie);
        }

        private void ConstruireListe()
        {
            bool aucuneHistoire = histoiresDisponibles.Count == 0;
            if (texteAucuneHistoire != null) texteAucuneHistoire.gameObject.SetActive(aucuneHistoire);
            if (aucuneHistoire) return;

            foreach (var disponible in histoiresDisponibles)
            {
                var instance = Instantiate(prefabBoutonHistoire, conteneurListeHistoires);
                instance.SetActive(true);
                var texte = instance.GetComponentInChildren<Text>();
                if (texte != null)
                {
                    string auteur = string.IsNullOrEmpty(disponible.histoire.auteur) ? "" : $" — {disponible.histoire.auteur}";
                    texte.text = $"{disponible.histoire.titre}{auteur}";
                }
                var bouton = instance.GetComponent<Button>();
                var capture = disponible;
                if (bouton != null) bouton.onClick.AddListener(() => SelectionnerHistoire(capture));
            }
        }

        private void SelectionnerHistoire(ChargeurHistoires.HistoireDisponible disponible)
        {
            histoireSelectionnee = disponible;
            if (panneauSelection != null) panneauSelection.SetActive(true);
            if (texteTitreSelection != null) texteTitreSelection.text = disponible.histoire.titre;

            int? dernierEmplacement = TrouverDernierEmplacement(disponible.cheminFichier);
            bool peutContinuer = dernierEmplacement.HasValue;
            if (boutonContinuer != null) boutonContinuer.interactable = peutContinuer;
            if (texteContinuer != null)
                texteContinuer.text = peutContinuer ? $"Continuer (emplacement {dernierEmplacement.Value})" : "Continuer (aucune sauvegarde)";
        }

        private int? TrouverDernierEmplacement(string cheminFichierHistoire)
        {
            int? meilleur = null;
            string meilleurHorodatage = null;
            for (int emplacement = 1; emplacement <= GestionnaireSauvegarde.NB_EMPLACEMENTS; emplacement++)
            {
                var donnees = GestionnaireSauvegarde.Charger(emplacement);
                if (donnees == null || donnees.histoireCheminFichier != cheminFichierHistoire) continue;
                if (meilleurHorodatage == null || string.CompareOrdinal(donnees.horodatage, meilleurHorodatage) > 0)
                {
                    meilleur = emplacement;
                    meilleurHorodatage = donnees.horodatage;
                }
            }
            return meilleur;
        }

        private void DemarrerNouvellePartie()
        {
            if (histoireSelectionnee == null) return;
            SelectionPartie.CheminFichierHistoire = histoireSelectionnee.cheminFichier;
            SelectionPartie.EmplacementACharger = null;
            SceneManager.LoadScene(NOM_SCENE_JEU);
        }

        private void ContinuerPartie()
        {
            if (histoireSelectionnee == null) return;
            int? emplacement = TrouverDernierEmplacement(histoireSelectionnee.cheminFichier);
            if (!emplacement.HasValue) return;
            SelectionPartie.CheminFichierHistoire = histoireSelectionnee.cheminFichier;
            SelectionPartie.EmplacementACharger = emplacement.Value;
            SceneManager.LoadScene(NOM_SCENE_JEU);
        }
    }
}
