using System.IO;
using System.Text.Json;

namespace TraductionIA.Core
{
    /// <summary>
    /// Lecture d'une clé dans un fichier <c>secrets.json</c> du Secret Manager .NET.
    /// Le fichier est un JSON plat : la commande
    /// <c>dotnet user-secrets set "OpenAI:ApiKey" "sk-..."</c> produit
    /// <c>{ "OpenAI:ApiKey": "sk-..." }</c>. Lecture sans dépendance externe.
    /// </summary>
    public static class UserSecretsReader
    {
        /// <summary>
        /// Renvoie la valeur de <paramref name="key"/> dans le fichier, ou <c>null</c>
        /// si le fichier est absent, illisible, ou la clé manquante/vide.
        /// </summary>
        public static string ReadKey(string secretsJsonPath, string key)
        {
            try
            {
                if (string.IsNullOrEmpty(secretsJsonPath) || !File.Exists(secretsJsonPath))
                {
                    return null;
                }

                using (JsonDocument doc = JsonDocument.Parse(File.ReadAllText(secretsJsonPath)))
                {
                    if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                        doc.RootElement.TryGetProperty(key, out JsonElement v) &&
                        v.ValueKind == JsonValueKind.String)
                    {
                        string s = v.GetString();
                        return string.IsNullOrWhiteSpace(s) ? null : s;
                    }
                }
            }
            catch
            {
                // Lecture best-effort : tout incident → null (l'appelant retombe sur l'env).
            }
            return null;
        }
    }
}
