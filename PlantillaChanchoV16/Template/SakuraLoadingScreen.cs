using PlantillaChanchoV16.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace PlantillaChanchoV16.Template
{
    // Écran de chargement "HUD" façon gaming-hub pro (Omen / Razer), identité sakura
    // conservée en filigrane : anneau de chargement + glyphe pétale, wordmark PaiPai,
    // barre "égaliseur" segmentée, statut en majuscules espacées avec pastille pulsante,
    // cadre HUD à crochets d'angle (cohérent avec le reste du relook). INDÉTERMINÉ (aucune
    // barre de pourcentage) -> ne se fige jamais. ~35 fps, animation coupée hors écran.
    internal class SakuraLoadingScreen : Control
    {
        private readonly Timer _timer;
        private readonly Random _rng = new Random();
        private readonly List<Petal> _petals = new List<Petal>();

        private float _t = 0f;          // temps global d'animation
        private float _spin = 0f;       // angle de l'anneau de chargement (degrés)

        private string _title;
        private string _message;

        private LinearGradientBrush _bgBrush;
        private PointF _center;
        private float _ringR;

        // Polices en cache : évite d'en recréer à chaque frame (~35 fps).
        private static readonly Font _fTitle = new Font("Inter Semibold", 20f);
        private static readonly Font _fMsg = new Font("Inter Semibold", 9f);

        public string Message
        {
            get => _message;
            set { _message = value; Invalidate(); }
        }

        private class Petal
        {
            public float X, Y, Size, Speed, SwayAmp, SwayPhase, SwaySpeed, Rotation, RotSpeed, Alpha;
            public Color Color;
        }

        public SakuraLoadingScreen(int width, int height, string title, string message)
        {
            _title = string.IsNullOrEmpty(title) ? "PaiPai" : title;
            _message = message ?? "";

            Width = width;
            Height = height;

            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;

            BuildLayout();
            SeedPetals(14); // discret : décor d'ambiance, pas le sujet principal

            _timer = new Timer { Interval = 28 }; // ~35 fps, fluide et léger
            _timer.Tick += (s, e) =>
            {
                _t += 0.07f;
                _spin += 6.5f; if (_spin > 360f) _spin -= 360f;
                StepPetals();
                Invalidate();
            };
            _timer.Start();
        }

        // ---------------- Mise en page (dépend de la taille) ----------------
        private void BuildLayout()
        {
            _bgBrush?.Dispose();
            _bgBrush = new LinearGradientBrush(new Rectangle(0, 0, Math.Max(Width, 1), Math.Max(Height, 1)),
                Color.FromArgb(22, 15, 25), Color.FromArgb(40, 20, 34), LinearGradientMode.Vertical);

            _center = new PointF(Width / 2f, Height * 0.40f);
            _ringR = Math.Max(30f, Math.Min(Width, Height) * 0.11f);
        }

        // ---------------- Pétales (décor d'ambiance discret) ----------------
        private void SeedPetals(int count)
        {
            _petals.Clear();
            for (int i = 0; i < count; i++)
                _petals.Add(NewPetal(false));
        }

        private Petal NewPetal(bool top)
        {
            var colors = Colors.sakuraPetals;
            return new Petal
            {
                X = (float)_rng.NextDouble() * Math.Max(Width, 1),
                Y = top ? -20f : (float)_rng.NextDouble() * Math.Max(Height, 1),
                Size = 5f + (float)_rng.NextDouble() * 7f,
                Speed = 0.4f + (float)_rng.NextDouble() * 1.0f,
                SwayAmp = 0.5f + (float)_rng.NextDouble() * 1.3f,
                SwayPhase = (float)(_rng.NextDouble() * Math.PI * 2),
                SwaySpeed = 0.02f + (float)_rng.NextDouble() * 0.03f,
                Rotation = (float)(_rng.NextDouble() * 360),
                RotSpeed = -1.8f + (float)_rng.NextDouble() * 3.6f,
                Alpha = 40 + (float)_rng.NextDouble() * 55f, // discret
                Color = colors[_rng.Next(colors.Length)]
            };
        }

        private void StepPetals()
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

        // ---------------- Rendu ----------------
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            if (_bgBrush != null) g.FillRectangle(_bgBrush, 0, 0, Width, Height);

            foreach (var p in _petals)
                DrawPetal(g, p.X, p.Y, p.Size, p.Rotation, p.Color, (int)p.Alpha);

            DrawLoadingRing(g);
            DrawTexts(g);
            DrawHudFrame(g);
        }

        // Anneau de chargement : piste fine statique + arc d'accent qui tourne, glyphe
        // pétale sakura fixe au centre (identité de marque, format compact et "tech").
        private void DrawLoadingRing(Graphics g)
        {
            float r = _ringR;
            var rect = new RectangleF(_center.X - r, _center.Y - r, r * 2, r * 2);

            using (var track = new Pen(Color.FromArgb(35, 255, 255, 255), 3f))
                g.DrawEllipse(track, rect);

            using (var arc = new Pen(Colors.mainColor, 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                g.DrawArc(arc, rect, _spin, 100f);

            // Halo discret derrière l'anneau.
            using (var glow = new GraphicsPath())
            {
                glow.AddEllipse(_center.X - r * 1.8f, _center.Y - r * 1.8f, r * 3.6f, r * 3.6f);
                using (var pgb = new PathGradientBrush(glow))
                {
                    pgb.CenterColor = Color.FromArgb(45, Colors.mainColor);
                    pgb.SurroundColors = new[] { Color.FromArgb(0, Colors.mainColor) };
                    g.FillPath(pgb, glow);
                }
            }

            // Glyphe pétale au centre, légère pulsation.
            float pulse = 0.9f + 0.1f * (float)Math.Sin(_t * 2.2f);
            DrawPetal(g, _center.X, _center.Y, r * 0.42f * pulse, _spin * 0.5f, Colors.mainColor, 235);
        }

        private void DrawTexts(Graphics g)
        {
            float cx = Width / 2f;
            float y = _center.Y + _ringR + 30f;

            using (var white = new SolidBrush(Color.White))
            using (var accent = new SolidBrush(Colors.mainColor))
            using (var soft = new SolidBrush(Color.FromArgb(190, 255, 255, 255)))
            {
                var fTitle = _fTitle; var fMsg = _fMsg;
                string a = "Pai", b = "Pai";
                SizeF sa = g.MeasureString(a, fTitle);
                SizeF sb = g.MeasureString(b, fTitle);
                float totalW = sa.Width + sb.Width - 8;
                float tx = cx - totalW / 2f;
                g.DrawString(a, fTitle, white, tx, y);
                g.DrawString(b, fTitle, accent, tx + sa.Width - 8, y);

                float barY = y + sa.Height + 16;
                DrawEqualizerBars(g, cx, barY, 108f, 18f);

                float msgY = barY + 18f + 18f;
                if (!string.IsNullOrEmpty(_message))
                {
                    string spaced = SpaceOut(_message.ToUpperInvariant());
                    SizeF sm = g.MeasureString(spaced, fMsg);

                    float dotD = 6f;
                    float groupW = dotD + 8f + sm.Width;
                    float gx = cx - groupW / 2f;
                    float dotY = msgY + sm.Height / 2f - dotD / 2f;

                    int dotA = (int)(140 + 115 * Math.Sin(_t * 3f));
                    using (var dot = new SolidBrush(Color.FromArgb(Math.Max(60, Math.Min(255, dotA)), Colors.mainColor)))
                        g.FillEllipse(dot, gx, dotY, dotD, dotD);

                    g.DrawString(spaced, fMsg, soft, gx + dotD + 8f, msgY);
                }
            }
        }

        // Petite barre "égaliseur" : plusieurs capsules dont la hauteur ondule en boucle
        // -> lecture immédiate "en cours de traitement", jamais figée, look HUD/gaming.
        private void DrawEqualizerBars(Graphics g, float cx, float y, float totalW, float maxH)
        {
            const int bars = 6;
            const float gap = 6f;
            float barW = (totalW - gap * (bars - 1)) / bars;
            float x0 = cx - totalW / 2f;

            using (var brush = new SolidBrush(Colors.mainColor))
            {
                for (int i = 0; i < bars; i++)
                {
                    float phase = i * 0.55f;
                    float k = 0.30f + 0.70f * (0.5f + 0.5f * (float)Math.Sin(_t * 2.4f + phase));
                    float h = Math.Max(3f, maxH * k);
                    float bx = x0 + i * (barW + gap);
                    float by = y + (maxH - h);
                    FillCapsuleVertical(g, brush, bx, by, barW, h);
                }
            }
        }

        private void FillCapsuleVertical(Graphics g, Brush brush, float x, float y, float w, float h)
        {
            using (var path = new GraphicsPath())
            {
                float r = Math.Min(w, h);
                path.AddArc(x, y, r, r, 180, 180);
                path.AddArc(x, y + h - r, r, r, 0, 180);
                path.CloseFigure();
                g.FillPath(brush, path);
            }
        }

        // Cadre HUD : crochets d'angle roses, même signature que la bannière d'accueil et
        // la carte de login -> relie visuellement le chargement au reste de l'appli.
        private void DrawHudFrame(Graphics g)
        {
            using (var ap = new Pen(Colors.mainColor, 2f))
            {
                int L = 20, m = 16;
                g.DrawLine(ap, m, m + 9, m, m);
                g.DrawLine(ap, m, m, m + L, m);
                g.DrawLine(ap, Width - m, Height - m - 9, Width - m, Height - m);
                g.DrawLine(ap, Width - m, Height - m, Width - m - L, Height - m);
            }
        }

        private static string SpaceOut(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return string.Join(" ", s.ToCharArray());
        }

        private void DrawPetal(Graphics g, float x, float y, float size, float rotation, Color color, int alpha)
        {
            var state = g.Save();
            g.TranslateTransform(x, y);
            g.RotateTransform(rotation);

            int a = Math.Max(0, Math.Min(255, alpha));
            using (var brush = new SolidBrush(Color.FromArgb(a, color)))
            using (var path = new GraphicsPath())
            {
                float w = size, h = size * 1.5f;
                path.AddBezier(0, -h / 2, w / 1.4f, -h / 4, w / 1.4f, h / 4, 0, h / 2);
                path.AddBezier(0, h / 2, -w / 1.4f, h / 4, -w / 1.4f, -h / 4, 0, -h / 2);
                path.CloseFigure();
                g.FillPath(brush, path);
            }

            g.Restore(state);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (Width > 0 && Height > 0) BuildLayout();
        }

        // N'anime (35 fps) que lorsque l'écran est réellement visible -> zéro CPU gaspillé.
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible) _timer?.Start();
            else _timer?.Stop();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer?.Dispose();
                _bgBrush?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
