using PlantillaChanchoV16.Utilities;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PlantillaChanchoV16.Template
{
    // Boîte de dialogue au style PaiPai / sakura. Remplace MessageBox.Show partout
    // (mêmes arguments, renvoie un DialogResult) -> plus aucun popup Windows.
    internal class SakuraMessageBox : Form
    {
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        private SakuraMessageBox(string text, string title, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Colors.bgColor;
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            int W = 440, pad = 28;
            Color accent = AccentFor(icon);

            using (var fTitle = new Font("Inter Semibold", 15f))
            using (var fMsg = new Font("Inter Medium", 10f))
            {
                // Wordmark PaiPai.
                var pai1 = MakeLabel("Pai", Color.White, new Font("Inter Semibold", 11.5f), new Point(pad, 20));
                var pai2 = MakeLabel("Pai", Colors.mainColor, new Font("Inter Semibold", 11.5f), new Point(pad + pai1.Width, 20));

                var titleLbl = MakeLabel(string.IsNullOrEmpty(title) ? "PaiPai" : title, accent, new Font("Inter Semibold", 15f), new Point(pad, 48));

                int msgW = W - pad * 2;
                Size msgSize = TextRenderer.MeasureText(text ?? "", fMsg, new Size(msgW, 2000),
                    TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
                int msgH = Math.Max(24, msgSize.Height);

                var msgLbl = new Label
                {
                    Parent = this,
                    Text = text ?? "",
                    ForeColor = Color.FromArgb(205, 255, 255, 255),
                    BackColor = Color.Transparent,
                    Font = fMsg,
                    AutoSize = false,
                    Size = new Size(msgW, msgH),
                    Location = new Point(pad, 82)
                };

                int btnY = msgLbl.Bottom + 22;
                int btnH = 40;

                if (buttons == MessageBoxButtons.YesNo || buttons == MessageBoxButtons.OKCancel)
                {
                    string yesTxt = buttons == MessageBoxButtons.YesNo ? "Yes" : "OK";
                    string noTxt = buttons == MessageBoxButtons.YesNo ? "No" : "Cancel";
                    var yes = MakeButton(yesTxt, Colors.mainColor, Color.White, 108, btnH);
                    yes.Location = new Point(W - pad - yes.Width, btnY);
                    yes.Click += (s, e) => { DialogResult = buttons == MessageBoxButtons.YesNo ? DialogResult.Yes : DialogResult.OK; Close(); };

                    var no = MakeButton(noTxt, Colors.scColor, Color.FromArgb(210, 255, 255, 255), 100, btnH);
                    no.Location = new Point(yes.Left - no.Width - 10, btnY);
                    no.Click += (s, e) => { DialogResult = buttons == MessageBoxButtons.YesNo ? DialogResult.No : DialogResult.Cancel; Close(); };

                    AcceptButton = yes; CancelButton = no;
                }
                else
                {
                    var ok = MakeButton("OK", Colors.mainColor, Color.White, 120, btnH);
                    ok.Location = new Point(W - pad - ok.Width, btnY);
                    ok.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
                    AcceptButton = ok; CancelButton = ok;
                }

                int H = btnY + btnH + pad;
                Size = new Size(W, H);
                using (var p = Rounded(new Rectangle(0, 0, W, H), 14))
                    Region = new Region(p);

                this.MouseDown += Drag;
                titleLbl.MouseDown += Drag;
                msgLbl.MouseDown += Drag;
            }
        }

        private Label MakeLabel(string text, Color color, Font font, Point loc)
        {
            return new Label
            {
                Parent = this,
                Text = text,
                ForeColor = color,
                BackColor = Color.Transparent,
                Font = font,
                AutoSize = true,
                Location = loc
            };
        }

        private Guna.UI2.WinForms.Guna2Button MakeButton(string text, Color fill, Color fore, int w, int h)
        {
            var b = new Guna.UI2.WinForms.Guna2Button
            {
                Parent = this,
                Text = text,
                Font = new Font("Inter Semibold", 10.5f),
                ForeColor = fore,
                FillColor = fill,
                BorderRadius = 10,
                BorderThickness = 0,
                Size = new Size(w, h),
                Animated = true,
                Cursor = Cursors.Hand,
                UseTransparentBackground = true
            };
            b.HoverState.FillColor = ControlPaint.Light(fill, 0.18f);
            b.PressedColor = ControlPaint.Dark(fill, 0.04f);
            // Halo rose premium sous le bouton d'accent (le bouton neutre reste sobre).
            bool isAccent = fill.ToArgb() == Colors.mainColor.ToArgb();
            b.ShadowDecoration.Enabled = isAccent;
            if (isAccent)
            {
                b.ShadowDecoration.Color = Color.FromArgb(120, Colors.mainColor);
                b.ShadowDecoration.Depth = 8;
                b.ShadowDecoration.Shadow = new Padding(3);
                Utilities.UiStyle.AddGlossySheen(b);
            }
            return b;
        }

        private static Color AccentFor(MessageBoxIcon icon)
        {
            switch (icon)
            {
                case MessageBoxIcon.Error: return Color.FromArgb(255, 107, 107);
                case MessageBoxIcon.Warning: return Color.FromArgb(254, 188, 46);
                case MessageBoxIcon.Information: return Colors.mainColor;
                default: return Colors.mainColor;
            }
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

        // ---- API compatible MessageBox.Show ----
        public static DialogResult Show(string text)
            => Show(text, "PaiPai", MessageBoxButtons.OK, MessageBoxIcon.None);

        public static DialogResult Show(string text, string title)
            => Show(text, title, MessageBoxButtons.OK, MessageBoxIcon.None);

        public static DialogResult Show(string text, string title, MessageBoxButtons buttons)
            => Show(text, title, buttons, MessageBoxIcon.None);

        public static DialogResult Show(string text, string title, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            DialogResult result = DialogResult.OK;
            void ShowIt() { using (var f = new SakuraMessageBox(text, title, buttons, icon)) result = f.ShowDialog(); }

            // Toujours afficher sur le thread UI.
            Form owner = Form.ActiveForm;
            if (owner != null && owner.InvokeRequired) owner.Invoke((Action)ShowIt);
            else ShowIt();
            return result;
        }
    }
}
