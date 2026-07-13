using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TraductionIA.Core
{
    /// <summary>
    /// Export du tableau en CSV : séparateur ';' et UTF-8 avec BOM (lecture correcte
    /// par Excel FR, y compris des caractères chinois et japonais).
    /// </summary>
    public static class CsvExporter
    {
        private const string EnTete =
            "Code;Libellé français;Libellé anglais;Libellé espagnol;Libellé chinois;Libellé japonais";

        public static string BuildCsv(IEnumerable<LabelRow> rows)
        {
            var sb = new StringBuilder();
            sb.Append(EnTete).Append("\r\n");
            foreach (var r in rows)
            {
                sb.Append(Escape(r.Code)).Append(';')
                  .Append(Escape(r.LibelleFr)).Append(';')
                  .Append(Escape(r.LibelleEn)).Append(';')
                  .Append(Escape(r.LibelleEs)).Append(';')
                  .Append(Escape(r.LibelleZh)).Append(';')
                  .Append(Escape(r.LibelleJa)).Append("\r\n");
            }
            return sb.ToString();
        }

        public static void ExportToFile(IEnumerable<LabelRow> rows, string path)
        {
            string csv = BuildCsv(rows);
            // encoderShouldEmitUTF8Identifier: true => BOM en tête de fichier.
            File.WriteAllText(path, csv, new UTF8Encoding(true));
        }

        private static string Escape(string field)
        {
            field = field ?? "";
            if (field.IndexOf(';') >= 0 || field.IndexOf('"') >= 0 ||
                field.IndexOf('\n') >= 0 || field.IndexOf('\r') >= 0)
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }
            return field;
        }
    }
}
