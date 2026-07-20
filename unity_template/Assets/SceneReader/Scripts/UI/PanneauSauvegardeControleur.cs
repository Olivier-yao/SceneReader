using System;
using SceneReader.Engine;
using UnityEngine;
using UnityEngine.UI;

namespace SceneReader.UI
{
    /// <summary>Panneau in-game listant les 3 emplacements de sauvegarde
    /// (cahier des charges §2.2 "Sauvegarde : … 3 emplacements").</summary>
    public class PanneauSauvegardeControleur : MonoBehaviour
    {
        [Serializable]
        public class LigneEmplacement
        {
            public Button bouton;
            public Text texte;
        }

        public GameObject panneau;
        public LigneEmplacement[] emplacements; // taille 3, index 0 → emplacement 1

        private Func<DonneesSauvegarde> fournirEtatCourant;

        private void Awake()
        {
            if (panneau != null) panneau.SetActive(false);
        }

        public void Initialiser(Func<DonneesSauvegarde> fournirEtatCourant)
        {
            this.fournirEtatCourant = fournirEtatCourant;
            for (int i = 0; i < emplacements.Length; i++)
            {
                int numeroEmplacement = i + 1;
                emplacements[i].bouton.onClick.AddListener(() => SauvegarderDans(numeroEmplacement));
            }
        }

        public void Ouvrir()
        {
            if (panneau != null) panneau.SetActive(true);
            RafraichirTextesEmplacements();
        }

        public void Fermer()
        {
            if (panneau != null) panneau.SetActive(false);
        }

        public void Basculer()
        {
            if (panneau == null) return;
            if (panneau.activeSelf) Fermer(); else Ouvrir();
        }

        private void SauvegarderDans(int emplacement)
        {
            if (fournirEtatCourant == null) return;
            GestionnaireSauvegarde.Sauvegarder(emplacement, fournirEtatCourant());
            RafraichirTextesEmplacements();
        }

        private void RafraichirTextesEmplacements()
        {
            for (int i = 0; i < emplacements.Length; i++)
            {
                int numeroEmplacement = i + 1;
                var donnees = GestionnaireSauvegarde.Charger(numeroEmplacement);
                emplacements[i].texte.text = donnees == null
                    ? $"Emplacement {numeroEmplacement} — vide"
                    : $"Emplacement {numeroEmplacement} — {donnees.histoireTitre}";
            }
        }
    }
}
