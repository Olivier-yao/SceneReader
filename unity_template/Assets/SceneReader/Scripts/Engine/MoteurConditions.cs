using System;
using System.Globalization;
using UnityEngine;

namespace SceneReader.Engine
{
    /// <summary>
    /// Évalue les expressions de condition du format pivot
    /// (format/FORMAT_SCENEREADER.md §5) : == != &amp;&amp; ||, parenthèses
    /// explicites, évaluation stricte de gauche à droite, sans priorité
    /// d'opérateur implicite entre &amp;&amp; et ||.
    /// </summary>
    public class MoteurConditions
    {
        private readonly GestionnaireVariables variables;
        private string expr;
        private int pos;

        public MoteurConditions(GestionnaireVariables variables)
        {
            this.variables = variables;
        }

        public bool Evaluer(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression)) return true;
            expr = expression;
            pos = 0;
            bool resultat = AnalyserExpression();
            IgnorerEspaces();
            if (pos != expr.Length)
                throw new FormatException($"Condition invalide (caractères en trop) : \"{expression}\"");
            return resultat;
        }

        private bool AnalyserExpression()
        {
            bool valeur = AnalyserTerme();
            while (true)
            {
                IgnorerEspaces();
                // Le terme de droite doit toujours être analysé (avance `pos`) même si le
                // court-circuit rendrait sa valeur booléenne sans effet sur le résultat.
                if (Consommer("&&")) { bool droite = AnalyserTerme(); valeur = valeur && droite; }
                else if (Consommer("||")) { bool droite = AnalyserTerme(); valeur = valeur || droite; }
                else break;
            }
            return valeur;
        }

        private bool AnalyserTerme()
        {
            IgnorerEspaces();
            if (Peek() == '(')
            {
                pos++;
                bool interieur = AnalyserExpression();
                IgnorerEspaces();
                Attendre(')');
                return interieur;
            }

            string ident = LireIdentifiant();
            IgnorerEspaces();
            string comparateur = Consommer("==") ? "==" : Consommer("!=") ? "!=" : throw new FormatException($"Opérateur de comparaison attendu (== ou !=) dans \"{expr}\" à la position {pos}");
            IgnorerEspaces();
            object valeurDroite = LireValeur();
            object valeurGauche = variables.Obtenir(ident);
            if (valeurGauche == null)
                Debug.LogWarning($"Condition référence une variable inconnue au runtime : \"{ident}\"");

            bool egal = ValeursEgales(valeurGauche, valeurDroite);
            return comparateur == "==" ? egal : !egal;
        }

        private static bool ValeursEgales(object a, object b)
        {
            if (a is bool ba && b is bool bb) return ba == bb;
            if (a is double da && b is double db) return Math.Abs(da - db) < 1e-9;
            if (a is string sa && b is string sb) return sa == sb;
            return false; // types incompatibles : jamais égaux (cohérent avec la validation de l'app Phase 1)
        }

        private string LireIdentifiant()
        {
            int debut = pos;
            while (pos < expr.Length && (char.IsLetterOrDigit(expr[pos]) || expr[pos] == '_')) pos++;
            if (pos == debut) throw new FormatException($"Identifiant de variable attendu dans \"{expr}\" à la position {pos}");
            return expr.Substring(debut, pos - debut);
        }

        private object LireValeur()
        {
            if (Peek() == '\'')
            {
                pos++;
                int debut = pos;
                while (pos < expr.Length && expr[pos] != '\'') pos++;
                string s = expr.Substring(debut, pos - debut);
                Attendre('\'');
                return s;
            }
            if (Consommer("true")) return true;
            if (Consommer("false")) return false;

            int debutNombre = pos;
            while (pos < expr.Length && (char.IsDigit(expr[pos]) || expr[pos] == '.' || expr[pos] == '-')) pos++;
            if (pos == debutNombre) throw new FormatException($"Valeur invalide dans \"{expr}\" à la position {pos}");
            return double.Parse(expr.Substring(debutNombre, pos - debutNombre), CultureInfo.InvariantCulture);
        }

        private char Peek() => pos < expr.Length ? expr[pos] : '\0';

        private bool Consommer(string mot)
        {
            if (pos + mot.Length <= expr.Length && expr.Substring(pos, mot.Length) == mot)
            {
                pos += mot.Length;
                return true;
            }
            return false;
        }

        private void Attendre(char c)
        {
            if (Peek() != c) throw new FormatException($"'{c}' attendu dans \"{expr}\" à la position {pos}");
            pos++;
        }

        private void IgnorerEspaces()
        {
            while (pos < expr.Length && char.IsWhiteSpace(expr[pos])) pos++;
        }
    }
}
