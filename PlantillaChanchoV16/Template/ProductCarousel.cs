using Guna.UI2.WinForms;
using PlantillaChanchoV16.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace PlantillaChanchoV16.Template
{
    // Carrousel horizontal "présentation rapide" façon sekai.one : cartes-affiches (image
    // + légende en bas) qui glissent d'une carte à la fois via des flèches, sous un titre
    // de section. Utilisé sur l'accueil pour présenter les produits en un coup d'œil.
    internal class ProductCarousel : Guna2Panel
    {
        // Non-readonly : FitTo() peut les réduire OU les agrandir (au-delà de la taille de
        // base) selon l'espace réellement disponible (largeur + hauteur), mesuré à chaque
        // affichage/redimensionnement -> jamais de débordement, petites en fenêtre réduite
        // et GRANDES en plein écran.
        private int _cardW, _cardH, _gap;
        private readonly int _baseCardW, _baseCardH, _baseGap;
        private readonly int _visibleCount, _titleAreaHeight;
        private readonly Guna2HtmlLabel _titleLabel;
        private readonly Guna2Panel _viewport;
        private readonly Guna2Panel _track;
        private readonly Guna2CircleButton _btnPrev, _btnNext;
        private readonly List<Guna2Button> _cards = new List<Guna2Button>();
        // (poster source, légende, action) pour pouvoir reconstruire les cartes si FitTo
        // change leur taille après coup.
        private readonly List<(Image poster, string caption, Action onClick)> _cardData = new List<(Image, string, Action)>();

        private int _index = 0;
        private int _targetX = 0;
        private Timer _slideTimer;

        // Survol animé (glow rose en fondu) : une seule carte survolée à la fois -> un seul
        // timer + une seule valeur d'intensité, associés à la carte courante.
        private Guna2Button _glowCard;
        private float _cardGlow = 0f, _cardGlowTarget = 0f;
        private Timer _glowTimer;

        public ProductCarousel(int width, int titleAreaHeight, int cardW, int cardH, int gap,
            int visibleCount, string title, Image arrowLeft, Image arrowRight)
        {
            _cardW = cardW; _cardH = cardH; _gap = gap; _visibleCount = visibleCount;
            _baseCardW = cardW; _baseCardH = cardH; _baseGap = gap;
            _titleAreaHeight = titleAreaHeight;

            Width = width;
            Height = titleAreaHeight + cardH;
            FillColor = Color.Transparent;
            BackColor = Color.Transparent;
            BorderThickness = 0;
            EnableDoubleBuffer(this);

            _titleLabel = new Guna2HtmlLabel
            {
                Parent = this,
                Text = title,
                Font = new Font("Inter Semibold", 13f),
                ForeColor = Color.White,
                AutoSize = true,
                BackColor = Color.Transparent,
            };
            _titleLabel.Location = new Point(Math.Max(0, (Width - _titleLabel.Width) / 2), 0);

            int viewportW = visibleCount * cardW + (visibleCount - 1) * gap;
            _viewport = new Guna2Panel
            {
                Parent = this,
                FillColor = Color.Transparent,
                BackColor = Color.Transparent,
                BorderThickness = 0,
                Size = new Size(Math.Min(viewportW, width), cardH),
            };
            _viewport.Location = new Point(Math.Max(0, (Width - _viewport.Width) / 2), titleAreaHeight);
            EnableDoubleBuffer(_viewport);

            _track = new Guna2Panel
            {
                Parent = _viewport,
                FillColor = Color.Transparent,
                BackColor = Color.Transparent,
                BorderThickness = 0,
                Location = new Point(0, 0),
                Height = cardH,
                Width = cardW,
            };
            EnableDoubleBuffer(_track);

            int arrowSize = 34;
            _btnPrev = new Guna2CircleButton
            {
                Parent = this,
                Size = new Size(arrowSize, arrowSize),
                Image = arrowLeft,
                ImageSize = new Size(18, 18),
                FillColor = Color.FromArgb(50, 255, 255, 255),
                BorderColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Animated = true,
                UseTransparentBackground = true,
                Visible = false,
            };
            _btnPrev.HoverState.FillColor = Color.FromArgb(120, Colors.mainColor);
            _btnPrev.Location = new Point(Math.Max(0, _viewport.Left - arrowSize - 8), titleAreaHeight + (cardH - arrowSize) / 2);
            _btnPrev.Click += (s, e) => Slide(-1);
            _btnPrev.BringToFront();

            _btnNext = new Guna2CircleButton
            {
                Parent = this,
                Size = new Size(arrowSize, arrowSize),
                Image = arrowRight,
                ImageSize = new Size(18, 18),
                FillColor = Color.FromArgb(50, 255, 255, 255),
                BorderColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Animated = true,
                UseTransparentBackground = true,
                Visible = false,
            };
            _btnNext.HoverState.FillColor = Color.FromArgb(120, Colors.mainColor);
            _btnNext.Location = new Point(Math.Min(Width - arrowSize, _viewport.Right + 8), titleAreaHeight + (cardH - arrowSize) / 2);
            _btnNext.Click += (s, e) => Slide(1);
            _btnNext.BringToFront();
        }

        // Ajoute une carte-affiche (image recadrée "cover" + légende en bas) qui ouvre
        // le produit au clic (le verrou de licence est géré par onClick, comme la grille).
        public void AddCard(Image poster, string caption, Action onClick)
        {
            _cardData.Add((poster, caption, onClick));
            BuildCardControl(poster, caption, onClick, _cards.Count);
            RefreshTrackAndArrows();
        }

        // Ajuste la taille des cartes à l'espace RÉELLEMENT disponible (largeur ET hauteur,
        // mesurées à l'affichage/redimensionnement -> fiable quel que soit le facteur DPI),
        // en conservant le ratio de base des affiches. RÉVERSIBLE et bidirectionnel : les
        // cartes RÉTRÉCISSENT dans une petite fenêtre ET GRANDISSENT en plein écran (images
        // beaucoup plus grandes, ce qui remplit l'espace au lieu de rester minuscules).
        // Toujours recalculé depuis la taille de base (pas d'effet cumulatif).
        public void FitTo(int availableWidth, int availableHeight)
        {
            int availH = Math.Max(90, availableHeight - _titleAreaHeight);

            // Échelle permise par la hauteur (garde le ratio de base).
            double scaleH = availH / (double)_baseCardH;
            // Échelle permise par la largeur : tout (cartes ET gaps) est mis à l'échelle
            // uniformément, donc viewport = scale * (visibleCount*baseCardW + gaps) ; on veut
            // qu'il remplisse ~94 % de la largeur disponible.
            double usableW = availableWidth * 0.94;
            double baseRowW = _visibleCount * (double)_baseCardW + (_visibleCount - 1) * _baseGap;
            double scaleW = usableW / baseRowW;

            // On prend la plus contraignante des deux -> jamais de débordement. Bornes :
            // pas plus petit que 0.55 (fenêtre très réduite), pas plus grand que 2.8x
            // (plein écran : grandes affiches, mais sans devenir démesuré).
            double scale = Math.Min(scaleH, scaleW);
            scale = Math.Max(0.55, Math.Min(2.8, scale));

            int newCardW = Math.Max(70, (int)Math.Round(_baseCardW * scale));
            int newCardH = Math.Max(80, (int)Math.Round(_baseCardH * scale));
            int newGap = Math.Max(6, (int)Math.Round(_baseGap * scale));

            if (newCardW == _cardW && newCardH == _cardH && newGap == _gap) return; // déjà à la bonne taille

            _cardW = newCardW;
            _cardH = newCardH;
            _gap = newGap;

            RebuildLayout();
        }

        // Active le double-buffering sur un contrôle (propriété protégée) pour un défilement
        // du carrousel sans scintillement.
        private static void EnableDoubleBuffer(Control c)
        {
            typeof(Control)
                .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(c, true, null);
        }

        private void RebuildLayout()
        {
            Height = _titleAreaHeight + _cardH;

            // Les cartes vont être détruites -> on coupe toute référence de survol en cours
            // (sinon le timer de glow invaliderait une carte disposée).
            _glowTimer?.Stop();
            _glowCard = null;
            _cardGlow = 0f; _cardGlowTarget = 0f;

            foreach (var c in _cards) { _track.Controls.Remove(c); c.Dispose(); }
            _cards.Clear();
            for (int i = 0; i < _cardData.Count; i++)
                BuildCardControl(_cardData[i].poster, _cardData[i].caption, _cardData[i].onClick, i);

            _track.Height = _cardH;

            _index = 0;
            _targetX = 0;
            _track.Left = 0;

            RepositionForWidth();
            RefreshTrackAndArrows();
        }

        private void RepositionForWidth()
        {
            _titleLabel.Location = new Point(Math.Max(0, (Width - _titleLabel.Width) / 2), 0);

            int viewportW = Math.Min(_visibleCount * _cardW + (_visibleCount - 1) * _gap, Width);
            _viewport.Size = new Size(viewportW, _cardH);
            _viewport.Location = new Point(Math.Max(0, (Width - viewportW) / 2), _titleAreaHeight);

            int arrowSize = _btnPrev.Width;
            _btnPrev.Location = new Point(Math.Max(0, _viewport.Left - arrowSize - 8), _titleAreaHeight + (_cardH - arrowSize) / 2);
            _btnNext.Location = new Point(Math.Min(Width - arrowSize, _viewport.Right + 8), _titleAreaHeight + (_cardH - arrowSize) / 2);
        }

        private void BuildCardControl(Image poster, string caption, Action onClick, int index)
        {
            int x = index * (_cardW + _gap);
            int captionH = Math.Max(22, Math.Min(32, _cardH / 5));

            var card = new Guna2Button
            {
                Parent = _track,
                Location = new Point(x, 0),
                Width = _cardW,
                Height = _cardH,
                FillColor = Color.Transparent,
                UseTransparentBackground = true,
                BorderRadius = 14,
                BorderThickness = 0,
                BorderColor = Color.Transparent,
                Image = CropToAspect(poster, _cardW, _cardH),
                ImageSize = new Size(_cardW, _cardH),
                Cursor = Cursors.Hand,
                Animated = true,
            };
            EnableDoubleBuffer(card);
            RoundCorners(card, 14);

            // Bandeau légende en bas (même technique que les cartes de la grille).
            var captionBar = new Guna2Panel
            {
                Parent = card,
                Size = new Size(_cardW, captionH),
                Location = new Point(0, _cardH - captionH),
                FillColor = Color.FromArgb(215, 0, 0, 0),
                BorderThickness = 0,
                UseTransparentBackground = true,
            };
            var lbl = new Label
            {
                Parent = captionBar,
                Text = caption,
                ForeColor = Color.White,
                Font = new Font("Inter Semibold", 9.3f),
                AutoSize = false,
                Size = new Size(_cardW - 12, captionH),
                Location = new Point(6, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
            };

            EnsureGlowTimer();
            card.MouseEnter += (s, e) =>
            {
                var prev = _glowCard;
                _glowCard = card;
                _cardGlow = 0f; _cardGlowTarget = 1f;
                prev?.Invalidate();   // l'ancienne carte se repeint éteinte
                _glowTimer.Start();
            };
            card.MouseLeave += (s, e) =>
            {
                if (_glowCard == card) { _cardGlowTarget = 0f; _glowTimer.Start(); }
            };
            card.Click += (s, e) => onClick?.Invoke();
            // Le clic sur la légende/le label doit aussi ouvrir le produit.
            captionBar.Click += (s, e) => onClick?.Invoke();
            lbl.Click += (s, e) => onClick?.Invoke();

            _cards.Add(card);
        }

        private void RefreshTrackAndArrows()
        {
            _track.Width = _cards.Count * _cardW + Math.Max(0, _cards.Count - 1) * _gap;

            bool needsArrows = _cards.Count > _visibleCount;
            _btnPrev.Visible = needsArrows && _index > 0;
            _btnNext.Visible = needsArrows;
        }

        private void Slide(int dir)
        {
            int maxIndex = Math.Max(0, _cards.Count - _visibleCount);
            _index = Math.Max(0, Math.Min(maxIndex, _index + dir));
            _targetX = -_index * (_cardW + _gap);

            _btnPrev.Visible = _index > 0;
            _btnNext.Visible = _index < maxIndex;

            if (_slideTimer == null)
            {
                _slideTimer = new Timer { Interval = 12 };
                _slideTimer.Tick += (s, e) =>
                {
                    int cur = _track.Left;
                    int diff = _targetX - cur;
                    if (Math.Abs(diff) <= 1) { _track.Left = _targetX; _slideTimer.Stop(); return; }
                    _track.Left = cur + (int)Math.Round(diff / 3.0);
                };
            }
            _slideTimer.Start();
        }

        private void EnsureGlowTimer()
        {
            if (_glowTimer != null) return;
            _glowTimer = new Timer { Interval = 15 };
            _glowTimer.Tick += (s, e) =>
            {
                float diff = _cardGlowTarget - _cardGlow;
                if (Math.Abs(diff) <= 0.05f) { _cardGlow = _cardGlowTarget; _glowTimer.Stop(); }
                else _cardGlow += diff * 0.28f;
                _glowCard?.Invalidate();
            };
        }

        private static Color LerpColor(Color a, Color b, float t)
        {
            if (t < 0) t = 0; else if (t > 1) t = 1;
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * t), (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
        }

        private void RoundCorners(Guna2Button button, int radius)
        {
            var rect = new Rectangle(0, 0, button.Width, button.Height);
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            button.Region = new Region(path);

            // Fondu du halo rose au survol (l'image est peinte par le Guna2Button, découpée
            // par la Region). Aucune allocation quand la carte n'est pas la carte survolée
            // (glow == 0) -> glissement fluide.
            button.Paint += (s, e) =>
            {
                float glow = (button == _glowCard) ? _cardGlow : 0f;
                if (glow <= 0.01f) return;

                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.HighQuality;

                // Halo qui remonte du bas (lumière douce).
                if (button.Height > 60)
                {
                    var glowRect = new Rectangle(0, button.Height - 60, button.Width, 60);
                    using (var gg = new LinearGradientBrush(glowRect,
                            Color.FromArgb(0, Colors.mainColor),
                            Color.FromArgb((int)(60 * glow), Colors.mainColor),
                            LinearGradientMode.Vertical))
                        g.FillRectangle(gg, glowRect);
                }

                // Bordure rose en fondu.
                using (var pen = new Pen(LerpColor(Color.FromArgb(0, Colors.mainColor), Colors.mainColor, glow), 2f))
                    g.DrawPath(pen, path);
            };
        }

        // Recadrage "cover" centré (aucune déformation), même logique que ProductView.
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _slideTimer?.Stop(); _slideTimer?.Dispose();
                _glowTimer?.Stop(); _glowTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
