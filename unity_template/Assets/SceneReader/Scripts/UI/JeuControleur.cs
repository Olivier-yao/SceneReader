using System.Collections;
using SceneReader.Engine;
using SceneReader.Modele;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SceneReader.UI
{
    /// <summary>
    /// Écran de jeu principal : décor, boîte de dialogue, portrait, défilement
    /// lettre à lettre, boutons de choix (cahier des charges §2.2 "Affichage",
    /// "Défilement", "Choix").
    /// </summary>
    public class JeuControleur : MonoBehaviour
    {
        [Header("Décor")]
        public Image imageDecor;
        public Sprite decorParDefaut;

        [Header("Boîte de dialogue")]
        public GameObject panneauDialogue;
        public GameObject panneauNomPersonnage;
        public Text texteNomPersonnage;
        public Text texteDialogue;
        public GameObject panneauPortrait;
        public Image imagePortrait;
        public Button boutonZoneClic;

        [Header("Choix")]
        public GameObject panneauChoix;
        public Text texteQuestionChoix;
        public Transform conteneurChoix;
        public GameObject prefabBoutonChoix;

        [Header("Vitesse de défilement")]
        public Slider curseurVitesse;
        [Tooltip("Caractères par seconde au minimum du curseur")] public float vitesseMin = 10f;
        [Tooltip("Caractères par seconde au maximum du curseur")] public float vitesseMax = 120f;

        [Header("Fin d'histoire")]
        public GameObject panneauFin;
        public Button boutonRetourMenuDepuisFin;

        [Header("Modules")]
        public HistoriqueControleur historique;
        public PanneauSauvegardeControleur panneauSauvegarde;
        public Button boutonOuvrirSauvegarde;
        public Button boutonRetourMenu;

        private const string CLE_PREF_VITESSE = "SceneReader_VitesseTexte";

        private ControleurPartie controleur;
        private Coroutine coroutineRevelation;
        private bool revelationEnCours;
        private float caracteresParSeconde = 40f;

        private void Awake()
        {
            controleur = new ControleurPartie();
            controleur.SurElementAffiche += SurElementAffiche;
            controleur.SurChangementScene += SurChangementScene;
            controleur.SurFinHistoire += SurFinHistoire;

            if (boutonZoneClic != null) boutonZoneClic.onClick.AddListener(SurClicZoneDialogue);
            if (boutonOuvrirSauvegarde != null) boutonOuvrirSauvegarde.onClick.AddListener(() => panneauSauvegarde?.Basculer());
            if (boutonRetourMenu != null) boutonRetourMenu.onClick.AddListener(RetourMenu);
            if (boutonRetourMenuDepuisFin != null) boutonRetourMenuDepuisFin.onClick.AddListener(RetourMenu);

            if (curseurVitesse != null)
            {
                curseurVitesse.value = PlayerPrefs.GetFloat(CLE_PREF_VITESSE, 0.3f);
                curseurVitesse.onValueChanged.AddListener(SurChangementVitesse);
                SurChangementVitesse(curseurVitesse.value);
            }

            panneauSauvegarde?.Initialiser(() => controleur.ObtenirEtatSauvegarde());
        }

        private void Start()
        {
            if (panneauFin != null) panneauFin.SetActive(false);
            if (panneauChoix != null) panneauChoix.SetActive(false);

            string chemin = SelectionPartie.CheminFichierHistoire;
            if (string.IsNullOrEmpty(chemin))
            {
                Debug.LogError("Aucune histoire sélectionnée — retour au menu.");
                RetourMenu();
                return;
            }

            var histoireChargee = ChargeurHistoires.ChargerDepuisFichier(chemin);
            if (SelectionPartie.EmplacementACharger.HasValue)
            {
                var donnees = GestionnaireSauvegarde.Charger(SelectionPartie.EmplacementACharger.Value);
                if (donnees != null) controleur.ChargerPartieSauvegardee(histoireChargee, chemin, donnees);
                else controleur.DemarrerNouvellePartie(histoireChargee, chemin);
            }
            else
            {
                controleur.DemarrerNouvellePartie(histoireChargee, chemin);
            }
        }

        private void SurChangementVitesse(float valeurCurseur)
        {
            caracteresParSeconde = Mathf.Lerp(vitesseMin, vitesseMax, valeurCurseur);
            PlayerPrefs.SetFloat(CLE_PREF_VITESSE, valeurCurseur);
        }

        private void SurChangementScene(SceneHistoire scene)
        {
            var sprite = ChargeurImages.ChargerSprite(scene.decor);
            imageDecor.sprite = sprite != null ? sprite : decorParDefaut;
        }

        private void SurElementAffiche(ElementHistoire element)
        {
            if (element.type == TypeElement.Choix)
            {
                AfficherChoix(element);
            }
            else
            {
                AfficherTexte(element);
            }
        }

        private void AfficherTexte(ElementHistoire element)
        {
            if (panneauChoix != null) panneauChoix.SetActive(false);
            if (panneauDialogue != null) panneauDialogue.SetActive(true);

            bool estDialogue = element.type == TypeElement.Dialogue;
            Personnage personnage = estDialogue ? controleur.HistoireCourante.TrouverPersonnage(element.personnage) : null;

            if (panneauNomPersonnage != null) panneauNomPersonnage.SetActive(estDialogue && personnage != null);
            if (estDialogue && personnage != null && texteNomPersonnage != null)
            {
                texteNomPersonnage.text = personnage.nom;
                texteNomPersonnage.color = personnage.CouleurUnity();
            }

            var spritePortrait = estDialogue && personnage != null ? ChargeurImages.ChargerSprite(personnage.portrait) : null;
            if (panneauPortrait != null) panneauPortrait.SetActive(spritePortrait != null);
            if (imagePortrait != null) imagePortrait.sprite = spritePortrait;

            if (coroutineRevelation != null) StopCoroutine(coroutineRevelation);
            coroutineRevelation = StartCoroutine(RevelerTexte(element.texte));

            historique?.AjouterLigne(personnage?.nom, element.texte);
        }

        private IEnumerator RevelerTexte(string texteComplet)
        {
            revelationEnCours = true;
            texteDialogue.text = "";
            float delai = 1f / Mathf.Max(1f, caracteresParSeconde);
            foreach (char c in texteComplet)
            {
                texteDialogue.text += c;
                yield return new WaitForSeconds(delai);
            }
            revelationEnCours = false;
        }

        private void SurClicZoneDialogue()
        {
            if (panneauChoix != null && panneauChoix.activeSelf) return; // on n'avance pas pendant un choix

            if (revelationEnCours)
            {
                StopCoroutine(coroutineRevelation);
                revelationEnCours = false;
                // Le texte a été construit caractère par caractère dans texteDialogue.text ;
                // on ne connaît le texte complet que via le contrôleur au prochain affichage,
                // donc on relit l'élément courant pour compléter instantanément l'affichage.
                var sceneActuelle = controleur.SceneCourante;
                var elementActuel = sceneActuelle.elements[controleur.IndexElementCourant];
                texteDialogue.text = elementActuel.texte;
                return;
            }

            controleur.Avancer();
        }

        private void AfficherChoix(ElementHistoire choix)
        {
            if (panneauDialogue != null) panneauDialogue.SetActive(false);
            if (panneauChoix != null) panneauChoix.SetActive(true);
            if (texteQuestionChoix != null) texteQuestionChoix.text = choix.question ?? "";

            foreach (Transform enfant in conteneurChoix) Destroy(enfant.gameObject);

            foreach (var option in choix.options)
            {
                var instance = Instantiate(prefabBoutonChoix, conteneurChoix);
                instance.SetActive(true);
                var texte = instance.GetComponentInChildren<Text>();
                if (texte != null) texte.text = option.texte;
                var bouton = instance.GetComponent<Button>();
                var captureOption = option;
                if (bouton != null) bouton.onClick.AddListener(() => controleur.ChoisirOption(captureOption));
            }
        }

        private void SurFinHistoire()
        {
            if (panneauDialogue != null) panneauDialogue.SetActive(false);
            if (panneauChoix != null) panneauChoix.SetActive(false);
            if (panneauFin != null) panneauFin.SetActive(true);
        }

        private const string NOM_SCENE_MENU = "MenuPrincipal";

        private void RetourMenu()
        {
            SceneManager.LoadScene(NOM_SCENE_MENU);
        }
    }
}
