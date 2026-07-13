using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Windows.Forms;
using TraductionIA.Core;

namespace TraductionIA.App
{
    /// <summary>
    /// Fenêtre principale : grille des libellés, bouton « Traduire » (orchestration
    /// séquentielle des 20 appels), bouton « Exporter CSV », zone de log des échanges.
    /// </summary>
    public sealed class MainForm : Form
    {
        // HttpClient unique réutilisé (évite l'épuisement de sockets).
        private static readonly HttpClient HttpClientPartage = CreerHttpClient();

        private readonly AppConfig _config;
        private readonly OpenAiTranslator _translator;
        private readonly List<LabelRow> _rows;

        private DataGridView _grid;
        private Button _btnTraduire;
        private Button _btnExporter;
        private TextBox _log;
        private Label _statut;

        // Langues cibles dans l'ordre de traitement, et la colonne de grille associée.
        private static readonly LangueCible[] LanguesCibles =
        {
            LangueCible.Anglais, LangueCible.Espagnol, LangueCible.Chinois, LangueCible.Japonais
        };

        private const int ColCode = 0, ColFr = 1, ColEn = 2, ColEs = 3, ColZh = 4, ColJa = 5;

        public MainForm()
        {
            _config = SecretResolver.Resolve();
            _translator = new OpenAiTranslator(HttpClientPartage, _config);
            _rows = LabelRow.DemoDataset();

            InitializeUi();
            ChargerGrille();
        }

        private static HttpClient CreerHttpClient()
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30); // timeout explicite par appel
            return client;
        }

        private void InitializeUi()
        {
            Text = "Traduction de libellés via IA (OpenAI)";
            Width = 900;
            Height = 640;
            StartPosition = FormStartPosition.CenterScreen;

            // Police couvrant le CJK pour la grille (rendu chinois / japonais).
            // Windows complète par font-linking si un glyphe manque.
            var policeCjk = new Font("Microsoft YaHei UI", 9.75f);

            _grid = new DataGridView
            {
                Dock = DockStyle.Top,
                Height = 220,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false
            };
            _grid.DefaultCellStyle.Font = policeCjk;
            _grid.Columns.Add("Code", "Code");
            _grid.Columns.Add("Fr", "Libellé français");
            _grid.Columns.Add("En", "Libellé anglais");
            _grid.Columns.Add("Es", "Libellé espagnol");
            _grid.Columns.Add("Zh", "Libellé chinois");
            _grid.Columns.Add("Ja", "Libellé japonais");

            _btnTraduire = new Button { Text = "Traduire", Width = 120, Height = 32, Left = 12, Top = 232 };
            _btnTraduire.Click += BtnTraduire_Click;

            _btnExporter = new Button { Text = "Exporter CSV", Width = 120, Height = 32, Left = 144, Top = 232 };
            _btnExporter.Click += BtnExporter_Click;

            _statut = new Label
            {
                AutoSize = true,
                Left = 280,
                Top = 240,
                Text = "Prêt."
            };

            _log = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Dock = DockStyle.Bottom,
                Height = 320,
                Font = new Font("Consolas", 9f)
            };

            Controls.Add(_log);
            Controls.Add(_statut);
            Controls.Add(_btnExporter);
            Controls.Add(_btnTraduire);
            Controls.Add(_grid);
        }

        private void ChargerGrille()
        {
            _grid.Rows.Clear();
            foreach (var r in _rows)
            {
                _grid.Rows.Add(r.Code, r.LibelleFr, r.LibelleEn, r.LibelleEs, r.LibelleZh, r.LibelleJa);
            }
        }

        private static int ColonnePour(LangueCible cible)
        {
            switch (cible)
            {
                case LangueCible.Anglais: return ColEn;
                case LangueCible.Espagnol: return ColEs;
                case LangueCible.Chinois: return ColZh;
                case LangueCible.Japonais: return ColJa;
                default: return ColEn;
            }
        }

        private async void BtnTraduire_Click(object sender, EventArgs e)
        {
            if (!_config.HasApiKey)
            {
                // RG9 : clé absente → message, aucun appel.
                MessageBox.Show(
                    "Aucune clé API trouvée.\r\n\r\n" +
                    "Définissez-la via user-secrets (recommandé) depuis le dossier du projet :\r\n" +
                    "    dotnet user-secrets set \"" + SecretResolver.CleSecret + "\" \"sk-...\"\r\n\r\n" +
                    "ou via la variable d'environnement :\r\n" +
                    "    setx " + AppConfig.CleEnv + " \"sk-...\"\r\n\r\n" +
                    "puis relancez l'application.",
                    "Clé API manquante", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _btnTraduire.Enabled = false;
            _btnExporter.Enabled = false;
            int traduites = 0, erreurs = 0;

            try
            {
                for (int i = 0; i < _rows.Count; i++)
                {
                    LabelRow row = _rows[i];
                    foreach (LangueCible cible in LanguesCibles)
                    {
                        _statut.Text = "Traduction " + row.Code + " → " + OpenAiTranslator.NomLangue(cible) + "...";

                        TranslationResult res = await _translator.TranslateAsync(row.LibelleFr, cible);

                        Journaliser(row, cible, res);

                        int col = ColonnePour(cible);
                        if (res.Success)
                        {
                            row.SetTranslation(cible, res.Text);
                            _grid.Rows[i].Cells[col].Value = res.Text;
                            traduites++;
                        }
                        else
                        {
                            row.SetTranslation(cible, "[ERREUR]");
                            _grid.Rows[i].Cells[col].Value = "[ERREUR]";
                            erreurs++;
                        }
                    }
                }

                _statut.Text = traduites + " cellules traduites, " + erreurs + " erreur(s) (sur 20).";
            }
            finally
            {
                _btnTraduire.Enabled = true;
                _btnExporter.Enabled = true;
            }
        }

        private void Journaliser(LabelRow row, LangueCible cible, TranslationResult res)
        {
            _log.AppendText("=== " + row.Code + " (" + row.LibelleFr + ") → " + OpenAiTranslator.NomLangue(cible) + " ===\r\n");
            _log.AppendText("--- Requête (corps JSON) ---\r\n" + res.RawRequest + "\r\n");
            _log.AppendText("--- Réponse ---\r\n" + (string.IsNullOrEmpty(res.RawResponse) ? "(aucune)" : res.RawResponse) + "\r\n");
            if (!res.Success)
            {
                _log.AppendText("!!! ERREUR : " + res.Error + "\r\n");
            }
            _log.AppendText("\r\n");
        }

        private void BtnExporter_Click(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "Fichier CSV (*.csv)|*.csv";
                dlg.FileName = "libelles-traduits.csv";
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        CsvExporter.ExportToFile(_rows, dlg.FileName);
                        _statut.Text = "Export CSV : " + dlg.FileName;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Échec de l'export : " + ex.Message, "Export CSV",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}
