using PlantillaChanchoV16.Utilities;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace PlantillaChanchoV16.Template
{
    // Petit bouton de fenêtre (Settings / Réduire / Plein écran / Fermer) avec icône
    // dessinée en GDI (aucune dépendance à une police) et fond au survol.
    internal class WindowButton : Control
    {
        public enum Glyph { Settings, Minimize, Maximize, Restore, Close }

        private Glyph _glyph;
        private readonly Color _hoverColor;
        private bool _hover;
        // Intensité de survol animée (0->1) : le fond + l'icône apparaissent en FONDU au lieu
        // d'un basculement sec = ressenti premium.
        private float _hoverT = 0f;
        private readonly Timer _anim;

        // Modifiable : le bouton plein écran change d'icône (carré <-> deux carrés) selon
        // l'état de la fenêtre (Maximize <-> Restore).
        public Glyph CurrentGlyph
        {
            get => _glyph;
            set { if (_glyph == value) return; _glyph = value; Invalidate(); }
        }

        public event EventHandler Clicked;

        public WindowButton(Glyph glyph, Color hoverColor)
        {
            _glyph = glyph;
            _hoverColor = hoverColor;

            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            Size = new Size(28, 28);
            Cursor = Cursors.Hand;

            _anim = new Timer { Interval = 15 };
            _anim.Tick += (s, e) =>
            {
                float target = _hover ? 1f : 0f;
                float diff = target - _hoverT;
                if (Math.Abs(diff) <= 0.06f) { _hoverT = target; _anim.Stop(); }
                else _hoverT += diff * 0.3f;
                Invalidate();
            };

            MouseEnter += (s, e) => { _hover = true; _anim.Start(); };
            MouseLeave += (s, e) => { _hover = false; _anim.Start(); };
            base.Click += (s, e) => Clicked?.Invoke(this, EventArgs.Empty);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _anim?.Stop(); _anim?.Dispose(); }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (_hoverT > 0.01f)
            {
                using (var path = Rounded(new Rectangle(0, 0, Width, Height), 7))
                using (var b = new SolidBrush(Color.FromArgb((int)(75 * _hoverT), _hoverColor)))
                    g.FillPath(b, path);
            }

            // Icône : gris au repos -> blanc au survol, en fondu.
            Color idle = Color.FromArgb(150, 152, 168);
            Color c = Color.FromArgb(
                (int)(idle.R + (255 - idle.R) * _hoverT),
                (int)(idle.G + (255 - idle.G) * _hoverT),
                (int)(idle.B + (255 - idle.B) * _hoverT));
            float cx = Width / 2f, cy = Height / 2f;
            using (var pen = new Pen(c, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (var brush = new SolidBrush(c))
            {
                switch (_glyph)
                {
                    case Glyph.Minimize:
                        g.DrawLine(pen, cx - 5, cy + 2, cx + 5, cy + 2);
                        break;
                    case Glyph.Maximize:
                        g.DrawRectangle(pen, cx - 5, cy - 5, 10, 10);
                        break;
                    case Glyph.Restore:
                        // Deux carrés décalés (icône "restaurer" standard).
                        g.DrawRectangle(pen, cx - 5, cy - 3, 8, 8);
                        g.DrawLine(pen, cx - 3, cy - 3, cx - 3, cy - 5);
                        g.DrawLine(pen, cx - 3, cy - 5, cx + 5, cy - 5);
                        g.DrawLine(pen, cx + 5, cy - 5, cx + 5, cy + 3);
                        g.DrawLine(pen, cx + 5, cy + 3, cx + 3, cy + 3);
                        break;
                    case Glyph.Close:
                        g.DrawLine(pen, cx - 5, cy - 5, cx + 5, cy + 5);
                        g.DrawLine(pen, cx + 5, cy - 5, cx - 5, cy + 5);
                        break;
                    case Glyph.Settings:
                        DrawGear(g, pen, brush, cx, cy);
                        break;
                }
            }
        }

        private void DrawGear(Graphics g, Pen pen, Brush brush, float cx, float cy)
        {
            float r = 4.2f;
            for (int i = 0; i < 8; i++)
            {
                double a = i * Math.PI / 4;
                float x1 = cx + (float)Math.Cos(a) * r;
                float y1 = cy + (float)Math.Sin(a) * r;
                float x2 = cx + (float)Math.Cos(a) * (r + 2.6f);
                float y2 = cy + (float)Math.Sin(a) * (r + 2.6f);
                g.DrawLine(pen, x1, y1, x2, y2);
            }
            g.DrawEllipse(pen, cx - r, cy - r, r * 2, r * 2);
            g.FillEllipse(brush, cx - 1.6f, cy - 1.6f, 3.2f, 3.2f);
        }

        private static GraphicsPath Rounded(Rectangle rr, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(rr.X, rr.Y, d, d, 180, 90);
            path.AddArc(rr.Right - d, rr.Y, d, d, 270, 90);
            path.AddArc(rr.Right - d, rr.Bottom - d, d, d, 0, 90);
            path.AddArc(rr.X, rr.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
