using Guna.UI2.WinForms;
using PlantillaChanchoV16.Utilities;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PlantillaChanchoV16.Template
{
    // Petits dialogues sakura reutilises par BotManagerScreen. Regroupes dans un
    // seul fichier (chacun est court) pour eviter d'eparpiller le chrome
    // borderless/drag/glow (copie de SakuraInputDialog.cs) dans 4 fichiers separes.

    // ---- Base commune : chrome borderless + glow + drag (identique aux autres dialogues sakura) ----
    internal abstract class SakuraDialogBase : Form
    {
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        protected SakuraDialogBase(int width, int height)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(width, height);
            BackColor = Colors.bgColor;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            using (var p = Rounded(new Rectangle(0, 0, Width, Height), 14)) Region = new Region(p);
        }

        protected Guna2HtmlLabel MakeWordmarkAndTitle(string title, int pad)
        {
            var pai1 = new Guna2HtmlLabel { Parent = this, Text = "Pai", ForeColor = Color.White, Font = new Font("Inter Semibold", 12f), AutoSize = true, BackColor = Color.Transparent, IsSelectionEnabled = false, Location = new Point(pad, 20) };
            var pai2 = new Guna2HtmlLabel { Parent = this, Text = "Pai", ForeColor = Colors.mainColor, Font = new Font("Inter Semibold", 12f), AutoSize = true, BackColor = Color.Transparent, IsSelectionEnabled = false };
            pai2.Location = new Point(pad + pai1.Width, 20);
            pai1.MouseDown += Drag; pai2.MouseDown += Drag;

            var titleLbl = new Guna2HtmlLabel { Parent = this, Text = title, ForeColor = Colors.mainColor, Font = new Font("Inter Semibold", 15f), AutoSize = true, BackColor = Color.Transparent, IsSelectionEnabled = false, Location = new Point(pad, 48) };
            titleLbl.MouseDown += Drag;
            return titleLbl;
        }

        protected Guna2Button MakeButton(string text, bool accent, int width = 110)
        {
            var b = new Guna2Button
            {
                Parent = this,
                Text = text,
                Font = new Font("Inter Semibold", 10.5f),
                ForeColor = accent ? Color.White : Color.FromArgb(200, 255, 255, 255),
                FillColor = accent ? Colors.mainColor : Colors.scColor,
                BorderRadius = 10,
                Size = new Size(width, 40),
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
    }

    // ---- Ajouter/Modifier un profil de bot ----
    internal class BotProfileDialog : SakuraDialogBase
    {
        private readonly Guna2TextBox _name, _token, _guildId, _folder;
        private readonly Guna2Button _togglePassword, _browseFolder;

        public BotProfile Result { get; private set; }

        public BotProfileDialog(BotProfile? existing) : base(460, 430)
        {
            Result = existing ?? new BotProfile();
            int pad = 28;
            MakeWordmarkAndTitle(existing == null ? "Add bot" : "Edit bot", pad);

            int y = 90;
            AddLabel("Name", pad, y);
            _name = MakeTextBox("e.g. PaiPai Community", new Point(pad, y + 20), new Size(Width - pad * 2, 42));
            _name.Text = Result.Name;

            y += 76;
            AddLabel("Bot token", pad, y);
            _token = MakeTextBox("Discord bot token", new Point(pad, y + 20), new Size(Width - pad * 2 - 46, 42), password: true);
            _token.Text = existing != null ? BotProfileStore.Decrypt(existing.EncryptedTokenBase64) : "";
            _togglePassword = new Guna2Button
            {
                Parent = this, Text = "👁", Font = new Font("Inter Medium", 11f), ForeColor = Color.White,
                FillColor = Colors.scColor, BorderRadius = 10, Size = new Size(38, 42),
                Location = new Point(_token.Right + 8, y + 20), Cursor = Cursors.Hand, UseTransparentBackground = true,
            };
            _togglePassword.Click += (s, e) => _token.UseSystemPasswordChar = !_token.UseSystemPasswordChar;

            y += 76;
            AddLabel("Guild (server) ID", pad, y);
            _guildId = MakeTextBox("Right-click your server -> Copy Server ID", new Point(pad, y + 20), new Size(Width - pad * 2, 42));
            _guildId.Text = Result.GuildId;

            y += 76;
            AddLabel("Local bot folder (optional — enables Start/Stop + tickets)", pad, y);
            _folder = MakeTextBox("Not set", new Point(pad, y + 20), new Size(Width - pad * 2 - 46, 42));
            _folder.ReadOnly = true;
            _folder.Text = Result.LocalFolderPath ?? "";
            _browseFolder = new Guna2Button
            {
                Parent = this, Text = "📁", Font = new Font("Inter Medium", 11f), ForeColor = Color.White,
                FillColor = Colors.scColor, BorderRadius = 10, Size = new Size(38, 42),
                Location = new Point(_folder.Right + 8, y + 20), Cursor = Cursors.Hand, UseTransparentBackground = true,
            };
            _browseFolder.Click += (s, e) =>
            {
                using var fbd = new FolderBrowserDialog { Description = "Select the bot project folder (contains index.js)" };
                if (fbd.ShowDialog(this) == DialogResult.OK) _folder.Text = fbd.SelectedPath;
            };

            var ok = MakeButton("Save", true);
            ok.Location = new Point(Width - pad - ok.Width, Height - 60);
            ok.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_name.Text) || string.IsNullOrWhiteSpace(_token.Text) || string.IsNullOrWhiteSpace(_guildId.Text))
                {
                    SakuraMessageBox.Show("Name, token and Guild ID are required.", "Bot Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                Result.Name = _name.Text.Trim();
                Result.EncryptedTokenBase64 = BotProfileStore.Encrypt(_token.Text.Trim());
                Result.GuildId = _guildId.Text.Trim();
                Result.LocalFolderPath = string.IsNullOrWhiteSpace(_folder.Text) ? null : _folder.Text.Trim();
                DialogResult = DialogResult.OK;
                Close();
            };

            var cancel = MakeButton("Cancel", false, 100);
            cancel.Location = new Point(ok.Left - cancel.Width - 10, Height - 60);
            cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            AcceptButton = ok;
            CancelButton = cancel;
            this.MouseDown += Drag;
        }

        private void AddLabel(string text, int x, int y)
        {
            var l = new Guna2HtmlLabel { Parent = this, Text = text, ForeColor = Color.FromArgb(170, 255, 255, 255), Font = new Font("Inter Medium", 9f), AutoSize = true, BackColor = Color.Transparent, Location = new Point(x, y) };
        }
    }

    // ---- Saisie a deux champs (titre + message) : Annonce / Update ----
    internal class SakuraTwoFieldDialog : SakuraDialogBase
    {
        private readonly Guna2TextBox _title;
        private readonly Guna2TextBox _body;
        public string TitleValue => _title.Text;
        public string BodyValue => _body.Text;

        public SakuraTwoFieldDialog(string dialogTitle, string titleLabel, string bodyLabel) : base(480, 400)
        {
            int pad = 28;
            MakeWordmarkAndTitle(dialogTitle, pad);

            var l1 = new Guna2HtmlLabel { Parent = this, Text = titleLabel, ForeColor = Color.FromArgb(170, 255, 255, 255), Font = new Font("Inter Medium", 9f), AutoSize = true, BackColor = Color.Transparent, Location = new Point(pad, 90) };
            _title = MakeTextBox("", new Point(pad, 110), new Size(Width - pad * 2, 42));

            var l2 = new Guna2HtmlLabel { Parent = this, Text = bodyLabel, ForeColor = Color.FromArgb(170, 255, 255, 255), Font = new Font("Inter Medium", 9f), AutoSize = true, BackColor = Color.Transparent, Location = new Point(pad, 166) };
            _body = new Guna2TextBox
            {
                Parent = this, Location = new Point(pad, 186), Size = new Size(Width - pad * 2, 130),
                Multiline = true, BorderRadius = 10, FillColor = Colors.scColor, BorderColor = Colors.scColor,
                ForeColor = Color.White, Font = new Font("Inter Medium", 10f), Animated = true,
            };
            _body.FocusedState.BorderColor = Colors.mainColor;

            var ok = MakeButton("Post", true);
            ok.Location = new Point(Width - pad - ok.Width, Height - 60);
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
            cancel.Location = new Point(ok.Left - cancel.Width - 10, Height - 60);
            cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            AcceptButton = ok;
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

        public BotStatusDialog(string[] productNames) : base(420, 300)
        {
            int pad = 28;
            MakeWordmarkAndTitle("Set product status", pad);

            var l1 = new Guna2HtmlLabel { Parent = this, Text = "Product", ForeColor = Color.FromArgb(170, 255, 255, 255), Font = new Font("Inter Medium", 9f), AutoSize = true, BackColor = Color.Transparent, Location = new Point(pad, 90) };
            _product = new Guna2ComboBox
            {
                Parent = this, Location = new Point(pad, 110), Size = new Size(Width - pad * 2, 42),
                FillColor = Colors.scColor, BorderColor = Colors.scColor, ForeColor = Color.White,
                Font = new Font("Inter Medium", 10f), BorderRadius = 10,
            };
            _product.Items.AddRange(productNames);
            if (_product.Items.Count > 0) _product.SelectedIndex = 0;

            var l2 = new Guna2HtmlLabel { Parent = this, Text = "New state", ForeColor = Color.FromArgb(170, 255, 255, 255), Font = new Font("Inter Medium", 9f), AutoSize = true, BackColor = Color.Transparent, Location = new Point(pad, 166) };
            _state = new Guna2ComboBox
            {
                Parent = this, Location = new Point(pad, 186), Size = new Size(Width - pad * 2, 42),
                FillColor = Colors.scColor, BorderColor = Colors.scColor, ForeColor = Color.White,
                Font = new Font("Inter Medium", 10f), BorderRadius = 10,
            };
            _state.Items.AddRange(new object[] { "🟢 Online", "🟡 Maintenance", "🔴 Offline" });
            _state.SelectedIndex = 0;

            var ok = MakeButton("Update", true);
            ok.Location = new Point(Width - pad - ok.Width, Height - 56);
            ok.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            var cancel = MakeButton("Cancel", false, 100);
            cancel.Location = new Point(ok.Left - cancel.Width - 10, Height - 56);
            cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            AcceptButton = ok;
            CancelButton = cancel;
            this.MouseDown += Drag;
        }
    }

    // ---- Affichage d'un texte en lecture seule (tickets, resultats, ...) ----
    internal class SakuraInfoDialog : SakuraDialogBase
    {
        public SakuraInfoDialog(string title, string content) : base(520, 460)
        {
            int pad = 28;
            MakeWordmarkAndTitle(title, pad);

            var box = new Guna2TextBox
            {
                Parent = this, Location = new Point(pad, 90), Size = new Size(Width - pad * 2, Height - 90 - 70),
                Multiline = true, ReadOnly = true, BorderRadius = 10, FillColor = Colors.scColor,
                BorderColor = Colors.scColor, ForeColor = Color.White, Font = new Font("Consolas", 9.5f),
                Text = content, ScrollBars = ScrollBars.Vertical,
            };

            var close = MakeButton("Close", true);
            close.Location = new Point(Width - pad - close.Width, Height - 56);
            close.Click += (s, e) => Close();

            this.MouseDown += Drag;
        }
    }
}
