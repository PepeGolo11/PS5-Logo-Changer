using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Data;
using System.Threading.Tasks;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Linq;

namespace PS5_Logo_Changer
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        private SplitContainer splitContainer;
        private Button toggleButton;
        private Button iconChangeButton;
        private Button logoChangeButton;
        private Button settingsButton;
        private System.Windows.Forms.Timer animateTimer;
        private int targetWidth;
        private bool isMenuExpanded = true;
        private const int expandedWidth = 150;
        private const int collapsedWidth = 60;

        // Global App Settings
        private AppSettings settings = AppSettings.Load();

        // Logo change controls
        private Button connectButton;
        private DataGridView resultsDataGridView;
        private TextBox debugTextBox;
        private TextBox searchTextBox;
        private CheckBox missingLogoCheckBox;
        private TextBox descriptionTextBox;

        // Cached state for Logo Change panel
        private DataTable masterGameTable = new DataTable();
        private string cachedSearchText = "";
        private bool cachedMissingLogoFilter = false;
        private string cachedDebugLog = "";
        private string cachedConnectionStatus = "●  Not Connected";
        private Color cachedConnectionStatusColor = Color.FromArgb(248, 81, 73);

        // Image display / action controls
        private PictureBox logoPictureBox;
        private Button browseLogoButton;
        private Button uploadLogoButton;
        private Button updateDescriptionButton;
        private string selectedLogoPngPath = null;  // path chosen by Browse

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.splitContainer   = new SplitContainer();
            this.toggleButton     = new Button();
            this.logoChangeButton = new Button();
            this.iconChangeButton = new Button();
            this.settingsButton   = new Button();
            this.animateTimer     = new System.Windows.Forms.Timer();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.SuspendLayout();

            this.splitContainer.Dock = DockStyle.Fill;
            this.splitContainer.Location = new Point(0, 0);
            this.splitContainer.Name = "splitContainer";
            this.splitContainer.Size = new Size(800, 450);
            this.splitContainer.SplitterDistance = 0;   // Left menu hidden for now
            this.splitContainer.Panel1MinSize = 0;
            this.splitContainer.Panel2MinSize = 0;
            this.splitContainer.IsSplitterFixed = true; // Lock splitter so user can't drag it
            this.splitContainer.TabIndex = 0;

            this.splitContainer.Panel1.BackColor = Color.FromArgb(18, 22, 31);
            this.splitContainer.Panel1.Controls.Add(this.toggleButton);
            this.splitContainer.Panel1.Controls.Add(this.logoChangeButton);
            this.splitContainer.Panel1.Controls.Add(this.iconChangeButton);
            this.splitContainer.Panel1.Controls.Add(this.settingsButton);
            this.splitContainer.Panel1.Padding = new Padding(5);
            this.splitContainer.Panel1.Visible = false; // Hide left panel entirely

            this.splitContainer.Panel2.BackColor = Color.FromArgb(28, 34, 45);
            this.splitContainer.Panel2.Padding   = new Padding(10);

            this.toggleButton.Dock = DockStyle.Top;
            this.toggleButton.FlatAppearance.BorderSize = 0;
            this.toggleButton.FlatStyle = FlatStyle.Flat;
            this.toggleButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.toggleButton.ForeColor = Color.FromArgb(240, 244, 248);
            this.toggleButton.BackColor = Color.FromArgb(18, 22, 31);
            this.toggleButton.Text = "Menu Collapse";
            this.toggleButton.TextAlign = ContentAlignment.MiddleCenter;
            this.toggleButton.Height = 40;
            this.toggleButton.Click += new EventHandler(this.ToggleButton_Click);

            this.logoChangeButton.Dock = DockStyle.Top;
            this.logoChangeButton.FlatAppearance.BorderSize = 0;
            this.logoChangeButton.FlatStyle = FlatStyle.Flat;
            this.logoChangeButton.Font = new Font("Segoe UI", 9F);
            this.logoChangeButton.ForeColor = Color.FromArgb(240, 244, 248);
            this.logoChangeButton.BackColor = Color.FromArgb(18, 22, 31);
            this.logoChangeButton.Text = "Logo Change";
            this.logoChangeButton.TextAlign = ContentAlignment.MiddleLeft;
            this.logoChangeButton.Height = 40;
            this.logoChangeButton.Click += new EventHandler(this.LogoChangeButton_Click);

            this.iconChangeButton.Dock = DockStyle.Top;
            this.iconChangeButton.FlatAppearance.BorderSize = 0;
            this.iconChangeButton.FlatStyle = FlatStyle.Flat;
            this.iconChangeButton.Font = new Font("Segoe UI", 9F);
            this.iconChangeButton.ForeColor = Color.FromArgb(240, 244, 248);
            this.iconChangeButton.BackColor = Color.FromArgb(18, 22, 31);
            this.iconChangeButton.Text = "Icon Change";
            this.iconChangeButton.TextAlign = ContentAlignment.MiddleLeft;
            this.iconChangeButton.Height = 40;
            this.iconChangeButton.Click += new EventHandler(this.IconChangeButton_Click);

            this.settingsButton.Dock = DockStyle.Top;
            this.settingsButton.FlatAppearance.BorderSize = 0;
            this.settingsButton.FlatStyle = FlatStyle.Flat;
            this.settingsButton.Font = new Font("Segoe UI", 9F);
            this.settingsButton.ForeColor = Color.FromArgb(240, 244, 248);
            this.settingsButton.BackColor = Color.FromArgb(18, 22, 31);
            this.settingsButton.Text = "Settings";
            this.settingsButton.TextAlign = ContentAlignment.MiddleLeft;
            this.settingsButton.Height = 40;
            this.settingsButton.Click += new EventHandler(this.SettingsButton_Click);

            this.animateTimer.Interval = 10;
            this.animateTimer.Tick += new EventHandler(this.AnimateTimer_Tick);

            this.AutoScaleDimensions = new SizeF(8F, 16F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(800, 700);
            this.Controls.Add(this.splitContainer);
            this.Name = "MainForm";
            this.Text = "PS5 Logo Changer";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(18, 22, 31);

            // Default page: Logo Change
            ShowLogoChangeUI();

            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private void ToggleButton_Click(object sender, EventArgs e)
        {
            isMenuExpanded = !isMenuExpanded;
            targetWidth = isMenuExpanded ? expandedWidth : collapsedWidth;
            UpdateButtonTextBasedOnMenuState();
            animateTimer.Start();
        }

        private void UpdateButtonTextBasedOnMenuState()
        {
            if (isMenuExpanded)
            {
                toggleButton.Text     = "Menu Collapse";
                logoChangeButton.Text = "Logo Change";
                iconChangeButton.Text = "Icon Change";
                settingsButton.Text   = "Settings";
            }
            else
            {
                toggleButton.Text     = "☰";
                logoChangeButton.Text = "L";
                iconChangeButton.Text = "I";
                settingsButton.Text   = "S";
            }
        }

        private void AnimateTimer_Tick(object sender, EventArgs e)
        {
            int currentWidth = splitContainer.SplitterDistance;
            int step = (targetWidth > currentWidth) ? 10 : -10;
            int newWidth = currentWidth + step;
            if ((step > 0 && newWidth >= targetWidth) || (step < 0 && newWidth <= targetWidth))
            {
                newWidth = targetWidth;
                animateTimer.Stop();
                UpdateButtonTextBasedOnMenuState();
            }
            splitContainer.SplitterDistance = newWidth;
        }

        private void IconChangeButton_Click(object sender, EventArgs e) => ShowGenericContent("Icon Change Settings");
        private void LogoChangeButton_Click(object sender, EventArgs e) => ShowLogoChangeUI();
        private void SettingsButton_Click(object sender, EventArgs e)   => ShowSettingsUI();

        private void ShowGenericContent(string title)
        {
            SaveLogoChangeState();
            this.splitContainer.Panel2.Controls.Clear();
            var titleLabel = new Label { Text = title, Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 244, 248), AutoSize = true, Location = new Point(20, 20), BackColor = Color.Transparent };
            var infoLabel  = new Label {
                Text = "This is the " + title + " panel.\nHere you can customize related settings.",
                Font = new Font("Segoe UI", 10F), ForeColor = Color.FromArgb(138, 153, 173), AutoSize = true, Location = new Point(20, 60), BackColor = Color.Transparent };
            this.splitContainer.Panel2.Controls.Add(titleLabel);
            this.splitContainer.Panel2.Controls.Add(infoLabel);
        }

        private void SaveLogoChangeState()
        {
            if (searchTextBox != null && !searchTextBox.IsDisposed)
                cachedSearchText = searchTextBox.Text;
            if (missingLogoCheckBox != null && !missingLogoCheckBox.IsDisposed)
                cachedMissingLogoFilter = missingLogoCheckBox.Checked;
            if (debugTextBox != null && !debugTextBox.IsDisposed)
                cachedDebugLog = debugTextBox.Text;
            var lbl = this.splitContainer.Panel2.Controls.Find("statusLabel", true).FirstOrDefault() as Label;
            if (lbl != null) { cachedConnectionStatus = lbl.Text; cachedConnectionStatusColor = lbl.ForeColor; }
        }

        private void EnsureMasterTableColumns()
        {
            if (masterGameTable.Columns.Count == 0)
            {
                masterGameTable.Columns.Add("Game", typeof(string));
                masterGameTable.Columns.Add("Logo Path", typeof(string));
                masterGameTable.Columns.Add("Description", typeof(string));
            }
        }

        private void SetupGridColumns()
        {
            if (resultsDataGridView == null) return;
            resultsDataGridView.Columns.Clear();

            var gameCol = new DataGridViewTextBoxColumn {
                Name = "Game",
                HeaderText = "Game",
                ReadOnly = true
            };
            var logoCol = new DataGridViewTextBoxColumn {
                Name = "Logo Path",
                HeaderText = "Logo Path",
                ReadOnly = true
            };
            var descCol = new DataGridViewTextBoxColumn {
                Name = "Description",
                HeaderText = "Description",
                ReadOnly = true
            };

            resultsDataGridView.Columns.Add(gameCol);
            resultsDataGridView.Columns.Add(logoCol);
            resultsDataGridView.Columns.Add(descCol);
            AdjustColumnWidths();
        }

        private void AdjustColumnWidths()
        {
            if (resultsDataGridView == null || resultsDataGridView.Columns.Count < 3) return;
            try
            {
                resultsDataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

                int clientW = resultsDataGridView.ClientSize.Width;
                if (clientW <= 0) clientW = resultsDataGridView.Width - 4;

                int vertScrollWidth = SystemInformation.VerticalScrollBarWidth;
                int available = clientW - vertScrollWidth - 2;

                var gameCol = resultsDataGridView.Columns["Game"];
                var logoCol = resultsDataGridView.Columns["Logo Path"];
                var descCol = resultsDataGridView.Columns["Description"];

                if (logoCol != null) logoCol.Width = 145;
                if (descCol != null) descCol.Width = 125;

                if (gameCol != null)
                {
                    int otherWidths = (logoCol?.Width ?? 145) + (descCol?.Width ?? 125);
                    int gameWidth = available - otherWidths;
                    gameCol.Width = Math.Max(gameWidth, 180);
                }
            }
            catch { }
        }

        private void ApplyTableFilter()
        {
            if (resultsDataGridView == null) return;

            string search = searchTextBox != null ? searchTextBox.Text.Trim() : (cachedSearchText ?? "");
            bool missingOnly = missingLogoCheckBox != null ? missingLogoCheckBox.Checked : cachedMissingLogoFilter;

            string selectedGame = null;
            if (resultsDataGridView.SelectedRows.Count > 0)
                selectedGame = GetCellValue(resultsDataGridView.SelectedRows[0], "Game");

            resultsDataGridView.Rows.Clear();

            foreach (DataRow dr in masterGameTable.Rows)
            {
                string game = dr["Game"]?.ToString() ?? "";
                string logo = dr["Logo Path"]?.ToString() ?? "";
                string desc = dr["Description"]?.ToString() ?? "";

                if (missingOnly && !string.IsNullOrWhiteSpace(logo))
                    continue;

                if (!string.IsNullOrEmpty(search) && game.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                int rowIdx = resultsDataGridView.Rows.Add(game, logo, desc);
                if (selectedGame != null && game.Equals(selectedGame, StringComparison.OrdinalIgnoreCase))
                {
                    resultsDataGridView.Rows[rowIdx].Selected = true;
                }
            }

            AdjustColumnWidths();
        }

        private string GetCellValue(DataGridViewRow row, string colName)
        {
            if (row == null || row.DataGridView == null) return "";
            if (row.DataGridView.Columns.Contains(colName))
            {
                var val = row.Cells[colName].Value;
                return (val != null && val != DBNull.Value) ? val.ToString() : "";
            }
            if (colName == "Game" || colName == "conceptName")
                return row.Cells[0].Value?.ToString() ?? "";
            if (colName == "Logo Path" || colName == "logoImageInfo")
                return row.Cells.Count > 1 ? (row.Cells[1].Value?.ToString() ?? "") : "";
            if (colName == "Description" || colName == "description")
                return row.Cells.Count > 2 ? (row.Cells[2].Value?.ToString() ?? "") : "";
            return "";
        }

        private void ShowSettingsUI()
        {
            SaveLogoChangeState();
            this.splitContainer.Panel2.Controls.Clear();

            var titleLabel = new Label {
                Text = "Settings",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 244, 248),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(20, 15)
            };
            this.splitContainer.Panel2.Controls.Add(titleLabel);

            // --- FTP Settings ---
            this.splitContainer.Panel2.Controls.Add(new Label {
                Text = "FTP IP Address:",
                AutoSize = true,
                Location = new Point(20, 60),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(230, 236, 245),
                BackColor = Color.Transparent
            });
            var ftpIpBox = new TextBox {
                Location = new Point(170, 58),
                Width = 200,
                Text = settings.FtpIpAddress ?? "",
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.FromArgb(24, 30, 40),
                ForeColor = Color.FromArgb(240, 244, 248),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.splitContainer.Panel2.Controls.Add(ftpIpBox);

            this.splitContainer.Panel2.Controls.Add(new Label {
                Text = "FTP Port:",
                AutoSize = true,
                Location = new Point(390, 60),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(230, 236, 245),
                BackColor = Color.Transparent
            });
            var ftpPortBox = new TextBox {
                Location = new Point(455, 58),
                Width = 70,
                Text = settings.FtpPort > 0 ? settings.FtpPort.ToString() : "2121",
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.FromArgb(24, 30, 40),
                ForeColor = Color.FromArgb(240, 244, 248),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.splitContainer.Panel2.Controls.Add(ftpPortBox);

            this.splitContainer.Panel2.Controls.Add(new Label {
                Text = "Path to Save Logos to:",
                AutoSize = true,
                Location = new Point(20, 95),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(230, 236, 245),
                BackColor = Color.Transparent
            });
            var savePathBox = new TextBox {
                Location = new Point(170, 93),
                Width = 355,
                Text = string.IsNullOrEmpty(settings.PathToSaveLogos) ? "/user/data/GameLogos/" : settings.PathToSaveLogos,
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.FromArgb(24, 30, 40),
                ForeColor = Color.FromArgb(240, 244, 248),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.splitContainer.Panel2.Controls.Add(savePathBox);

            // --- API Setup ---
            this.splitContainer.Panel2.Controls.Add(new Label {
                Text = "API Setup - Dsiabled - Work in progress",
                AutoSize = true,
                Location = new Point(20, 135),
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 244, 248),
                BackColor = Color.Transparent
            });

            var apiPanel = new Panel {
                Location = new Point(20, 168),
                Width = 650,
                Height = 360,
                BackColor = Color.FromArgb(23, 28, 38),
                BorderStyle = BorderStyle.None
            };
            apiPanel.Paint += (s, e) => {
                using (var pen = new Pen(Color.FromArgb(46, 56, 70), 1))
                    e.Graphics.DrawRectangle(pen, 0, 0, apiPanel.Width - 1, apiPanel.Height - 1);
            };
            this.splitContainer.Panel2.Controls.Add(apiPanel);

            // 1. ScreenScraper.fr
            var ssCheck = new CheckBox {
                Text = "ScreenScraper.fr",
                Location = new Point(15, 15),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(138, 153, 173),
                BackColor = Color.Transparent,
                Checked = false,
                Enabled = false
            };
            apiPanel.Controls.Add(ssCheck);

            var ssUserLabel = new Label {
                Text = "Username:",
                Location = new Point(35, 45),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(100, 115, 135),
                BackColor = Color.Transparent,
                Enabled = false
            };
            apiPanel.Controls.Add(ssUserLabel);

            var ssUserBox = new TextBox {
                Location = new Point(125, 42),
                Width = 220,
                Text = settings.ScreenScraperUsername ?? "",
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.FromArgb(20, 25, 34),
                ForeColor = Color.FromArgb(138, 153, 173),
                BorderStyle = BorderStyle.FixedSingle,
                Enabled = false
            };
            apiPanel.Controls.Add(ssUserBox);

            var ssPassLabel = new Label {
                Text = "Password:",
                Location = new Point(35, 75),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(100, 115, 135),
                BackColor = Color.Transparent,
                Enabled = false
            };
            apiPanel.Controls.Add(ssPassLabel);

            var ssPassBox = new TextBox {
                Location = new Point(125, 72),
                Width = 220,
                Text = settings.ScreenScraperPassword ?? "",
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.FromArgb(20, 25, 34),
                ForeColor = Color.FromArgb(138, 153, 173),
                BorderStyle = BorderStyle.FixedSingle,
                PasswordChar = '●',
                Enabled = false
            };
            apiPanel.Controls.Add(ssPassBox);

            // 2. Launchbox
            var lbCheck = new CheckBox {
                Text = "Launchbox",
                Location = new Point(15, 125),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(138, 153, 173),
                BackColor = Color.Transparent,
                Checked = false,
                Enabled = false
            };
            apiPanel.Controls.Add(lbCheck);

            // 3. SteamGridDB
            var sgCheck = new CheckBox {
                Text = "SteamGridDB",
                Location = new Point(15, 175),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(138, 153, 173),
                BackColor = Color.Transparent,
                Checked = false,
                Enabled = false
            };
            apiPanel.Controls.Add(sgCheck);

            var sgKeyLabel = new Label {
                Text = "API Key:",
                Location = new Point(35, 205),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(100, 115, 135),
                BackColor = Color.Transparent,
                Enabled = false
            };
            apiPanel.Controls.Add(sgKeyLabel);

            var sgKeyBox = new TextBox {
                Location = new Point(125, 202),
                Width = 350,
                Text = settings.SteamGridDbApiKey ?? "",
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.FromArgb(20, 25, 34),
                ForeColor = Color.FromArgb(138, 153, 173),
                BorderStyle = BorderStyle.FixedSingle,
                Enabled = false
            };
            apiPanel.Controls.Add(sgKeyBox);

            // Go Back Button
            var goBackButton = new Button {
                Text = "Go Back",
                Width = 110,
                Height = 35,
                Location = new Point(20, 545),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(235, 240, 248),
                BackColor = Color.FromArgb(34, 41, 54)
            };
            goBackButton.FlatAppearance.BorderSize = 1;
            goBackButton.FlatAppearance.BorderColor = Color.FromArgb(58, 70, 90);
            goBackButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(46, 56, 72);
            goBackButton.Click += (s, e) => ShowLogoChangeUI();
            this.splitContainer.Panel2.Controls.Add(goBackButton);

            // Save Button
            var saveButton = new Button {
                Text = "Save",
                Width = 110,
                Height = 35,
                Location = new Point(145, 545),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(45, 135, 245)
            };
            saveButton.FlatAppearance.BorderSize = 1;
            saveButton.FlatAppearance.BorderColor = Color.FromArgb(90, 170, 255);
            saveButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(65, 155, 255);
            this.splitContainer.Panel2.Controls.Add(saveButton);

            var saveStatusLabel = new Label {
                Location = new Point(270, 553),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 229, 157),
                BackColor = Color.Transparent,
                Text = ""
            };
            this.splitContainer.Panel2.Controls.Add(saveStatusLabel);

            saveButton.Click += (s, e) => {
                string ip = ftpIpBox.Text.Trim();
                if (!int.TryParse(ftpPortBox.Text.Trim(), out int port) || port <= 0 || port > 65535)
                {
                    MessageBox.Show("Please enter a valid FTP Port (1-65535).", "Invalid Port", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string savePath = savePathBox.Text.Trim();
                if (string.IsNullOrEmpty(savePath)) savePath = "/user/data/GameLogos/";
                if (!savePath.StartsWith("/")) savePath = "/" + savePath;
                if (!savePath.EndsWith("/")) savePath += "/";

                settings.FtpIpAddress = ip;
                settings.FtpPort = port;
                settings.PathToSaveLogos = savePath;

                settings.ScreenScraperEnabled  = ssCheck.Checked;
                settings.ScreenScraperUsername = ssUserBox.Text.Trim();
                settings.ScreenScraperPassword = ssPassBox.Text;

                settings.LaunchboxEnabled = lbCheck.Checked;

                settings.SteamGridDbEnabled = sgCheck.Checked;
                settings.SteamGridDbApiKey  = sgKeyBox.Text.Trim();

                settings.Save();
                saveStatusLabel.Text = "Settings saved!";
                MessageBox.Show("Settings saved successfully!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
        }

        private void ShowLogoChangeUI()
        {
            SaveLogoChangeState();
            this.splitContainer.Panel2.Controls.Clear();

            // 1A) Title: "Logo Change"
            var titleLabel = new Label {
                Text = "Logo Change",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 244, 248),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(20, 15)
            };
            this.splitContainer.Panel2.Controls.Add(titleLabel);

            // Settings button top-right
            var settingsBtnInline = new Button {
                Text = "⚙ Settings",
                Width = 100,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(235, 240, 248),
                BackColor = Color.FromArgb(34, 41, 54),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            settingsBtnInline.FlatAppearance.BorderSize = 1;
            settingsBtnInline.FlatAppearance.BorderColor = Color.FromArgb(58, 70, 90);
            settingsBtnInline.FlatAppearance.MouseOverBackColor = Color.FromArgb(46, 56, 72);
            settingsBtnInline.Location = new Point(
                this.splitContainer.Panel2.ClientSize.Width - settingsBtnInline.Width - 15, 15);
            settingsBtnInline.Click += (s, e) => ShowSettingsUI();
            this.splitContainer.Panel2.Controls.Add(settingsBtnInline);

            // Connect button
            connectButton = new Button {
                Text = "Connect",
                Width = 100,
                Height = 30,
                Location = new Point(20, 55),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(45, 135, 245)
            };
            connectButton.FlatAppearance.BorderSize = 1;
            connectButton.FlatAppearance.BorderColor = Color.FromArgb(90, 170, 255);
            connectButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(65, 155, 255);
            connectButton.Click += ConnectButton_Click;
            this.splitContainer.Panel2.Controls.Add(connectButton);

            // Status indicator
            var statusLabel = new Label {
                Name = "statusLabel",
                Text = cachedConnectionStatus,
                AutoSize = true,
                Location = new Point(135, 60),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = cachedConnectionStatusColor,
                BackColor = Color.Transparent
            };
            this.splitContainer.Panel2.Controls.Add(statusLabel);

            // Target FTP display from Settings
            string ftpInfo = string.IsNullOrWhiteSpace(settings.FtpIpAddress)
                ? "(FTP IP not set in Settings)"
                : $"(Target: {settings.FtpIpAddress}:{settings.FtpPort})";
            var targetLabel = new Label {
                Name = "targetLabel",
                Text = ftpInfo,
                AutoSize = true,
                Location = new Point(310, 62),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(138, 153, 173),
                BackColor = Color.Transparent
            };
            this.splitContainer.Panel2.Controls.Add(targetLabel);

            // ------------------------------------------------------------------
            // Content area: split panel2 into LEFT (table + search) and RIGHT (preview)
            // ------------------------------------------------------------------
            int panelW    = this.splitContainer.Panel2.ClientSize.Width - 30;  // minus left padding
            int leftW     = panelW * 55 / 100;   // ~55% for table
            int rightW    = panelW - leftW - 15; // remaining for preview, 15px gap
            int contentTop = 95;

            // ---- Search & Filter Controls (above table) ----
            var searchLabel = new Label {
                Text = "Search:",
                AutoSize = true,
                Location = new Point(20, contentTop + 3),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(230, 236, 245),
                BackColor = Color.Transparent
            };
            this.splitContainer.Panel2.Controls.Add(searchLabel);

            searchTextBox = new TextBox {
                Location = new Point(75, contentTop),
                Width = 175,
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.FromArgb(24, 30, 40),
                ForeColor = Color.FromArgb(240, 244, 248),
                BorderStyle = BorderStyle.FixedSingle,
                Text = cachedSearchText ?? ""
            };
            searchTextBox.TextChanged += (s, e) => {
                cachedSearchText = searchTextBox.Text;
                ApplyTableFilter();
            };
            this.splitContainer.Panel2.Controls.Add(searchTextBox);

            missingLogoCheckBox = new CheckBox {
                Text = "Missing Logo",
                AutoSize = true,
                Location = new Point(265, contentTop + 2),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(230, 236, 245),
                BackColor = Color.Transparent,
                Checked = cachedMissingLogoFilter
            };
            missingLogoCheckBox.CheckedChanged += (s, e) => {
                cachedMissingLogoFilter = missingLogoCheckBox.Checked;
                ApplyTableFilter();
            };
            this.splitContainer.Panel2.Controls.Add(missingLogoCheckBox);

            // ---- LEFT: DataGridView ----
            int tableTop = contentTop + 32;
            int tableHeight = 410;

            resultsDataGridView = new DataGridView {
                Width  = leftW,
                Height = tableHeight,
                Location = new Point(20, tableTop),
                BackgroundColor = Color.FromArgb(24, 29, 38),
                ForeColor = Color.FromArgb(235, 240, 245),
                GridColor = Color.FromArgb(38, 46, 59),
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                EnableHeadersVisualStyles = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,   // Remove left "row" header column
                ScrollBars = ScrollBars.Vertical,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true
            };
            resultsDataGridView.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(32, 39, 52);
            resultsDataGridView.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(220, 228, 240);
            resultsDataGridView.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(32, 39, 52);
            resultsDataGridView.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.FromArgb(220, 228, 240);
            resultsDataGridView.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            resultsDataGridView.ColumnHeadersHeight = 32;
            resultsDataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            resultsDataGridView.DefaultCellStyle.BackColor = Color.FromArgb(24, 29, 38);
            resultsDataGridView.DefaultCellStyle.ForeColor = Color.FromArgb(235, 240, 245);
            resultsDataGridView.DefaultCellStyle.SelectionBackColor = Color.FromArgb(45, 115, 205);
            resultsDataGridView.DefaultCellStyle.SelectionForeColor = Color.White;
            resultsDataGridView.DefaultCellStyle.Font = new Font("Segoe UI", 9F);

            resultsDataGridView.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(20, 25, 34);
            resultsDataGridView.AlternatingRowsDefaultCellStyle.ForeColor = Color.FromArgb(235, 240, 245);
            resultsDataGridView.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(45, 115, 205);
            resultsDataGridView.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.White;
            resultsDataGridView.AlternatingRowsDefaultCellStyle.Font = new Font("Segoe UI", 9F);

            resultsDataGridView.Click += ResultsDataGridView_Click;
            resultsDataGridView.Resize += (s, e) => AdjustColumnWidths();
            this.splitContainer.Panel2.Controls.Add(resultsDataGridView);

            SetupGridColumns();

            if (masterGameTable.Rows.Count > 0)
            {
                ApplyTableFilter();
            }

            // ---- RIGHT: Logo Preview ----
            int rightX = 20 + leftW + 15;

            // Description textbox above logo preview
            this.splitContainer.Panel2.Controls.Add(new Label {
                Text = "Description:",
                AutoSize = true,
                Location = new Point(rightX, contentTop),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(230, 236, 245),
                BackColor = Color.Transparent
            });

            descriptionTextBox = new TextBox {
                Name = "descriptionTextBox",
                Multiline = true,
                Width = rightW,
                Height = 70,
                MaxLength = 150,
                Location = new Point(rightX, contentTop + 20),
                BackColor = Color.FromArgb(24, 30, 40),
                ForeColor = Color.FromArgb(240, 244, 248),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9F),
                ScrollBars = ScrollBars.Vertical
            };
            this.splitContainer.Panel2.Controls.Add(descriptionTextBox);

            // ---- Button below Description: Update Description ----
            updateDescriptionButton = new Button {
                Text = "Update Description",
                Width = 145,
                Height = 28,
                Location = new Point(rightX, contentTop + 95),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(235, 240, 248),
                BackColor = Color.FromArgb(34, 41, 54)
            };
            updateDescriptionButton.FlatAppearance.BorderSize = 1;
            updateDescriptionButton.FlatAppearance.BorderColor = Color.FromArgb(58, 70, 90);
            updateDescriptionButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(46, 56, 72);
            updateDescriptionButton.Click += UpdateDescriptionButton_Click;
            this.splitContainer.Panel2.Controls.Add(updateDescriptionButton);

            this.splitContainer.Panel2.Controls.Add(new Label {
                Text = "Logo Preview:",
                AutoSize = true,
                Location = new Point(rightX, contentTop + 130),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(230, 236, 245),
                BackColor = Color.Transparent
            });

            logoPictureBox = new PictureBox {
                Width  = rightW,
                Height = 120, // Reduced by 25% from 160
                Location = new Point(rightX, contentTop + 150),
                BackColor = Color.FromArgb(24, 30, 40),
                BorderStyle = BorderStyle.None,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            logoPictureBox.Paint += (s, e) => {
                using (var pen = new Pen(Color.FromArgb(46, 56, 70), 1))
                    e.Graphics.DrawRectangle(pen, 0, 0, logoPictureBox.Width - 1, logoPictureBox.Height - 1);
            };
            this.splitContainer.Panel2.Controls.Add(logoPictureBox);

            // ---- Browse Button ----
            browseLogoButton = new Button {
                Text = "Browse PNG...",
                Width = 130,
                Height = 28,
                Location = new Point(rightX, contentTop + 276),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(235, 240, 248),
                BackColor = Color.FromArgb(34, 41, 54)
            };
            browseLogoButton.FlatAppearance.BorderSize = 1;
            browseLogoButton.FlatAppearance.BorderColor = Color.FromArgb(58, 70, 90);
            browseLogoButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(46, 56, 72);
            browseLogoButton.Click += BrowseLogoButton_Click;
            this.splitContainer.Panel2.Controls.Add(browseLogoButton);

            // Label showing chosen file
            var chosenFileLabel = new Label {
                Name = "chosenFileLabel",
                Text = string.IsNullOrEmpty(selectedLogoPngPath) ? "No file selected" : Path.GetFileName(selectedLogoPngPath),
                AutoSize = false,
                Width = rightW,
                Height = 18,
                Location = new Point(rightX, contentTop + 308),
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(138, 153, 173),
                BackColor = Color.Transparent
            };
            this.splitContainer.Panel2.Controls.Add(chosenFileLabel);

            // ---- Upload Button (disabled until Browse) ----
            uploadLogoButton = new Button {
                Text = "Upload Logo",
                Width = 130,
                Height = 30,
                Location = new Point(rightX, contentTop + 328),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(45, 135, 245),
                Enabled = !string.IsNullOrEmpty(selectedLogoPngPath) // disabled until Browse
            };
            uploadLogoButton.FlatAppearance.BorderSize = 1;
            uploadLogoButton.FlatAppearance.BorderColor = Color.FromArgb(90, 170, 255);
            uploadLogoButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(65, 155, 255);
            uploadLogoButton.Click += UploadLogoButton_Click;
            this.splitContainer.Panel2.Controls.Add(uploadLogoButton);

            // ---- Note for user ----
            var noteLabel = new Label {
                Text = "ℹ️ If the game has NO existing logo, the change is instant.\n" +
                       "   If a logo already exists, a console restart may be\n" +
                       "   required to see the updated logo.",
                AutoSize = false,
                Width = rightW,
                Height = 65,
                Location = new Point(rightX, contentTop + 365),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(138, 153, 173),
                BackColor = Color.Transparent
            };
            this.splitContainer.Panel2.Controls.Add(noteLabel);

            // ---- Debug Log (bottom, full width) ----
            int belowTable = contentTop + 450;

            this.splitContainer.Panel2.Controls.Add(new Label {
                Text = "Debug Log:",
                AutoSize = true,
                Location = new Point(20, belowTable),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(138, 153, 173),
                BackColor = Color.Transparent
            });

            debugTextBox = new TextBox {
                Multiline = true,
                Width = this.splitContainer.Panel2.ClientSize.Width - 30, // full panel width
                Height = 90,
                Location = new Point(20, belowTable + 20),
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(13, 16, 23),
                ForeColor = Color.FromArgb(74, 246, 131),
                Font = new Font("Consolas", 9F),
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true
            };
            if (!string.IsNullOrEmpty(cachedDebugLog))
            {
                debugTextBox.Text = cachedDebugLog;
                debugTextBox.SelectionStart = debugTextBox.TextLength;
                debugTextBox.ScrollToCaret();
            }
            this.splitContainer.Panel2.Controls.Add(debugTextBox);
        }

        private void LogDebug(string message)
        {
            if (debugTextBox != null && !debugTextBox.IsDisposed)
            {
                debugTextBox.AppendText(DateTime.Now.ToString("HH:mm:ss") + " - " + message + Environment.NewLine);
                debugTextBox.SelectionStart = debugTextBox.TextLength;
                debugTextBox.ScrollToCaret();
            }
        }

        private bool TryGetConnectionParams(out string ip, out int port)
        {
            ip = settings.FtpIpAddress?.Trim() ?? "";
            port = settings.FtpPort;
            if (string.IsNullOrEmpty(ip))
            {
                MessageBox.Show("Please configure the FTP IP Address in Settings first.", "FTP Not Configured", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (port <= 0 || port > 65535)
            {
                MessageBox.Show("Please enter a valid FTP Port (1-65535) in Settings.", "Invalid Port", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        private async void ConnectButton_Click(object sender, EventArgs e)
        {
            if (!TryGetConnectionParams(out string ip, out int port)) return;

            var statusLabel = this.splitContainer.Panel2.Controls.Find("statusLabel", true).FirstOrDefault() as Label;
            if (statusLabel != null) { statusLabel.Text = "●  Connecting..."; statusLabel.ForeColor = Color.FromArgb(255, 204, 0); }

            LogDebug("Starting FTP connection...");
            LogDebug("IP: " + ip + "  Port: " + port);

            connectButton.Enabled = false;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                string tempDbPath = await DownloadDatabaseFromFtpAsync(ip, port);
                if (string.IsNullOrEmpty(tempDbPath)) throw new Exception("Failed to download database");
                LogDebug("Download successful. Reading DB...");
                await ReadSqliteDatabaseAndPopulateGridAsync(tempDbPath);
                if (statusLabel != null) { statusLabel.Text = "●  Connected"; statusLabel.ForeColor = Color.FromArgb(46, 229, 157); }
                LogDebug("Database read successfully");
                // No success popup — status indicator is enough
            }
            catch (Exception ex)
            {
                if (statusLabel != null) { statusLabel.Text = "●  Not Connected"; statusLabel.ForeColor = Color.FromArgb(248, 81, 73); }
                LogDebug("Error: " + ex.Message);
                MessageBox.Show("Failed to connect: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { connectButton.Enabled = true; this.Cursor = Cursors.Default; }
        }

        private void BrowseLogoButton_Click(object sender, EventArgs e)
        {
            if (resultsDataGridView == null || resultsDataGridView.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a game row first.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string conceptName = GetCellValue(resultsDataGridView.SelectedRows[0], "Game");
            using (var ofd = new OpenFileDialog {
                Title = "Select logo PNG for: " + conceptName,
                Filter = "PNG Images|*.png"
            })
            {
                if (ofd.ShowDialog() != DialogResult.OK) return;
                selectedLogoPngPath = ofd.FileName;
            }

            // Update the chosen file label
            var lbl = this.splitContainer.Panel2.Controls.Find("chosenFileLabel", true).FirstOrDefault() as Label;
            if (lbl != null) lbl.Text = Path.GetFileName(selectedLogoPngPath);

            // Enable Upload button now that a file is selected
            if (uploadLogoButton != null) uploadLogoButton.Enabled = true;

            // Show a local preview of the selected PNG
            try
            {
                var img = Image.FromFile(selectedLogoPngPath);
                if (logoPictureBox != null) logoPictureBox.Image = img;
            }
            catch { }

            LogDebug("Selected file: " + selectedLogoPngPath);
        }

        private async void UploadLogoButton_Click(object sender, EventArgs e)
        {
            if (!TryGetConnectionParams(out string ip, out int port)) return;

            if (resultsDataGridView == null || resultsDataGridView.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a game row first.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(selectedLogoPngPath) || !File.Exists(selectedLogoPngPath))
            {
                MessageBox.Show("Please use Browse to select a PNG file first.", "No File Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string conceptName = GetCellValue(resultsDataGridView.SelectedRows[0], "Game");
            if (string.IsNullOrEmpty(conceptName))
            {
                MessageBox.Show("Selected row has no game name.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Critical warning confirmation before modifying / overwriting database on PS5
            string warningMessage = "::WARNING::\r\n" +
                                   "This app is about to copy and overwrite the app.db on your /system_ex/ folder.\r\n" +
                                   "Make sure all games/apps are closed on the PS5 before proceeding.\r\n\r\n" +
                                   "DB Rebuild or Kernel Panic can occur; Use at your own risk";
            var warnConfirm = MessageBox.Show(warningMessage, "::WARNING::", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
            if (warnConfirm != DialogResult.OK)
            {
                LogDebug("Upload cancelled by user.");
                return;
            }

            string sanitizedName = SanitizeGameName(conceptName);
            string saveDir = string.IsNullOrWhiteSpace(settings.PathToSaveLogos) ? "/user/data/GameLogos/" : settings.PathToSaveLogos.Trim();
            if (!saveDir.StartsWith("/")) saveDir = "/" + saveDir;
            if (!saveDir.EndsWith("/")) saveDir += "/";
            string remoteLogoPath = saveDir + sanitizedName + ".png";
            string localPngPath   = selectedLogoPngPath;

            LogDebug("Game: " + conceptName);
            LogDebug("Sanitized: " + sanitizedName);
            LogDebug("Remote logo path: " + remoteLogoPath);
            LogDebug("Local PNG: " + localPngPath);

            uploadLogoButton.Enabled = false;
            browseLogoButton.Enabled = false;
            this.Cursor = Cursors.WaitCursor;
            string tempDbPath    = null;
            string uploadCopyPath = null;

            try
            {
                // A) Upload PNG to FTP
                LogDebug("Uploading logo PNG to FTP...");
                await UploadFileToFtpAsync(ip, port, localPngPath, remoteLogoPath);
                LogDebug("Logo PNG uploaded.");

                // B) Download app.db
                LogDebug("Downloading app.db...");
                tempDbPath = await DownloadDatabaseFromFtpAsync(ip, port);
                if (string.IsNullOrEmpty(tempDbPath)) throw new Exception("Failed to download app.db");
                LogDebug("app.db downloaded.");

                // C) Update database
                LogDebug("Updating database...");
                int rowsAffected = await UpdateLogoInDatabaseAsync(tempDbPath, conceptName, remoteLogoPath);
                if (rowsAffected == 0)
                {
                    LogDebug("WARNING: UPDATE affected 0 rows.");
                    MessageBox.Show(
                        "UPDATE affected 0 rows.\nConceptName: '" + conceptName + "'\nVerify it matches exactly in the DB.",
                        "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                LogDebug("Database updated. Rows affected: " + rowsAffected);

                // D) Create upload copy
                LogDebug("Creating upload copy of updated database...");
                uploadCopyPath = Path.Combine(Path.GetTempPath(), "app_upload_" + Guid.NewGuid().ToString("N") + ".db");
                using (var src = new FileStream(tempDbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var dst = new FileStream(uploadCopyPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    await src.CopyToAsync(dst);
                LogDebug("Upload copy created.");

                // E) Upload modified app.db back
                LogDebug("Uploading updated app.db...");
                await UploadFileToFtpAsync(ip, port, uploadCopyPath, "/system_data/priv/mms/app.db");
                LogDebug("app.db uploaded.");

                // F) Refresh grid
                LogDebug("Refreshing data grid...");
                string tempDbPath2 = await DownloadDatabaseFromFtpAsync(ip, port);
                if (!string.IsNullOrEmpty(tempDbPath2))
                    await ReadSqliteDatabaseAndPopulateGridAsync(tempDbPath2);

                LogDebug("Upload completed!");
                MessageBox.Show(
                    "Logo updated!\n\nGame: " + conceptName + "\nPath: " + remoteLogoPath,
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogDebug("Error: " + ex.Message);
                MessageBox.Show("Failed to upload logo: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (!string.IsNullOrEmpty(tempDbPath))
                    try { if (File.Exists(tempDbPath)) File.Delete(tempDbPath); } catch { }
                if (!string.IsNullOrEmpty(uploadCopyPath))
                    try { if (File.Exists(uploadCopyPath)) File.Delete(uploadCopyPath); } catch { }
                uploadLogoButton.Enabled = true;
                browseLogoButton.Enabled = true;
                this.Cursor = Cursors.Default;
            }
        }

        private async void UpdateDescriptionButton_Click(object sender, EventArgs e)
        {
            if (resultsDataGridView == null || resultsDataGridView.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a game row first.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string conceptName = GetCellValue(resultsDataGridView.SelectedRows[0], "Game");
            if (string.IsNullOrEmpty(conceptName))
            {
                MessageBox.Show("Selected row has no game name.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string newDesc = descriptionTextBox != null ? descriptionTextBox.Text : null;
            if (!string.IsNullOrEmpty(newDesc) && newDesc.Length > 150)
            {
                newDesc = newDesc.Substring(0, 150);
            }
            if (string.IsNullOrWhiteSpace(newDesc))
            {
                newDesc = null;
            }

            // Update in-memory table first
            var row = resultsDataGridView.SelectedRows[0];
            if (row.DataGridView.Columns.Contains("Description"))
                row.Cells["Description"].Value = newDesc ?? "";
            foreach (DataRow dr in masterGameTable.Rows)
            {
                if ((dr["Game"]?.ToString() ?? "") == conceptName)
                {
                    dr["Description"] = newDesc ?? "";
                    break;
                }
            }

            if (!TryGetConnectionParams(out string ip, out int port))
            {
                LogDebug("Description updated locally for: " + conceptName);
                MessageBox.Show("Description updated in table for '" + conceptName + "'. Connect to FTP to save changes to PS5.", "Updated Locally", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string warningMessage = "::WARNING::\r\n" +
                                   "This app is about to copy and overwrite the app.db on your /system_ex/ folder.\r\n" +
                                   "Make sure all games/apps are closed on the PS5 before proceeding.\r\n\r\n" +
                                   "DB Rebuild or Kernel Panic can occur; Use at your own risk";
            var warnConfirm = MessageBox.Show(warningMessage, "::WARNING::", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
            if (warnConfirm != DialogResult.OK)
            {
                LogDebug("Description update cancelled by user.");
                return;
            }

            if (updateDescriptionButton != null) updateDescriptionButton.Enabled = false;
            this.Cursor = Cursors.WaitCursor;
            string tempDbPath = null;
            string uploadCopyPath = null;

            try
            {
                LogDebug("Downloading app.db for description update...");
                tempDbPath = await DownloadDatabaseFromFtpAsync(ip, port);
                if (string.IsNullOrEmpty(tempDbPath)) throw new Exception("Failed to download app.db");

                LogDebug("Updating description in database...");
                int rowsAffected = await UpdateDescriptionInDatabaseAsync(tempDbPath, conceptName, newDesc);
                LogDebug("Description update affected: " + rowsAffected + " row(s)");

                uploadCopyPath = Path.Combine(Path.GetTempPath(), "app_upload_" + Guid.NewGuid().ToString("N") + ".db");
                using (var src = new FileStream(tempDbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var dst = new FileStream(uploadCopyPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    await src.CopyToAsync(dst);

                LogDebug("Uploading updated app.db...");
                await UploadFileToFtpAsync(ip, port, uploadCopyPath, "/system_data/priv/mms/app.db");
                LogDebug("app.db uploaded successfully.");

                MessageBox.Show("Description updated successfully on PS5!\n\nGame: " + conceptName, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogDebug("Error updating description: " + ex.Message);
                MessageBox.Show("Failed to update description: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (!string.IsNullOrEmpty(tempDbPath))
                    try { if (File.Exists(tempDbPath)) File.Delete(tempDbPath); } catch { }
                if (!string.IsNullOrEmpty(uploadCopyPath))
                    try { if (File.Exists(uploadCopyPath)) File.Delete(uploadCopyPath); } catch { }
                if (updateDescriptionButton != null) updateDescriptionButton.Enabled = true;
                this.Cursor = Cursors.Default;
            }
        }

        private async void ResultsDataGridView_Click(object sender, EventArgs e)
        {
            if (logoPictureBox != null) logoPictureBox.Image = null;
            if (resultsDataGridView == null || resultsDataGridView.SelectedRows.Count == 0) return;

            var row = resultsDataGridView.SelectedRows[0];
            string conceptName = GetCellValue(row, "Game");
            string logoPath    = GetCellValue(row, "Logo Path");

            // Populate description textbox from selected row
            var descBox = this.splitContainer.Panel2.Controls.Find("descriptionTextBox", true).FirstOrDefault() as TextBox;
            if (descBox != null)
            {
                try   { descBox.Text = GetCellValue(row, "Description"); }
                catch { descBox.Text = ""; }
            }

            LogDebug("Selected: " + conceptName);
            LogDebug("Logo path: " + logoPath);

            if (string.IsNullOrEmpty(logoPath)) { LogDebug("No logo path."); return; }
            if (!TryGetConnectionParams(out string ip, out int port)) return;

            try
            {
                string cleanPath = GetCleanFilePath(logoPath).TrimStart('/');
                string tmpPath   = await DownloadImageFromFtpAsync(ip, port, cleanPath);
                if (!string.IsNullOrEmpty(tmpPath) && File.Exists(tmpPath))
                {
                    try
                    {
                        var img = Image.FromFile(tmpPath);
                        logoPictureBox.Invoke(new Action(() => logoPictureBox.Image = img));
                        LogDebug("Logo preview loaded.");
                    }
                    catch (Exception imgEx) { LogDebug("Image load error: " + imgEx.Message); }
                    finally { try { File.Delete(tmpPath); } catch { } }
                }
                else LogDebug("Logo could not be downloaded.");
            }
            catch (Exception ex) { LogDebug("Preview error: " + ex.Message); logoPictureBox.Image = null; }
        }

        private async Task<string> DownloadDatabaseFromFtpAsync(string ip, int port)
        {
            string tmp = Path.Combine(Path.GetTempPath(), "app_" + Guid.NewGuid().ToString("N") + ".db");
            try
            {
                string ftpUrl = "ftp://" + ip + ":" + port + "/system_data/priv/mms/app.db";
                LogDebug("FTP download: " + ftpUrl);
                var req = (FtpWebRequest)WebRequest.Create(ftpUrl);
                req.Method = WebRequestMethods.Ftp.DownloadFile;
                req.UseBinary = true; req.KeepAlive = false;
                req.Credentials = new NetworkCredential("anonymous", "anonymous@");
                using (var resp = (FtpWebResponse)await req.GetResponseAsync())
                using (var rs   = resp.GetResponseStream())
                using (var fs   = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                    await rs.CopyToAsync(fs);
                LogDebug("Download complete.");
                return tmp;
            }
            catch (Exception ex)
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
                LogDebug("FTP download failed: " + ex.Message);
                throw new Exception("FTP download failed: " + ex.Message, ex);
            }
        }

        private async Task UploadFileToFtpAsync(string ip, int port, string localPath, string remotePath)
        {
            string ftpUrl = "ftp://" + ip + ":" + port + "/" + remotePath.TrimStart('/');
            LogDebug("FTP upload: " + ftpUrl);
            var req = (FtpWebRequest)WebRequest.Create(ftpUrl);
            req.Method = WebRequestMethods.Ftp.UploadFile;
            req.UseBinary = true; req.KeepAlive = false;
            req.Credentials = new NetworkCredential("anonymous", "anonymous@");
            using (var fs = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                req.ContentLength = fs.Length;
                using (var ws = await req.GetRequestStreamAsync())
                    await fs.CopyToAsync(ws);
            }
            using (var resp = (FtpWebResponse)await req.GetResponseAsync())
                LogDebug("Upload done. Status: " + (resp.StatusDescription?.Trim() ?? ""));
        }

        private async Task<string> DownloadImageFromFtpAsync(string ip, int port, string remotePath)
        {
            string tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + Path.GetExtension(remotePath));
            try
            {
                string ftpUrl = "ftp://" + ip + ":" + port + "/" + remotePath;
                LogDebug("Image download: " + ftpUrl);
                var req = (FtpWebRequest)WebRequest.Create(ftpUrl);
                req.Method = WebRequestMethods.Ftp.DownloadFile;
                req.UseBinary = true; req.KeepAlive = false;
                req.Credentials = new NetworkCredential("anonymous", "anonymous@");
                using (var resp = (FtpWebResponse)await req.GetResponseAsync())
                using (var rs   = resp.GetResponseStream())
                using (var fs   = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                    await rs.CopyToAsync(fs);
                return tmp;
            }
            catch (Exception ex)
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
                LogDebug("Image download failed: " + ex.Message);
                throw new Exception("FTP image download failed: " + ex.Message, ex);
            }
        }

        private async Task ReadSqliteDatabaseAndPopulateGridAsync(string dbPath)
        {
            SetupGridColumns();
            try
            {
                if (!File.Exists(dbPath)) throw new Exception("DB file not found");
                LogDebug("Opening database...");

                bool ok = false;
                try { Assembly.Load("System.Data.SQLite"); ok = true; LogDebug("SQLite found."); }
                catch { LogDebug("SQLite assembly not found."); }

                if (ok)
                {
                    try
                    {
                        Type connType = Type.GetType("System.Data.SQLite.SQLiteConnection, System.Data.SQLite");
                        Type cmdType  = Type.GetType("System.Data.SQLite.SQLiteCommand, System.Data.SQLite");
                        Type rdType   = Type.GetType("System.Data.SQLite.SQLiteDataReader, System.Data.SQLite");
                        if (connType == null || cmdType == null || rdType == null)
                            throw new Exception("SQLite types not found");

                        object conn = Activator.CreateInstance(connType, "Data Source=" + dbPath + ";Version=3;Pooling=False;");
                        connType.GetMethod("Open").Invoke(conn, null);
                        try
                        {
                            string query = "SELECT conceptName, logoImageInfo, description FROM tbl_conceptmetadata ORDER BY conceptName ASC";
                            LogDebug("Query: " + query);
                            object cmd = Activator.CreateInstance(cmdType, query, conn);
                            try
                            {
                                object rd  = cmdType.GetMethod("ExecuteReader", Type.EmptyTypes).Invoke(cmd, null);
                                try
                                {
                                    EnsureMasterTableColumns();
                                    masterGameTable.Rows.Clear();
                                    int rows = 0;
                                    while ((bool)rd.GetType().GetMethod("Read").Invoke(rd, null))
                                    {
                                        string game = rd.GetType().GetMethod("GetValue", new[] { typeof(int) }).Invoke(rd, new object[] { 0 })?.ToString() ?? "";
                                        string logo = rd.GetType().GetMethod("GetValue", new[] { typeof(int) }).Invoke(rd, new object[] { 1 })?.ToString() ?? "";
                                        string desc = rd.GetType().GetMethod("GetValue", new[] { typeof(int) }).Invoke(rd, new object[] { 2 })?.ToString() ?? "";
                                        masterGameTable.Rows.Add(game, logo, desc);
                                        rows++;
                                    }
                                    LogDebug("Read " + rows + " rows.");
                                    ApplyTableFilter();
                                }
                                finally
                                {
                                    rd.GetType().GetMethod("Close")?.Invoke(rd, null);
                                    rd.GetType().GetMethod("Dispose")?.Invoke(rd, null);
                                }
                            }
                            finally
                            {
                                cmdType.GetMethod("Dispose")?.Invoke(cmd, null);
                            }
                        }
                        finally
                        {
                            connType.GetMethod("Close")?.Invoke(conn, null);
                            connType.GetMethod("Dispose")?.Invoke(conn, null);
                            connType.GetMethod("ClearAllPools", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                        }
                    }
                    catch (Exception sqlEx)
                    {
                        LogDebug("SQLite error: " + sqlEx.Message);
                        MessageBox.Show("SQLite error: " + sqlEx.Message + "\n\nInstall System.Data.SQLite via NuGet.\nShowing sample data.", "SQLite Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        PopulateSampleData();
                    }
                }
                else
                {
                    MessageBox.Show("SQLite not available. Install System.Data.SQLite via NuGet.\nShowing sample data.", "SQLite Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    PopulateSampleData();
                }
            }
            catch (Exception ex)
            {
                LogDebug("DB read error: " + ex.Message);
                MessageBox.Show("Error reading DB: " + ex.Message + "\nShowing sample data.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                PopulateSampleData();
            }
            finally
            {
                try { if (File.Exists(dbPath)) File.Delete(dbPath); } catch { }
                LogDebug("Temp DB cleaned up.");
            }
        }

        private async Task<int> UpdateLogoInDatabaseAsync(string dbPath, string conceptName, string newLogoPath)
        {
            if (!File.Exists(dbPath)) throw new Exception("DB file not found");

            bool ok = false;
            try { Assembly.Load("System.Data.SQLite"); ok = true; } catch { }
            if (!ok) throw new Exception("SQLite not available. Install System.Data.SQLite.");

            Type connType = Type.GetType("System.Data.SQLite.SQLiteConnection, System.Data.SQLite");
            Type cmdType  = Type.GetType("System.Data.SQLite.SQLiteCommand, System.Data.SQLite");
            if (connType == null || cmdType == null) throw new Exception("SQLite types not found");

            object conn = Activator.CreateInstance(connType, "Data Source=" + dbPath + ";Version=3;Pooling=False;");
            connType.GetMethod("Open").Invoke(conn, null);
            LogDebug("SQLite opened for UPDATE");

            int affected = 0;
            try
            {
                string safeName = conceptName.Replace("'", "''");
                string safePath = newLogoPath.Replace("'", "''");
                string sql = "UPDATE tbl_conceptmetadata SET logoImageInfo = '" + safePath + "' WHERE conceptName = '" + safeName + "'";
                LogDebug("Executing: " + sql);
                object cmd = Activator.CreateInstance(cmdType, sql, conn);
                try
                {
                    affected = (int)cmd.GetType().GetMethod("ExecuteNonQuery", Type.EmptyTypes).Invoke(cmd, null);
                    LogDebug("UPDATE affected " + affected + " row(s)");
                }
                finally
                {
                    cmdType.GetMethod("Dispose")?.Invoke(cmd, null);
                }
            }
            finally
            {
                connType.GetMethod("Close")?.Invoke(conn, null);
                connType.GetMethod("Dispose")?.Invoke(conn, null);
                connType.GetMethod("ClearAllPools", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            return affected;
        }

        private async Task<int> UpdateDescriptionInDatabaseAsync(string dbPath, string conceptName, string newDescription)
        {
            if (!File.Exists(dbPath)) throw new Exception("DB file not found");

            bool ok = false;
            try { Assembly.Load("System.Data.SQLite"); ok = true; } catch { }
            if (!ok) throw new Exception("SQLite not available. Install System.Data.SQLite.");

            Type connType = Type.GetType("System.Data.SQLite.SQLiteConnection, System.Data.SQLite");
            Type cmdType  = Type.GetType("System.Data.SQLite.SQLiteCommand, System.Data.SQLite");
            if (connType == null || cmdType == null) throw new Exception("SQLite types not found");

            object conn = Activator.CreateInstance(connType, "Data Source=" + dbPath + ";Version=3;Pooling=False;");
            connType.GetMethod("Open").Invoke(conn, null);
            LogDebug("SQLite opened for description UPDATE");

            int affected = 0;
            try
            {
                string safeName = conceptName.Replace("'", "''");
                string sql;
                if (string.IsNullOrEmpty(newDescription))
                {
                    sql = "UPDATE tbl_conceptmetadata SET description = NULL WHERE conceptName = '" + safeName + "'";
                }
                else
                {
                    string safeDesc = newDescription.Replace("'", "''");
                    sql = "UPDATE tbl_conceptmetadata SET description = '" + safeDesc + "' WHERE conceptName = '" + safeName + "'";
                }
                LogDebug("Executing: " + sql);
                object cmd = Activator.CreateInstance(cmdType, sql, conn);
                try
                {
                    affected = (int)cmd.GetType().GetMethod("ExecuteNonQuery", Type.EmptyTypes).Invoke(cmd, null);
                    LogDebug("Description UPDATE affected " + affected + " row(s)");
                }
                finally
                {
                    cmdType.GetMethod("Dispose")?.Invoke(cmd, null);
                }
            }
            finally
            {
                connType.GetMethod("Close")?.Invoke(conn, null);
                connType.GetMethod("Dispose")?.Invoke(conn, null);
                connType.GetMethod("ClearAllPools", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            return affected;
        }

        private void PopulateSampleData()
        {
            SetupGridColumns();
            EnsureMasterTableColumns();
            masterGameTable.Rows.Clear();
            string saveDir = string.IsNullOrWhiteSpace(settings.PathToSaveLogos) ? "/user/data/GameLogos/" : settings.PathToSaveLogos.Trim();
            if (!saveDir.StartsWith("/")) saveDir = "/" + saveDir;
            if (!saveDir.EndsWith("/")) saveDir += "/";
            masterGameTable.Rows.Add("Sample Game 1", saveDir + "SampleGame1.png", "First sample game description.");
            masterGameTable.Rows.Add("Sample Game 2", "", "Second sample game with missing logo.");
            masterGameTable.Rows.Add("Sample Game 3", saveDir + "SampleGame3.png", "Third sample game description.");
            masterGameTable.Rows.Add("Sample Game 4", "", "Fourth sample game with missing logo.");
            masterGameTable.Rows.Add("Sample Game 5", saveDir + "SampleGame5.png", "Fifth sample game description.");
            ApplyTableFilter();
        }

        /// <summary>
        /// Strips special chars, keeps letters/digits/spaces/hyphens.
        /// "STAR WARS: Galactic Racer(TM)" becomes "STAR WARS Galactic Racer"
        /// </summary>
        private string SanitizeGameName(string name)
        {
            string s = Regex.Replace(name, @"[^a-zA-Z0-9 \-]", "");
            s = Regex.Replace(s, @" {2,}", " ");
            return s.Trim();
        }

        private string GetCleanFilePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return filePath;
            string lower = filePath.ToLowerInvariant();
            int png  = lower.LastIndexOf(".png");
            int jpg  = lower.LastIndexOf(".jpg");
            int jpeg = lower.LastIndexOf(".jpeg");
            int dds  = lower.LastIndexOf(".dds");
            int idx  = Math.Max(Math.Max(png, jpg), Math.Max(jpeg, dds));
            if (idx < 0) return filePath;
            int len = (idx == jpeg) ? 5 : 4;
            return filePath.Substring(0, idx + len);
        }
    }

    public class AppSettings
    {
        public string FtpIpAddress { get; set; } = "";
        public int FtpPort { get; set; } = 2121;
        public string PathToSaveLogos { get; set; } = "/user/data/GameLogos/";

        // API Setup
        public bool ScreenScraperEnabled { get; set; } = false;
        public string ScreenScraperUsername { get; set; } = "";
        public string ScreenScraperPassword { get; set; } = "";

        public bool LaunchboxEnabled { get; set; } = false;

        public bool SteamGridDbEnabled { get; set; } = false;
        public string SteamGridDbApiKey { get; set; } = "";

        private static string GetConfigDirectory()
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PS5-Logo-Changer");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                try
                {
                    string oldDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PS5-Customizing-Utility");
                    string oldPath = Path.Combine(oldDir, "settings.json");
                    string newPath = Path.Combine(dir, "settings.json");
                    if (File.Exists(oldPath) && !File.Exists(newPath))
                    {
                        File.Copy(oldPath, newPath);
                    }
                }
                catch { }
            }
            return dir;
        }

        private static string GetConfigPath()
        {
            return Path.Combine(GetConfigDirectory(), "settings.json");
        }

        public static AppSettings Load()
        {
            try
            {
                string path = GetConfigPath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                    if (loaded != null) return loaded;
                }
            }
            catch { }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                string path = GetConfigPath();
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save settings: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
