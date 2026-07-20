using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SceneReader.Json
{
    /// <summary>
    /// Sérialise le même graphe générique que JsonAnalyseur produit
    /// (Dictionary&lt;string, object&gt;, List&lt;object&gt;, string, bool, nombres, null)
    /// vers une chaîne JSON. Utilisé pour écrire les fichiers de sauvegarde.
    /// </summary>
    public static class JsonEcrivain
    {
        public static string Ecrire(object valeur)
        {
            var sb = new StringBuilder();
            EcrireValeur(valeur, sb);
            return sb.ToString();
        }

        private static void EcrireValeur(object valeur, StringBuilder sb)
        {
            switch (valeur)
            {
                case null:
                    sb.Append("null");
                    break;
                case bool b:
                    sb.Append(b ? "true" : "false");
                    break;
                case string s:
                    EcrireChaine(s, sb);
                    break;
                case IDictionary<string, object> dict:
                    EcrireObjet(dict, sb);
                    break;
                case IEnumerable liste when !(valeur is string):
                    EcrireTableau(liste, sb);
                    break;
                default:
                    // nombres (double, int, float…)
                    sb.Append(System.Convert.ToDouble(valeur, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture));
                    break;
            }
        }

        private static void EcrireObjet(IDictionary<string, object> dict, StringBuilder sb)
        {
            sb.Append('{');
            bool premier = true;
            foreach (var paire in dict)
            {
                if (!premier) sb.Append(',');
                premier = false;
                EcrireChaine(paire.Key, sb);
                sb.Append(':');
                EcrireValeur(paire.Value, sb);
            }
            sb.Append('}');
        }

        private static void EcrireTableau(IEnumerable liste, StringBuilder sb)
        {
            sb.Append('[');
            bool premier = true;
            foreach (var element in liste)
            {
                if (!premier) sb.Append(',');
                premier = false;
                EcrireValeur(element, sb);
            }
            sb.Append(']');
        }

        private static void EcrireChaine(string s, StringBuilder sb)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < ' ')
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }
}
