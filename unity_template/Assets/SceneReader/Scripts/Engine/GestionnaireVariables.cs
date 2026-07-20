using System.Collections.Generic;
using SceneReader.Modele;

namespace SceneReader.Engine
{
    /// <summary>État runtime des variables de la partie en cours.</summary>
    public class GestionnaireVariables
    {
        private readonly Dictionary<string, object> valeurs = new Dictionary<string, object>();

        public void Initialiser(Histoire histoire)
        {
            valeurs.Clear();
            foreach (var v in histoire.variables) valeurs[v.id] = v.valeurDefaut;
        }

        public object Obtenir(string id) => valeurs.TryGetValue(id, out var v) ? v : null;

        public void Definir(string id, object valeur) => valeurs[id] = valeur;

        public void ChargerDepuis(Dictionary<string, object> source)
        {
            valeurs.Clear();
            if (source == null) return;
            foreach (var paire in source) valeurs[paire.Key] = paire.Value;
        }

        public Dictionary<string, object> Copier() => new Dictionary<string, object>(valeurs);
    }
}
