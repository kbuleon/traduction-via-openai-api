using System;

namespace TraductionIA.Core
{
    /// <summary>
    /// Configuration de l'appel OpenAI. La clé API est lue UNIQUEMENT depuis la
    /// variable d'environnement OPENAI_API_KEY (jamais en dur, jamais dans le dépôt).
    /// </summary>
    public sealed class AppConfig
    {
        public const string CleEnv = "OPENAI_API_KEY";
        public const string EndpointParDefaut = "https://api.openai.com/v1/chat/completions";
        public const string ModeleParDefaut = "gpt-4o-mini";

        public string ApiKey { get; }
        public string Endpoint { get; }
        public string Model { get; }

        public bool HasApiKey
        {
            get { return !string.IsNullOrWhiteSpace(ApiKey); }
        }

        public AppConfig(string apiKey, string endpoint, string model)
        {
            ApiKey = apiKey;
            Endpoint = endpoint;
            Model = model;
        }

        /// <summary>Construit la configuration à partir de l'environnement.</summary>
        public static AppConfig FromEnvironment()
        {
            return new AppConfig(
                Environment.GetEnvironmentVariable(CleEnv),
                EndpointParDefaut,
                ModeleParDefaut);
        }
    }
}
