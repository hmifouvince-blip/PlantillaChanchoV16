using Guna.UI2.WinForms;
using PlantillaChanchoV16.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace PlantillaChanchoV16.Template
{
    // Bannière d'accueil PaiPai avec pétales de sakura qui tombent (animation fluide).
    // Remplace l'ancien "produit vedette" (Valorant) en haut de l'accueil.
    internal class WelcomeBanner : Guna2Panel
    {
        private readonly Timer _timer;
        private readonly List<Petal> _petals = new List<Petal>();
        private readonly Random _rng = new Random();
        private readonly int _cornerRadius = 8;

        private string _username;
        private string _timeLeft;

        private class Petal
        {
            public float X, Y, Size, Speed, SwayAmp, SwayPhase, SwaySpeed, Rotation, RotSpeed, Alpha;
            public Color Color;
        }

        // Polices mises en cache (partagées) : évite d'en recréer ~4 à CHAQUE frame (~60 fps)
        // -> plus de churn de handles GDI. Indépendantes du thème.
        private static readonly Font _fEyebrow = new Font("Inter Semibold", 9.5f);
        private static readonly Font _fBig = new Font("Inter Semibold", 29f);
        private static readonly Font _fSub = new Font("Inter Medium", 11f);
        private static readonly Font _fPill = new Font("Inter Semibold", 10.5f);

        public WelcomeBanner(int width, int height, string username)
        {
            _username = string.IsNullOrWhiteSpace(username) ? "player" : username;

            Width = width;
            Height = height;
            FillColor = Colors.bgColor;
            BackColor = Colors.bgColor;
            BorderThickness = 0;

            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            _timeLeft = ComputeTimeLeft();

            ApplyRoundedRegion();
            SeedPetals(22);

            // Plus de bouton "Add license" ici : redondant avec l'onglet "Claim Key" de la
            // barre de nav (qui ouvre le même dialogue via ShowClaimDialog()).

            _timer = new Timer { Interval = 16 }; // ~60 fps
            _timer.Tick += (s, e) => { StepPetals(); Invalidate(); };
            AnimationHub.ActiveChanged += UpdateTimer;
            UpdateTimer();
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

        // Temps restant sur la 1ère clé du compte : "Lifetime", "X day(s) left", "Expired"...
        // (localisé, logique centralisée dans LicenseGate.FormatTimeLeft).
        private string ComputeTimeLeft()
        {
            var subs = Login.KeyAuthApp?.user_data?.subscriptions;
            if (subs == null || subs.Count == 0) return Localization.T("time.no_active_key");
            if (!long.TryParse(subs[0].expiry, out long exp)) return Localization.T("time.unknown");

            return LicenseGate.FormatTimeLeft(DateTimeOffset.FromUnixTimeSeconds(exp).LocalDateTime);
        }

        // Déclenché après un claim réussi : permet à Main de rafraîchir le rail de
        // navigation / la page User (les nouveaux abonnements sont déjà en mémoire).
        public event Action LicenseClaimed;

        // Ouvre le dialogue de saisie de clé (utilisé par le bouton de la bannière ET par
        // l'onglet "Claim Key" de la barre de navigation).
        public void ShowClaimDialog()
        {
            string key;
            using (var dlg = new SakuraInputDialog(
                Localization.T("banner.claim_title"),
                Localization.T("banner.claim_body"),
                Localization.T("login.field_license")))
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                key = dlg.Value;
            }
            if (string.IsNullOrWhiteSpace(key)) return;

            try
            {
                // Instantané "avant" (nom d'abonnement -> expiry unix) pour détecter ensuite
                // ce qui a changé : nouveau produit débloqué, ou temps prolongé sur un produit
                // déjà possédé.
                var before = SnapshotSubscriptions();

                string user = Login.KeyAuthApp?.user_data?.username;
                Login.KeyAuthApp.upgrade(user, key);
                string msg = Login.KeyAuthApp.response.message;

                // upgrade() recharge désormais user_data.subscriptions immédiatement en cas
                // de succès -> on peut comparer avant/après sans reconnexion.
                var after = SnapshotSubscriptions();
                var claimed = FindClaimedEntry(before, after);

                if (claimed != null)
                {
                    string productName = Utilities.ProductCatalog.DisplayNameOf(claimed.Value.Key);
                    string duration = Utilities.LicenseGate.FormatTimeLeft(
                        DateTimeOffset.FromUnixTimeSeconds(claimed.Value.Value).LocalDateTime);

                    PlantillaChanchoV16.Template.SakuraMessageBox.Show(
                        Localization.T("banner.claim_added_body", productName, duration),
                        Localization.T("banner.claim_added_title"), MessageBoxButtons.OK, MessageBoxIcon.Information);

                    _timeLeft = ComputeTimeLeft();
                    Invalidate();
                    LicenseClaimed?.Invoke();
                }
                else
                {
                    // Pas de changement détecté (clé invalide, expirée, déjà utilisée...) ->
                    // on retombe sur le message brut renvoyé par KeyAuth.
                    PlantillaChanchoV16.Template.SakuraMessageBox.Show(
                        string.IsNullOrEmpty(msg) ? Localization.T("banner.claim_failed") : msg,
                        Localization.T("banner.claim_title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                PlantillaChanchoV16.Template.SakuraMessageBox.Show(Localization.T("banner.claim_error", ex.Message), Localization.T("banner.claim_title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Dictionary<string, long> SnapshotSubscriptions()
        {
            var map = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            var subs = Login.KeyAuthApp?.user_data?.subscriptions;
            if (subs == null) return map;
            foreach (var s in subs)
                if (long.TryParse(s.expiry, out long exp))
                    map[s.subscription] = exp;
            return map;
        }

        // Trouve l'abonnement nouvellement apparu, ou dont l'expiry a avancé, entre les
        // deux instantanés. Renvoie (nom d'abonnement, nouvelle expiry unix) ou null.
        private KeyValuePair<string, long>? FindClaimedEntry(Dictionary<string, long> before, Dictionary<string, long> after)
        {
            KeyValuePair<string, long>? best = null;
            foreach (var kv in after)
            {
                bool isNew = !before.TryGetValue(kv.Key, out long prevExp);
                bool extended = !isNew && kv.Value > prevExp;
                if (isNew || extended)
                {
                    if (best == null || kv.Value > best.Value.Value) best = kv;
                }
            }
            return best;
        }

        private void ApplyRoundedRegion()
        {
            using (GraphicsPath path = RoundedRect(new Rectangle(0, 0, Width, Height), _cornerRadius))
                Region = new Region(path);
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void SeedPetals(int count)
        {
            _petals.Clear();
            for (int i = 0; i < count; i++)
                _petals.Add(NewPetal(startAtTop: false));
        }

        private Petal NewPetal(bool startAtTop)
        {
            var colors = Colors.sakuraPetals;
            return new Petal
            {
                X = (float)_rng.NextDouble() * Math.Max(Width, 1),
                Y = startAtTop ? -20f : (float)_rng.NextDouble() * Math.Max(Height, 1),
                Size = 7f + (float)_rng.NextDouble() * 9f,
                Speed = 0.5f + (float)_rng.NextDouble() * 1.2f,
                SwayAmp = 0.6f + (float)_rng.NextDouble() * 1.4f,
                SwayPhase = (float)(_rng.NextDouble() * Math.PI * 2),
                SwaySpeed = 0.02f + (float)_rng.NextDouble() * 0.04f,
                Rotation = (float)(_rng.NextDouble() * 360),
                RotSpeed = -2f + (float)_rng.NextDouble() * 4f,
                Alpha = 120 + (float)_rng.NextDouble() * 110f,
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
                    var np = NewPetal(startAtTop: true);
                    p.X = np.X; p.Y = -p.Size; p.Size = np.Size; p.Speed = np.Speed;
                    p.SwayAmp = np.SwayAmp; p.SwayPhase = np.SwayPhase; p.SwaySpeed = np.SwaySpeed;
                    p.Rotation = np.Rotation; p.RotSpeed = np.RotSpeed; p.Alpha = np.Alpha; p.Color = np.Color;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(0, 0, Width, Height);

            // Fond en dégradé prune -> rosé sombre (diagonale).
            Color topLeft = Colors.bgColor;
            Color bottomRight = Color.FromArgb(46, 24, 40); // #2E1828 teinte sakura sombre
            using (var bg = new LinearGradientBrush(rect, topLeft, bottomRight, LinearGradientMode.ForwardDiagonal))
                g.FillRectangle(bg, rect);

            // Halo rose diffus en haut à droite pour l'ambiance sakura.
            using (var glow = new GraphicsPath())
            {
                glow.AddEllipse(Width - 260, -160, 360, 320);
                using (var pgb = new PathGradientBrush(glow))
                {
                    pgb.CenterColor = Color.FromArgb(60, Colors.mainColor);
                    pgb.SurroundColors = new[] { Color.FromArgb(0, Colors.mainColor) };
                    g.FillPath(pgb, glow);
                }
            }

            // Pétales.
            foreach (var p in _petals)
                DrawPetal(g, p);

            // Textes. (polices en cache : _fEyebrow/_fBig/_fSub/_fPill)
            using (var brushSoft = new SolidBrush(Color.FromArgb(205, 255, 255, 255)))
            using (var brushDim = new SolidBrush(Color.FromArgb(150, 255, 255, 255)))
            using (var brushWhite = new SolidBrush(Color.White))
            using (var brushAccent = new SolidBrush(Colors.mainColor))
            {
                var fEyebrow = _fEyebrow; var fBig = _fBig; var fSub = _fSub; var fPill = _fPill;
                int left = 36;

                g.DrawString(Localization.T("banner.welcome_back"), fEyebrow, brushAccent, left, 38);
                g.DrawString(_username, fBig, brushWhite, left - 2, 56);

                float uw = g.MeasureString(_username, fBig).Width;
                using (var bar = new SolidBrush(Colors.mainColor))
                    FillRoundedRectF(g, bar, left, 104, Math.Min(54f, uw), 3f, 1.5f);

                g.DrawString(Localization.T("banner.subtitle"), fSub, brushDim, left, 116);

                // Badge pilule "License · <temps restant>".
                string pre = Localization.T("banner.license_label"), dot = "  ·  ", val = _timeLeft ?? "";
                float wPre = g.MeasureString(pre, fPill).Width;
                float wDot = g.MeasureString(dot, fPill).Width;
                float wVal = g.MeasureString(val, fPill).Width;
                float padX = 13f, pillH = 28f, pillY = 150f;
                float pillW = padX * 2 + wPre + wDot + wVal;

                using (var pf = new SolidBrush(Color.FromArgb(40, Colors.mainColor)))
                    FillRoundedRectF(g, pf, left, pillY, pillW, pillH, pillH / 2f);
                using (var pp = new Pen(Color.FromArgb(120, Colors.mainColor), 1f))
                    DrawRoundedRectF(g, pp, left, pillY, pillW, pillH, pillH / 2f);

                float th = g.MeasureString(pre, fPill).Height;
                float ty = pillY + (pillH - th) / 2f;
                float tx = left + padX;
                g.DrawString(pre, fPill, brushSoft, tx, ty);
                g.DrawString(dot, fPill, brushDim, tx + wPre, ty);
                g.DrawString(val, fPill, brushAccent, tx + wPre + wDot, ty);
            }

            // ---- Cadre "HUD" premium facon Omen / Razer : lisere arrondi + accents d'angle ----
            var frame = new Rectangle(1, 1, Width - 3, Height - 3);
            using (var fp = new GraphicsPath())
            {
                int d = _cornerRadius * 2;
                fp.AddArc(frame.X, frame.Y, d, d, 180, 90);
                fp.AddArc(frame.Right - d, frame.Y, d, d, 270, 90);
                fp.AddArc(frame.Right - d, frame.Bottom - d, d, d, 0, 90);
                fp.AddArc(frame.X, frame.Bottom - d, d, d, 90, 90);
                fp.CloseFigure();
                using (var pen = new Pen(Color.FromArgb(55, Colors.mainColor), 1.4f))
                    g.DrawPath(pen, fp);
            }
            using (var ap = new Pen(Colors.mainColor, 2f))
            {
                int L = 18, m = 12;
                // Crochet haut-gauche.
                g.DrawLine(ap, m, m + 8, m, m);
                g.DrawLine(ap, m, m, m + L, m);
                // Crochet bas-droite.
                g.DrawLine(ap, Width - m, Height - m - 8, Width - m, Height - m);
                g.DrawLine(ap, Width - m, Height - m, Width - m - L, Height - m);
            }
        }

        private void FillRoundedRectF(Graphics g, Brush brush, float x, float y, float w, float h, float r = 0)
        {
            if (r <= 0) { g.FillRectangle(brush, x, y, w, h); return; }
            using (var path = RoundedF(x, y, w, h, r))
                g.FillPath(brush, path);
        }

        private void DrawRoundedRectF(Graphics g, Pen pen, float x, float y, float w, float h, float r)
        {
            using (var path = RoundedF(x, y, w, h, r))
                g.DrawPath(pen, path);
        }

        private static GraphicsPath RoundedF(float x, float y, float w, float h, float r)
        {
            float d = Math.Min(r * 2, Math.Min(w, h));
            var path = new GraphicsPath();
            path.AddArc(x, y, d, d, 180, 90);
            path.AddArc(x + w - d, y, d, d, 270, 90);
            path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
            path.AddArc(x, y + h - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void DrawPetal(Graphics g, Petal p)
        {
            var state = g.Save();
            g.TranslateTransform(p.X, p.Y);
            g.RotateTransform(p.Rotation);

            int a = (int)Math.Max(0, Math.Min(255, p.Alpha));
            using (var brush = new SolidBrush(Color.FromArgb(a, p.Color)))
            using (var path = new GraphicsPath())
            {
                // Forme de pétale (goutte arrondie) construite avec deux courbes.
                float w = p.Size, h = p.Size * 1.5f;
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
            if (Width > 0 && Height > 0) ApplyRoundedRegion();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { AnimationHub.ActiveChanged -= UpdateTimer; _timer?.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
