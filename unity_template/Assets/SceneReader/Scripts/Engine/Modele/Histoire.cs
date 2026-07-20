using System.Collections.Generic;
using System.Linq;

namespace SceneReader.Modele
{
    /// <summary>Miroir du document racine .scenereader.json (format/FORMAT_SCENEREADER.md §1).</summary>
    public class Histoire
    {
        public string version;
        public string titre;
        public string auteur;
        public List<Personnage> personnages = new List<Personnage>();
        public List<VariableDef> variables = new List<VariableDef>();
        public List<SceneHistoire> scenes = new List<SceneHistoire>();
        public string sceneDepart;

        public SceneHistoire TrouverScene(string id) => scenes.FirstOrDefault(s => s.id == id);
        public Personnage TrouverPersonnage(string id) => personnages.FirstOrDefault(p => p.id == id);
        public VariableDef TrouverVariable(string id) => variables.FirstOrDefault(v => v.id == id);
    }
}
