using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TraductionIA.Core
{
    /// <summary>
    /// Résultat d'une traduction. <see cref="OpenAiTranslator.TranslateAsync"/> ne lève
    /// jamais d'exception métier : tout échec est porté par <see cref="Success"/> = false
    /// et <see cref="Error"/>. Les champs Raw* tracent l'échange pour la pédagogie / le log.
    /// </summary>
    public sealed class TranslationResult
    {
        public bool Success { get; set; }
        public string Text { get; set; } = "";
        public string Error { get; set; } = "";
        public string RawRequest { get; set; } = "";
        public string RawResponse { get; set; } = "";
    }

    // ---- DTO (dé)sérialisés, mappés sur le contrat OpenAI Chat Completions ----

    internal sealed class ChatRequest
    {
        [JsonPropertyName("model")] public string Model { get; set; }
        [JsonPropertyName("temperature")] public double Temperature { get; set; }
        [JsonPropertyName("messages")] public List<ChatMessage> Messages { get; set; }
    }

    internal sealed class ChatMessage
    {
        [JsonPropertyName("role")] public string Role { get; set; }
        [JsonPropertyName("content")] public string Content { get; set; }
    }

    internal sealed class ChatResponse
    {
        [JsonPropertyName("choices")] public List<Choice> Choices { get; set; }
    }

    internal sealed class Choice
    {
        [JsonPropertyName("message")] public ResponseMessage Message { get; set; }
    }

    internal sealed class ResponseMessage
    {
        [JsonPropertyName("role")] public string Role { get; set; }
        [JsonPropertyName("content")] public string Content { get; set; }
    }

    /// <summary>
    /// Service d'échange avec l'API OpenAI : construit la requête, fait le POST HTTPS,
    /// lit la réponse JSON et en extrait le terme traduit. Un appel = une cellule.
    /// </summary>
    public sealed class OpenAiTranslator
    {
        private readonly HttpClient _http;
        private readonly AppConfig _config;

        // UnsafeRelaxedJsonEscaping : les accents et caractères CJK restent lisibles
        // dans le JSON tracé (pas de séquences \uXXXX), ce qui sert la documentation.
        private static readonly JsonSerializerOptions RequestJsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public OpenAiTranslator(HttpClient http, AppConfig config)
        {
            _http = http;
            _config = config;
        }

        /// <summary>Nom de langue inséré dans la consigne (RG5).</summary>
        public static string NomLangue(LangueCible cible)
        {
            switch (cible)
            {
                case LangueCible.Anglais: return "anglais";
                case LangueCible.Espagnol: return "espagnol";
                case LangueCible.Chinois: return "chinois simplifié";
                case LangueCible.Japonais: return "japonais";
                default: throw new ArgumentOutOfRangeException(nameof(cible));
            }
        }

        /// <summary>Consigne système : terme seul, langue cible explicite (RG4/RG5).</summary>
        public static string BuildSystemPrompt(LangueCible cible)
        {
            return "Tu es un traducteur. Traduis le terme fourni en " + NomLangue(cible) +
                   ". Réponds UNIQUEMENT par le terme traduit, sans guillemets, " +
                   "sans ponctuation finale, sans explication.";
        }

        public async Task<TranslationResult> TranslateAsync(string termeFr, LangueCible cible)
        {
            var result = new TranslationResult();

            var requete = new ChatRequest
            {
                Model = _config.Model,
                Temperature = 0,
                Messages = new List<ChatMessage>
                {
                    new ChatMessage { Role = "system", Content = BuildSystemPrompt(cible) },
                    new ChatMessage { Role = "user", Content = termeFr }
                }
            };

            // Tracé : uniquement le corps JSON. L'en-tête Authorization n'est jamais journalisé (RG8/§9).
            result.RawRequest = JsonSerializer.Serialize(requete, RequestJsonOptions);

            try
            {
                using (var httpRequest = new HttpRequestMessage(HttpMethod.Post, _config.Endpoint))
                {
                    httpRequest.Content = new StringContent(result.RawRequest, Encoding.UTF8, "application/json");
                    httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.ApiKey);

                    using (var response = await _http.SendAsync(httpRequest).ConfigureAwait(false))
                    {
                        string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        result.RawResponse = body;

                        if (!response.IsSuccessStatusCode)
                        {
                            result.Success = false;
                            result.Error = "HTTP " + (int)response.StatusCode + " " + response.StatusCode;
                            return result;
                        }

                        ChatResponse parsed = JsonSerializer.Deserialize<ChatResponse>(body);
                        string content = null;
                        if (parsed != null && parsed.Choices != null && parsed.Choices.Count > 0 && parsed.Choices[0].Message != null)
                        {
                            content = parsed.Choices[0].Message.Content;
                        }

                        string cleaned = OutputCleaner.Clean(content ?? "");
                        if (string.IsNullOrEmpty(cleaned))
                        {
                            result.Success = false;
                            result.Error = "Réponse vide";
                            return result;
                        }

                        result.Success = true;
                        result.Text = cleaned;
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                // Réseau, timeout, JSON illisible, etc. → échec porté, jamais propagé (RG6).
                result.Success = false;
                result.Error = ex.Message;
                return result;
            }
        }
    }
}
