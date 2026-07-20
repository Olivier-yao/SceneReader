using System.Collections.Generic;

namespace SceneReader.Modele
{
    /// <summary>Miroir des types d'éléments de scène (format/FORMAT_SCENEREADER.md §4.1).</summary>
    public enum TypeElement
    {
        Narration,
        Dialogue,
        Set,
        Choix,
        SuggestionChoix
    }

    /// <summary>
    /// Un seul élément de scène, tous les champs possibles réunis (plus simple
    /// à consommer côté moteur/UI qu'une hiérarchie polymorphe en C#). Seuls les
    /// champs pertinents pour `type` sont renseignés, les autres restent null.
    /// </summary>
    public class ElementHistoire
    {
        public TypeElement type;

        // narration / dialogue
        public string texte;

        // dialogue
        public string personnage;
        public string emotion;

        // set
        public string variable;
        public object valeur;

        // choix
        public string question;
        public List<OptionChoix> options;

        // suggestion_choix (ne doit jamais apparaître dans un export final validé)
        public string note;
    }

    /// <summary>Une option de choix (format/FORMAT_SCENEREADER.md §4.1 "choix").</summary>
    public class OptionChoix
    {
        public string texte;
        public string vers;
        public string condition;
        public List<ActionSet> set;
    }

    /// <summary>Une action `set` déclenchée au moment où une option est choisie.</summary>
    public class ActionSet
    {
        public string variable;
        public object valeur;
    }
}
