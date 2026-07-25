using PlantillaChanchoV16.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace PlantillaChanchoV16.Template
{
    // Écran de mise à jour PaiPai : fond sakura animé + barre de progression RÉELLE du
    // téléchargement, puis relance automatiquement sur la nouvelle version.
    internal class SakuraUpdateScreen : Form
    {
        private readonly Timer _anim;
        private readonly Random _rng = new Random();
        private readonly List<Petal> _petals = new List<Petal>();

        private readonly string _url, _tempPath, _toVersion;
        private WebClient _wc;
        private float _progress = 0f;      // 0..1

        // Polices en cache : évite d'en recréer à chaque frame (~35 fps).
        private static readonly Font _fTitle = new Font("Inter Semibold", 20f);
        private static readonly Font _fSub = new Font("Inter Medium", 10.5f);
        private static readonly Font _fSt = new Font("Inter Semibold", 9f);
        private static readonly Font _fPct = new Font("Inter Semibold", 15f);
        private string _status = "Preparing update...";
        private float _spin = 0f;

        private class Petal { public float X, Y, Size, Speed, Sway, Phase, PhaseSpeed, Rot, RotSpeed, Alpha; public Color Color; }

        public SakuraUpdateScreen(string url, string toVersion, string tempPath)
        {
            _url = url; _toVersion = toVersion ?? ""; _tempPath = tempPath;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(520, 340);
            BackColor = Colors.bgColor;
            ShowInTaskbar = true;
            TopMost = true;
            ControlBox = false;
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            using (var p = Rounded(new Rectangle(0, 0, Width, Height), Default.borderForms))
                Region = new Region(p);

            for (int i = 0; i < 20; i++) _petals.Add(NewPetal(false));

            _anim = new Timer { Interval = 28 };
            _anim.Tick += (s, e) => { _spin = (_spin + 2.2f) % 360f; Step(); Invalidate(); };
            _anim.Start();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            StartDownload();
        }

        private void StartDownload()
        {
            try
            {
                if (File.Exists(_tempPath)) File.Delete(_tempPath);

                _wc = new WebClient();
                _wc.DownloadProgressChanged += (s, ev) =>
                {
                    _progress = ev.ProgressPercentage / 100f;
                    _status = $"Downloading update... {ev.ProgressPercentage}%";
                    Invalidate();
                };
                _wc.DownloadFileCompleted += (s, ev) =>
                {
                    if (ev.Error != null)
                    {
                        _anim.Stop();
                        SakuraMessageBox.Show("Update failed: " + ev.Error.Message + "\n\nDownload manually:\n" + _url,
                            "Update", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        try { Process.Start(new ProcessStartInfo(_url) { UseShellExecute = true }); } catch { }
                        DialogResult = DialogResult.Abort;
                        Close();
                        return;
                    }

                    _progress = 1f;
                    _status = "Installing update...";
                    Invalidate();
                    Application.DoEvents();

                    // Remplace l'exe actuel PAR le nouveau (en place), mémorise la version,
                    // puis relance -> pas de retour arrière, et plus besoin de toucher AppVersion.
                    Updater.ReplaceAndRelaunch(_tempPath, _toVersion);
                    Environment.Exit(0);
                };
                _wc.DownloadFileAsync(new Uri(_url), _tempPath);
            }
            catch (Exception ex)
            {
                SakuraMessageBox.Show("Update error: " + ex.Message, "Update", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.Abort;
                Close();
            }
        }

        // ---------- Pétales ----------
        private Petal NewPetal(bool top)
        {
            var c = Colors.sakuraPetals;
            return new Petal
            {
                X = (float)_rng.NextDouble() * Width,
                Y = top ? -20f : (float)_rng.NextDouble() * Height,
                Size = 6f + (float)_rng.NextDouble() * 9f,
                Speed = 0.5f + (float)_rng.NextDouble() * 1.2f,
                Sway = 0.6f + (float)_rng.NextDouble() * 1.4f,
                Phase = (float)(_rng.NextDouble() * Math.PI * 2),
                PhaseSpeed = 0.02f + (float)_rng.NextDouble() * 0.04f,
                Rot = (float)(_rng.NextDouble() * 360),
                RotSpeed = -2f + (float)_rng.NextDouble() * 4f,
                Alpha = 100 + (float)_rng.NextDouble() * 120f,
                Color = c[_rng.Next(c.Length)]
            };
        }

        private void Step()
        {
            foreach (var p in _petals)
            {
                p.Y += p.Speed; p.Phase += p.PhaseSpeed;
                p.X += (float)Math.Sin(p.Phase) * p.Sway; p.Rot += p.RotSpeed;
                if (p.Y - p.Size > Height)
                {
                    var n = NewPetal(true);
                    p.X = n.X; p.Y = -p.Size; p.Size = n.Size; p.Speed = n.Speed; p.Sway = n.Sway;
                    p.Phase = n.Phase; p.PhaseSpeed = n.PhaseSpeed; p.Rot = n.Rot; p.RotSpeed = n.RotSpeed;
                    p.Alpha = n.Alpha; p.Color = n.Color;
                }
            }
        }

        // ---------- Rendu ----------
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = new Rectangle(0, 0, Width, Height);
            using (var bg = new LinearGradientBrush(rect, Colors.bgColor, Color.FromArgb(40, 20, 34), LinearGradientMode.Vertical))
                g.FillRectangle(bg, rect);

            foreach (var p in _petals) DrawPetal(g, p);

            int cx = Width / 2;

            // Jauge circulaire de progression RÉELLE (remplace la fleur décorative) : lecture
            // immédiate du pourcentage, cohérent avec l'anneau de chargement du reste de l'appli.
            DrawProgressRing(g, cx, 100, 48f);

            using (var white = new SolidBrush(Color.White))
            using (var accent = new SolidBrush(Colors.mainColor))
            using (var soft = new SolidBrush(Color.FromArgb(190, 255, 255, 255)))
            {
                var fTitle = _fTitle; var fSub = _fSub;
                string a = "Pai", b = "Pai";
                SizeF sa = g.MeasureString(a, fTitle), sb = g.MeasureString(b, fTitle);
                float tx = cx - (sa.Width + sb.Width - 8) / 2f, ty = 172;
                g.DrawString(a, fTitle, white, tx, ty);
                g.DrawString(b, fTitle, accent, tx + sa.Width - 8, ty);

                string upd = string.IsNullOrEmpty(_toVersion) ? "Updating PaiPai" : $"Updating to v{_toVersion}";
                SizeF su = g.MeasureString(upd, fSub);
                g.DrawString(upd, fSub, soft, cx - su.Width / 2f, ty + sa.Height + 4);
            }

            // Statut en majuscules espacées + pastille pulsante (même langage que le
            // chargement indéterminé) au lieu d'une barre linéaire redondante avec la jauge.
            using (var soft = new SolidBrush(Color.FromArgb(190, 255, 255, 255)))
            {
                var fSt = _fSt;
                string spaced = SpaceOut(_status.ToUpperInvariant());
                SizeF ss = g.MeasureString(spaced, fSt);
                float msgY = 250;
                float dotD = 6f;
                float groupW = dotD + 8f + ss.Width;
                float gx = cx - groupW / 2f;
                float dotY = msgY + ss.Height / 2f - dotD / 2f;

                int dotA = (int)(140 + 115 * Math.Sin(_spin * 0.0349f)); // dérivé de _spin -> pulsation continue
                using (var dot = new SolidBrush(Color.FromArgb(Math.Max(60, Math.Min(255, dotA)), Colors.mainColor)))
                    g.FillEllipse(dot, gx, dotY, dotD, dotD);

                g.DrawString(spaced, fSt, soft, gx + dotD + 8f, msgY);
            }

            DrawHudFrame(g);
        }

        // Jauge circulaire : piste fine + arc de progression réel (0..1) + pourcentage
        // centré. Le trait fin qui tourne en continu (indépendant du %) signale "actif".
        private void DrawProgressRing(Graphics g, int cx, int cy, float r)
        {
            var rect = new RectangleF(cx - r, cy - r, r * 2, r * 2);

            using (var track = new Pen(Color.FromArgb(35, 255, 255, 255), 4f))
                g.DrawEllipse(track, rect);

            float sweep = 360f * Math.Max(0f, Math.Min(1f, _progress));
            if (sweep > 0.5f)
                using (var arc = new Pen(Colors.mainColor, 4f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    g.DrawArc(arc, rect, -90f, sweep);

            // Petit repère qui tourne en continu autour de la piste -> signale "actif" même
            // quand le téléchargement stagne un instant (jamais l'impression de gel).
            using (var tick = new Pen(Color.FromArgb(120, 255, 255, 255), 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                g.DrawArc(tick, rect, _spin, 18f);

            using (var pct = new SolidBrush(Color.White))
            {
                var fPct = _fPct;
                string t = $"{(int)Math.Round(_progress * 100)}%";
                SizeF st = g.MeasureString(t, fPct);
                g.DrawString(t, fPct, pct, cx - st.Width / 2f, cy - st.Height / 2f);
            }
        }

        // Cadre HUD : crochets d'angle roses, cohérents avec le reste du relook.
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

        private void DrawPetal(Graphics g, Petal p)
        {
            var st = g.Save();
            g.TranslateTransform(p.X, p.Y); g.RotateTransform(p.Rot);
            int a = Math.Max(0, Math.Min(255, (int)p.Alpha));
            using (var br = new SolidBrush(Color.FromArgb(a, p.Color)))
            using (var path = new GraphicsPath())
            {
                float w = p.Size, h = p.Size * 1.5f;
                path.AddBezier(0, -h / 2, w / 1.4f, -h / 4, w / 1.4f, h / 4, 0, h / 2);
                path.AddBezier(0, h / 2, -w / 1.4f, h / 4, -w / 1.4f, -h / 4, 0, -h / 2);
                path.CloseFigure();
                g.FillPath(br, path);
            }
            g.Restore(st);
        }

        private static GraphicsPath Rounded(Rectangle r, int radius)
        {
            int d = Math.Max(2, radius * 2);
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _anim?.Dispose(); _wc?.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
