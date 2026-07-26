using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PlantillaChanchoV16.Utilities
{
    // Habillage visuel "gaming hub" facon Omen Gaming Hub / Razer Cortex.
    // 100% VISUEL : on peint des degrades sombres + un halo d'accent PAR-DESSUS les
    // panneaux existants via leur event Paint. Aucune logique, aucun texte, aucune
    // structure de donnees n'est touchee. L'accent reste la couleur du theme (sakura).
    internal static class UiStyle
    {
        // Active le double tampon sur un controle ET toute sa descendance.
        //
        // Panel/Form exposent DoubleBuffered en PROTECTED : impossible de le poser de
        // l'exterieur autrement que par reflexion. Sans lui, chaque survol, defilement
        // ou changement d'onglet repeint panneau par panneau -> le scintillement et
        // les a-coups ressentis dans les fiches produit, qui empilent des dizaines de
        // panneaux imbriques.
        //
        // A appeler UNE FOIS, apres avoir rempli l'arbre de controles.
        public static void EnableDoubleBuffer(Control root)
        {
            if (root == null) return;

            var prop = typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var setStyle = typeof(Control).GetMethod("SetStyle",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            void Apply(Control c)
            {
                try
                {
                    prop?.SetValue(c, true, null);
                    setStyle?.Invoke(c, new object[]
                    {
                        ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true
                    });
                }
                catch { /* un controle tiers peut refuser : jamais bloquant, c'est du confort */ }

                foreach (Control child in c.Controls) Apply(child);
            }

            Apply(root);
        }

        // Fond de la zone centrale : degrade vertical sombre + halo d'accent diffus en haut.
        // Donne de la profondeur au lieu d'un aplat noir.
        public static void AttachContentBackdrop(Control c)
        {
            c.Paint += (s, e) =>
            {
                var g = e.Graphics;
                Rectangle r = c.ClientRectangle;
                if (r.Width <= 1 || r.Height <= 1) return;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                Color top = Lighten(Colors.bgColor, 0.05f);
                Color bottom = Darken(Colors.bgColor, 0.16f);
                using (var lg = new LinearGradientBrush(r, top, bottom, LinearGradientMode.Vertical))
                    g.FillRectangle(lg, r);

                // Halo d'accent diffus, haut-centre (subtil).
                DrawGlow(g, new PointF(r.Width * 0.34f, r.Height * 0.01f),
                         Math.Max(r.Width, r.Height) * 0.5f,
                         Color.FromArgb(38, Colors.mainColor));
            };
        }

        // Barre laterale facon "rail" : degrade + lisere d'accent sur le bord droit +
        // glow d'accent derriere le logo (haut).
        public static void AttachSidebar(Control c)
        {
            c.Paint += (s, e) =>
            {
                var g = e.Graphics;
                Rectangle r = c.ClientRectangle;
                if (r.Width <= 1 || r.Height <= 1) return;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                Color top = Lighten(Colors.bgColor, 0.10f);
                Color bottom = Darken(Colors.bgColor, 0.08f);
                using (var lg = new LinearGradientBrush(r, top, bottom, LinearGradientMode.Vertical))
                    g.FillRectangle(lg, r);

                // Glow d'accent derriere le logo.
                DrawGlow(g, new PointF(r.Width / 2f, 46), 130,
                         Color.FromArgb(55, Colors.mainColor));

                // Lisere d'accent degrade sur le bord droit (haut transparent -> bas colore).
                var edge = new Rectangle(r.Right - 2, 0, 2, r.Height);
                using (var lg2 = new LinearGradientBrush(edge,
                        Color.FromArgb(0, Colors.mainColor),
                        Color.FromArgb(140, Colors.mainColor),
                        LinearGradientMode.Vertical))
                    g.FillRectangle(lg2, edge);
            };
        }

        // Barre de navigation HORIZONTALE (en haut) : degrade + glow d'accent derriere le
        // logo (gauche) + fin lisere d'accent sur le bord BAS (au lieu du bord droit d'un
        // rail vertical).
        public static void AttachTopBar(Control c)
        {
            c.Paint += (s, e) =>
            {
                var g = e.Graphics;
                Rectangle r = c.ClientRectangle;
                if (r.Width <= 1 || r.Height <= 1) return;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                Color top = Lighten(Colors.bgColor, 0.10f);
                Color bottom = Darken(Colors.bgColor, 0.08f);
                using (var lg = new LinearGradientBrush(r, top, bottom, LinearGradientMode.Vertical))
                    g.FillRectangle(lg, r);

                // Glow d'accent discret derriere le logo, a gauche.
                DrawGlow(g, new PointF(38, r.Height / 2f), 110,
                         Color.FromArgb(50, Colors.mainColor));

                // Lisere d'accent degrade sur toute la largeur du bord bas (gauche transparent
                // -> centre colore -> droite transparent), signature de la barre.
                var edge = new Rectangle(0, r.Bottom - 2, r.Width, 2);
                using (var lg2 = new LinearGradientBrush(edge,
                        Color.FromArgb(160, Colors.mainColor),
                        Color.FromArgb(20, Colors.mainColor),
                        LinearGradientMode.Horizontal))
                {
                    var blend = new ColorBlend(3)
                    {
                        Colors = new[] { Color.FromArgb(0, Colors.mainColor), Color.FromArgb(150, Colors.mainColor), Color.FromArgb(0, Colors.mainColor) },
                        Positions = new[] { 0f, 0.5f, 1f }
                    };
                    lg2.InterpolationColors = blend;
                    g.FillRectangle(lg2, edge);
                }
            };
        }

        // Scrim sombre en bas d'une carte produit (lisibilite du logo/nom) + fin lisere
        // clair en haut (profondeur "premium"). A appeler depuis le Paint de la carte.
        public static void PaintCardScrim(Graphics g, Rectangle r)
        {
            if (r.Width <= 1 || r.Height <= 1) return;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int h = (int)(r.Height * 0.55f);
            var scrim = new Rectangle(r.X, r.Bottom - h, r.Width, h);
            using (var lg = new LinearGradientBrush(scrim,
                    Color.FromArgb(0, 0, 0, 0),
                    Color.FromArgb(205, 0, 0, 0),
                    LinearGradientMode.Vertical))
                g.FillRectangle(lg, scrim);

            // Fin reflet clair sur le bord haut.
            using (var pen = new Pen(Color.FromArgb(28, 255, 255, 255)))
                g.DrawLine(pen, r.X + 6, r.Y + 1, r.Right - 6, r.Y + 1);
        }

        // Petit accent (barre arrondie) sous un titre de section -> touche
        // "gaming hub". Dessine dans le coin bas-gauche du label, sans decaler la mise en page.
        public static void AttachTitleAccent(Control label)
        {
            label.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                int barW = 26, barH = 3;
                int y = label.Height - barH;
                var r = new Rectangle(1, y, barW, barH);
                using (var path = Rounded(r, barH / 2))
                using (var b = new LinearGradientBrush(r,
                        Colors.mainColor, Lighten(Colors.mainColor, 0.18f),
                        LinearGradientMode.Horizontal))
                    g.FillPath(b, path);
            };
            label.Invalidate();
        }

        // Reflet "verre" premium sur un bouton d'accent : léger dégradé blanc sur la moitié
        // haute (brillant en haut -> transparent au milieu), clipé aux coins arrondis du
        // bouton. 100% visuel (Paint par-dessus), aucune logique touchée.
        public static void AddGlossySheen(Guna.UI2.WinForms.Guna2Button btn)
        {
            btn.Paint += (s, e) =>
            {
                int w = btn.Width, h = btn.Height;
                if (w <= 2 || h <= 2) return;
                int r = Math.Max(1, btn.BorderRadius);

                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var clip = Rounded(new Rectangle(0, 0, w, h), r))
                {
                    var old = g.Clip;
                    g.SetClip(clip);
                    var top = new Rectangle(0, 0, w, (int)(h * 0.52f));
                    using (var lg = new LinearGradientBrush(top,
                            Color.FromArgb(42, 255, 255, 255),
                            Color.FromArgb(0, 255, 255, 255),
                            LinearGradientMode.Vertical))
                        g.FillRectangle(lg, top);
                    g.Clip = old;
                }
            };
            btn.Invalidate();
        }

        private static GraphicsPath Rounded(Rectangle r, int radius)
        {
            int d = Math.Max(1, radius * 2);
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        // ---- helpers ----
        private static void DrawGlow(Graphics g, PointF center, float radius, Color color)
        {
            if (radius <= 0) return;
            using (var gp = new GraphicsPath())
            {
                gp.AddEllipse(center.X - radius, center.Y - radius, radius * 2, radius * 2);
                using (var pgb = new PathGradientBrush(gp))
                {
                    pgb.CenterColor = color;
                    pgb.SurroundColors = new[] { Color.FromArgb(0, color) };
                    pgb.CenterPoint = center;
                    g.FillPath(pgb, gp);
                }
            }
        }

        public static Color Lighten(Color c, float amt)
        {
            return Color.FromArgb(c.A,
                (int)Math.Min(255, c.R + 255 * amt),
                (int)Math.Min(255, c.G + 255 * amt),
                (int)Math.Min(255, c.B + 255 * amt));
        }

        public static Color Darken(Color c, float amt)
        {
            return Color.FromArgb(c.A,
                (int)Math.Max(0, c.R - 255 * amt),
                (int)Math.Max(0, c.G - 255 * amt),
                (int)Math.Max(0, c.B - 255 * amt));
        }
    }
}
