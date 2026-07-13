using System.Collections.Generic;

namespace TraductionIA.Core
{
    /// <summary>
    /// Une ligne du tableau de libellés. Code et libellé français sont des données
    /// d'entrée non modifiées ; les quatre colonnes de traduction sont calculées.
    /// </summary>
    public sealed class LabelRow
    {
        public string Code { get; set; }
        public string LibelleFr { get; set; }
        public string LibelleEn { get; set; }
        public string LibelleEs { get; set; }
        public string LibelleZh { get; set; }
        public string LibelleJa { get; set; }

        public LabelRow(string code, string libelleFr)
        {
            Code = code;
            LibelleFr = libelleFr;
            LibelleEn = "";
            LibelleEs = "";
            LibelleZh = "";
            LibelleJa = "";
        }

        /// <summary>Écrit la traduction dans la colonne correspondant à la langue cible.</summary>
        public void SetTranslation(LangueCible cible, string valeur)
        {
            switch (cible)
            {
                case LangueCible.Anglais: LibelleEn = valeur; break;
                case LangueCible.Espagnol: LibelleEs = valeur; break;
                case LangueCible.Chinois: LibelleZh = valeur; break;
                case LangueCible.Japonais: LibelleJa = valeur; break;
            }
        }

        /// <summary>Jeu de démonstration figé (les 5 lignes de la spec).</summary>
        public static List<LabelRow> DemoDataset()
        {
            return new List<LabelRow>
            {
                new LabelRow("A01", "Client"),
                new LabelRow("A02", "Commande"),
                new LabelRow("A03", "Inventaire"),
                new LabelRow("A04", "Planification"),
                new LabelRow("A05", "Histogramme"),
            };
        }
    }
}
