using System.Collections.Generic;
using System.IO;
using SceneReader.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SceneReader.EditeurOutils
{
    /// <summary>
    /// Construit par code les deux scènes du player (MenuPrincipal, Jeu), les
    /// prefabs de boutons dynamiques, et les enregistre dans les Build
    /// Settings. Exécutable depuis le menu Editor OU en ligne de commande :
    /// Unity.exe -batchmode -quit -projectPath &lt;proj&gt;
    ///   -executeMethod SceneReader.EditeurOutils.SceneReaderSceneBuilder.ConstruireTout
    /// </summary>
    public static class SceneReaderSceneBuilder
    {
        private const string DOSSIER_SCENES = "Assets/SceneReader/Scenes";
        private const string DOSSIER_PREFABS = "Assets/SceneReader/Prefabs";

        private static readonly Color CouleurFondSombre = new Color(0.10f, 0.10f, 0.13f, 1f);
        private static readonly Color CouleurPanneau = new Color(0.16f, 0.16f, 0.20f, 0.96f);
        private static readonly Color CouleurBouton = new Color(0.24f, 0.24f, 0.30f, 1f);
        private static readonly Color CouleurTexte = new Color(0.93f, 0.93f, 0.95f, 1f);
        private static readonly Color CouleurTexteAttenue = new Color(0.65f, 0.65f, 0.70f, 1f);
        private static readonly Color CouleurAccent = new Color(0.91f, 0.45f, 0.29f, 1f);

        [MenuItem("SceneReader/Construire les scènes du player")]
        public static void ConstruireTout()
        {
            Directory.CreateDirectory(DOSSIER_SCENES);
            Directory.CreateDirectory(DOSSIER_PREFABS);

            var prefabBoutonHistoire = ConstruirePrefabBouton("BoutonHistoire", TextAnchor.MiddleLeft);
            var prefabBoutonChoix = ConstruirePrefabBouton("BoutonChoix", TextAnchor.MiddleCenter);

            ConstruireSceneMenu(prefabBoutonHistoire);
            ConstruireSceneJeu(prefabBoutonChoix);

            EnregistrerScenesDansBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("SceneReader : scènes MenuPrincipal.unity et Jeu.unity construites avec succès.");
        }

        // ------------------------------------------------------------------
        // Scène Menu
        // ------------------------------------------------------------------

        private static void ConstruireSceneMenu(GameObject prefabBoutonHistoire)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreerCameraRequisePourOverlay();
            CreerEventSystem();
            var canvas = CreerCanvasRacine("Canvas");

            var fond = CreerPanel(canvas.transform, "Fond", CouleurFondSombre);
            Etirer(fond.GetComponent<RectTransform>());

            var titre = CreerTexte(canvas.transform, "Titre", "SceneReader", 48, TextAnchor.MiddleCenter, CouleurAccent, FontStyle.Bold);
            var rtTitre = titre.GetComponent<RectTransform>();
            AncrerHaut(rtTitre, 0.5f, 1f, new Vector2(0, -70), new Vector2(800, 80));

            // Liste des histoires (ScrollView)
            var scrollView = CreerScrollView(canvas.transform, "ListeHistoires", out var conteneurListe);
            var rtScroll = scrollView.GetComponent<RectTransform>();
            AncrerCentre(rtScroll, new Vector2(0, 40), new Vector2(640, 420));
            var layoutListe = conteneurListe.gameObject.AddComponent<VerticalLayoutGroup>();
            layoutListe.spacing = 8;
            layoutListe.childForceExpandHeight = false;
            layoutListe.childControlHeight = false;
            layoutListe.padding = new RectOffset(4, 4, 4, 4);
            var fitterListe = conteneurListe.gameObject.AddComponent<ContentSizeFitter>();
            fitterListe.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var texteAucune = CreerTexte(canvas.transform, "AucuneHistoire",
                "Aucune histoire trouvée dans Assets/StreamingAssets/Histoires/.\nDéposez-y un fichier .scenereader.json (exporté depuis l'app Phase 1).",
                22, TextAnchor.MiddleCenter, CouleurTexteAttenue, FontStyle.Normal);
            AncrerCentre(texteAucune.GetComponent<RectTransform>(), Vector2.zero, new Vector2(700, 120));

            // Panneau de sélection (Nouvelle partie / Continuer)
            var panneauSelection = CreerPanel(canvas.transform, "PanneauSelection", CouleurPanneau);
            AncrerBas(panneauSelection.GetComponent<RectTransform>(), 0.5f, 0f, new Vector2(0, 30), new Vector2(680, 160));

            var texteTitreSelection = CreerTexte(panneauSelection.transform, "TitreSelection", "", 26, TextAnchor.MiddleCenter, CouleurTexte, FontStyle.Bold);
            AncrerHaut(texteTitreSelection.GetComponent<RectTransform>(), 0.5f, 1f, new Vector2(0, -30), new Vector2(640, 40));

            var boutonNouvelle = CreerBouton(panneauSelection.transform, "BoutonNouvellePartie", "Nouvelle partie");
            PositionnerBas(boutonNouvelle.GetComponent<RectTransform>(), new Vector2(-165, 24), new Vector2(300, 60));

            var boutonContinuer = CreerBouton(panneauSelection.transform, "BoutonContinuer", "Continuer");
            PositionnerBas(boutonContinuer.GetComponent<RectTransform>(), new Vector2(165, 24), new Vector2(300, 60));
            var texteContinuer = boutonContinuer.GetComponentInChildren<Text>();

            // Câblage du contrôleur
            var racine = new GameObject("MenuPrincipalControleur");
            var controleur = racine.AddComponent<MenuPrincipalControleur>();
            controleur.conteneurListeHistoires = conteneurListe;
            controleur.prefabBoutonHistoire = prefabBoutonHistoire;
            controleur.texteAucuneHistoire = texteAucune.GetComponent<Text>();
            controleur.panneauSelection = panneauSelection;
            controleur.texteTitreSelection = texteTitreSelection.GetComponent<Text>();
            controleur.boutonNouvellePartie = boutonNouvelle.GetComponent<Button>();
            controleur.boutonContinuer = boutonContinuer.GetComponent<Button>();
            controleur.texteContinuer = texteContinuer;

            EditorSceneManager.SaveScene(scene, $"{DOSSIER_SCENES}/MenuPrincipal.unity");
        }

        // ------------------------------------------------------------------
        // Scène Jeu
        // ------------------------------------------------------------------

        private static void ConstruireSceneJeu(GameObject prefabBoutonChoix)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreerEventSystem();
            var canvas = CreerCanvasRacine("Canvas");

            // --- Décor plein écran ---
            var decor = CreerImage(canvas.transform, "Decor");
            Etirer(decor.GetComponent<RectTransform>());
            decor.GetComponent<Image>().color = CouleurFondSombre;
            decor.GetComponent<Image>().preserveAspect = false;

            // --- Barre du haut : retour menu / sauvegarde ---
            var boutonRetourMenu = CreerBouton(canvas.transform, "BoutonRetourMenu", "Menu");
            AncrerHaut(boutonRetourMenu.GetComponent<RectTransform>(), 0f, 1f, new Vector2(90, -30), new Vector2(140, 44));

            var boutonOuvrirSauvegarde = CreerBouton(canvas.transform, "BoutonSauvegarder", "Sauvegarder");
            AncrerHaut(boutonOuvrirSauvegarde.GetComponent<RectTransform>(), 1f, 1f, new Vector2(-110, -30), new Vector2(160, 44));

            // --- Panneau de choix (question + boutons), au-dessus de la boîte de dialogue ---
            var panneauChoix = CreerPanel(canvas.transform, "PanneauChoix", CouleurPanneau);
            AncrerBas(panneauChoix.GetComponent<RectTransform>(), 0.5f, 0f, new Vector2(0, 40), new Vector2(900, 420));
            var texteQuestion = CreerTexte(panneauChoix.transform, "Question", "", 26, TextAnchor.MiddleCenter, CouleurTexte, FontStyle.Bold);
            AncrerHaut(texteQuestion.GetComponent<RectTransform>(), 0.5f, 1f, new Vector2(0, -20), new Vector2(840, 50));
            var conteneurChoix = CreerPanel(panneauChoix.transform, "ConteneurOptions", new Color(0, 0, 0, 0));
            AncrerCentre(conteneurChoix.GetComponent<RectTransform>(), new Vector2(0, -20), new Vector2(840, 320));
            var layoutChoix = conteneurChoix.AddComponent<VerticalLayoutGroup>();
            layoutChoix.spacing = 10;
            layoutChoix.childForceExpandHeight = false;
            layoutChoix.childControlHeight = false;
            layoutChoix.childAlignment = TextAnchor.UpperCenter;

            // --- Boîte de dialogue (décor par-dessus, choix par-dessus elle) ---
            var panneauDialogue = CreerPanel(canvas.transform, "PanneauDialogue", CouleurPanneau);
            AncrerBas(panneauDialogue.GetComponent<RectTransform>(), 0.5f, 0f, new Vector2(0, 30), new Vector2(1500, 260));

            var boutonZoneClic = panneauDialogue.AddComponent<Button>();
            boutonZoneClic.transition = Selectable.Transition.None;

            var panneauPortrait = CreerPanel(panneauDialogue.transform, "PanneauPortrait", new Color(0, 0, 0, 0));
            AncrerGauche(panneauPortrait.GetComponent<RectTransform>(), new Vector2(20, 0), new Vector2(180, 220));
            var imagePortrait = CreerImage(panneauPortrait.transform, "ImagePortrait");
            Etirer(imagePortrait.GetComponent<RectTransform>());
            imagePortrait.GetComponent<Image>().preserveAspect = true;
            imagePortrait.GetComponent<Image>().raycastTarget = false;

            var panneauNom = CreerPanel(panneauDialogue.transform, "PanneauNomPersonnage", new Color(0, 0, 0, 0));
            AncrerHaut(panneauNom.GetComponent<RectTransform>(), 0f, 1f, new Vector2(220, -18), new Vector2(400, 34));
            var texteNom = CreerTexte(panneauNom.transform, "TexteNom", "", 24, TextAnchor.MiddleLeft, CouleurAccent, FontStyle.Bold);
            Etirer(texteNom.GetComponent<RectTransform>());
            texteNom.GetComponent<Text>().raycastTarget = false;

            var texteDialogue = CreerTexte(panneauDialogue.transform, "TexteDialogue", "", 24, TextAnchor.UpperLeft, CouleurTexte, FontStyle.Normal);
            var rtTexteDialogue = texteDialogue.GetComponent<RectTransform>();
            rtTexteDialogue.anchorMin = new Vector2(0, 0);
            rtTexteDialogue.anchorMax = new Vector2(1, 1);
            rtTexteDialogue.offsetMin = new Vector2(220, 16);
            rtTexteDialogue.offsetMax = new Vector2(-20, -56);
            var composantTexteDialogue = texteDialogue.GetComponent<Text>();
            composantTexteDialogue.raycastTarget = false;
            composantTexteDialogue.horizontalOverflow = HorizontalWrapMode.Wrap;
            composantTexteDialogue.verticalOverflow = VerticalWrapMode.Overflow;

            // --- Curseur de vitesse de défilement ---
            var panneauVitesse = CreerPanel(canvas.transform, "PanneauVitesse", new Color(0, 0, 0, 0));
            AncrerHaut(panneauVitesse.GetComponent<RectTransform>(), 0.5f, 1f, new Vector2(0, -30), new Vector2(300, 40));
            var texteVitesse = CreerTexte(panneauVitesse.transform, "Label", "Vitesse du texte", 16, TextAnchor.MiddleLeft, CouleurTexteAttenue, FontStyle.Normal);
            AncrerGauche(texteVitesse.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(140, 30));
            var curseurGO = new GameObject("Curseur", typeof(RectTransform));
            curseurGO.transform.SetParent(panneauVitesse.transform, false);
            AncrerDroite(curseurGO.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(140, 20));
            var curseur = ConstruireSlider(curseurGO);

            // --- Écran de fin ---
            var panneauFin = CreerPanel(canvas.transform, "PanneauFin", CouleurFondSombre);
            Etirer(panneauFin.GetComponent<RectTransform>());
            var texteFin = CreerTexte(panneauFin.transform, "TexteFin", "Fin de l'histoire", 40, TextAnchor.MiddleCenter, CouleurTexte, FontStyle.Bold);
            AncrerCentre(texteFin.GetComponent<RectTransform>(), new Vector2(0, 40), new Vector2(700, 80));
            var boutonRetourFin = CreerBouton(panneauFin.transform, "BoutonRetourMenu", "Retour au menu");
            AncrerCentre(boutonRetourFin.GetComponent<RectTransform>(), new Vector2(0, -40), new Vector2(260, 56));

            // --- Historique (touche H) ---
            var panneauHistorique = CreerPanel(canvas.transform, "PanneauHistorique", CouleurPanneau);
            AncrerCentre(panneauHistorique.GetComponent<RectTransform>(), Vector2.zero, new Vector2(1000, 700));
            var scrollHistorique = CreerScrollView(panneauHistorique.transform, "Scroll", out var contenuHistorique);
            Etirer(scrollHistorique.GetComponent<RectTransform>());
            var texteHistorique = CreerTexte(contenuHistorique, "Texte", "", 20, TextAnchor.UpperLeft, CouleurTexte, FontStyle.Normal);
            var rtTexteHisto = texteHistorique.GetComponent<RectTransform>();
            rtTexteHisto.anchorMin = new Vector2(0, 1);
            rtTexteHisto.anchorMax = new Vector2(1, 1);
            rtTexteHisto.pivot = new Vector2(0.5f, 1f);
            rtTexteHisto.sizeDelta = new Vector2(0, 2000);
            var composantTexteHisto = texteHistorique.GetComponent<Text>();
            composantTexteHisto.horizontalOverflow = HorizontalWrapMode.Wrap;
            composantTexteHisto.verticalOverflow = VerticalWrapMode.Overflow;
            var fitterHisto = texteHistorique.AddComponent<ContentSizeFitter>();
            fitterHisto.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var rectScrollRect = scrollHistorique.GetComponent<ScrollRect>();
            var historiqueGO = new GameObject("HistoriqueControleur");
            var historiqueControleur = historiqueGO.AddComponent<HistoriqueControleur>();
            historiqueControleur.panneau = panneauHistorique;
            historiqueControleur.texteContenu = composantTexteHisto;
            historiqueControleur.scrollRect = rectScrollRect;

            // --- Panneau de sauvegarde (3 emplacements) ---
            var panneauSauvegarde = CreerPanel(canvas.transform, "PanneauSauvegarde", CouleurPanneau);
            AncrerCentre(panneauSauvegarde.GetComponent<RectTransform>(), Vector2.zero, new Vector2(560, 300));
            var lignes = new PanneauSauvegardeControleur.LigneEmplacement[3];
            for (int i = 0; i < 3; i++)
            {
                var boutonSlot = CreerBouton(panneauSauvegarde.transform, $"Emplacement{i + 1}", $"Emplacement {i + 1} — vide");
                AncrerHaut(boutonSlot.GetComponent<RectTransform>(), 0.5f, 1f, new Vector2(0, -30 - i * 80), new Vector2(500, 60));
                lignes[i] = new PanneauSauvegardeControleur.LigneEmplacement
                {
                    bouton = boutonSlot.GetComponent<Button>(),
                    texte = boutonSlot.GetComponentInChildren<Text>(),
                };
            }
            var sauvegardeGO = new GameObject("PanneauSauvegardeControleur");
            var panneauSauvegardeControleur = sauvegardeGO.AddComponent<PanneauSauvegardeControleur>();
            panneauSauvegardeControleur.panneau = panneauSauvegarde;
            panneauSauvegardeControleur.emplacements = lignes;

            // --- Câblage du contrôleur de jeu ---
            var racineJeu = new GameObject("JeuControleur");
            var jeu = racineJeu.AddComponent<JeuControleur>();
            jeu.imageDecor = decor.GetComponent<Image>();
            jeu.decorParDefaut = null;
            jeu.panneauDialogue = panneauDialogue;
            jeu.panneauNomPersonnage = panneauNom;
            jeu.texteNomPersonnage = texteNom.GetComponent<Text>();
            jeu.texteDialogue = composantTexteDialogue;
            jeu.panneauPortrait = panneauPortrait;
            jeu.imagePortrait = imagePortrait.GetComponent<Image>();
            jeu.boutonZoneClic = boutonZoneClic;
            jeu.panneauChoix = panneauChoix;
            jeu.texteQuestionChoix = texteQuestion.GetComponent<Text>();
            jeu.conteneurChoix = conteneurChoix.transform;
            jeu.prefabBoutonChoix = prefabBoutonChoix;
            jeu.curseurVitesse = curseur;
            jeu.panneauFin = panneauFin;
            jeu.boutonRetourMenuDepuisFin = boutonRetourFin.GetComponent<Button>();
            jeu.historique = historiqueControleur;
            jeu.panneauSauvegarde = panneauSauvegardeControleur;
            jeu.boutonOuvrirSauvegarde = boutonOuvrirSauvegarde.GetComponent<Button>();
            jeu.boutonRetourMenu = boutonRetourMenu.GetComponent<Button>();

            EditorSceneManager.SaveScene(scene, $"{DOSSIER_SCENES}/Jeu.unity");
        }

        // ------------------------------------------------------------------
        // Prefabs
        // ------------------------------------------------------------------

        private static GameObject ConstruirePrefabBouton(string nom, TextAnchor alignementTexte)
        {
            var bouton = CreerBouton(null, nom, "Option");
            bouton.GetComponentInChildren<Text>().alignment = alignementTexte;
            string chemin = $"{DOSSIER_PREFABS}/{nom}.prefab";
            var asset = PrefabUtility.SaveAsPrefabAsset(bouton, chemin);
            Object.DestroyImmediate(bouton);
            return asset;
        }

        // ------------------------------------------------------------------
        // Aides de construction UI (uGUI legacy — cf. app/README.md "activeInputHandler")
        // ------------------------------------------------------------------

        private static void CreerEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static GameObject CreerCanvasRacine(string nom)
        {
            var go = new GameObject(nom, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return go;
        }

        private static GameObject CreerPanel(Transform parent, string nom, Color couleur)
        {
            var go = new GameObject(nom, typeof(RectTransform), typeof(Image));
            if (parent != null) go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = couleur;
            return go;
        }

        private static GameObject CreerImage(Transform parent, string nom)
        {
            var go = new GameObject(nom, typeof(RectTransform), typeof(Image));
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject CreerTexte(Transform parent, string nom, string contenu, int taille, TextAnchor alignement, Color couleur, FontStyle style)
        {
            var go = new GameObject(nom, typeof(RectTransform), typeof(Text));
            if (parent != null) go.transform.SetParent(parent, false);
            var texte = go.GetComponent<Text>();
            texte.text = contenu;
            texte.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texte.fontSize = taille;
            texte.alignment = alignement;
            texte.color = couleur;
            texte.fontStyle = style;
            texte.horizontalOverflow = HorizontalWrapMode.Wrap;
            texte.verticalOverflow = VerticalWrapMode.Truncate;
            return go;
        }

        private static GameObject CreerBouton(Transform parent, string nom, string libelle)
        {
            var go = new GameObject(nom, typeof(RectTransform), typeof(Image), typeof(Button));
            if (parent != null) go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = CouleurBouton;
            var bouton = go.GetComponent<Button>();
            var couleurs = bouton.colors;
            couleurs.highlightedColor = CouleurAccent;
            couleurs.pressedColor = CouleurAccent * 0.8f;
            couleurs.selectedColor = CouleurBouton;
            bouton.colors = couleurs;
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 50);

            var texte = CreerTexte(go.transform, "Texte", libelle, 20, TextAnchor.MiddleCenter, CouleurTexte, FontStyle.Normal);
            texte.GetComponent<Text>().raycastTarget = false;
            Etirer(texte.GetComponent<RectTransform>());
            return go;
        }

        private static GameObject CreerScrollView(Transform parent, string nom, out RectTransform contenu)
        {
            var viewport = new GameObject(nom, typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
            if (parent != null) viewport.transform.SetParent(parent, false);
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0.001f); // support du masque, quasi invisible
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var contenuGO = new GameObject("Contenu", typeof(RectTransform));
            contenuGO.transform.SetParent(viewport.transform, false);
            var rtContenu = contenuGO.GetComponent<RectTransform>();
            rtContenu.anchorMin = new Vector2(0, 1);
            rtContenu.anchorMax = new Vector2(1, 1);
            rtContenu.pivot = new Vector2(0.5f, 1f);
            rtContenu.anchoredPosition = Vector2.zero;

            var scrollRect = viewport.GetComponent<ScrollRect>();
            scrollRect.content = rtContenu;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.viewport = viewport.GetComponent<RectTransform>();

            contenu = rtContenu;
            return viewport;
        }

        private static Slider ConstruireSlider(GameObject go)
        {
            var fond = go.AddComponent<Image>();
            fond.color = CouleurBouton;
            var slider = go.AddComponent<Slider>();

            var zoneGlissement = new GameObject("ZoneGlissement", typeof(RectTransform));
            zoneGlissement.transform.SetParent(go.transform, false);
            Etirer(zoneGlissement.GetComponent<RectTransform>());

            var poignee = new GameObject("Poignee", typeof(RectTransform), typeof(Image));
            poignee.transform.SetParent(zoneGlissement.transform, false);
            poignee.GetComponent<Image>().color = CouleurAccent;
            var rtPoignee = poignee.GetComponent<RectTransform>();
            rtPoignee.sizeDelta = new Vector2(16, 16);

            slider.targetGraphic = poignee.GetComponent<Image>();
            slider.handleRect = rtPoignee;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            return slider;
        }

        // --- Ancrage RectTransform ---

        private static void Etirer(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void AncrerCentre(RectTransform rt, Vector2 position, Vector2 taille)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = taille;
        }

        private static void AncrerHaut(RectTransform rt, float ancreX, float ancreY, Vector2 position, Vector2 taille)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(ancreX, ancreY);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = position;
            rt.sizeDelta = taille;
        }

        private static void AncrerBas(RectTransform rt, float ancreX, float ancreY, Vector2 position, Vector2 taille)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(ancreX, ancreY);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = position;
            rt.sizeDelta = taille;
        }

        private static void AncrerGauche(RectTransform rt, Vector2 position, Vector2 taille)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = taille;
        }

        private static void AncrerDroite(RectTransform rt, Vector2 position, Vector2 taille)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = taille;
        }

        private static void PositionnerBas(RectTransform rt, Vector2 position, Vector2 taille)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = position;
            rt.sizeDelta = taille;
        }

        // ------------------------------------------------------------------

        private static void EnregistrerScenesDansBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene($"{DOSSIER_SCENES}/MenuPrincipal.unity", true),
                new EditorBuildSettingsScene($"{DOSSIER_SCENES}/Jeu.unity", true),
            };
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
