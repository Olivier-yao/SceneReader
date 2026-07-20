using System.Collections.Generic;

namespace SceneReader.Modele
{
    /// <summary>Miroir de "Scene" dans format/FORMAT_SCENEREADER.md §4.</summary>
    public class SceneHistoire
    {
        public string id;
        public string titre;
        public string decor;
        public List<ElementHistoire> elements = new List<ElementHistoire>();
        public string suivant;
        public bool fin;

        public bool FinitParChoix()
        {
            return elements.Count > 0 && elements[elements.Count - 1].type == TypeElement.Choix;
        }
    }
}
