using Guna.UI2.WinForms;
using Newtonsoft.Json.Linq;
using PlantillaChanchoV16.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlantillaChanchoV16.Template
{
    // Petits dialogues sakura reutilises par BotManagerScreen. Regroupes dans un
    // seul fichier (chacun est court) pour eviter d'eparpiller le chrome
    // borderless/drag/glow (copie de SakuraInputDialog.cs) dans 4 fichiers separes.
    //
    // Chaque dialogue a sa hauteur calculee a la main a partir de la position
    // reelle de son dernier controle (+ marge) -> evite le bug rencontre en v1
    // (boutons Save/Cancel qui chevauchaient le champ dossier de
    // BotProfileDialog car la hauteur du Form avait ete choisie au pif).

    // ---- Base commune : chrome borderless + glow + drag (identique aux autres dialogues sakura) ----
    internal abstract class SakuraDialogBase : Form
    {
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        protected SakuraDialogBase(int width, int height)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(width, height);
            BackColor = Colors.bgColor;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            using (var p = Rounded(new Rectangle(0, 0, Width, Height), 14)) Region = new Region(p);
        }

        protected void MakeWordmarkAndTitle(string title, int pad)
        {
            var pai1 = new Guna2HtmlLabel { Parent = this, Text = "Pai", ForeColor = Color.White, Font = new Font("Inter Semibold", 12f), AutoSize = true, BackColor = Color.Transparent, IsSelectionEnabled = false, Location = new Point(pad, 20) };
            var pai2 = new Guna2HtmlLabel { Parent = this, Text = "Pai", ForeColor = Colors.mainColor, Font = new Font("Inter Semibold", 12f), AutoSize = true, BackColor = Color.Transparent, IsSelectionEnabled = false };
            pai2.Location = new Point(pad + pai1.Width, 20);
            pai1.MouseDown += Drag; pai2.MouseDown += Drag;

            var titleLbl = new Guna2HtmlLabel { Parent = this, Text = title, ForeColor = Colors.mainColor, Font = new Font("Inter Semibold", 15f), AutoSize = true, BackColor = Color.Transparent, IsSelectionEnabled = false, Location = new Point(pad, 48) };
            titleLbl.MouseDown += Drag;
        }

        protected Guna2Button MakeButton(string text, bool accent, int width = 110, int height = 44)
        {
            var b = new Guna2Button
            {
                Parent = this,
                Text = text,
                Font = new Font("Inter Semibold", 10.5f),
                ForeColor = accent ? Color.White : Color.FromArgb(200, 255, 255, 255),
                FillColor = accent ? Colors.mainColor : Colors.scColor,
                BorderRadius = 10,
                Size = new Size(width, height),
                Animated = true,
                Cursor = Cursors.Hand,
                UseTransparentBackground = true,
            };
            b.HoverState.FillColor = ControlPaint.Light(b.FillColor, accent ? 0.25f : 0.3f);
            if (accent) b.ShadowDecoration.Enabled = true;
            return b;
        }

        protected void Drag(object? sender, MouseEventArgs e)
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
                glow.AddEllipse(Width - 180, -120, 260, 240);
                using (var pgb = new PathGradientBrush(glow))
                {
                    pgb.CenterColor = Color.FromArgb(55, Colors.mainColor);
                    pgb.SurroundColors = new[] { Color.FromArgb(0, Colors.mainColor) };
                    g.FillPath(pgb, glow);
                }
            }
            using (var path = Rounded(new Rectangle(0, 0, Width - 1, Height - 1), 14))
            using (var pen = new Pen(Color.FromArgb(90, Colors.mainColor), 1f))
                g.DrawPath(pen, path);
        }

        protected static GraphicsPath Rounded(Rectangle r, int radius)
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

        protected Guna2TextBox MakeTextBox(string placeholder, Point loc, Size size, bool password = false)
        {
            var tb = new Guna2TextBox
            {
                Parent = this,
                PlaceholderText = placeholder,
                Location = loc,
                Size = size,
                BorderRadius = 10,
                FillColor = Colors.scColor,
                BorderColor = Colors.scColor,
                ForeColor = Color.White,
                Font = new Font("Inter Medium", 10f),
                Animated = true,
                UseSystemPasswordChar = password,
            };
            tb.FocusedState.BorderColor = Colors.mainColor;
            return tb;
        }

        protected Guna2HtmlLabel MakeFieldLabel(string text, int x, int y)
        {
            return new Guna2HtmlLabel { Parent = this, Text = text, ForeColor = Color.FromArgb(170, 255, 255, 255), Font = new Font("Inter Medium", 9f), AutoSize = true, BackColor = Color.Transparent, Location = new Point(x, y) };
        }

        // Petit bouton icône (badge rond BotIcon rendu cliquable) réutilisé pour
        // afficher/masquer le token et parcourir un dossier -> remplace les
        // boutons texte-emoji de la v1 (👁/📁), incohérents avec le thème.
        protected BotIcon MakeIconButton(BotIcon.Kind kind, Point loc, int size = 44)
        {
            var icon = new BotIcon(kind) { Parent = this, Location = loc, Size = new Size(size, size), Cursor = Cursors.Hand };
            return icon;
        }
    }

    // ---- Ajouter/Modifier un profil de bot ----
    internal class BotProfileDialog : SakuraDialogBase
    {
        private readonly Guna2TextBox _name, _token, _guildId, _folder, _remoteUrl, _controlKey;
        private readonly BotIcon _togglePassword, _toggleControlKey;

        public BotProfile Result { get; private set; }

        public BotProfileDialog(BotProfile? existing) : base(480, 700)
        {
            Result = existing ?? new BotProfile();
            int pad = 30;
            int fieldW = Width - pad * 2;
            int iconFieldW = fieldW - 44 - 10; // largeur du champ quand une icône l'accompagne

            MakeWordmarkAndTitle(existing == null ? "Add bot" : "Edit bot", pad);

            int y = 92;
            MakeFieldLabel("Name", pad, y);
            _name = MakeTextBox("e.g. PaiPai Community", new Point(pad, y + 20), new Size(fieldW, 44));
            _name.Text = Result.Name;

            y += 88;
            MakeFieldLabel("Bot token", pad, y);
            _token = MakeTextBox("Discord bot token", new Point(pad, y + 20), new Size(iconFieldW, 44), password: true);
            _token.Text = existing != null ? BotProfileStore.Decrypt(existing.EncryptedTokenBase64) : "";
            _togglePassword = MakeIconButton(BotIcon.Kind.Eye, new Point(_token.Right + 10, y + 20));
            _togglePassword.Click += (s, e) =>
            {
                _token.UseSystemPasswordChar = !_token.UseSystemPasswordChar;
                _togglePassword.IconKind = _token.UseSystemPasswordChar ? BotIcon.Kind.Eye : BotIcon.Kind.EyeOff;
                _togglePassword.Invalidate();
            };

            y += 88;
            MakeFieldLabel("Guild (server) ID", pad, y);
            _guildId = MakeTextBox("Right-click your server -> Copy Server ID", new Point(pad, y + 20), new Size(fieldW, 44));
            _guildId.Text = Result.GuildId;

            y += 88;
            MakeFieldLabel("Local bot folder — leave empty if the bot is hosted elsewhere (24/7)", pad, y);
            _folder = MakeTextBox("Not set — paste a path or use the folder button", new Point(pad, y + 20), new Size(iconFieldW, 44));
            _folder.Text = Result.LocalFolderPath ?? "";
            var browseFolder = MakeIconButton(BotIcon.Kind.Folder, new Point(_folder.Right + 10, y + 20));
            browseFolder.Click += (s, e) => PickFolderAsync();

            y += 88;
            MakeFieldLabel("Control URL — for a bot hosted 24/7 (e.g. 1.2.3.4:8080)", pad, y);
            _remoteUrl = MakeTextBox("Empty = bot runs locally only", new Point(pad, y + 20), new Size(fieldW, 44));
            _remoteUrl.Text = Result.RemoteUrl ?? "";

            y += 88;
            MakeFieldLabel("Control key — optional, or use \"Link Discord\" instead", pad, y);
            _controlKey = MakeTextBox("CONTROL_KEY from the bot's .env", new Point(pad, y + 20), new Size(iconFieldW, 44), password: true);
            _controlKey.Text = existing != null ? BotProfileStore.Decrypt(existing.EncryptedControlKeyBase64) : "";
            _toggleControlKey = MakeIconButton(BotIcon.Kind.Eye, new Point(_controlKey.Right + 10, y + 20));
            _toggleControlKey.Click += (s, e) =>
            {
                _controlKey.UseSystemPasswordChar = !_controlKey.UseSystemPasswordChar;
                _toggleControlKey.IconKind = _controlKey.UseSystemPasswordChar ? BotIcon.Kind.Eye : BotIcon.Kind.EyeOff;
                _toggleControlKey.Invalidate();
            };

            int buttonsY = y + 20 + 44 + 28; // sous le dernier champ, jamais chevauché
            var ok = MakeButton("Save", true);
            ok.Location = new Point(Width - pad - ok.Width, buttonsY);
            ok.Click += (s, e) =>
            {
                bool hasUrl = !string.IsNullOrWhiteSpace(_remoteUrl.Text);
                bool hasKey = !string.IsNullOrWhiteSpace(_controlKey.Text);

                if (string.IsNullOrWhiteSpace(_name.Text))
                {
                    SakuraMessageBox.Show("Name is required.", "Bot Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Token + Guild ID ne sont exiges QUE sans URL de controle : avec
                // une URL, tout passe par le bot lui-meme. C'est ce qui permet a
                // un membre de l'equipe d'utiliser le Bot Manager sans jamais
                // detenir le token du bot — il se contentera de « Link Discord ».
                if (!hasUrl && (string.IsNullOrWhiteSpace(_token.Text) || string.IsNullOrWhiteSpace(_guildId.Text)))
                {
                    SakuraMessageBox.Show(
                        "Without a control URL, the bot token and Guild ID are required.\n" +
                        "For a hosted bot, fill the control URL instead and use \"Link Discord\".",
                        "Bot Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Une cle sans URL ne sert a rien : il n'y a personne a appeler.
                if (hasKey && !hasUrl)
                {
                    SakuraMessageBox.Show("A control key needs a control URL to go with it.",
                        "Bot Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Result.Name = _name.Text.Trim();
                Result.EncryptedTokenBase64 = BotProfileStore.Encrypt(_token.Text.Trim());
                Result.GuildId = _guildId.Text.Trim();
                Result.LocalFolderPath = string.IsNullOrWhiteSpace(_folder.Text) ? null : _folder.Text.Trim();
                Result.RemoteUrl = hasUrl ? _remoteUrl.Text.Trim() : null;
                Result.EncryptedControlKeyBase64 = hasKey ? BotProfileStore.Encrypt(_controlKey.Text.Trim()) : "";
                DialogResult = DialogResult.OK;
                Close();
            };

            var cancel = MakeButton("Cancel", false, 100);
            cancel.Location = new Point(ok.Left - cancel.Width - 10, buttonsY);
            cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            AcceptButton = ok;
            CancelButton = cancel;
            this.MouseDown += Drag;
        }

        // Le sélecteur de dossier Windows se bloque ("PaiPai ne répond pas") quand
        // il est ouvert AVEC POUR PARENT une fenêtre borderless à Region
        // personnalisée — c'est le cas de tout le chrome sakura de l'app. On le
        // lance donc sur son propre thread STA, SANS parent : il ne peut plus
        // geler l'UI, et le champ reste saisissable à la main de toute façon.
        private void PickFolderAsync()
        {
            var t = new Thread(() =>
            {
                string? picked = null;
                try
                {
                    using var fbd = new FolderBrowserDialog
                    {
                        Description = "Select the bot project folder (contains index.js)",
                        AutoUpgradeEnabled = false,
                        ShowNewFolderButton = false,
                    };
                    if (fbd.ShowDialog() == DialogResult.OK) picked = fbd.SelectedPath;
                }
                catch { /* si le shell refuse d'ouvrir le picker, l'utilisateur peut taper le chemin */ }

                if (picked == null) return;
                try
                {
                    if (!IsDisposed) BeginInvoke(new Action(() => _folder.Text = picked));
                }
                catch { /* dialogue déjà fermé entre-temps */ }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.IsBackground = true;
            t.Start();
        }
    }

    // ---- Saisie a deux champs (titre + message) : Annonce / Update ----
    internal class SakuraTwoFieldDialog : SakuraDialogBase
    {
        private readonly Guna2TextBox _title;
        private readonly Guna2TextBox _body;
        public string TitleValue => _title.Text;
        public string BodyValue => _body.Text;

        public SakuraTwoFieldDialog(string dialogTitle, string titleLabel, string bodyLabel) : base(500, 476)
        {
            int pad = 30;
            int fieldW = Width - pad * 2;
            MakeWordmarkAndTitle(dialogTitle, pad);

            MakeFieldLabel(titleLabel, pad, 94);
            _title = MakeTextBox("", new Point(pad, 114), new Size(fieldW, 46));

            MakeFieldLabel(bodyLabel, pad, 184);
            _body = new Guna2TextBox
            {
                Parent = this, Location = new Point(pad, 204), Size = new Size(fieldW, 180),
                Multiline = true, BorderRadius = 10, FillColor = Colors.scColor, BorderColor = Colors.scColor,
                ForeColor = Color.White, Font = new Font("Inter Medium", 10f), Animated = true,
            };
            _body.FocusedState.BorderColor = Colors.mainColor;

            int buttonsY = 204 + 180 + 26;
            var ok = MakeButton("Post", true);
            ok.Location = new Point(Width - pad - ok.Width, buttonsY);
            ok.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_title.Text) || string.IsNullOrWhiteSpace(_body.Text))
                {
                    SakuraMessageBox.Show("Both fields are required.", "Bot Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                DialogResult = DialogResult.OK;
                Close();
            };
            var cancel = MakeButton("Cancel", false, 100);
            cancel.Location = new Point(ok.Left - cancel.Width - 10, buttonsY);
            cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            AcceptButton = ok;
            CancelButton = cancel;
            this.MouseDown += Drag;
        }
    }

    // ---- Lier un compte Discord (commande /link cote bot) ----
    // Le membre tape /link dans Discord, obtient un code court, le colle ici.
    // L'echange se fait DANS le dialogue : c'est le seul moment ou l'on peut
    // afficher l'erreur exacte renvoyee par le bot ("role manquant", "code
    // expire"...) juste sous le champ concerne, plutot qu'un echec opaque
    // apres fermeture.
    internal class BotLinkDialog : SakuraDialogBase
    {
        private readonly Guna2TextBox _url, _code;
        private readonly Guna2HtmlLabel _feedback;
        private readonly Guna2Button _linkBtn;

        public string Token { get; private set; } = "";
        public string LinkedTag { get; private set; } = "";
        public string Url { get; private set; } = "";

        public BotLinkDialog(string? existingUrl) : base(500, 470)
        {
            int pad = 30;
            int fieldW = Width - pad * 2;
            MakeWordmarkAndTitle("Link Discord", pad);

            var intro = new Guna2HtmlLabel
            {
                Parent = this,
                Text = "In Discord, type <b>/link</b> and paste the code below.<br/>"
                     + "Requires the <b>PaiPai</b> or <b>PeiPei</b> role.",
                ForeColor = Color.FromArgb(180, 255, 255, 255),
                Font = new Font("Inter Medium", 9.5f),
                AutoSize = true, BackColor = Color.Transparent, IsSelectionEnabled = false,
                Location = new Point(pad, 88),
            };

            int y = 132;
            MakeFieldLabel("Control URL — the hosted bot's address", pad, y);
            _url = MakeTextBox("e.g. 51.79.44.111:9697", new Point(pad, y + 20), new Size(fieldW, 44));
            _url.Text = existingUrl ?? "";

            y += 88;
            MakeFieldLabel("Link code", pad, y);
            _code = MakeTextBox("8 characters", new Point(pad, y + 20), new Size(fieldW, 44));
            _code.Font = new Font("Consolas", 13f);
            _code.CharacterCasing = CharacterCasing.Upper;

            y += 80;
            _feedback = new Guna2HtmlLabel
            {
                Parent = this, Text = "", ForeColor = Color.FromArgb(255, 130, 120),
                Font = new Font("Inter Medium", 9f), AutoSize = false,
                Size = new Size(fieldW, 40), BackColor = Color.Transparent,
                IsSelectionEnabled = false, Location = new Point(pad, y),
            };

            int buttonsY = y + 52;
            _linkBtn = MakeButton("Link", true);
            _linkBtn.Location = new Point(Width - pad - _linkBtn.Width, buttonsY);
            _linkBtn.Click += async (s, e) => await TryLinkAsync();

            var cancel = MakeButton("Cancel", false, 100);
            cancel.Location = new Point(_linkBtn.Left - cancel.Width - 10, buttonsY);
            cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            AcceptButton = _linkBtn;
            CancelButton = cancel;
            this.MouseDown += Drag;
        }

        private async Task TryLinkAsync()
        {
            string url = _url.Text.Trim();
            string code = _code.Text.Trim();

            if (url.Length == 0 || code.Length == 0)
            {
                Say("Control URL and code are both required.", error: true);
                return;
            }

            _linkBtn.Enabled = false;
            Say("Contacting the bot…", error: false);

            var result = await BotRemoteApi.RedeemLink(url, code);

            if (IsDisposed) return;
            _linkBtn.Enabled = true;

            if (!result.Success)
            {
                Say(result.Error ?? "Link failed.", error: true);
                return;
            }

            Token = result.Data?["token"]?.ToString() ?? "";
            LinkedTag = result.Data?["tag"]?.ToString() ?? "";
            Url = url;

            if (Token.Length == 0)
            {
                Say("The bot answered without a token — update the bot's code.", error: true);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void Say(string text, bool error)
        {
            _feedback.ForeColor = error ? Color.FromArgb(255, 130, 120) : Color.FromArgb(180, 255, 255, 255);
            _feedback.Text = text;
        }
    }

    // ---- Publier une mise a jour (changelog structure) ----
    // Le corps est un texte libre ou chaque ligne prefixee par +, - ou !
    // ressort coloree dans Discord (bloc ```diff cote bot). On ne construit
    // AUCUN embed ici : la mise en page vit dans utils/embeds.js, sinon les
    // deux divergeraient des la premiere retouche visuelle.
    internal class BotUpdateDialog : SakuraDialogBase
    {
        private readonly Guna2TextBox _title, _version, _changelog, _note;
        private readonly Guna2ComboBox _product;
        private readonly Guna2CustomCheckBox _ping;
        private readonly string[] _productKeys;

        public string TitleValue => _title.Text.Trim();
        public string VersionValue => _version.Text.Trim();
        public string ChangelogValue => _changelog.Text.Trim();
        public string NoteValue => _note.Text.Trim();
        public bool PingValue => _ping.Checked;

        // index 0 = "Aucun produit" -> renvoie null, le bot omet alors la
        // vignette et le champ Produit de l'embed.
        public string? ProductKey =>
            _product.SelectedIndex <= 0 ? null : _productKeys[_product.SelectedIndex - 1];

        public BotUpdateDialog((string Key, string Name)[] products) : base(560, 690)
        {
            _productKeys = products.Select(p => p.Key).ToArray();

            int pad = 30;
            int fieldW = Width - pad * 2;
            int halfW = (fieldW - 12) / 2;
            MakeWordmarkAndTitle("Post update", pad);

            int y = 94;
            MakeFieldLabel("Title", pad, y);
            _title = MakeTextBox("e.g. Woofer", new Point(pad, y + 20), new Size(halfW, 44));
            MakeFieldLabel("Version (optional)", pad + halfW + 12, y);
            _version = MakeTextBox("e.g. v3.9", new Point(pad + halfW + 12, y + 20), new Size(halfW, 44));

            y += 86;
            MakeFieldLabel("Product (optional)", pad, y);
            _product = new Guna2ComboBox
            {
                Parent = this, Location = new Point(pad, y + 20), Size = new Size(fieldW, 44),
                Font = new Font("Inter Medium", 10f), BorderRadius = 10,
            };
            GunaTheme.StyleCombo(_product);
            _product.Items.Add("— No product —");
            foreach (var p in products) _product.Items.Add(p.Name);
            _product.SelectedIndex = 0;

            y += 86;
            MakeFieldLabel("Changes — one per line:   + added    - removed    ! fixed", pad, y);
            _changelog = new Guna2TextBox
            {
                Parent = this, Location = new Point(pad, y + 20), Size = new Size(fieldW, 170),
                Multiline = true, BorderRadius = 10, FillColor = Colors.scColor, BorderColor = Colors.scColor,
                ForeColor = Color.White, Font = new Font("Consolas", 10f), Animated = true,
                ScrollBars = ScrollBars.Vertical, AcceptsReturn = true,
                PlaceholderText = "+ Added Windows 11 24H2 support",
            };
            _changelog.FocusedState.BorderColor = Colors.mainColor;

            y += 212;
            MakeFieldLabel("Warning note (optional)", pad, y);
            _note = MakeTextBox("Shown under the changelog", new Point(pad, y + 20), new Size(fieldW, 44));

            y += 82;
            _ping = new Guna2CustomCheckBox
            {
                Parent = this, Size = new Size(20, 20), Location = new Point(pad, y), Animated = true,
            };
            _ping.CheckedState.FillColor = Colors.mainColor;
            _ping.CheckedState.BorderColor = Colors.mainColor;
            _ping.UncheckedState.FillColor = Colors.scColor;
            _ping.UncheckedState.BorderColor = Colors.scColor;
            var pingLabel = MakeFieldLabel("Mention @everyone", pad + 28, y + 1);
            pingLabel.Cursor = Cursors.Hand;
            pingLabel.Click += (s, e) => _ping.Checked = !_ping.Checked;

            int buttonsY = y + 44;
            var ok = MakeButton("Post", true);
            ok.Location = new Point(Width - pad - ok.Width, buttonsY);
            ok.Click += (s, e) =>
            {
                if (TitleValue.Length == 0 || ChangelogValue.Length == 0)
                {
                    SakuraMessageBox.Show("Title and changes are required.", "Bot Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                DialogResult = DialogResult.OK;
                Close();
            };
            var cancel = MakeButton("Cancel", false, 100);
            cancel.Location = new Point(ok.Left - cancel.Width - 10, buttonsY);
            cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            CancelButton = cancel;
            this.MouseDown += Drag;
        }
    }

    // ---- Choix produit + etat (Set Status) ----
    internal class BotStatusDialog : SakuraDialogBase
    {
        private readonly Guna2ComboBox _product;
        private readonly Guna2ComboBox _state;
        public string SelectedProductName => _product.SelectedItem?.ToString() ?? "";
        public string StateValue => _state.SelectedIndex switch { 0 => "online", 1 => "maintenance", _ => "offline" };

        public BotStatusDialog(string[] productNames) : base(440, 344)
        {
            int pad = 30;
            int fieldW = Width - pad * 2;
            MakeWordmarkAndTitle("Set product status", pad);

            MakeFieldLabel("Product", pad, 94);
            _product = new Guna2ComboBox
            {
                Parent = this, Location = new Point(pad, 114), Size = new Size(fieldW, 46),
                Font = new Font("Inter Medium", 10f), BorderRadius = 10,
            };
            GunaTheme.StyleCombo(_product);
            _product.Items.AddRange(productNames);
            if (_product.Items.Count > 0) _product.SelectedIndex = 0;

            MakeFieldLabel("New state", pad, 184);
            _state = new Guna2ComboBox
            {
                Parent = this, Location = new Point(pad, 204), Size = new Size(fieldW, 46),
                Font = new Font("Inter Medium", 10f), BorderRadius = 10,
            };
            GunaTheme.StyleCombo(_state);
            _state.Items.AddRange(new object[] { "🟢 Online", "🟡 Maintenance", "🔴 Offline" });
            _state.SelectedIndex = 0;

            int buttonsY = 204 + 46 + 28;
            var ok = MakeButton("Update", true);
            ok.Location = new Point(Width - pad - ok.Width, buttonsY);
            ok.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            var cancel = MakeButton("Cancel", false, 100);
            cancel.Location = new Point(ok.Left - cancel.Width - 10, buttonsY);
            cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            AcceptButton = ok;
            CancelButton = cancel;
            this.MouseDown += Drag;
        }
    }

    // ---- Catalogue produit : liste + creation/edition ----
    // Le catalogue vit CHEZ LE BOT (produits ecrits dans son code + produits
    // crees depuis ici, stockes dans son store.json) : ce dialogue ne garde
    // aucune copie locale, il relit la liste apres chaque enregistrement.
    // Il fait lui-meme les appels reseau pour pouvoir afficher l'erreur exacte
    // du bot et rafraichir la liste sans se refermer.
    internal class BotProductsDialog : SakuraDialogBase
    {
        private readonly string _url;
        private readonly BotRemoteApi.Auth _auth;
        private readonly Guna2Panel _list;
        private readonly Guna2HtmlLabel _feedback;
        private readonly Guna2Button _newBtn;

        // Vrai des qu'un produit a ete cree ou modifie -> l'ecran appelant
        // recharge sa propre liste (menus Statut et Update).
        public bool Changed { get; private set; }

        public BotProductsDialog(string url, BotRemoteApi.Auth auth) : base(640, 580)
        {
            _url = url;
            _auth = auth;

            int pad = 30;
            MakeWordmarkAndTitle("Products", pad);

            var intro = new Guna2HtmlLabel
            {
                Parent = this,
                Text = "Everything the bot shows on Discord. Changes are published right away.",
                ForeColor = Color.FromArgb(170, 255, 255, 255), Font = new Font("Inter Medium", 9f),
                AutoSize = true, BackColor = Color.Transparent, IsSelectionEnabled = false,
                Location = new Point(pad, 82),
            };

            _newBtn = MakeButton("+ New product", true, 150, 40);
            _newBtn.Location = new Point(Width - pad - _newBtn.Width, 62);
            _newBtn.Click += async (s, e) => await EditProductAsync(null);

            int listY = 116, listH = Height - listY - 96;
            _list = new Guna2Panel
            {
                Parent = this, Location = new Point(pad, listY), Size = new Size(Width - pad * 2, listH),
                FillColor = Colors.bgColor, BorderRadius = 10, BorderThickness = 0, AutoScroll = true,
            };

            _feedback = new Guna2HtmlLabel
            {
                Parent = this, Text = "Loading…", ForeColor = Color.FromArgb(170, 255, 255, 255),
                Font = new Font("Inter Medium", 9f), AutoSize = false, Size = new Size(Width - pad * 2 - 130, 40),
                BackColor = Color.Transparent, IsSelectionEnabled = false,
                Location = new Point(pad, listY + listH + 16),
            };

            var close = MakeButton("Close", false, 100);
            close.Location = new Point(Width - pad - close.Width, listY + listH + 16);
            close.Click += (s, e) => Close();

            CancelButton = close;
            this.MouseDown += Drag;
            this.Shown += async (s, e) => await ReloadAsync();
        }

        private async Task ReloadAsync()
        {
            Say("Loading…", error: false);
            var result = await BotRemoteApi.Products(_url, _auth);
            if (IsDisposed) return;

            if (!result.Success)
            {
                Say(result.Error ?? "Could not read the product list.", error: true);
                return;
            }

            var products = result.Data?["products"] as JArray ?? new JArray();
            BuildRows(products);
            Say($"{products.Count} product(s).", error: false);
        }

        private void BuildRows(JArray products)
        {
            _list.Controls.Clear();

            int rowH = 62, gap = 8, y = 8;
            // Largeur reduite de la barre de defilement : sans cette marge, la
            // derniere carte passe SOUS la barre des que la liste depasse.
            int rowW = _list.Width - 16 - SystemInformation.VerticalScrollBarWidth;

            foreach (var item in products.OfType<JObject>())
            {
                string key = (string?)item["key"] ?? "";
                string name = (string?)item["name"] ?? key;
                string emoji = (string?)item["emoji"] ?? "";
                string channel = (string?)item["channelName"] ?? "";
                string status = (string?)item["status"] ?? "online";
                bool builtin = (bool?)item["builtin"] ?? false;

                var row = new Guna2Panel
                {
                    Parent = _list, Location = new Point(8, y), Size = new Size(rowW, rowH),
                    FillColor = Colors.scColor, BorderRadius = 10, BorderThickness = 0,
                    CustomBorderThickness = new Padding(3, 0, 0, 0), CustomBorderColor = StatusColor(status),
                };

                new Guna2HtmlLabel
                {
                    Parent = row, Text = $"{emoji} {name}".Trim(), ForeColor = Color.White,
                    Font = new Font("Inter Semibold", 10f), AutoSize = true, BackColor = Color.Transparent,
                    IsSelectionEnabled = false, Location = new Point(16, 11),
                };
                new Guna2HtmlLabel
                {
                    Parent = row, Text = $"#{channel} • {status}{(builtin ? "" : " • created from PaiPai")}",
                    ForeColor = Color.FromArgb(150, 255, 255, 255), Font = new Font("Inter Medium", 8f),
                    AutoSize = true, BackColor = Color.Transparent, IsSelectionEnabled = false,
                    Location = new Point(16, 33),
                };

                var edit = new Guna2Button
                {
                    Parent = row, Text = "Edit", Font = new Font("Inter Semibold", 9f), ForeColor = Color.White,
                    FillColor = Colors.bgColor, BorderRadius = 8, BorderThickness = 0, Size = new Size(74, 34),
                    Cursor = Cursors.Hand, Animated = true,
                };
                edit.Location = new Point(rowW - 16 - edit.Width, (rowH - edit.Height) / 2);
                edit.HoverState.FillColor = ControlPaint.Light(Colors.bgColor, 0.3f);
                var captured = item;
                edit.Click += async (s, e) => await EditProductAsync(captured);

                y += rowH + gap;
            }
        }

        private static Color StatusColor(string status) => status switch
        {
            "maintenance" => Color.FromArgb(235, 200, 120),
            "offline" => Color.FromArgb(255, 130, 120),
            _ => Colors.mainColor,
        };

        private async Task EditProductAsync(JObject? existing)
        {
            using var dlg = new BotProductDialog(existing);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            _newBtn.Enabled = false;
            Say(existing == null ? "Creating the product…" : "Saving…", error: false);

            var result = await BotRemoteApi.SaveProduct(_url, _auth, dlg.Payload);
            if (IsDisposed) return;
            _newBtn.Enabled = true;

            if (!result.Success)
            {
                Say(result.Error ?? "Save failed.", error: true);
                return;
            }

            Changed = true;
            await ReloadAsync();
            if (IsDisposed) return;

            // Le journal vient du bot (salon cree, fiche postee, annuaire et
            // page de statut rafraichis) : c'est la seule confirmation que la
            // publication est reellement passee cote Discord.
            var log = result.Data?["log"] as JArray;
            string details = log == null ? "" : string.Join(Environment.NewLine, log.Select(l => l.ToString()));
            using var info = new SakuraInfoDialog(
                (bool?)result.Data?["created"] == true ? "Product created" : "Product updated",
                details.Length > 0 ? details : "Saved.");
            info.ShowDialog(this);
        }

        private void Say(string text, bool error)
        {
            _feedback.ForeColor = error ? Color.FromArgb(255, 130, 120) : Color.FromArgb(170, 255, 255, 255);
            _feedback.Text = text;
        }
    }

    // ---- Formulaire d'un produit (creation ou edition) ----
    // Ne parle PAS au reseau : il produit juste le corps JSON, envoye par
    // BotProductsDialog. Les champs laisses vides sont envoyes vides (donc
    // effaces cote bot) : c'est voulu, vider un champ doit retirer la section
    // correspondante de la fiche Discord.
    internal class BotProductDialog : SakuraDialogBase
    {
        private readonly Guna2TextBox _name, _emoji, _key, _tagline, _description, _prices, _delivery, _website, _note;
        private readonly bool _isNew;
        private readonly string _existingKey;

        public JObject Payload { get; private set; } = new JObject();

        public BotProductDialog(JObject? existing) : base(580, 820)
        {
            _isNew = existing == null;
            _existingKey = (string?)existing?["key"] ?? "";

            int pad = 30;
            int fieldW = Width - pad * 2;
            int halfW = (fieldW - 12) / 2;
            MakeWordmarkAndTitle(_isNew ? "New product" : "Edit product", pad);

            int y = 94;
            MakeFieldLabel("Name", pad, y);
            _name = MakeTextBox("e.g. PaiPai Val + Emulator", new Point(pad, y + 20), new Size(fieldW - 100, 44));
            MakeFieldLabel("Emoji", pad + fieldW - 88, y);
            _emoji = MakeTextBox("🌸", new Point(pad + fieldW - 88, y + 20), new Size(88, 44));

            y += 86;
            MakeFieldLabel(_isNew ? "Key & channel — leave empty to derive it from the name" : "Key (fixed)", pad, y);
            _key = MakeTextBox("e.g. valorant", new Point(pad, y + 20), new Size(170, 44));
            // La cle nomme le salon Discord du produit : la changer apres coup
            // casserait les liens deja publies -> lecture seule en edition.
            _key.ReadOnly = !_isNew;
            if (!_isNew) _key.ForeColor = Color.FromArgb(150, 255, 255, 255);

            MakeFieldLabel("Tagline — the italic line above the description", pad + 182, y);
            _tagline = MakeTextBox("e.g. Every round, under control.", new Point(pad + 182, y + 20), new Size(fieldW - 182, 44));

            y += 86;
            MakeFieldLabel("Description — supports **bold** and • bullets", pad, y);
            _description = MakeMultiline(new Point(pad, y + 20), new Size(fieldW, 150));

            y += 192;
            MakeFieldLabel("Pricing — one per line:   1 month = 15 €", pad, y);
            _prices = MakeMultiline(new Point(pad, y + 20), new Size(fieldW, 76));

            y += 118;
            MakeFieldLabel("Delivery (optional)", pad, y);
            _delivery = MakeTextBox("e.g. Instant, in your ticket", new Point(pad, y + 20), new Size(halfW, 44));
            MakeFieldLabel("Website (optional)", pad + halfW + 12, y);
            _website = MakeTextBox("https://…", new Point(pad + halfW + 12, y + 20), new Size(halfW, 44));

            y += 86;
            MakeFieldLabel("Warning note (optional) — shown in bold on the card", pad, y);
            _note = MakeTextBox("e.g. Disable your antivirus before launching", new Point(pad, y + 20), new Size(fieldW, 44));

            if (existing != null) Fill(existing);

            int buttonsY = y + 20 + 44 + 26;
            var ok = MakeButton(_isNew ? "Create" : "Save", true, 120);
            ok.Location = new Point(Width - pad - ok.Width, buttonsY);
            ok.Click += (s, e) => Submit();

            var cancel = MakeButton("Cancel", false, 100);
            cancel.Location = new Point(ok.Left - cancel.Width - 10, buttonsY);
            cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            CancelButton = cancel;
            this.MouseDown += Drag;
        }

        private Guna2TextBox MakeMultiline(Point loc, Size size)
        {
            var tb = new Guna2TextBox
            {
                Parent = this, Location = loc, Size = size, Multiline = true, BorderRadius = 10,
                FillColor = Colors.scColor, BorderColor = Colors.scColor, ForeColor = Color.White,
                Font = new Font("Inter Medium", 10f), Animated = true, ScrollBars = ScrollBars.Vertical,
                AcceptsReturn = true,
            };
            tb.FocusedState.BorderColor = Colors.mainColor;
            return tb;
        }

        private void Fill(JObject p)
        {
            _key.Text = (string?)p["key"] ?? "";
            _name.Text = (string?)p["name"] ?? "";
            _emoji.Text = (string?)p["emoji"] ?? "";
            _tagline.Text = (string?)p["tagline"] ?? "";
            // Le bot renvoie des sauts de ligne \n : sans conversion, un
            // TextBox Windows les affiche tous colles sur une seule ligne.
            _description.Text = ToBox((string?)p["description"]);
            _delivery.Text = (string?)p["delivery"] ?? "";
            _website.Text = (string?)p["website"] ?? "";
            _note.Text = (string?)p["note"] ?? "";

            var sb = new StringBuilder();
            foreach (var price in (p["prices"] as JArray ?? new JArray()).OfType<JObject>())
                sb.AppendLine($"{(string?)price["label"]} = {(string?)price["price"]}");
            _prices.Text = sb.ToString().TrimEnd();
        }

        private static string ToBox(string? text)
            => (text ?? "").Replace("\r\n", "\n").Replace("\n", Environment.NewLine);

        private static string ToJson(string text)
            => text.Replace("\r\n", "\n").Trim();

        private void Submit()
        {
            string name = _name.Text.Trim();
            if (name.Length == 0)
            {
                SakuraMessageBox.Show("Name is required.", "Bot Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var prices = new JArray();
            foreach (string line in _prices.Text.Split('\n'))
            {
                string row = line.Trim();
                if (row.Length == 0) continue;
                int sep = row.IndexOfAny(new[] { '=', '|' });
                if (sep <= 0 || sep == row.Length - 1)
                {
                    SakuraMessageBox.Show(
                        $"Pricing line not understood:\n{row}\n\nUse: label = price   (e.g. 1 month = 15 €)",
                        "Bot Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                prices.Add(new JObject
                {
                    ["label"] = row.Substring(0, sep).Trim(),
                    ["price"] = row.Substring(sep + 1).Trim(),
                });
            }

            Payload = new JObject
            {
                ["key"] = _isNew ? _key.Text.Trim() : _existingKey,
                ["name"] = name,
                ["emoji"] = _emoji.Text.Trim(),
                ["tagline"] = _tagline.Text.Trim(),
                ["description"] = ToJson(_description.Text),
                ["prices"] = prices,
                ["delivery"] = _delivery.Text.Trim(),
                ["website"] = _website.Text.Trim(),
                ["note"] = _note.Text.Trim(),
            };

            DialogResult = DialogResult.OK;
            Close();
        }
    }

    // ---- Affichage d'un texte en lecture seule (tickets, resultats, ...) ----
    internal class SakuraInfoDialog : SakuraDialogBase
    {
        public SakuraInfoDialog(string title, string content) : base(560, 500)
        {
            int pad = 30;
            MakeWordmarkAndTitle(title, pad);

            int boxY = 94, boxH = Height - boxY - 80;
            var box = new Guna2TextBox
            {
                Parent = this, Location = new Point(pad, boxY), Size = new Size(Width - pad * 2, boxH),
                Multiline = true, ReadOnly = true, BorderRadius = 10, FillColor = Colors.scColor,
                BorderColor = Colors.scColor, ForeColor = Color.White, Font = new Font("Consolas", 9.5f),
                Text = content, ScrollBars = ScrollBars.Vertical,
            };

            var close = MakeButton("Close", true);
            close.Location = new Point(Width - pad - close.Width, boxY + boxH + 20);
            close.Click += (s, e) => Close();

            this.MouseDown += Drag;
        }
    }
}
