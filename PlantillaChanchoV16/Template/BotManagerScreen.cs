using Guna.UI2.WinForms;
using Newtonsoft.Json.Linq;
using PlantillaChanchoV16.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlantillaChanchoV16.Template
{
    // Bot Manager : pilote le bot Discord PaiPai depuis l'appli elle-meme.
    // Calque visuel de WindowsPaiScreen.cs (fenetre borderless sakura, glow,
    // drag, bannieres a accent), avec une geometrie a marge de securite genereuse
    // (contrairement a la v1, chaque section utilise un curseur Y cumulatif ->
    // aucune section ne peut chevaucher la suivante par erreur de calcul).
    // Deux volets independants :
    // - Process local (node index.js) si un dossier est renseigne pour le profil actif ;
    // - Actions API Discord directes (marchent meme sans process local).
    internal class BotManagerScreen : Form
    {
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        // Cle = key discord-bot (config/products.js), Value = nom affiche.
        private static readonly (string Key, string Name)[] Products =
        {
            ("woofer", "Woofer"),
            ("valorant", "Valorant"),
            ("roblox", "Roblox"),
            ("windowspai", "Windows PaiPai"),
        };

        private static readonly BotProcessManager Proc = new BotProcessManager();

        private const int Pad = 34;

        private Guna2ComboBox _profileCombo;
        private Guna2Button _addBtn, _editBtn, _delBtn;
        private Guna2Button _startStopBtn, _restartBtn;
        private Label _procStatus, _procHint;
        private Guna2TextBox _log;
        private BotProfile? _active;

        public BotManagerScreen()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(880, 760);
            BackColor = Colors.bgColor;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            using (var p = Rounded(new Rectangle(0, 0, Width, Height), 14)) Region = new Region(p);

            // En-tete.
            MakeLbl("Pai", Color.White, new Font("Inter Semibold", 12f), new Point(Pad, 22), true);
            float pW = TextRenderer.MeasureText("Pai", new Font("Inter Semibold", 12f)).Width;
            MakeLbl("Pai", Colors.mainColor, new Font("Inter Semibold", 12f), new Point(Pad + (int)pW - 6, 22), true);
            MakeLbl("Bot Manager", Colors.mainColor, new Font("Inter Semibold", 18f), new Point(Pad, 48), true);
            MakeLbl("Control your Discord bot directly from PaiPai.", Color.FromArgb(170, 255, 255, 255),
                new Font("Inter Medium", 10f), new Point(Pad, 80), true);

            var close = new WindowButton(WindowButton.Glyph.Close, Color.FromArgb(255, 95, 87))
            { Parent = this, Location = new Point(Width - 42, 18) };
            close.Clicked += (s, e) => Close();

            int y = BuildProfileBar(112);
            y = BuildProcessBanner(y + 22);
            y = BuildLogPanel(y + 26);
            BuildQuickActions(y + 26);

            ReloadProfileCombo();
            Proc.OutputReceived += line => AppendLog(line);
            Proc.Exited += () => BeginInvoke(new Action(() => { AppendLog("[process] Le bot s'est arrêté."); RefreshProcUi(); }));

            this.MouseDown += Drag;
        }

        // ---- Barre de profils (multi-bot) ----
        private int BuildProfileBar(int y)
        {
            var lbl = new Guna2HtmlLabel
            {
                Parent = this, Text = "ACTIVE BOT", ForeColor = Color.FromArgb(150, 255, 255, 255),
                Font = new Font("Inter Semibold", 8.5f), AutoSize = true, BackColor = Color.Transparent,
                Location = new Point(Pad, y)
            };

            int rowY = y + 20, rowH = 44;

            _delBtn = MakeSmallButton("Delete", rowH, 92);
            _delBtn.Location = new Point(Width - Pad - _delBtn.Width, rowY);
            _delBtn.ForeColor = Color.FromArgb(255, 130, 120);
            _delBtn.Click += (s, e) =>
            {
                if (_active == null) return;
                var r = SakuraMessageBox.Show($"Delete bot profile \"{_active.Name}\"?", "Bot Manager", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (r != DialogResult.Yes) return;
                if (Proc.IsRunning) Proc.Stop();
                BotProfileStore.Delete(_active.Id);
                ReloadProfileCombo();
            };

            _editBtn = MakeSmallButton("Edit", rowH, 80);
            _editBtn.Location = new Point(_delBtn.Left - 10 - _editBtn.Width, rowY);
            _editBtn.Click += (s, e) =>
            {
                if (_active == null) return;
                using var dlg = new BotProfileDialog(_active);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    BotProfileStore.AddOrUpdate(dlg.Result);
                    ReloadProfileCombo();
                }
            };

            _addBtn = MakeSmallButton("+ Add", rowH, 92);
            _addBtn.Location = new Point(_editBtn.Left - 10 - _addBtn.Width, rowY);
            _addBtn.Click += (s, e) =>
            {
                using var dlg = new BotProfileDialog(null);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    BotProfileStore.AddOrUpdate(dlg.Result);
                    BotProfileStore.SetActive(dlg.Result.Id);
                    ReloadProfileCombo();
                }
            };

            int comboW = _addBtn.Left - 14 - Pad;
            _profileCombo = new Guna2ComboBox
            {
                Parent = this, Location = new Point(Pad, rowY), Size = new Size(comboW, rowH),
                Font = new Font("Inter Medium", 10.5f), BorderRadius = 10,
            };
            GunaTheme.StyleCombo(_profileCombo);
            _profileCombo.SelectedIndexChanged += (s, e) =>
            {
                if (_profileCombo.SelectedIndex < 0) return;
                var profiles = BotProfileStore.GetAll();
                if (_profileCombo.SelectedIndex >= profiles.Count) return;
                var chosen = profiles[_profileCombo.SelectedIndex];
                if (_active?.Id == chosen.Id) return;
                if (Proc.IsRunning) Proc.Stop();
                BotProfileStore.SetActive(chosen.Id);
                _active = chosen;
                RefreshProcUi();
            };

            return rowY + rowH;
        }

        private Guna2Button MakeSmallButton(string text, int height, int width)
        {
            var b = new Guna2Button
            {
                Parent = this, Text = text, Font = new Font("Inter Semibold", 9.5f), ForeColor = Color.White,
                FillColor = Colors.scColor, BorderRadius = 10, BorderThickness = 0, Size = new Size(width, height),
                Cursor = Cursors.Hand, Animated = true, UseTransparentBackground = true,
            };
            b.HoverState.FillColor = ControlPaint.Light(Colors.scColor, 0.3f);
            return b;
        }

        private void ReloadProfileCombo()
        {
            var profiles = BotProfileStore.GetAll();
            _profileCombo.Items.Clear();
            foreach (var p in profiles) _profileCombo.Items.Add(p.Name);

            _active = BotProfileStore.GetActive();
            if (_active != null)
            {
                int idx = profiles.FindIndex(p => p.Id == _active.Id);
                if (idx >= 0) _profileCombo.SelectedIndex = idx;
            }
            RefreshProcUi();
        }

        // ---- Banniere de controle du process local ----
        private Guna2Panel _banner;
        private int _bannerTop;
        private int BuildProcessBanner(int y)
        {
            _bannerTop = y;
            int bannerH = 78;
            _banner = new Guna2Panel
            {
                Parent = this, Location = new Point(Pad, y), Size = new Size(Width - Pad * 2, bannerH),
                FillColor = Colors.scColor, BorderRadius = 12, BorderThickness = 0, UseTransparentBackground = true,
                CustomBorderThickness = new Padding(3, 0, 0, 0), CustomBorderColor = Colors.mainColor
            };

            new BotIcon(BotIcon.Kind.Bot) { Parent = _banner, Location = new Point(18, (bannerH - 48) / 2), Size = new Size(48, 48) };

            _procStatus = new Label
            {
                Parent = _banner, Text = "No bot selected", ForeColor = Color.FromArgb(190, 255, 255, 255),
                BackColor = Color.Transparent, Font = new Font("Inter Semibold", 11f), AutoSize = true,
                Location = new Point(80, 16)
            };
            _procHint = new Label
            {
                Parent = _banner, Text = "Add a bot profile to get started.", ForeColor = Color.FromArgb(140, 255, 255, 255),
                BackColor = Color.Transparent, Font = new Font("Inter Medium", 8.5f), AutoSize = true,
                Location = new Point(80, 42)
            };

            _restartBtn = new Guna2Button
            {
                Parent = _banner, Text = "Restart", Font = new Font("Inter Semibold", 10f), ForeColor = Color.White,
                FillColor = Colors.bgColor, BorderRadius = 9, BorderThickness = 0, Size = new Size(112, 42),
                Cursor = Cursors.Hand, Animated = true, UseTransparentBackground = true,
            };
            _restartBtn.Location = new Point(_banner.Width - 18 - _restartBtn.Width, (bannerH - 42) / 2);
            _restartBtn.HoverState.FillColor = ControlPaint.Light(Colors.bgColor, 0.3f);
            _restartBtn.Click += (s, e) =>
            {
                if (_active?.LocalFolderPath == null) return;
                AppendLog("[process] Redémarrage…");
                var (ok, error) = Proc.Restart(_active.LocalFolderPath);
                if (!ok) AppendLog($"[erreur] {error}");
                RefreshProcUi();
            };

            _startStopBtn = new Guna2Button
            {
                Parent = _banner, Text = "Start", Font = new Font("Inter Semibold", 10f), ForeColor = Color.White,
                FillColor = Colors.mainColor, BorderRadius = 9, BorderThickness = 0, Size = new Size(124, 42),
                Cursor = Cursors.Hand, Animated = true, UseTransparentBackground = true,
            };
            _startStopBtn.Location = new Point(_restartBtn.Left - 10 - _startStopBtn.Width, (bannerH - 42) / 2);
            _startStopBtn.HoverState.FillColor = ControlPaint.Light(Colors.mainColor, 0.2f);
            _startStopBtn.Click += (s, e) => ToggleProcess();

            return y + bannerH;
        }

        private void ToggleProcess()
        {
            if (_active == null) return;

            if (Proc.IsRunning)
            {
                Proc.Stop();
                AppendLog("[process] Bot arrêté.");
                RefreshProcUi();
                return;
            }

            if (string.IsNullOrEmpty(_active.LocalFolderPath))
            {
                SakuraMessageBox.Show(
                    "No local folder configured for this bot.\nEdit the profile and pick the bot's project folder (the one containing index.js) to enable Start/Stop.",
                    "Bot Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            AppendLog("[process] Démarrage…");
            var (ok, error) = Proc.Start(_active.LocalFolderPath!);
            if (!ok) AppendLog($"[erreur] {error}");
            RefreshProcUi();
        }

        private void RefreshProcUi()
        {
            bool hasProfile = _active != null;
            bool hasFolder = hasProfile && !string.IsNullOrEmpty(_active!.LocalFolderPath);
            bool running = Proc.IsRunning;

            _startStopBtn.Enabled = hasProfile;
            _restartBtn.Enabled = hasProfile && hasFolder;

            if (!hasProfile)
            {
                _procStatus.Text = "No bot selected";
                _procStatus.ForeColor = Color.FromArgb(150, 152, 168);
                _procHint.Text = "Add a bot profile to get started.";
            }
            else
            {
                // Sans dossier local, "stopped" serait mensonger : le bot tourne
                // peut-être très bien sur un hébergeur distant, on n'en sait rien ici.
                _procStatus.Text = !hasFolder ? $"{_active!.Name} — remote hosting"
                                 : running ? $"{_active!.Name} — running"
                                 : $"{_active!.Name} — stopped";
                _procStatus.ForeColor = running ? Colors.mainColor : Color.FromArgb(190, 255, 255, 255);
                _procHint.Text = hasFolder
                    ? (running ? "Process controlled locally." : "Ready to start.")
                    : "Hosted elsewhere — quick actions below still work.";
            }

            _startStopBtn.Text = running ? "Stop" : "Start";
            _startStopBtn.FillColor = running ? Colors.bgColor : Colors.mainColor;
            _startStopBtn.HoverState.FillColor = running ? ControlPaint.Light(Colors.bgColor, 0.3f) : ControlPaint.Light(Colors.mainColor, 0.2f);
            _banner.Invalidate(true);
        }

        // ---- Panneau de logs en direct ----
        private int BuildLogPanel(int y)
        {
            var lbl = new Guna2HtmlLabel
            {
                Parent = this, Text = "LIVE CONSOLE", ForeColor = Color.FromArgb(150, 255, 255, 255),
                Font = new Font("Inter Semibold", 8.5f), AutoSize = true, BackColor = Color.Transparent,
                Location = new Point(Pad, y)
            };

            var clearBtn = MakeSmallButton("Clear", 28, 70);
            clearBtn.Font = new Font("Inter Semibold", 8.5f);
            clearBtn.Location = new Point(Width - Pad - clearBtn.Width, y - 4);
            clearBtn.Click += (s, e) => _log.Clear();

            int boxY = y + 26, boxH = 234;
            _log = new Guna2TextBox
            {
                Parent = this, Location = new Point(Pad, boxY), Size = new Size(Width - Pad * 2, boxH),
                Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
                FillColor = Colors.bgColor, BorderColor = Colors.scColor, ForeColor = Color.FromArgb(210, 255, 255, 255),
                Font = new Font("Consolas", 9f), BorderRadius = 10,
            };
            _log.FocusedState.BorderColor = Colors.scColor;

            return boxY + boxH;
        }

        private void AppendLog(string line)
        {
            if (_log.InvokeRequired) { _log.BeginInvoke(new Action(() => AppendLog(line))); return; }
            _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {line}{Environment.NewLine}");
        }

        // ---- Actions rapides (API Discord directe) ----
        private void BuildQuickActions(int y)
        {
            var lbl = new Guna2HtmlLabel
            {
                Parent = this, Text = "QUICK ACTIONS", ForeColor = Color.FromArgb(150, 255, 255, 255),
                Font = new Font("Inter Semibold", 8.5f), AutoSize = true, BackColor = Color.Transparent,
                Location = new Point(Pad, y)
            };

            int rowY = y + 24, gap = 16, h = 92;
            int contentW = Width - Pad * 2;
            int w = (contentW - gap * 3) / 4;

            BuildActionCard(BotIcon.Kind.Announcement, "Announcement", "→ #announcements", new Point(Pad, rowY), w, h, async () => await DoAnnounce());
            BuildActionCard(BotIcon.Kind.Update, "Update", "→ #updates", new Point(Pad + (w + gap) * 1, rowY), w, h, async () => await DoUpdate());
            BuildActionCard(BotIcon.Kind.Status, "Status", "→ #status", new Point(Pad + (w + gap) * 2, rowY), w, h, async () => await DoStatus());
            BuildActionCard(BotIcon.Kind.Tickets, "Tickets", "Open list", new Point(Pad + (w + gap) * 3, rowY), w, h, DoViewTickets);
        }

        private void BuildActionCard(BotIcon.Kind kind, string title, string subtitle, Point loc, int w, int h, Action onClick)
        {
            var card = new Guna2Panel
            {
                Parent = this, Location = loc, Size = new Size(w, h),
                FillColor = Colors.scColor, BorderRadius = 10, BorderThickness = 0,
                UseTransparentBackground = true, Cursor = Cursors.Hand,
                CustomBorderThickness = new Padding(3, 0, 0, 0), CustomBorderColor = Colors.mainColor
            };
            var icon = new BotIcon(kind) { Parent = card, Location = new Point(14, (h - 40) / 2), Size = new Size(40, 40) };
            var t1 = new Guna2HtmlLabel { Parent = card, Text = title, ForeColor = Color.White, Font = new Font("Inter Semibold", 10f), AutoSize = true, BackColor = Color.Transparent, IsSelectionEnabled = false, Location = new Point(64, 26) };
            var t2 = new Guna2HtmlLabel { Parent = card, Text = subtitle, ForeColor = Color.FromArgb(150, 255, 255, 255), Font = new Font("Inter Medium", 8f), AutoSize = true, BackColor = Color.Transparent, IsSelectionEnabled = false, Location = new Point(64, 46) };

            var controls = new Control[] { card, icon, t1, t2 };
            foreach (Control c in controls) c.Click += (s, e) => onClick();

            Color normal = Colors.scColor, hover = ControlPaint.Light(Colors.scColor, 0.18f);
            void SetHover(bool on) { card.FillColor = on ? hover : normal; card.Invalidate(); }
            foreach (Control c in controls)
            {
                c.MouseEnter += (s, e) => SetHover(true);
                c.MouseLeave += (s, e) => SetHover(false);
            }
        }

        private bool EnsureConnected(out string token, out string guildId)
        {
            token = ""; guildId = "";
            if (_active == null)
            {
                SakuraMessageBox.Show("Add a bot profile first.", "Bot Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            token = BotProfileStore.Decrypt(_active.EncryptedTokenBase64);
            guildId = _active.GuildId;
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(guildId))
            {
                SakuraMessageBox.Show("This profile's token or Guild ID is missing. Edit the profile first.", "Bot Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private async Task DoAnnounce()
        {
            if (!EnsureConnected(out var token, out var guildId)) return;
            using var dlg = new SakuraTwoFieldDialog("Post announcement", "Title", "Message");
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            string? channelId = await DiscordApi.FindTextChannelIdByName(token, guildId, "announcements");
            if (channelId == null)
            {
                SakuraMessageBox.Show("#announcements channel not found on this server.", "Bot Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var embed = DiscordApi.BuildEmbed($"📢 {dlg.TitleValue}", dlg.BodyValue, 0xE384AE, "PaiPai");
            var result = await DiscordApi.PostMessage(token, channelId, embed, pingEveryone: false);
            SakuraMessageBox.Show(result.Success ? "Announcement posted." : $"Failed: {result.Error}", "Bot Manager",
                MessageBoxButtons.OK, result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        }

        private async Task DoUpdate()
        {
            if (!EnsureConnected(out var token, out var guildId)) return;
            using var dlg = new SakuraTwoFieldDialog("Post update", "Title", "Description");
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            string? channelId = await DiscordApi.FindTextChannelIdByName(token, guildId, "updates");
            if (channelId == null)
            {
                SakuraMessageBox.Show("#updates channel not found on this server.", "Bot Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var embed = DiscordApi.BuildEmbed($"🆕 {dlg.TitleValue}", dlg.BodyValue, 0xE384AE, "PaiPai");
            var result = await DiscordApi.PostMessage(token, channelId, embed, pingEveryone: false);
            SakuraMessageBox.Show(result.Success ? "Update posted." : $"Failed: {result.Error}", "Bot Manager",
                MessageBoxButtons.OK, result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        }

        private async Task DoStatus()
        {
            if (!EnsureConnected(out var token, out var guildId)) return;
            using var dlg = new BotStatusDialog(Products.Select(p => p.Name).ToArray());
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            string productKey = Products.First(p => p.Name == dlg.SelectedProductName).Key;
            string state = dlg.StateValue;

            var storeData = TryReadLocalStore();
            if (storeData?["statusMessage"] is JObject sm && sm["channelId"] != null && sm["messageId"] != null)
            {
                var productStatus = (storeData["productStatus"] as JObject) ?? new JObject();
                productStatus[productKey] = state;
                var embed = BuildStatusEmbed(productStatus);
                var result = await DiscordApi.EditMessage(token, sm["channelId"]!.ToString(), sm["messageId"]!.ToString(), embed);
                if (result.Success) WriteLocalProductStatus(productKey, state);
                SakuraMessageBox.Show(result.Success ? "Status updated." : $"Failed: {result.Error}", "Bot Manager",
                    MessageBoxButtons.OK, result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
                return;
            }

            // Pas de dossier local / pas de message de statut suivi -> on ne peut pas
            // savoir quel message editer. On previent plutôt que de risquer un
            // doublon silencieux.
            var r = SakuraMessageBox.Show(
                "No tracked status message found (needs the bot's local folder, or /setup-server was never run).\nPost a brand-new status message in #status instead?",
                "Bot Manager", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (r != DialogResult.Yes) return;

            string? channelId = await DiscordApi.FindTextChannelIdByName(token, guildId, "status");
            if (channelId == null)
            {
                SakuraMessageBox.Show("#status channel not found on this server.", "Bot Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var freshStatus = new JObject { [productKey] = state };
            var freshEmbed = BuildStatusEmbed(freshStatus);
            var postResult = await DiscordApi.PostMessage(token, channelId, freshEmbed);
            SakuraMessageBox.Show(postResult.Success ? "New status message posted." : $"Failed: {postResult.Error}", "Bot Manager",
                MessageBoxButtons.OK, postResult.Success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        }

        private static JObject BuildStatusEmbed(JObject productStatus)
        {
            var labels = new Dictionary<string, string> { ["online"] = "🟢 Online", ["maintenance"] = "🟡 Maintenance", ["offline"] = "🔴 Offline" };
            var sb = new StringBuilder();
            foreach (var (key, name) in Products)
            {
                string state = productStatus[key]?.ToString() ?? "online";
                sb.AppendLine($"**{name}** — {(labels.TryGetValue(state, out var lab) ? lab : labels["online"])}");
            }
            return DiscordApi.BuildEmbed("📊 PaiPai Product Status", sb.ToString().TrimEnd(), 0xE384AE, "PaiPai • Last updated");
        }

        private void DoViewTickets()
        {
            var storeData = TryReadLocalStore();
            if (storeData == null)
            {
                SakuraMessageBox.Show(
                    "No local folder configured for this bot (or data/store.json not found yet).\nEdit the profile and set the bot's project folder to view tickets.",
                    "Bot Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var open = storeData["openTickets"] as JObject;
            if (open == null || !open.Properties().Any())
            {
                using var empty = new SakuraInfoDialog("Open tickets", "No open tickets right now.");
                empty.ShowDialog(this);
                return;
            }

            var sb = new StringBuilder();
            int n = 1;
            foreach (var prop in open.Properties())
            {
                var t = prop.Value;
                string productKey = t["productKey"]?.ToString() ?? "?";
                string userId = t["userId"]?.ToString() ?? "?";
                string claimedBy = t["claimedBy"]?.ToString() ?? "";
                string productName = Products.FirstOrDefault(p => p.Key == productKey).Name ?? productKey;
                sb.AppendLine($"#{n} — channel {prop.Name}");
                sb.AppendLine($"   product: {productName}");
                sb.AppendLine($"   opened by user ID: {userId}");
                sb.AppendLine($"   claimed by: {(string.IsNullOrEmpty(claimedBy) ? "(unclaimed)" : claimedBy)}");
                sb.AppendLine();
                n++;
            }
            using var dlg = new SakuraInfoDialog($"Open tickets ({open.Properties().Count()})", sb.ToString().TrimEnd());
            dlg.ShowDialog(this);
        }

        // Lit data/store.json dans le dossier local du profil actif (si configure).
        private JObject? TryReadLocalStore()
        {
            if (_active?.LocalFolderPath == null) return null;
            string path = Path.Combine(_active.LocalFolderPath, "data", "store.json");
            try
            {
                if (!File.Exists(path)) return null;
                return JObject.Parse(File.ReadAllText(path));
            }
            catch { return null; }
        }

        private void WriteLocalProductStatus(string productKey, string state)
        {
            if (_active?.LocalFolderPath == null) return;
            string path = Path.Combine(_active.LocalFolderPath, "data", "store.json");
            try
            {
                var data = File.Exists(path) ? JObject.Parse(File.ReadAllText(path)) : new JObject();
                if (data["productStatus"] is not JObject ps) { ps = new JObject(); data["productStatus"] = ps; }
                ps[productKey] = state;
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, data.ToString());
            }
            catch { /* best-effort: le prochain /status-set du bot re-synchronisera de toute facon */ }
        }

        private Label MakeLbl(string text, Color color, Font font, Point loc, bool drag)
        {
            var l = new Label { Parent = this, Text = text, ForeColor = color, BackColor = Color.Transparent, Font = font, AutoSize = true, Location = loc };
            if (drag) l.MouseDown += Drag;
            return l;
        }

        private void Drag(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(this.Handle, 0xA1, 0x2, 0); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, Width, Height);
            using (var bg = new LinearGradientBrush(rect, Colors.bgColor, Color.FromArgb(46, 24, 40), LinearGradientMode.ForwardDiagonal))
                g.FillRectangle(bg, rect);
            using (var glow = new GraphicsPath())
            {
                glow.AddEllipse(Width - 240, -160, 340, 320);
                using (var pgb = new PathGradientBrush(glow))
                {
                    pgb.CenterColor = Color.FromArgb(45, Colors.mainColor);
                    pgb.SurroundColors = new[] { Color.FromArgb(0, Colors.mainColor) };
                    g.FillPath(pgb, glow);
                }
            }
            using (var path = Rounded(new Rectangle(0, 0, Width - 1, Height - 1), 14))
            using (var pen = new Pen(Color.FromArgb(90, Colors.mainColor), 1f))
                g.DrawPath(pen, path);
        }

        private static GraphicsPath Rounded(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
