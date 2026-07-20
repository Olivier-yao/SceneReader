using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SceneReader.Json
{
    /// <summary>
    /// Analyseur JSON minimal, sans dépendance externe (le projet ne doit pas
    /// nécessiter de connexion réseau pour résoudre un package tiers).
    /// Produit un graphe d'objets génériques :
    /// Dictionary&lt;string, object&gt;, List&lt;object&gt;, string, double, bool, null.
    /// </summary>
    public static class JsonAnalyseur
    {
        public static object Analyser(string texte)
        {
            int position = 0;
            object resultat = AnalyserValeur(texte, ref position);
            IgnorerEspaces(texte, ref position);
            return resultat;
        }

        private static object AnalyserValeur(string s, ref int i)
        {
            IgnorerEspaces(s, ref i);
            if (i >= s.Length) throw new FormatException("JSON tronqué (fin de chaîne inattendue)");
            char c = s[i];
            switch (c)
            {
                case '{': return AnalyserObjet(s, ref i);
                case '[': return AnalyserTableau(s, ref i);
                case '"': return AnalyserChaine(s, ref i);
                case 't':
                    Attendre(s, ref i, "true");
                    return true;
                case 'f':
                    Attendre(s, ref i, "false");
                    return false;
                case 'n':
                    Attendre(s, ref i, "null");
                    return null;
                default:
                    return AnalyserNombre(s, ref i);
            }
        }

        private static Dictionary<string, object> AnalyserObjet(string s, ref int i)
        {
            var objet = new Dictionary<string, object>();
            i++; // '{'
            IgnorerEspaces(s, ref i);
            if (Peek(s, i) == '}') { i++; return objet; }
            while (true)
            {
                IgnorerEspaces(s, ref i);
                string cle = AnalyserChaine(s, ref i);
                IgnorerEspaces(s, ref i);
                AttendreCaractere(s, ref i, ':');
                object valeur = AnalyserValeur(s, ref i);
                objet[cle] = valeur;
                IgnorerEspaces(s, ref i);
                char suivant = Peek(s, i);
                if (suivant == ',') { i++; continue; }
                if (suivant == '}') { i++; break; }
                throw new FormatException($"JSON invalide : ',' ou '}}' attendu à la position {i}");
            }
            return objet;
        }

        private static List<object> AnalyserTableau(string s, ref int i)
        {
            var tableau = new List<object>();
            i++; // '['
            IgnorerEspaces(s, ref i);
            if (Peek(s, i) == ']') { i++; return tableau; }
            while (true)
            {
                object valeur = AnalyserValeur(s, ref i);
                tableau.Add(valeur);
                IgnorerEspaces(s, ref i);
                char suivant = Peek(s, i);
                if (suivant == ',') { i++; continue; }
                if (suivant == ']') { i++; break; }
                throw new FormatException($"JSON invalide : ',' ou ']' attendu à la position {i}");
            }
            return tableau;
        }

        private static string AnalyserChaine(string s, ref int i)
        {
            AttendreCaractere(s, ref i, '"');
            var sb = new StringBuilder();
            while (true)
            {
                if (i >= s.Length) throw new FormatException("Chaîne JSON non terminée");
                char c = s[i++];
                if (c == '"') break;
                if (c == '\\')
                {
                    char echappe = s[i++];
                    switch (echappe)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            string hex = s.Substring(i, 4);
                            i += 4;
                            sb.Append((char)int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            break;
                        default:
                            throw new FormatException($"Séquence d'échappement JSON inconnue : \\{echappe}");
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static double AnalyserNombre(string s, ref int i)
        {
            int debut = i;
            while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '-' || s[i] == '+' || s[i] == '.' || s[i] == 'e' || s[i] == 'E'))
            {
                i++;
            }
            string brut = s.Substring(debut, i - debut);
            if (brut.Length == 0) throw new FormatException($"Nombre JSON invalide à la position {debut}");
            return double.Parse(brut, CultureInfo.InvariantCulture);
        }

        private static void IgnorerEspaces(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        }

        private static char Peek(string s, int i) => i < s.Length ? s[i] : '\0';

        private static void AttendreCaractere(string s, ref int i, char attendu)
        {
            if (i >= s.Length || s[i] != attendu)
                throw new FormatException($"JSON invalide : '{attendu}' attendu à la position {i}");
            i++;
        }

        private static void Attendre(string s, ref int i, string mot)
        {
            if (i + mot.Length > s.Length || s.Substring(i, mot.Length) != mot)
                throw new FormatException($"JSON invalide : littéral '{mot}' attendu à la position {i}");
            i += mot.Length;
        }
    }
}
