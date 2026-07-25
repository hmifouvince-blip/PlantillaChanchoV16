using Guna.UI2.WinForms;
using PlantillaChanchoV16.Utilities;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PlantillaChanchoV16.Template
{
    // Petit dialogue modal au style PaiPai / sakura (remplace la boîte Windows moche).
    internal class SakuraInputDialog : Form
    {
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        private readonly Guna2TextBox _input;
        public string Value => _input?.Text ?? string.Empty;

        public SakuraInputDialog(string title, string message, string placeholder)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(440, 250);
            BackColor = Colors.bgColor;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            using (var p = Rounded(new Rectangle(0, 0, Width, Height), 14))
                Region = new Region(p);

            int pad = 28;

            // Wordmark PaiPai (deux tons).
            var pai1 = new Guna2HtmlLabel { Parent = this, Text = "Pai", ForeColor = Color.White, Font = new Font("Inter Semibold", 12f), AutoSize = true, BackColor = Color.Transparent, IsSelectionEnabled = false, Location = new Point(pad, 20) };
            var pai2 = new Guna2HtmlLabel { Parent = this, Text = "Pai", ForeColor = Colors.mainColor, Font = new Font("Inter Semibold", 12f), AutoSize = true, BackColor = Color.Transparent, IsSelectionEnabled = false };
            pai2.Location = new Point(pad + pai1.Width, 20);

            var titleLbl = new Guna2HtmlLabel { Parent = this, Text = title, ForeColor = Colors.mainColor, Font = new Font("Inter Semibold", 16f), AutoSize = true, BackColor = Color.Transparent, IsSelectionEnabled = false, Location = new Point(pad, 50) };
            var msgLbl = new Guna2HtmlLabel { Parent = this, Text = message, ForeColor = Color.FromArgb(190, 255, 255, 255), Font = new Font("Inter Medium", 9.5f), AutoSize = false, Size = new Size(Width - pad * 2, 42), BackColor = Color.Transparent, IsSelectionEnabled = false, Location = new Point(pad, 82) };

            _input = new Guna2TextBox
            {
                Parent = this,
                PlaceholderText = placeholder,
                Location = new Point(pad, 130),
                Size = new Size(Width - pad * 2, 44),
                BorderRadius = 10,
                FillColor = Colors.scColor,
                BorderColor = Colors.scColor,
                ForeColor = Color.White,
                Font = new Font("Inter Medium", 10f),
                Animated = true
            };
            _input.FocusedState.BorderColor = Colors.mainColor;

            var ok = new Guna2Button { Parent = this, Text = "OK", Font = new Font("Inter Semibold", 10.5f), ForeColor = Color.White, FillColor = Colors.mainColor, BorderRadius = 10, Size = new Size(110, 40), Animated = true, Cursor = Cursors.Hand, UseTransparentBackground = true };
            ok.HoverState.FillColor = ControlPaint.Light(Colors.mainColor, 0.25f);
            ok.ShadowDecoration.Enabled = true;
            ok.Location = new Point(Width - pad - ok.Width, 190);
            ok.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };

            var cancel = new Guna2Button { Parent = this, Text = "Cancel", Font = new Font("Inter Medium", 10.5f), ForeColor = Color.FromArgb(200, 255, 255, 255), FillColor = Colors.scColor, BorderRadius = 10, Size = new Size(100, 40), Animated = true, Cursor = Cursors.Hand, UseTransparentBackground = true };
            cancel.Location = new Point(ok.Left - cancel.Width - 10, 190);
            cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            AcceptButton = ok;
            CancelButton = cancel;

            // Déplaçable en tirant sur le fond.
            this.MouseDown += Drag;
            titleLbl.MouseDown += Drag;
            msgLbl.MouseDown += Drag;
        }

        private void Drag(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, 0xA1, 0x2, 0);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(0, 0, Width, Height);
            using (var bg = new LinearGradientBrush(rect, Colors.bgColor, Color.FromArgb(46, 24, 40), LinearGradientMode.ForwardDiagonal))
                g.FillRectangle(bg, rect);

            // Halo rose diffus en haut à droite.
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

            // Bordure rose subtile.
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
