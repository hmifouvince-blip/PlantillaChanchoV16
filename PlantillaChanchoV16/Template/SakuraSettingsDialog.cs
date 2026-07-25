using Guna.UI2.WinForms;
using PlantillaChanchoV16.Utilities;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PlantillaChanchoV16.Template
{
    // Fenêtre "Settings" au style PaiPai : thèmes de couleurs, mises à jour, compte.
    internal class SakuraSettingsDialog : Form
    {
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        public SakuraSettingsDialog()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(460, 580);
            BackColor = Colors.bgColor;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            using (var p = Rounded(new Rectangle(0, 0, Width, Height), 14))
                Region = new Region(p);

            int pad = 30;

            MakeLabel("Pai", Color.White, new Font("Inter Semibold", 12f), new Point(pad, 20));
            var pai1W = TextRenderer.MeasureText("Pai", new Font("Inter Semibold", 12f)).Width;
            MakeLabel("Pai", Colors.mainColor, new Font("Inter Semibold", 12f), new Point(pad + pai1W - 6, 20));
            MakeLabel(Localization.T("settings.title"), Colors.mainColor, new Font("Inter Semibold", 16f), new Point(pad, 48));

            // ----- THEME -----
            MakeLabel(Localization.T("settings.theme"), Color.FromArgb(150, 255, 255, 255), new Font("Inter Semibold", 8.5f), new Point(pad, 92));
            int sx = pad, sy = 112;
            foreach (var t in ThemeManager.Themes)
            {
                var sw = new Guna2CircleButton
                {
                    Parent = this,
                    Size = new Size(34, 34),
                    FillColor = t.Main,
                    BorderThickness = t == ThemeManager.Current ? 3 : 0,
                    BorderColor = Color.White,
                    Location = new Point(sx, sy),
                    Cursor = Cursors.Hand,
                    Animated = true,
                    BackColor = Color.Transparent,
                    UseTransparentBackground = true   // coins transparents -> pas de carré noir
                };
                var theme = t;
                sw.Click += (s, e) => OnThemePick(theme);
                sx += 44;
            }
            MakeLabel(Localization.T("settings.theme_hint"),
                Color.FromArgb(150, 255, 255, 255), new Font("Inter Medium", 8.5f), new Point(pad, sy + 42));

            // ----- LANGUAGE -----
            MakeLabel(Localization.T("settings.language"), Color.FromArgb(150, 255, 255, 255), new Font("Inter Semibold", 8.5f), new Point(pad, 186));
            int lx = pad;
            foreach (var lang in new[] { Lang.EN, Lang.FR, Lang.ES })
            {
                bool active = lang == Localization.Current;
                var lb = MakeButton(lang.ToString(),
                    active ? Colors.mainColor : Colors.scColor,
                    active ? Color.White : Color.FromArgb(220, 255, 255, 255),
                    new Point(lx, 206), new Size(60, 32));
                var picked = lang;
                lb.Click += (s, e) => OnLanguagePick(picked);
                lx += 68;
            }

            // ----- UPDATES -----
            MakeLabel(Localization.T("settings.updates"), Color.FromArgb(150, 255, 255, 255), new Font("Inter Semibold", 8.5f), new Point(pad, 266));
            var upd = MakeButton(Localization.T("settings.check_updates"), Colors.mainColor, Color.White, new Point(pad, 286), new Size(190, 40));
            upd.Click += (s, e) => CheckUpdate();

            // ----- PERFORMANCE -----
            MakeLabel(Localization.T("settings.performance"), Color.FromArgb(150, 255, 255, 255), new Font("Inter Semibold", 8.5f), new Point(pad, 346));
            var lowPower = new Guna2CustomCheckBox
            {
                Parent = this,
                Size = new Size(20, 20),
                Location = new Point(pad, 370),
                Animated = true,
                Checked = AnimationHub.LowPower
            };
            lowPower.CheckedState.FillColor = Colors.mainColor;
            lowPower.CheckedState.BorderColor = Colors.mainColor;
            lowPower.UncheckedState.FillColor = Colors.scColor;
            lowPower.UncheckedState.BorderColor = Colors.scColor;
            lowPower.CheckedChanged += (s, e) => AnimationHub.LowPower = lowPower.Checked;
            var lpLabel = MakeLabel(Localization.T("settings.low_power"),
                Color.FromArgb(210, 255, 255, 255), new Font("Inter Medium", 9.5f), new Point(pad + 28, 371));
            lpLabel.Cursor = Cursors.Hand;
            lpLabel.Click += (s, e) => lowPower.Checked = !lowPower.Checked;

            // ----- ACCOUNT -----
            MakeLabel(Localization.T("settings.account"), Color.FromArgb(150, 255, 255, 255), new Font("Inter Semibold", 8.5f), new Point(pad, 412));
            var clear = MakeButton(Localization.T("settings.clear_login"), Colors.scColor, Color.FromArgb(220, 255, 255, 255), new Point(pad, 432), new Size(190, 40));
            clear.Click += (s, e) => ClearLogin();

            // ----- Close -----
            var close = MakeButton(Localization.T("settings.close"), Colors.scColor, Color.FromArgb(220, 255, 255, 255), new Point(Width - pad - 120, Height - pad - 40), new Size(120, 40));
            close.Click += (s, e) => Close();
            AcceptButton = close; CancelButton = close;

            this.MouseDown += Drag;
        }

        public bool ThemeChanged { get; private set; }
        public bool LanguageChanged { get; private set; }

        private void OnThemePick(Theme t)
        {
            if (t == ThemeManager.Current) return;
            ThemeManager.Save(t);   // applique + sauvegarde
            ThemeChanged = true;
            DialogResult = DialogResult.OK;
            Close();                // Main se reconstruit avec le nouveau thème (sans relancer l'appli)
        }

        private void OnLanguagePick(Lang lang)
        {
            if (lang == Localization.Current) return;
            Localization.Set(lang);  // applique + sauvegarde
            LanguageChanged = true;
            DialogResult = DialogResult.OK;
            Close();                  // Main se reconstruit avec la nouvelle langue (sans relancer l'appli)
        }

        private void CheckUpdate()
        {
            if (!Updater.CheckAndUpdate())
                SakuraMessageBox.Show(Localization.T("settings.up_to_date", Updater.CurrentVersion()),
                    Localization.T("settings.updates_title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ClearLogin()
        {
            var r = SakuraMessageBox.Show(Localization.T("settings.forget_login_confirm"), Localization.T("settings.account_title"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (r == DialogResult.Yes)
            {
                RememberMe.Clear();
                SakuraMessageBox.Show(Localization.T("settings.login_cleared"), Localization.T("settings.account_title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private Label MakeLabel(string text, Color color, Font font, Point loc)
        {
            var l = new Label { Parent = this, Text = text, ForeColor = color, BackColor = Color.Transparent, Font = font, AutoSize = true, Location = loc };
            l.MouseDown += Drag;
            return l;
        }

        private Guna2Button MakeButton(string text, Color fill, Color fore, Point loc, Size size)
        {
            var font = new Font("Inter Semibold", 10.2f);
            // Largeur ADAPTATIVE : jamais plus étroit que le texte + une marge confortable.
            // Les libellés traduits sont plus longs (ex. "Vérifier les mises à jour" en
            // français) et débordaient d'une largeur fixe de 190 px.
            int textW = TextRenderer.MeasureText(text, font).Width;
            int w = Math.Max(size.Width, textW + 34);

            var b = new Guna2Button
            {
                Parent = this,
                Text = text,
                Font = font,
                ForeColor = fore,
                FillColor = fill,
                BorderRadius = 10,
                BorderThickness = 0,
                Size = new Size(w, size.Height),
                Location = loc,
                Animated = true,
                Cursor = Cursors.Hand,
                UseTransparentBackground = true   // coins transparents -> plus de carré noir autour
            };
            // Halo rose premium sur les boutons d'accent (le neutre reste sobre).
            bool isAccent = fill.ToArgb() == Colors.mainColor.ToArgb();
            b.ShadowDecoration.Enabled = isAccent;
            if (isAccent)
            {
                b.ShadowDecoration.Color = Color.FromArgb(110, Colors.mainColor);
                b.ShadowDecoration.Depth = 7;
                b.ShadowDecoration.Shadow = new Padding(3);
                Utilities.UiStyle.AddGlossySheen(b);
            }
            b.HoverState.FillColor = ControlPaint.Light(fill, 0.18f);
            b.PressedColor = ControlPaint.Dark(fill, 0.04f);
            return b;
        }

        private void Drag(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(this.Handle, 0xA1, 0x2, 0); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(0, 0, Width, Height);
            using (var bg = new LinearGradientBrush(rect, Colors.bgColor, Blend(Colors.bgColor, Colors.mainColor, 0.14f), LinearGradientMode.ForwardDiagonal))
                g.FillRectangle(bg, rect);

            using (var glow = new GraphicsPath())
            {
                glow.AddEllipse(Width - 200, -140, 300, 280);
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

        private static Color Blend(Color a, Color b, float t)
            => Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));

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
