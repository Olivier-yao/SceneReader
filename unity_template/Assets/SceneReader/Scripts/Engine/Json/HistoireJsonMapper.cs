using System;
using System.Collections.Generic;
using SceneReader.Modele;

namespace SceneReader.Json
{
    /// <summary>
    /// Convertit le graphe générique produit par JsonAnalyseur en objets du
    /// modèle SceneReader.Modele, en suivant strictement le contrat
    /// format/FORMAT_SCENEREADER.md.
    /// </summary>
    public static class HistoireJsonMapper
    {
        public static Histoire VersHistoire(string texteJson)
        {
            var racine = JsonAnalyseur.Analyser(texteJson) as Dictionary<string, object>;
            if (racine == null) throw new FormatException("Le fichier .scenereader.json ne contient pas un objet JSON à sa racine.");

            var histoire = new Histoire
            {
                version = Texte(racine, "version"),
                titre = Texte(racine, "titre"),
                auteur = Texte(racine, "auteur"),
                sceneDepart = Texte(racine, "scene_depart"),
            };

            foreach (var brut in Liste(racine, "personnages"))
            {
                var p = (Dictionary<string, object>)brut;
                histoire.personnages.Add(new Personnage
                {
                    id = Texte(p, "id"),
                    nom = Texte(p, "nom"),
                    description = Texte(p, "description"),
                    couleurHex = Texte(p, "couleur"),
                    portrait = Texte(p, "portrait"),
                });
            }

            foreach (var brut in Liste(racine, "variables"))
            {
                var v = (Dictionary<string, object>)brut;
                var type = VariableDef.AnalyserType(Texte(v, "type"));
                histoire.variables.Add(new VariableDef
                {
                    id = Texte(v, "id"),
                    type = type,
                    valeurDefaut = ConvertirValeur(v["defaut"], type),
                });
            }

            foreach (var brut in Liste(racine, "scenes"))
            {
                histoire.scenes.Add(VersScene((Dictionary<string, object>)brut, histoire));
            }

            return histoire;
        }

        private static SceneHistoire VersScene(Dictionary<string, object> s, Histoire histoire)
        {
            var scene = new SceneHistoire
            {
                id = Texte(s, "id"),
                titre = Texte(s, "titre"),
                decor = Texte(s, "decor"),
                suivant = Texte(s, "suivant"),
                fin = Bool(s, "fin"),
            };
            foreach (var brut in Liste(s, "elements"))
            {
                scene.elements.Add(VersElement((Dictionary<string, object>)brut, histoire));
            }
            return scene;
        }

        private static ElementHistoire VersElement(Dictionary<string, object> e, Histoire histoire)
        {
            string typeBrut = Texte(e, "type");
            var element = new ElementHistoire { type = AnalyserTypeElement(typeBrut) };

            switch (element.type)
            {
                case TypeElement.Narration:
                    element.texte = Texte(e, "texte");
                    break;
                case TypeElement.Dialogue:
                    element.texte = Texte(e, "texte");
                    element.personnage = Texte(e, "personnage");
                    element.emotion = Texte(e, "emotion");
                    break;
                case TypeElement.Set:
                    element.variable = Texte(e, "variable");
                    element.valeur = ConvertirValeurPourVariable(e["valeur"], element.variable, histoire);
                    break;
                case TypeElement.Choix:
                    element.question = Texte(e, "question");
                    element.options = new List<OptionChoix>();
                    foreach (var brut in Liste(e, "options"))
                    {
                        element.options.Add(VersOption((Dictionary<string, object>)brut, histoire));
                    }
                    break;
                case TypeElement.SuggestionChoix:
                    element.note = Texte(e, "note");
                    break;
            }
            return element;
        }

        private static OptionChoix VersOption(Dictionary<string, object> o, Histoire histoire)
        {
            var option = new OptionChoix
            {
                texte = Texte(o, "texte"),
                vers = Texte(o, "vers"),
                condition = Texte(o, "condition"),
                set = new List<ActionSet>(),
            };
            foreach (var brut in Liste(o, "set"))
            {
                var a = (Dictionary<string, object>)brut;
                string idVariable = Texte(a, "variable");
                option.set.Add(new ActionSet
                {
                    variable = idVariable,
                    valeur = ConvertirValeurPourVariable(a["valeur"], idVariable, histoire),
                });
            }
            return option;
        }

        private static TypeElement AnalyserTypeElement(string brut)
        {
            switch (brut)
            {
                case "narration": return TypeElement.Narration;
                case "dialogue": return TypeElement.Dialogue;
                case "set": return TypeElement.Set;
                case "choix": return TypeElement.Choix;
                case "suggestion_choix": return TypeElement.SuggestionChoix;
                default: throw new FormatException($"Type d'élément de scène inconnu : \"{brut}\"");
            }
        }

        private static object ConvertirValeurPourVariable(object valeurJson, string idVariable, Histoire histoire)
        {
            var def = histoire.TrouverVariable(idVariable);
            var type = def?.type ?? Deviner(valeurJson);
            return ConvertirValeur(valeurJson, type);
        }

        private static TypeVariable Deviner(object v)
        {
            if (v is bool) return TypeVariable.Bool;
            if (v is double) return TypeVariable.Nombre;
            return TypeVariable.Texte;
        }

        private static object ConvertirValeur(object brut, TypeVariable type)
        {
            switch (type)
            {
                case TypeVariable.Bool: return brut is bool b && b;
                case TypeVariable.Nombre: return brut is double d ? d : 0d;
                default: return brut?.ToString() ?? "";
            }
        }

        // --- Accès défensifs au graphe JSON générique ---

        private static string Texte(Dictionary<string, object> objet, string cle)
            => objet.TryGetValue(cle, out var v) ? v as string : null;

        private static bool Bool(Dictionary<string, object> objet, string cle)
            => objet.TryGetValue(cle, out var v) && v is bool b && b;

        private static List<object> Liste(Dictionary<string, object> objet, string cle)
            => objet.TryGetValue(cle, out var v) && v is List<object> liste ? liste : new List<object>();
    }
}
