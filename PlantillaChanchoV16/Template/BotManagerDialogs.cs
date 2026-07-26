using Guna.UI2.WinForms;
using PlantillaChanchoV16.Utilities;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
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
