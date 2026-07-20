using System.IO;
using NUnit.Framework;
using SceneReader.Engine;
using SceneReader.Modele;
using UnityEngine;

namespace SceneReader.Tests.Editor
{
    /// <summary>
    /// Vérifie les critères de validation Phase 2 (cahier des charges §2, en
    /// bas) sur l'histoire d'exemple exportée en Phase 1 :
    /// - le JSON se joue de bout en bout sans erreur ;
    /// - un choix modifie une variable et une scène ultérieure en tient compte ;
    /// - sauvegarder/recharger reprend exactement où on était.
    /// Tests EditMode purs (aucune scène chargée) : ControleurPartie ne dépend
    /// d'aucun GameObject, seulement de fichiers sous StreamingAssets/persistentDataPath.
    /// </summary>
    public class HistoireDemoTests
    {
        private const int EMPLACEMENT_TEST = 3;
        private string cheminHistoire;

        [SetUp]
        public void PreparerTest()
        {
            cheminHistoire = Path.Combine(ChargeurHistoires.DossierHistoires, "histoire_demo.scenereader.json");
            Assert.IsTrue(File.Exists(cheminHistoire), $"histoire_demo.scenereader.json introuvable à {cheminHistoire} — copiez-le depuis exemples/.");
            GestionnaireSauvegarde.Supprimer(EMPLACEMENT_TEST);
        }

        [TearDown]
        public void NettoyerTest()
        {
            GestionnaireSauvegarde.Supprimer(EMPLACEMENT_TEST);
        }

        [Test]
        public void HistoireSeChargeSansErreur()
        {
            var histoire = ChargeurHistoires.ChargerDepuisFichier(cheminHistoire);
            Assert.AreEqual("La Porte", histoire.titre);
            Assert.AreEqual(5, histoire.scenes.Count);
            Assert.AreEqual(2, histoire.personnages.Count);
            Assert.AreEqual(2, histoire.variables.Count);
            Assert.IsNotNull(histoire.TrouverScene(histoire.sceneDepart), "scene_depart doit exister");
        }

        [Test]
        public void ChoixModifieVariableEtSceneUlterieureEnTientCompte()
        {
            var histoire = ChargeurHistoires.ChargerDepuisFichier(cheminHistoire);
            var controleur = new ControleurPartie();

            ElementHistoire dernierElementAffiche = null;
            controleur.SurElementAffiche += e => dernierElementAffiche = e;

            controleur.DemarrerNouvellePartie(histoire, cheminHistoire);
            Assert.AreEqual("scene_01", controleur.SceneCourante.id);

            // Avance jusqu'au choix de fin de scene_01 (4 éléments narration/dialogue avant).
            for (int i = 0; i < 4; i++) controleur.Avancer();
            Assert.AreEqual(TypeElement.Choix, dernierElementAffiche.type, "scene_01 doit se terminer par un choix");
            Assert.AreEqual(2, dernierElementAffiche.options.Count, "les deux options de scene_01 sont toujours visibles (pas de condition)");

            // Choisit "Mentir" → a_menti passe à true, direction scene_03.
            var optionMentir = dernierElementAffiche.options.Find(o => o.vers == "scene_03");
            Assert.IsNotNull(optionMentir);
            controleur.ChoisirOption(optionMentir);

            Assert.AreEqual("scene_03", controleur.SceneCourante.id);
            Assert.AreEqual(true, controleur.Variables.Obtenir("a_menti"), "le `set` de l'option choisie doit avoir été appliqué");

            // Avance jusqu'au choix de fin de scene_03 (3 éléments dialogue/narration avant).
            for (int i = 0; i < 3; i++) controleur.Avancer();
            Assert.AreEqual(TypeElement.Choix, dernierElementAffiche.type, "scene_03 doit se terminer par un choix");

            // La scène ULTÉRIEURE (scene_03) doit tenir compte de la variable modifiée en scene_01 :
            // l'option "Avouer" a pour condition a_menti == true, donc doit être visible.
            var optionAvouer = dernierElementAffiche.options.Find(o => o.vers == "scene_02");
            Assert.IsNotNull(optionAvouer, "l'option conditionnelle \"Avouer\" (a_menti == true) doit être visible puisque a_menti est vrai");
            Assert.AreEqual(2, dernierElementAffiche.options.Count, "les deux options de scene_03 sont visibles ici");

            controleur.ChoisirOption(optionAvouer);
            Assert.AreEqual("scene_02", controleur.SceneCourante.id);

            // Avance jusqu'à la transition automatique scene_02 → scene_04 (suivant),
            // en passant par le `set` porte_ouverte = true de fin de scene_02.
            SceneHistoire derniereScene = null;
            controleur.SurChangementScene += s => derniereScene = s;
            for (int i = 0; i < 3; i++) controleur.Avancer();

            Assert.AreEqual("scene_04", controleur.SceneCourante.id, "scene_02.suivant doit charger scene_04 automatiquement");
            Assert.AreEqual("scene_04", derniereScene?.id);
            Assert.AreEqual(true, controleur.Variables.Obtenir("porte_ouverte"), "le `set` de fin de scene_02 doit avoir été appliqué avant la transition");

            // Avance jusqu'à la fin de l'histoire (scene_04.fin == true, 4 éléments).
            bool finAtteinte = false;
            controleur.SurFinHistoire += () => finAtteinte = true;
            for (int i = 0; i < 4; i++) controleur.Avancer();
            Assert.IsTrue(finAtteinte, "scene_04.fin == true doit déclencher SurFinHistoire");
        }

        [Test]
        public void ChoixAvecConditionFausseEstMasque()
        {
            var histoire = ChargeurHistoires.ChargerDepuisFichier(cheminHistoire);
            var controleur = new ControleurPartie();
            ElementHistoire dernierElementAffiche = null;
            controleur.SurElementAffiche += e => dernierElementAffiche = e;

            controleur.DemarrerNouvellePartie(histoire, cheminHistoire);
            // Choisit "Dire la vérité" → a_menti reste false, direction scene_02 directement.
            for (int i = 0; i < 4; i++) controleur.Avancer();
            var optionVerite = dernierElementAffiche.options.Find(o => o.vers == "scene_02");
            controleur.ChoisirOption(optionVerite);
            Assert.AreEqual(false, controleur.Variables.Obtenir("a_menti"));

            // scene_02 n'a pas de choix (elle a un suivant direct) : ce test vérifie plutôt
            // le chemin scene_03 avec a_menti == false, en y allant directement pour l'exercer.
            var controleur2 = new ControleurPartie();
            ElementHistoire dernierElementAffiche2 = null;
            controleur2.SurElementAffiche += e => dernierElementAffiche2 = e;
            controleur2.DemarrerNouvellePartie(histoire, cheminHistoire);
            for (int i = 0; i < 4; i++) controleur2.Avancer();
            var optionMentir = dernierElementAffiche2.options.Find(o => o.vers == "scene_03");
            controleur2.ChoisirOption(optionMentir); // a_menti = true, requis pour atteindre scene_03 dans cette histoire

            // Force artificiellement a_menti à false pour vérifier que l'option conditionnelle disparaît.
            controleur2.Variables.Definir("a_menti", false);
            for (int i = 0; i < 3; i++) controleur2.Avancer();
            Assert.AreEqual(TypeElement.Choix, dernierElementAffiche2.type);
            var optionAvouer = dernierElementAffiche2.options.Find(o => o.vers == "scene_02");
            Assert.IsNull(optionAvouer, "l'option conditionnelle (a_menti == true) doit être masquée quand la condition est fausse");
            Assert.AreEqual(1, dernierElementAffiche2.options.Count, "seule l'option sans condition (\"Partir\") doit rester visible");
        }

        [Test]
        public void SauvegarderQuitterRechargerRepredExactementOuOnEtait()
        {
            var histoire = ChargeurHistoires.ChargerDepuisFichier(cheminHistoire);
            var controleur = new ControleurPartie();
            ElementHistoire dernierElementAffiche = null;
            controleur.SurElementAffiche += e => dernierElementAffiche = e;

            controleur.DemarrerNouvellePartie(histoire, cheminHistoire);
            for (int i = 0; i < 4; i++) controleur.Avancer();
            var optionMentir = dernierElementAffiche.options.Find(o => o.vers == "scene_03");
            controleur.ChoisirOption(optionMentir);
            controleur.Avancer(); // avance d'un élément dans scene_03, position non triviale

            string sceneAvantSauvegarde = controleur.SceneCourante.id;
            int indexAvantSauvegarde = controleur.IndexElementCourant;
            var variablesAvantSauvegarde = controleur.Variables.Copier();

            GestionnaireSauvegarde.Sauvegarder(EMPLACEMENT_TEST, controleur.ObtenirEtatSauvegarde());

            // "Quitter" : on abandonne ce contrôleur et on en crée un tout nouveau, comme au
            // redémarrage de l'application.
            var donneesRechargees = GestionnaireSauvegarde.Charger(EMPLACEMENT_TEST);
            Assert.IsNotNull(donneesRechargees);

            var histoireRechargee = ChargeurHistoires.ChargerDepuisFichier(cheminHistoire);
            var controleurRecharge = new ControleurPartie();
            ElementHistoire elementApresRechargement = null;
            controleurRecharge.SurElementAffiche += e => elementApresRechargement = e;
            controleurRecharge.ChargerPartieSauvegardee(histoireRechargee, cheminHistoire, donneesRechargees);

            Assert.AreEqual(sceneAvantSauvegarde, controleurRecharge.SceneCourante.id, "on doit reprendre sur la même scène");
            Assert.AreEqual(indexAvantSauvegarde, controleurRecharge.IndexElementCourant, "on doit reprendre exactement sur le même élément");
            Assert.AreEqual(variablesAvantSauvegarde["a_menti"], controleurRecharge.Variables.Obtenir("a_menti"), "les variables doivent être restaurées à l'identique");
            Assert.IsNotNull(elementApresRechargement, "l'élément courant doit être ré-affiché immédiatement après le chargement");
        }
    }
}
