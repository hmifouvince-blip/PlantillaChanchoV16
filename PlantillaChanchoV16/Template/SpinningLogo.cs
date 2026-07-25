using PlantillaChanchoV16.Utilities;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace PlantillaChanchoV16.Template
{
    // Logo affiché en rond, avec une rotation douce et continue + un anneau d'accent sakura.
    // Remplace le logo actuel quand un logo PaiPai sera fourni : il suffit de changer l'image.
    internal class SpinningLogo : Control
    {
        private readonly Timer _timer;
        private float _angle = 0f;
        private Image _logo;

        public Image LogoImage
        {
            get => _logo;
            set { _logo = value; Invalidate(); }
        }

        public SpinningLogo(Image logo, int size)
        {
            _logo = logo;
            Size = new Size(size, size);

            // SupportsTransparentBackColor doit être activé AVANT de mettre BackColor transparent.
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            DoubleBuffered = true;
            BackColor = Color.Transparent;

            _timer = new Timer { Interval = 16 }; // ~60 fps
            _timer.Tick += (s, e) => { _angle = (_angle + 1.1f) % 360f; Invalidate(); };
            AnimationHub.ActiveChanged += UpdateTimer;
            UpdateTimer();
        }

        private void UpdateTimer()
        {
            if (Visible && AnimationHub.Active) _timer?.Start();
            else _timer?.Stop();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            float pad = 3f;
            RectangleF circle = new RectangleF(pad, pad, Width - pad * 2, Height - pad * 2);
            float cx = Width / 2f, cy = Height / 2f;

            // Halo doux.
            using (var glow = new GraphicsPath())
            {
                glow.AddEllipse(-2, -2, Width + 4, Height + 4);
                using (var pgb = new PathGradientBrush(glow))
                {
                    pgb.CenterColor = Color.FromArgb(70, Colors.mainColor);
                    pgb.SurroundColors = new[] { Color.FromArgb(0, Colors.mainColor) };
                    g.FillPath(pgb, glow);
                }
            }

            // Image détourée en cercle et pivotée.
            if (_logo != null)
            {
                var state = g.Save();
                using (var clip = new GraphicsPath())
                {
                    clip.AddEllipse(circle);
                    g.SetClip(clip);
                }
                g.TranslateTransform(cx, cy);
                g.RotateTransform(_angle);

                float d = Math.Max(circle.Width, circle.Height);
                g.DrawImage(_logo, new RectangleF(-d / 2f, -d / 2f, d, d));
                g.Restore(state);
            }

            // Anneau d'accent sakura.
            using (var ring = new Pen(Colors.mainColor, 2f))
                g.DrawEllipse(ring, circle.X, circle.Y, circle.Width, circle.Height);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            UpdateTimer();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { AnimationHub.ActiveChanged -= UpdateTimer; _timer?.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
