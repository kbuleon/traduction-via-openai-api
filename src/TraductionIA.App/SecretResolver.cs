using System;
using System.IO;
using TraductionIA.Core;

namespace TraductionIA.App
{
    /// <summary>
    /// Résout la clé API selon l'ordre de priorité : 1) user-secrets .NET
    /// (<c>secrets.json</c> du profil), 2) variable d'environnement OPENAI_API_KEY.
    /// La clé n'est jamais en dur ni dans le dépôt (RG8).
    /// </summary>
    internal static class SecretResolver
    {
        // DOIT rester synchronisé avec <UserSecretsId> de TraductionIA.App.csproj.
        public const string UserSecretsId = "traductionia-openai-proto";

        // Nom de la clé tel qu'écrit par : dotnet user-secrets set "OpenAI:ApiKey" "sk-..."
        public const string CleSecret = "OpenAI:ApiKey";

        public static AppConfig Resolve()
        {
            string apiKey = LireUserSecret() ?? Environment.GetEnvironmentVariable(AppConfig.CleEnv);
            return new AppConfig(apiKey, AppConfig.EndpointParDefaut, AppConfig.ModeleParDefaut);
        }

        /// <summary>Chemin standard du secrets.json pour cet UserSecretsId, ou null si introuvable.</summary>
        public static string SecretsJsonPath()
        {
            string appData = Environment.GetEnvironmentVariable("APPDATA");
            if (string.IsNullOrEmpty(appData))
            {
                return null;
            }
            return Path.Combine(appData, "Microsoft", "UserSecrets", UserSecretsId, "secrets.json");
        }

        private static string LireUserSecret()
        {
            return UserSecretsReader.ReadKey(SecretsJsonPath(), CleSecret);
        }
    }
}
