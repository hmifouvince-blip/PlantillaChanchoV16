using Guna.UI2.WinForms;
using PlantillaChanchoV16.Utilities;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PlantillaChanchoV16.Template
{
    // Icônes vectorielles dessinées (même esprit que CategoryIcon dans
    // WindowsPaiScreen.cs) : badge circulaire translucide + glyphe monochrome
    // rose. Remplace les emojis (rendu incohérent avec le thème sakura, coloré
    // et non maîtrisé selon la police système) utilisés dans la 1re version du
    // Bot Manager.
    internal class BotIcon : Control
    {
        public enum Kind { Bot, Announcement, Update, Status, Tickets, Eye, EyeOff, Folder }

        public Kind IconKind { get; set; }

        public BotIcon(Kind kind)
        {
            IconKind = kind;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            DoubleBuffered = true;
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rc = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var bg = new SolidBrush(Color.FromArgb(45, Colors.mainColor)))
                g.FillEllipse(bg, rc);

            float cx = Width / 2f, cy = Height / 2f;
            using (var pen = new Pen(Colors.mainColor, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
            using (var br = new SolidBrush(Colors.mainColor))
            {
                switch (IconKind)
                {
                    case Kind.Bot:
                        g.DrawLine(pen, cx, cy - 12, cx, cy - 8);
                        g.FillEllipse(br, cx - 2, cy - 14, 4, 4);
                        using (var head = Rounded(new Rectangle((int)(cx - 9), (int)(cy - 6), 18, 14), 5))
                            g.DrawPath(pen, head);
                        g.FillEllipse(br, cx - 5, cy - 1, 3, 3);
                        g.FillEllipse(br, cx + 2, cy - 1, 3, 3);
                        break;

                    case Kind.Announcement:
                        using (var body = new GraphicsPath())
                        {
                            body.AddLine(cx - 10, cy - 2, cx - 1, cy - 7);
                            body.AddLine(cx - 1, cy - 7, cx - 1, cy + 7);
                            body.AddLine(cx - 1, cy + 7, cx - 10, cy + 2);
                            body.CloseFigure();
                            g.FillPath(br, body);
                        }
                        g.FillRectangle(br, cx - 12, cy - 2, 3, 4);
                        g.DrawArc(pen, cx + 1, cy - 8, 11, 16, -55, 110);
                        break;

                    case Kind.Update:
                        g.DrawLine(pen, cx, cy + 8, cx, cy - 6);
                        g.DrawLine(pen, cx - 6, cy, cx, cy - 6);
                        g.DrawLine(pen, cx + 6, cy, cx, cy - 6);
                        break;

                    case Kind.Status:
                        g.FillRectangle(br, cx - 9, cy + 1, 5, 8);
                        g.FillRectangle(br, cx - 2, cy - 5, 5, 14);
                        g.FillRectangle(br, cx + 5, cy - 9, 5, 18);
                        break;

                    case Kind.Tickets:
                        using (var outline = Rounded(new Rectangle((int)(cx - 11), (int)(cy - 7), 22, 14), 4))
                            g.DrawPath(pen, outline);
                        using (var dashed = new Pen(Colors.mainColor, 1.5f) { DashStyle = DashStyle.Dash })
                            g.DrawLine(dashed, cx, cy - 5, cx, cy + 5);
                        break;

                    case Kind.Eye:
                        g.DrawEllipse(pen, cx - 9, cy - 5, 18, 10);
                        g.FillEllipse(br, cx - 2, cy - 2, 4, 4);
                        break;

                    case Kind.EyeOff:
                        g.DrawEllipse(pen, cx - 9, cy - 5, 18, 10);
                        g.FillEllipse(br, cx - 2, cy - 2, 4, 4);
                        g.DrawLine(pen, cx - 10, cy + 7, cx + 10, cy - 7);
                        break;

                    case Kind.Folder:
                        using (var path = new GraphicsPath())
                        {
                            path.AddLine(cx - 10, cy - 3, cx - 3, cy - 3);
                            path.AddLine(cx - 3, cy - 3, cx - 1, cy - 1);
                            path.AddLine(cx - 1, cy - 1, cx + 10, cy - 1);
                            path.AddLine(cx + 10, cy - 1, cx + 10, cy + 7);
                            path.AddLine(cx + 10, cy + 7, cx - 10, cy + 7);
                            path.CloseFigure();
                            g.DrawPath(pen, path);
                        }
                        break;
                }
            }
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

    // Petits réglages Guna2 partagés qui n'ont pas de valeur par défaut cohérente
    // avec le thème sakura (1re utilisation de Guna2ComboBox dans ce projet ->
    // sans ceci, la bordure "focus" reste bleue et la liste déroulante reste
    // blanche/noire par défaut = casse complètement le thème sombre).
    internal static class GunaTheme
    {
        public static void StyleCombo(Guna2ComboBox combo)
        {
            combo.FocusedColor = Colors.mainColor;
            combo.FillColor = Colors.scColor;
            combo.BorderColor = Colors.scColor;
            combo.ForeColor = Color.White;
            combo.ItemsAppearance.BackColor = Colors.scColor;
            combo.ItemsAppearance.ForeColor = Color.White;
            combo.ItemsAppearance.SelectedBackColor = Colors.mainColor;
            combo.ItemsAppearance.SelectedForeColor = Color.White;
            combo.ItemsAppearance.Font = combo.Font;
        }
    }
}
