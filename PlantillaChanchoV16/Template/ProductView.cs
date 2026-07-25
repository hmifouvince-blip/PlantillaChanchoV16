using Guna.UI2.WinForms;
using PlantillaChanchoV16.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CuoreUI;
using CuoreUI.Controls;
using System.Diagnostics.Eventing.Reader;

namespace PlantillaChanchoV16.Template
{
    internal class ProductView : Guna2Button
    {
        Images images = new Images();

        public ProductView(bool productActive, bool productUnderMaintenance, string productName, string updateProduct, Image productLogo, Image productImage, Action openDetailsProduct)
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);

            FillColor = Color.Transparent;
            UseTransparentBackground = true;
            Width = widthProduct;
            Height = heightProduct;
            BackColor = Color.Transparent;
            BorderRadius = 10;
            BorderThickness = 9;
            // Recadrage "cover" centré : l'image remplit la carte sans déformation.
            Image = CropToAspect(productImage, Width, Height);
            ImageSize = new Size(Width + 0, Height + 0);
            ImageOffset = new Point(0, 0);
            BorderColor = Colors.bgColor;


            RoundGunaButtonCorners(this, 10);

            CreateItemsProduct(productActive, productUnderMaintenance, productName, updateProduct, productLogo, productImage, openDetailsProduct);

            AttachHoverEffect();





        }

        // ---- Interactions modernes : survol = soulèvement + halo rose ; clic = pression ----
        private readonly Color _idleBorder = Colors.bgColor;
        // Bouton flèche discret (ghost) au repos, rose au survol -> "se fond dans le décor".
        private readonly Color _arrowIdle = Color.FromArgb(55, 255, 255, 255);
        private bool _hovered = false;
        private bool _pressed = false;
        private bool _baseCaptured = false;
        private int _baseTop;
        private System.Windows.Forms.Timer _liftTimer;
        // Intensité du survol (0 = repos, 1 = survolé), animée en fondu -> le halo rose monte
        // et descend en douceur au lieu d'apparaître d'un coup (rendu premium).
        private float _hoverGlow = 0f;
        private float _glowTarget = 0f;

        private void AttachHoverEffect()
        {
            _liftTimer = new System.Windows.Forms.Timer { Interval = 15 };
            _liftTimer.Tick += (s, e) =>
            {
                float diff = _glowTarget - _hoverGlow;
                if (Math.Abs(diff) <= 0.04f) { _hoverGlow = _glowTarget; _liftTimer.Stop(); }
                else _hoverGlow += diff * 0.25f;
                Invalidate();
            };

            AttachHoverRecursive(this);
        }

        private static Color LerpColor(Color a, Color b, float t)
        {
            if (t < 0) t = 0; else if (t > 1) t = 1;
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * t),
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        private void AttachHoverRecursive(Control control)
        {
            control.MouseEnter += (s, e) => SetHover(true);
            control.MouseLeave += (s, e) =>
            {
                // On ne retire le survol que si le curseur a réellement quitté la carte
                // (les enfants déclenchent MouseLeave quand on passe de l'un à l'autre).
                Point cursor = PointToClient(Cursor.Position);
                if (!ClientRectangle.Contains(cursor)) SetHover(false);
            };
            control.MouseDown += (s, e) => SetPressed(true);
            control.MouseUp += (s, e) => SetPressed(false);

            // Toute la carte est cliquable pour ouvrir le produit (sauf le bouton flèche,
            // qui déclenche déjà l'ouverture -> évite un double-déclenchement).
            if (control != btnStartProduct)
                control.Click += (s, e) => { if (_openable) openDetailsProduct?.Invoke(); };

            foreach (Control child in control.Controls)
                AttachHoverRecursive(child);
        }

        private void SetHover(bool hovered)
        {
            if (!_baseCaptured) { _baseTop = Top; _baseCaptured = true; }
            if (_hovered == hovered) return;
            _hovered = hovered;
            if (btnStartProduct != null)
                btnStartProduct.FillColor = hovered ? Colors.mainColor : _arrowIdle;
            // Fondu du halo (la couleur de bordure/accent est calculée dans Paint à partir de
            // _hoverGlow). Pas de déplacement de la carte : ça faisait trembler (le curseur
            // sortait de la carte déplacée -> leave/enter en boucle).
            _glowTarget = hovered ? 1f : 0f;
            _liftTimer?.Start();
        }

        private void SetPressed(bool pressed)
        {
            _pressed = pressed;
        }

        // Animation d'apparition : la carte glisse doucement vers le haut jusqu'à sa place.
        private System.Windows.Forms.Timer _entranceTimer;
        public void PlayEntrance(int delayMs)
        {
            int finalTop = Top;
            _baseTop = finalTop; _baseCaptured = true; // évite une mauvaise capture pendant l'anim
            Top = finalTop + 26;

            var delay = new System.Windows.Forms.Timer { Interval = Math.Max(1, delayMs) };
            delay.Tick += (s, e) =>
            {
                delay.Stop(); delay.Dispose();
                _entranceTimer?.Stop();
                _entranceTimer = new System.Windows.Forms.Timer { Interval = 15 };
                _entranceTimer.Tick += (s2, e2) =>
                {
                    int diff = finalTop - Top;
                    if (Math.Abs(diff) <= 1) { Top = finalTop; _entranceTimer.Stop(); }
                    else Top += (int)(diff / 2.5);
                };
                _entranceTimer.Start();
            };
            delay.Start();
        }

        // Libère les timers d'animation (survol + apparition) sinon ils fuient à chaque
        // reconstruction de la fenêtre (changement de thème / langue).
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _liftTimer?.Stop(); _liftTimer?.Dispose();
                _entranceTimer?.Stop(); _entranceTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;
                return cp;
            }
        }


        // Recadre l'image en "cover" centré vers la taille cible (aucune déformation).
        private static Image CropToAspect(Image src, int w, int h)
        {
            if (src == null || w <= 0 || h <= 0) return src;
            try
            {
                float targetAspect = (float)w / h;
                float srcAspect = (float)src.Width / src.Height;
                Rectangle crop;
                if (srcAspect > targetAspect)
                {
                    int cw = (int)(src.Height * targetAspect);
                    int cx = (src.Width - cw) / 2;
                    crop = new Rectangle(cx, 0, cw, src.Height);
                }
                else
                {
                    int ch = (int)(src.Width / targetAspect);
                    int cy = (src.Height - ch) / 2;
                    crop = new Rectangle(0, cy, src.Width, ch);
                }

                var bmp = new Bitmap(w, h);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.DrawImage(src, new Rectangle(0, 0, w, h), crop, GraphicsUnit.Pixel);
                }
                return bmp;
            }
            catch { return src; }
        }

        public void RoundGunaButtonCorners(Guna2Button button, int cornerRadius)
        {
            Rectangle buttonRect = new Rectangle(0, 0, button.Width, button.Height);

            GraphicsPath path = new GraphicsPath();
            path.StartFigure();
            path.AddArc(buttonRect.X, buttonRect.Y, cornerRadius * 2, cornerRadius * 2, 180, 90);
            path.AddArc(buttonRect.Right - cornerRadius * 2, buttonRect.Y, cornerRadius * 2, cornerRadius * 2, 270, 90);
            path.AddArc(buttonRect.Right - cornerRadius * 2, buttonRect.Bottom - cornerRadius * 2, cornerRadius * 2, cornerRadius * 2, 0, 90);
            path.AddArc(buttonRect.X, buttonRect.Bottom - cornerRadius * 2, cornerRadius * 2, cornerRadius * 2, 90, 90);
            path.CloseFigure();

            button.Region = new Region(path);

            button.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.HighQuality;

                // Plus d'allocation inutile par frame : le fond est transparent (l'image est
                // peinte par le Guna2Button, découpée par la Region) -> on saute le remplissage.
                if (button.FillColor.A > 0)
                    using (var br = new SolidBrush(button.FillColor)) g.FillPath(br, path);

                // Bordure : fond -> rose sakura selon l'intensité de survol (_hoverGlow, en
                // fondu animé) au lieu d'un basculement instantané = rendu premium.
                Color bc = LerpColor(_idleBorder, Colors.mainColor, _hoverGlow);
                if (button.BorderThickness > 0)
                    using (var pen = new Pen(bc, button.BorderThickness)) g.DrawPath(pen, path);

                var oldClip = g.Clip;
                g.SetClip(path);

                // Fin liseré d'accent en haut (façon gaming hub) : discret au repos, vif au survol.
                int topAlpha = (int)(70 + 150 * _hoverGlow);
                var top = new Rectangle(0, 0, buttonRect.Width, 3);
                using (var lg = new LinearGradientBrush(top,
                        Color.FromArgb(0, Colors.mainColor),
                        Color.FromArgb(topAlpha, Colors.mainColor),
                        LinearGradientMode.Horizontal))
                {
                    lg.SetSigmaBellShape(0.5f);
                    g.FillRectangle(lg, top);
                }

                // Au survol : léger halo rose qui remonte du bas de la carte (lumière douce).
                if (_hoverGlow > 0.01f && buttonRect.Height > 70)
                {
                    int gh = 70;
                    var glowRect = new Rectangle(0, buttonRect.Height - gh, buttonRect.Width, gh);
                    using (var gg = new LinearGradientBrush(glowRect,
                            Color.FromArgb(0, Colors.mainColor),
                            Color.FromArgb((int)(55 * _hoverGlow), Colors.mainColor),
                            LinearGradientMode.Vertical))
                        g.FillRectangle(gg, glowRect);
                }

                g.Clip = oldClip;
            };
        }

        Guna2Panel containerInfoProduct;
        Guna2PictureBox logoProduct;
        Label nameProduct;
        Label lastUpdateProduct;
        Guna2Button btnStartProduct;
        Guna2Button favoriteButton;
        Action openDetailsProduct;
        Action addProductFavorite;

        static int sizeIncrement = 0;

        static int widthProduct = Default.widthProduct + sizeIncrement;
        static int heightProduct = Default.heightProduct + sizeIncrement * 1;



        private bool _openable = true;

        private void CreateItemsProduct(bool productActive, bool productUnderMaintenance, string productName, string updateProduct, Image productLogo, Image productImage, Action openDetailsProduct)
        {
            this.openDetailsProduct = openDetailsProduct;
            // Un produit inactif ou en maintenance ne s'ouvre pas au clic sur la carte.
            _openable = productActive && !productUnderMaintenance;

            BlurPanelFull panel = new BlurPanelFull
            {
                BackColor = Color.Transparent,
                FillColor = Color.FromArgb(200, 0, 0, 0),
                UseTransparentBackground = true,
                Size = new Size(Width - 4, Height - 3),
                BorderRadius = 6,
                BorderColor = Color.White,
                BorderThickness = 0,

                BlurAmount = 6,
                CornerRadius = 7,
                BlurColor = Color.FromArgb(148, 0, 0, 0),
            };

            panel.Location = new Point(3, 3);


            Guna2PictureBox containerIconComingSoon = new Guna2PictureBox
            {
                Image = Utils.ChangeIconsColor(new Bitmap(images.CsIcon), Color.White),
                SizeMode = PictureBoxSizeMode.Zoom,
                UseTransparentBackground = true,
                Size = new Size(27, 27)
            };



            Guna2HtmlLabel textDisable = new Guna2HtmlLabel
            {
                Text = "Coming Soon...",
                ForeColor = Color.White,
                Font = new Font("Inter Medium", 12f, FontStyle.Regular),
            };

            int totalHeight = containerIconComingSoon.Height + textDisable.Height + 10;
            int verticalCenter = (panel.Height - totalHeight) / 2;

            textDisable.Location = new Point((panel.Width - textDisable.Width) / 2, verticalCenter + containerIconComingSoon.Height + 5);
            containerIconComingSoon.Location = new Point((panel.Width - containerIconComingSoon.Width) / 2, verticalCenter);

            if (productActive == false)
            {
                Controls.Add(panel);
                panel.Controls.Add(textDisable);
                panel.Controls.Add(containerIconComingSoon);
            }






            Guna2PictureBox containerIconMC = new Guna2PictureBox
            {
                Image = Utils.ChangeIconsColor(new Bitmap(images.UmIcon), Color.Yellow),
                SizeMode = PictureBoxSizeMode.Zoom,
                UseTransparentBackground = true,
                Size = new Size(27, 27)
            };



            Guna2HtmlLabel textUnderMC = new Guna2HtmlLabel
            {
                Text = "Under maintenance.",
                ForeColor = Color.White,
                Font = new Font("Inter Medium", 12f, FontStyle.Regular),
            };

            int totalHeightMC = containerIconMC.Height + textUnderMC.Height + 10;
            int verticalCenterMC = (panel.Height - totalHeight) / 2;

            textUnderMC.Location = new Point((panel.Width - textUnderMC.Width) / 2, verticalCenterMC + containerIconMC.Height + 5);
            containerIconMC.Location = new Point((panel.Width - containerIconMC.Width) / 2, verticalCenterMC);


            if (productUnderMaintenance == true)
            {
                Controls.Add(panel);
                panel.Controls.Add(textUnderMC);
                panel.Controls.Add(containerIconMC);
            };







            containerInfoProduct = new BlurPanel
            {
                Height = 55,
                Width = Width - 3,
                FillColor = Color.Transparent,
                UseTransparentBackground = true,
                BlurAmount = 10,
                BackColor = Color.Transparent,
                BottomRadius = 9,
                BlurColor = Color.FromArgb(138, 0, 0, 0),
            };

            //containerInfoProduct = new Guna2Panel
            //{
            //    Height = 55,
            //    Width = Width - 3,
            //    FillColor = Color.FromArgb(198, 0, 0, 0),
            //    UseTransparentBackground = true,
            //    BackColor = Color.Transparent,
            //};

            //containerInfoProduct = new BlurPanel
            //{
            //    Height = 55,
            //    Width = Width - 3,
            //    FillColor = Color.Transparent,
            //    UseTransparentBackground = true,
            //    BlurAmount = 10,
            //    BackColor = Color.Transparent,
            //    BottomRadius = 13,
            //    BlurColor = Color.FromArgb(138, 0, 0, 0),
            //};


            containerInfoProduct.Location = new Point(3, Bottom - containerInfoProduct.Height);

            logoProduct = new Guna2PictureBox
            {
                Image = productLogo,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Size = new Size(34, 34),
                UseTransparentBackground = true,
                BorderRadius = 0,
            };
            logoProduct.Location = new Point(14, (containerInfoProduct.Height - logoProduct.Height) / 2);

            nameProduct = new Label
            {
                ForeColor = Color.White,
                Text = productName,
                Font = new Font("Inter Semibold", 11.4f, FontStyle.Regular),
                AutoSize = false,
                AutoEllipsis = true,
                Width = 126,
                //BorderStyle = BorderStyle.None,
            };

            lastUpdateProduct = new Label
            {
                ForeColor = Color.White,
                Text = updateProduct,
                Font = new Font("Inter Semibold", 8.4f, FontStyle.Regular),
                AutoSize = false,
                AutoEllipsis = true,
                //BorderStyle= BorderStyle.None,
                Width = 126
            };

            nameProduct.Location = new Point(logoProduct.Right + 2, logoProduct.Top + (logoProduct.Bottom - nameProduct.Height - lastUpdateProduct.Height - 3) / 2);

            lastUpdateProduct.Location = new Point(nameProduct.Left, nameProduct.Bottom - 2);



            btnStartProduct = new Guna2Button
            {
                FillColor = _arrowIdle,   // discret par défaut : se fond dans la carte
                Image = Utils.ChangeIconsColor(new Bitmap(images.IconOpenDetails), Color.FromArgb(210, 255, 255, 255)),
                Size = new Size(36, 36),
                ImageSize = new Size(14, 14),
                ImageAlign = HorizontalAlignment.Center,
                ImageOffset = new Point(0, 0),
                BorderRadius = 9,
                BorderColor = _arrowIdle,
                BorderThickness = 0,
                BackColor = Color.Transparent,
                UseTransparentBackground = true,
                Animated = true,
            };
            btnStartProduct.HoverState.FillColor = Colors.mainColor;
            btnStartProduct.Location = new Point(containerInfoProduct.Right - btnStartProduct.Width - 14, (containerInfoProduct.Height - btnStartProduct.Height) / 2);


            btnStartProduct.Click += (s, e) =>
            {

                openDetailsProduct.Invoke();
            };

            containerInfoProduct.Controls.Add(logoProduct);
            containerInfoProduct.Controls.Add(nameProduct);
            containerInfoProduct.Controls.Add(lastUpdateProduct);
            containerInfoProduct.Controls.Add(btnStartProduct);






            Controls.Add(containerInfoProduct);

        }
    }
}
