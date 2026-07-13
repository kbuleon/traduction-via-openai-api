namespace TraductionIA.Core
{
    /// <summary>
    /// Nettoyage du terme renvoyé par l'IA : suppression des espaces de bord et
    /// des guillemets encadrants éventuels (droits, typographiques, ou CJK).
    /// </summary>
    public static class OutputCleaner
    {
        // Paires de guillemets ouvrant/fermant pouvant encadrer la réponse.
        private static readonly char[] GuillemetsGauche = { '"', '\'', '«', '“', '‘', '「', '『' };
        private static readonly char[] GuillemetsDroite = { '"', '\'', '»', '”', '’', '」', '』' };

        public static string Clean(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return "";
            }

            string t = raw.Trim();

            // Retire une éventuelle paire de guillemets encadrants (une seule passe suffit ici).
            if (t.Length >= 2)
            {
                int g = System.Array.IndexOf(GuillemetsGauche, t[0]);
                int d = System.Array.IndexOf(GuillemetsDroite, t[t.Length - 1]);
                if (g >= 0 && d >= 0)
                {
                    t = t.Substring(1, t.Length - 2).Trim();
                }
            }

            return t;
        }
    }
}
