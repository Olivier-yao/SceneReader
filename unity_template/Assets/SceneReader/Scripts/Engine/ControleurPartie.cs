using System;
using System.Collections.Generic;
using SceneReader.Modele;
using UnityEngine;

namespace SceneReader.Engine
{
    /// <summary>
    /// Moteur de jeu : fait avancer une Histoire élément par élément, applique
    /// les `set`, filtre les options de `choix` selon leurs conditions, gère
    /// les transitions de scène. Ne connaît rien de l'UI (Scripts/UI/ s'abonne
    /// à ses événements) — cahier des charges §2.2 "Scripts/Engine/ (moteur)
    /// et Scripts/UI/ (interface)".
    /// </summary>
    public class ControleurPartie
    {
        public Histoire HistoireCourante { get; private set; }
        public string CheminFichierHistoire { get; private set; }
        public SceneHistoire SceneCourante { get; private set; }
        public int IndexElementCourant { get; private set; }

        public GestionnaireVariables Variables { get; } = new GestionnaireVariables();
        private MoteurConditions moteurConditions;

        /// <summary>Élément à afficher (narration, dialogue, ou choix avec options déjà filtrées).</summary>
        public event Action<ElementHistoire> SurElementAffiche;
        public event Action<SceneHistoire> SurChangementScene;
        public event Action SurFinHistoire;

        public void DemarrerNouvellePartie(Histoire histoire, string cheminFichier)
        {
            HistoireCourante = histoire;
            CheminFichierHistoire = cheminFichier;
            moteurConditions = new MoteurConditions(Variables);
            Variables.Initialiser(histoire);
            ChargerScene(histoire.sceneDepart, 0);
        }

        public void ChargerPartieSauvegardee(Histoire histoire, string cheminFichier, DonneesSauvegarde donnees)
        {
            HistoireCourante = histoire;
            CheminFichierHistoire = cheminFichier;
            moteurConditions = new MoteurConditions(Variables);
            Variables.Initialiser(histoire);
            Variables.ChargerDepuis(donnees.variables);
            ChargerScene(donnees.sceneId, donnees.indexElement);
        }

        /// <summary>Appelé par l'UI quand le joueur clique pour passer à l'élément suivant.</summary>
        public void Avancer()
        {
            AvancerJusquAAffichage(IndexElementCourant + 1);
        }

        /// <summary>Appelé par l'UI quand le joueur sélectionne une option de choix.</summary>
        public void ChoisirOption(OptionChoix option)
        {
            foreach (var action in option.set)
                Variables.Definir(action.variable, action.valeur);
            ChargerScene(option.vers, 0);
        }

        public DonneesSauvegarde ObtenirEtatSauvegarde()
        {
            return new DonneesSauvegarde
            {
                histoireCheminFichier = CheminFichierHistoire,
                histoireTitre = HistoireCourante.titre,
                sceneId = SceneCourante.id,
                indexElement = IndexElementCourant,
                variables = Variables.Copier(),
                horodatage = DateTime.Now.ToString("o"),
            };
        }

        private void ChargerScene(string sceneId, int indexDepart)
        {
            var scene = HistoireCourante.TrouverScene(sceneId);
            if (scene == null)
            {
                Debug.LogError($"Scène introuvable : \"{sceneId}\" — arrêt de la partie.");
                SurFinHistoire?.Invoke();
                return;
            }
            SceneCourante = scene;
            SurChangementScene?.Invoke(scene);
            AvancerJusquAAffichage(indexDepart);
        }

        private void AvancerJusquAAffichage(int index)
        {
            while (true)
            {
                if (index >= SceneCourante.elements.Count)
                {
                    TerminerScene();
                    return;
                }

                var element = SceneCourante.elements[index];
                switch (element.type)
                {
                    case TypeElement.Set:
                        Variables.Definir(element.variable, element.valeur);
                        index++;
                        continue;

                    case TypeElement.SuggestionChoix:
                        Debug.LogWarning("suggestion_choix rencontrée en jeu — élément ignoré (ne devrait jamais apparaître dans un export final validé).");
                        index++;
                        continue;

                    case TypeElement.Choix:
                        IndexElementCourant = index;
                        SurElementAffiche?.Invoke(FiltrerOptionsVisibles(element));
                        return;

                    default: // narration, dialogue
                        IndexElementCourant = index;
                        SurElementAffiche?.Invoke(element);
                        return;
                }
            }
        }

        private ElementHistoire FiltrerOptionsVisibles(ElementHistoire choix)
        {
            var visible = new ElementHistoire
            {
                type = TypeElement.Choix,
                question = choix.question,
                options = new List<OptionChoix>(),
            };
            foreach (var option in choix.options)
            {
                if (string.IsNullOrEmpty(option.condition) || moteurConditions.Evaluer(option.condition))
                    visible.options.Add(option);
            }
            return visible;
        }

        private void TerminerScene()
        {
            if (SceneCourante.fin || string.IsNullOrEmpty(SceneCourante.suivant))
            {
                SurFinHistoire?.Invoke();
            }
            else
            {
                ChargerScene(SceneCourante.suivant, 0);
            }
        }
    }
}
