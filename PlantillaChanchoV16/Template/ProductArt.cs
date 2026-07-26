using PlantillaChanchoV16.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace PlantillaChanchoV16.Template
{
    // Affiches de produit generees a la volee (aucun fichier image a fournir).
    //
    // Pourquoi : trois cartes du carrousel d'accueil partageaient la MEME capture
    // generique (images.Img2Anydesk) faute de visuel dedie -> deux rectangles
    // bleu/cyan identiques et hors charte a cote des vraies affiches Valorant et
    // Roblox. Un visuel dessine vaut mieux qu'une image d'un autre produit : il
    // est toujours a la bonne taille, toujours dans la charte sakura, et chaque
    // produit garde sa propre couleur + son propre glyphe.
    //
    // Meme approche que BotIcon/CategoryIcon (glyphes vectoriels dessines) mais a
    // l'echelle d'une affiche.
    internal static class ProductArt
    {
        public enum Glyph { Shield, Crosshair, Blocks, Window, Bot }

        // Rendu large puis reduit par le carrousel : FitTo() peut agrandir les
        // cartes jusqu'a 2.8x (158x182 -> ~442x510), dessiner a la taille de base
        // donnerait une affiche floue en plein ecran.
        private const int W = 560, H = 640;

        private static readonly Dictionary<string, Image> Cache = new Dictionary<string, Image>();

        // Une affiche par produit : couleur d'accent + glyphe propres, sur un fond
        // sakura commun -> chaque carte est reconnaissable au premier coup d'oeil
        // tout en restant visiblement de la meme famille.
        public static Image Spoofer => Get("spoofer", "SPOOFER", Glyph.Shield, Color.FromArgb(244, 114, 182));
        public static Image Valorant => Get("valorant", "VALORANT", Glyph.Crosshair, Color.FromArgb(255, 99, 104));
        public static Image Roblox => Get("roblox", "ROBLOX", Glyph.Blocks, Color.FromArgb(120, 170, 255));
        public static Image WindowsPai => Get("windowspai", "WINDOWS", Glyph.Window, Color.FromArgb(96, 214, 190));
        public static Image BotManager => Get("botmanager", "BOT MANAGER", Glyph.Bot, Color.FromArgb(186, 140, 255));

        private static Image Get(string key, string caption, Glyph glyph, Color accent)
        {
            if (Cache.TryGetValue(key, out var cached)) return cached;
            var bmp = Render(caption, glyph, accent);
            Cache[key] = bmp;
            return bmp;
        }

        private static Image Render(string caption, Glyph glyph, Color accent)
        {
            var bmp = new Bitmap(W, H);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                var rect = new Rectangle(0, 0, W, H);

                // Fond : prune profond -> teinte du produit. L'accent reste tres
                // desature ici pour que le glyphe garde le contraste.
                Color top = Blend(Colors.bgColor, accent, 0.28f);
                Color bottom = Color.FromArgb(20, 12, 20);
                using (var bg = new LinearGradientBrush(rect, top, bottom, LinearGradientMode.Vertical))
                    g.FillRectangle(bg, rect);

                DrawGlow(g, new Rectangle(W - 300, -180, 460, 420), accent, 110);
                DrawGlow(g, new Rectangle(-200, H - 260, 420, 380), accent, 60);
                DrawPetals(g, accent);

                // Glyphe centre, legerement remonte : le bas de l'affiche est
                // occupe par le voile + le nom.
                var glyphBox = new Rectangle((W - 230) / 2, 168, 230, 230);
                DrawGlyph(g, glyph, glyphBox, accent);

                // Voile bas : garantit que le nom reste lisible quelle que soit la
                // luminosite du degrade derriere.
                var scrim = new Rectangle(0, H - 240, W, 240);
                using (var veil = new LinearGradientBrush(scrim, Color.FromArgb(0, 0, 0, 0), Color.FromArgb(225, 10, 6, 12), LinearGradientMode.Vertical))
                    g.FillRectangle(veil, scrim);

                DrawCaption(g, caption, accent);

                // Liseré interieur : donne un bord net a la carte une fois recadree.
                using (var pen = new Pen(Color.FromArgb(60, accent), 2f))
                    g.DrawRectangle(pen, 1, 1, W - 3, H - 3);
            }
            return bmp;
        }

        private static void DrawCaption(Graphics g, string caption, Color accent)
        {
            // Barre d'accent + nom : compose comme une vraie affiche produit
            // plutot que comme une simple icone posee sur un fond.
            using (var bar = new SolidBrush(accent))
                g.FillRectangle(bar, 46, H - 150, 54, 4);

            using (var font = new Font("Inter Semibold", 30f, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var brush = new SolidBrush(Color.White))
                g.DrawString(caption, font, brush, 44, H - 132);

            using (var font = new Font("Inter Medium", 17f, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var brush = new SolidBrush(Color.FromArgb(150, 255, 255, 255)))
                g.DrawString("PaiPai", font, brush, 46, H - 88);
        }

        private static void DrawGlow(Graphics g, Rectangle box, Color color, int alpha)
        {
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(box);
                using (var pgb = new PathGradientBrush(path))
                {
                    pgb.CenterColor = Color.FromArgb(alpha, color);
                    pgb.SurroundColors = new[] { Color.FromArgb(0, color) };
                    g.FillPath(pgb, path);
                }
            }
        }

        // Motif de petales tres discret (alpha faible) : signature visuelle
        // commune a toutes les affiches, sans jamais concurrencer le glyphe.
        private static void DrawPetals(Graphics g, Color accent)
        {
            var rnd = new Random(7); // graine fixe -> affiche identique a chaque lancement
            using (var brush = new SolidBrush(Color.FromArgb(16, accent)))
            {
                for (int i = 0; i < 14; i++)
                {
                    int size = rnd.Next(26, 74);
                    int x = rnd.Next(-20, W - 20);
                    int y = rnd.Next(-20, H - 120);
                    var state = g.Save();
                    g.TranslateTransform(x + size / 2f, y + size / 2f);
                    g.RotateTransform(rnd.Next(0, 360));
                    g.FillEllipse(brush, -size / 2f, -size / 3f, size, size * 0.66f);
                    g.Restore(state);
                }
            }
        }

        private static void DrawGlyph(Graphics g, Glyph glyph, Rectangle b, Color accent)
        {
            // Halo doux derriere le glyphe -> il se detache du degrade sans avoir
            // besoin d'un aplat opaque.
            DrawGlow(g, Rectangle.Inflate(b, 70, 70), accent, 90);

            float thickness = b.Width * 0.055f;
            using (var pen = new Pen(Color.White, thickness)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round,
            })
            using (var fill = new SolidBrush(Color.FromArgb(235, 255, 255, 255)))
            {
                switch (glyph)
                {
                    case Glyph.Shield: DrawShield(g, b, pen); break;
                    case Glyph.Crosshair: DrawCrosshair(g, b, pen, fill); break;
                    case Glyph.Blocks: DrawBlocks(g, b, pen, fill); break;
                    case Glyph.Window: DrawWindow(g, b, pen); break;
                    case Glyph.Bot: DrawBot(g, b, pen, fill); break;
                }
            }
        }

        private static void DrawShield(Graphics g, Rectangle b, Pen pen)
        {
            using (var path = new GraphicsPath())
            {
                float cx = b.X + b.Width / 2f;
                path.AddLine(cx, b.Y, b.Right, b.Y + b.Height * 0.22f);
                path.AddBezier(b.Right, b.Y + b.Height * 0.22f,
                               b.Right, b.Y + b.Height * 0.72f,
                               cx + b.Width * 0.30f, b.Bottom - b.Height * 0.04f,
                               cx, b.Bottom);
                path.AddBezier(cx, b.Bottom,
                               cx - b.Width * 0.30f, b.Bottom - b.Height * 0.04f,
                               b.X, b.Y + b.Height * 0.72f,
                               b.X, b.Y + b.Height * 0.22f);
                path.CloseFigure();
                g.DrawPath(pen, path);
            }

            // Coche interieure : lit "protege" d'un coup d'oeil.
            using (var check = new Pen(pen.Color, pen.Width * 0.85f)
            { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
            {
                g.DrawLines(check, new[]
                {
                    new PointF(b.X + b.Width * 0.32f, b.Y + b.Height * 0.50f),
                    new PointF(b.X + b.Width * 0.45f, b.Y + b.Height * 0.63f),
                    new PointF(b.X + b.Width * 0.70f, b.Y + b.Height * 0.36f),
                });
            }
        }

        private static void DrawCrosshair(Graphics g, Rectangle b, Pen pen, Brush fill)
        {
            var inner = Rectangle.Inflate(b, -(int)(b.Width * 0.10f), -(int)(b.Height * 0.10f));
            g.DrawEllipse(pen, inner);

            float cx = b.X + b.Width / 2f, cy = b.Y + b.Height / 2f;
            float outer = b.Width * 0.56f, gap = b.Width * 0.30f;
            g.DrawLine(pen, cx, cy - outer, cx, cy - gap);
            g.DrawLine(pen, cx, cy + gap, cx, cy + outer);
            g.DrawLine(pen, cx - outer, cy, cx - gap, cy);
            g.DrawLine(pen, cx + gap, cy, cx + outer, cy);

            float dot = b.Width * 0.10f;
            g.FillEllipse(fill, cx - dot / 2f, cy - dot / 2f, dot, dot);
        }

        private static void DrawBlocks(Graphics g, Rectangle b, Pen pen, Brush fill)
        {
            float s = b.Width * 0.36f, r = b.Width * 0.07f;
            float x0 = b.X + b.Width * 0.10f, y0 = b.Y + b.Height * 0.10f;
            float step = s + b.Width * 0.08f;

            for (int row = 0; row < 2; row++)
            {
                for (int col = 0; col < 2; col++)
                {
                    var cell = new RectangleF(x0 + col * step, y0 + row * step, s, s);
                    using (var path = RoundedF(cell, r))
                    {
                        // Une seule tuile pleine : casse la symetrie et evite un
                        // motif qui ressemblerait a une icone de chargement.
                        if (row == 1 && col == 0) g.FillPath(fill, path);
                        else g.DrawPath(pen, path);
                    }
                }
            }
        }

        private static void DrawWindow(Graphics g, Rectangle b, Pen pen)
        {
            var frame = new RectangleF(b.X + b.Width * 0.08f, b.Y + b.Height * 0.14f,
                                       b.Width * 0.84f, b.Height * 0.72f);
            using (var path = RoundedF(frame, b.Width * 0.08f))
                g.DrawPath(pen, path);

            float midX = frame.X + frame.Width / 2f;
            float midY = frame.Y + frame.Height / 2f;
            g.DrawLine(pen, midX, frame.Y, midX, frame.Bottom);
            g.DrawLine(pen, frame.X, midY, frame.Right, midY);
        }

        private static void DrawBot(Graphics g, Rectangle b, Pen pen, Brush fill)
        {
            // Antenne
            float cx = b.X + b.Width / 2f;
            g.DrawLine(pen, cx, b.Y + b.Height * 0.02f, cx, b.Y + b.Height * 0.16f);
            float knob = b.Width * 0.10f;
            g.FillEllipse(fill, cx - knob / 2f, b.Y - knob * 0.15f, knob, knob);

            // Tete
            var head = new RectangleF(b.X + b.Width * 0.10f, b.Y + b.Height * 0.16f,
                                      b.Width * 0.80f, b.Height * 0.58f);
            using (var path = RoundedF(head, b.Width * 0.16f))
                g.DrawPath(pen, path);

            // Yeux
            float eye = b.Width * 0.11f;
            float eyeY = head.Y + head.Height * 0.36f;
            g.FillEllipse(fill, head.X + head.Width * 0.24f - eye / 2f, eyeY, eye, eye);
            g.FillEllipse(fill, head.X + head.Width * 0.76f - eye / 2f, eyeY, eye, eye);

            // Bouche
            g.DrawLine(pen, head.X + head.Width * 0.33f, head.Bottom - head.Height * 0.20f,
                            head.X + head.Width * 0.67f, head.Bottom - head.Height * 0.20f);

            // Socle
            g.DrawLine(pen, b.X + b.Width * 0.26f, b.Bottom - b.Height * 0.06f,
                            b.X + b.Width * 0.74f, b.Bottom - b.Height * 0.06f);
        }

        private static GraphicsPath RoundedF(RectangleF r, float radius)
        {
            float d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static Color Blend(Color a, Color b, float t)
        {
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }
    }
}
