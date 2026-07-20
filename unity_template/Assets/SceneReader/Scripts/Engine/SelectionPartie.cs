namespace SceneReader.Engine
{
    /// <summary>
    /// Pont statique entre la scène Menu et la scène Jeu : Unity ne permet pas
    /// de passer des paramètres directement à SceneManager.LoadScene, donc le
    /// menu renseigne ces champs juste avant de charger la scène de jeu.
    /// </summary>
    public static class SelectionPartie
    {
        public static string CheminFichierHistoire;

        /// <summary>null = nouvelle partie ; sinon numéro d'emplacement (1..3) à charger.</summary>
        public static int? EmplacementACharger;
    }
}
