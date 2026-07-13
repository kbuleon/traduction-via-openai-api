using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TraductionIA.Core;

namespace TraductionIA.Tests
{
    /// <summary>
    /// Harnais de vérification (zéro dépendance externe). Exerce réellement la logique
    /// traduisible : OpenAiTranslator (HTTP simulé), OutputCleaner, CsvExporter.
    /// Code de sortie 0 si tout passe, 1 sinon.
    /// </summary>
    internal static class Program
    {
        private static int _ok;
        private static int _ko;

        private static async Task<int> Main()
        {
            Console.OutputEncoding = Encoding.UTF8;

            await Test_Succes_Anglais();
            await Test_Succes_Chinois_PreserveCjk();
            await Test_Requete_ContientModeleEtLangue();
            await Test_Erreur_401();
            await Test_Erreur_429();
            await Test_JsonInvalide();
            await Test_ReponseVide();
            await Test_Nettoyage_Guillemets();
            Test_OutputCleaner();
            Test_SystemPrompt_Langues();
            Test_Csv_Entete_Et_Cjk();
            Test_Csv_Echappement_PointVirgule();
            Test_Csv_Bom();
            Test_UserSecrets_Lecture();

            Console.WriteLine();
            Console.WriteLine("=== Résultat : " + _ok + " PASS, " + _ko + " FAIL ===");
            return _ko == 0 ? 0 : 1;
        }

        // ---- Fabriques ----

        private static OpenAiTranslator TranslatorAvec(HttpStatusCode code, string body)
        {
            var handler = new FakeHandler(req => new HttpResponseMessage(code)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
            var http = new HttpClient(handler);
            var config = new AppConfig("sk-test-fake", AppConfig.EndpointParDefaut, AppConfig.ModeleParDefaut);
            return new OpenAiTranslator(http, config);
        }

        private static string ReponseOpenAi(string content)
        {
            // Réponse Chat Completions minimale mais réaliste.
            string c = content.Replace("\\", "\\\\").Replace("\"", "\\\"");
            return "{ \"choices\": [ { \"message\": { \"role\": \"assistant\", \"content\": \"" + c + "\" } } ] }";
        }

        // ---- Tests OpenAiTranslator ----

        private static async Task Test_Succes_Anglais()
        {
            var t = TranslatorAvec(HttpStatusCode.OK, ReponseOpenAi("Customer"));
            var r = await t.TranslateAsync("Client", LangueCible.Anglais);
            Check("Succès FR→EN", r.Success && r.Text == "Customer");
        }

        private static async Task Test_Succes_Chinois_PreserveCjk()
        {
            var t = TranslatorAvec(HttpStatusCode.OK, ReponseOpenAi("客户"));
            var r = await t.TranslateAsync("Client", LangueCible.Chinois);
            Check("Succès FR→ZH (CJK préservé)", r.Success && r.Text == "客户");
        }

        private static async Task Test_Requete_ContientModeleEtLangue()
        {
            var handler = new FakeHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ReponseOpenAi("顧客"), Encoding.UTF8, "application/json")
            });
            var http = new HttpClient(handler);
            var config = new AppConfig("sk-test-fake", AppConfig.EndpointParDefaut, AppConfig.ModeleParDefaut);
            var t = new OpenAiTranslator(http, config);

            var r = await t.TranslateAsync("Client", LangueCible.Japonais);

            bool corpsOk = r.RawRequest.Contains("gpt-4o-mini")
                           && r.RawRequest.Contains("japonais")
                           && r.RawRequest.Contains("Client");
            bool enteteAuth = handler.LastRequest.Headers.Authorization != null
                              && handler.LastRequest.Headers.Authorization.Scheme == "Bearer";
            bool cleHorsCorps = !r.RawRequest.Contains("sk-test-fake"); // la clé n'est jamais dans le corps tracé
            Check("Requête : modèle + langue + terme dans le corps", corpsOk);
            Check("Requête : en-tête Authorization Bearer présent", enteteAuth);
            Check("Sécurité : clé API absente du JSON tracé", cleHorsCorps);
        }

        private static async Task Test_Erreur_401()
        {
            var t = TranslatorAvec(HttpStatusCode.Unauthorized, "{\"error\":\"invalid key\"}");
            var r = await t.TranslateAsync("Client", LangueCible.Anglais);
            Check("Erreur 401 → Success=false", !r.Success && r.Error.Contains("401"));
        }

        private static async Task Test_Erreur_429()
        {
            var t = TranslatorAvec((HttpStatusCode)429, "{\"error\":\"rate limit\"}");
            var r = await t.TranslateAsync("Client", LangueCible.Anglais);
            Check("Erreur 429 → Success=false", !r.Success && r.Error.Contains("429"));
        }

        private static async Task Test_JsonInvalide()
        {
            var t = TranslatorAvec(HttpStatusCode.OK, "ceci n'est pas du JSON");
            var r = await t.TranslateAsync("Client", LangueCible.Anglais);
            Check("JSON illisible → Success=false", !r.Success);
        }

        private static async Task Test_ReponseVide()
        {
            var t = TranslatorAvec(HttpStatusCode.OK, ReponseOpenAi(""));
            var r = await t.TranslateAsync("Client", LangueCible.Anglais);
            Check("Réponse vide → Success=false", !r.Success && r.Error.Contains("vide"));
        }

        private static async Task Test_Nettoyage_Guillemets()
        {
            var t = TranslatorAvec(HttpStatusCode.OK, ReponseOpenAi("\"Customer\""));
            var r = await t.TranslateAsync("Client", LangueCible.Anglais);
            Check("Nettoyage des guillemets encadrants", r.Success && r.Text == "Customer");
        }

        // ---- Tests OutputCleaner ----

        private static void Test_OutputCleaner()
        {
            Check("Cleaner : trim simple", OutputCleaner.Clean("  Customer  ") == "Customer");
            Check("Cleaner : guillemets droits", OutputCleaner.Clean("\"Cliente\"") == "Cliente");
            Check("Cleaner : guillemets typographiques", OutputCleaner.Clean("«Cliente»") == "Cliente");
            Check("Cleaner : chaîne vide", OutputCleaner.Clean("   ") == "");
        }

        // ---- Tests prompt système ----

        private static void Test_SystemPrompt_Langues()
        {
            Check("Prompt ZH mentionne 'chinois simplifié'",
                OpenAiTranslator.BuildSystemPrompt(LangueCible.Chinois).Contains("chinois simplifié"));
            Check("Prompt JA mentionne 'japonais'",
                OpenAiTranslator.BuildSystemPrompt(LangueCible.Japonais).Contains("japonais"));
        }

        // ---- Tests CsvExporter ----

        private static void Test_Csv_Entete_Et_Cjk()
        {
            var rows = LabelRow.DemoDataset();
            rows[0].SetTranslation(LangueCible.Chinois, "客户");
            rows[0].SetTranslation(LangueCible.Japonais, "顧客");
            string csv = CsvExporter.BuildCsv(rows);
            bool ok = csv.Contains("Libellé chinois;Libellé japonais")
                      && csv.Contains("客户")
                      && csv.Contains("顧客");
            Check("CSV : en-tête 6 colonnes + CJK présent", ok);
        }

        private static void Test_Csv_Echappement_PointVirgule()
        {
            var rows = new System.Collections.Generic.List<LabelRow>
            {
                new LabelRow("A99", "Vente; Achat")
            };
            string csv = CsvExporter.BuildCsv(rows);
            Check("CSV : champ avec ';' entre guillemets", csv.Contains("\"Vente; Achat\""));
        }

        private static void Test_Csv_Bom()
        {
            string path = Path.Combine(Path.GetTempPath(), "traductionia_test_" + Guid.NewGuid().ToString("N") + ".csv");
            try
            {
                var rows = LabelRow.DemoDataset();
                rows[0].SetTranslation(LangueCible.Chinois, "客户");
                CsvExporter.ExportToFile(rows, path);

                byte[] bytes = File.ReadAllBytes(path);
                bool bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
                // Relit en UTF-8 et vérifie que le CJK est intact.
                string relu = File.ReadAllText(path, Encoding.UTF8);
                Check("CSV : BOM UTF-8 présent + CJK relu intact", bom && relu.Contains("客户"));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        // ---- Tests UserSecretsReader ----

        private static void Test_UserSecrets_Lecture()
        {
            string dir = Path.Combine(Path.GetTempPath(), "traductionia_secrets_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "secrets.json");
            try
            {
                File.WriteAllText(path, "{ \"OpenAI:ApiKey\": \"sk-xyz-123\", \"Autre\": \"\" }");

                Check("UserSecrets : clé présente lue",
                    UserSecretsReader.ReadKey(path, "OpenAI:ApiKey") == "sk-xyz-123");
                Check("UserSecrets : clé absente → null",
                    UserSecretsReader.ReadKey(path, "Inexistant") == null);
                Check("UserSecrets : valeur vide → null",
                    UserSecretsReader.ReadKey(path, "Autre") == null);
                Check("UserSecrets : fichier absent → null",
                    UserSecretsReader.ReadKey(Path.Combine(dir, "absent.json"), "OpenAI:ApiKey") == null);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
        }

        // ---- Mini-assertion ----

        private static void Check(string nom, bool condition)
        {
            if (condition)
            {
                _ok++;
                Console.WriteLine("[PASS] " + nom);
            }
            else
            {
                _ko++;
                Console.WriteLine("[FAIL] " + nom);
            }
        }
    }
}
