namespace SceneReader.Modele
{
    /// <summary>Miroir de "type" dans format/FORMAT_SCENEREADER.md §3.</summary>
    public enum TypeVariable
    {
        Bool,
        Nombre,
        Texte
    }

    /// <summary>Miroir de "Variable" dans format/FORMAT_SCENEREADER.md §3.</summary>
    public class VariableDef
    {
        public string id;
        public TypeVariable type;

        /// <summary>bool, double ou string selon `type`.</summary>
        public object valeurDefaut;

        public static TypeVariable AnalyserType(string brut)
        {
            switch (brut)
            {
                case "bool": return TypeVariable.Bool;
                case "nombre": return TypeVariable.Nombre;
                case "texte": return TypeVariable.Texte;
                default: throw new System.FormatException($"Type de variable inconnu : \"{brut}\"");
            }
        }
    }
}
