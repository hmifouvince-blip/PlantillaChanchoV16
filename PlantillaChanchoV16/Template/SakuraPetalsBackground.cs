using PlantillaChanchoV16.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace PlantillaChanchoV16.Template
{
    // Fond décoratif animé sakura : dégradé prune + pétales qui tombent + halos.
    // Non-interactif (les clics/drag traversent vers la fenêtre parente).
    internal class SakuraPetalsBackground : Control
    {
        private readonly Timer _timer;
        private readonly Random _rng = new Random();
        private readonly List<Petal> _petals = new List<Petal>();
        private LinearGradientBrush _bgBrush;

        private class Petal
        {
            public float X, Y, Size, Speed, SwayAmp, SwayPhase, SwaySpeed, Rotation, RotSpeed, Alpha;
            public Color Color;
        }

        public SakuraPetalsBackground()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
            TabStop = false;

            _timer = new Timer { Interval = 16 }; // ~60 fps
            _timer.Tick += (s, e) => { Step(); Invalidate(); };
            AnimationHub.ActiveChanged += UpdateTimer;
            UpdateTimer();
        }

        private void BuildBg()
        {
            _bgBrush?.Dispose();
            _bgBrush = new LinearGradientBrush(new Rectangle(0, 0, Math.Max(Width, 1), Math.Max(Height, 1)),
                Color.FromArgb(24, 16, 28), Color.FromArgb(48, 24, 44), LinearGradientMode.ForwardDiagonal);
        }

        private void Seed()
        {
            _petals.Clear();
            int count = Math.Max(18, Width / 26);
            for (int i = 0; i < count; i++) _petals.Add(NewPetal(false));
        }

        private Petal NewPetal(bool top)
        {
            var colors = Colors.sakuraPetals;
            return new Petal
            {
                X = (float)_rng.NextDouble() * Math.Max(Width, 1),
                Y = top ? -20f : (float)_rng.NextDouble() * Math.Max(Height, 1),
                Size = 6f + (float)_rng.NextDouble() * 10f,
                Speed = 0.5f + (float)_rng.NextDouble() * 1.3f,
                SwayAmp = 0.6f + (float)_rng.NextDouble() * 1.6f,
                SwayPhase = (float)(_rng.NextDouble() * Math.PI * 2),
                SwaySpeed = 0.02f + (float)_rng.NextDouble() * 0.04f,
                Rotation = (float)(_rng.NextDouble() * 360),
                RotSpeed = -2.2f + (float)_rng.NextDouble() * 4.4f,
                Alpha = 90 + (float)_rng.NextDouble() * 120f,
                Color = colors[_rng.Next(colors.Length)]
            };
        }

        private void Step()
        {
            foreach (var p in _petals)
            {
                p.Y += p.Speed;
                p.SwayPhase += p.SwaySpeed;
                p.X += (float)Math.Sin(p.SwayPhase) * p.SwayAmp;
                p.Rotation += p.RotSpeed;
                if (p.Y - p.Size > Height)
                {
                    var n = NewPetal(true);
                    p.X = n.X; p.Y = -p.Size; p.Size = n.Size; p.Speed = n.Speed;
                    p.SwayAmp = n.SwayAmp; p.SwayPhase = n.SwayPhase; p.SwaySpeed = n.SwaySpeed;
                    p.Rotation = n.Rotation; p.RotSpeed = n.RotSpeed; p.Alpha = n.Alpha; p.Color = n.Color;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (_bgBrush == null) BuildBg();
            g.FillRectangle(_bgBrush, 0, 0, Width, Height);

            // Deux halos roses diffus (coins) pour la profondeur.
            DrawGlow(g, Width * 0.85f, Height * 0.15f, Math.Min(Width, Height) * 0.5f, 55);
            DrawGlow(g, Width * 0.12f, Height * 0.9f, Math.Min(Width, Height) * 0.45f, 40);

            foreach (var p in _petals)
            {
                var st = g.Save();
                g.TranslateTransform(p.X, p.Y);
                g.RotateTransform(p.Rotation);
                int a = Math.Max(0, Math.Min(255, (int)p.Alpha));
                using (var brush = new SolidBrush(Color.FromArgb(a, p.Color)))
                using (var path = new GraphicsPath())
                {
                    float w = p.Size, h = p.Size * 1.5f;
                    path.AddBezier(0, -h / 2, w / 1.4f, -h / 4, w / 1.4f, h / 4, 0, h / 2);
                    path.AddBezier(0, h / 2, -w / 1.4f, h / 4, -w / 1.4f, -h / 4, 0, -h / 2);
                    path.CloseFigure();
                    g.FillPath(brush, path);
                }
                g.Restore(st);
            }
        }

        private void DrawGlow(Graphics g, float cx, float cy, float r, int alpha)
        {
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(cx - r, cy - r, r * 2, r * 2);
                using (var pgb = new PathGradientBrush(path))
                {
                    pgb.CenterColor = Color.FromArgb(alpha, Colors.mainColor);
                    pgb.SurroundColors = new[] { Color.FromArgb(0, Colors.mainColor) };
                    g.FillPath(pgb, path);
                }
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (Width > 0 && Height > 0) { BuildBg(); Seed(); }
        }

        // Laisse passer les clics/drag vers la fenêtre parente (fond décoratif).
        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x84;
            const int HTTRANSPARENT = -1;
            if (m.Msg == WM_NCHITTEST)
            {
                m.Result = (IntPtr)HTTRANSPARENT;
                return;
            }
            base.WndProc(ref m);
        }

        private void UpdateTimer()
        {
            if (Visible && AnimationHub.Active) _timer?.Start();
            else _timer?.Stop();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            UpdateTimer();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { AnimationHub.ActiveChanged -= UpdateTimer; _timer?.Dispose(); _bgBrush?.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
