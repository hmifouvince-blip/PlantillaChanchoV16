using Guna.UI2.WinForms;
using PlantillaChanchoV16.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace PlantillaChanchoV16.Template
{
    // Fiche produit — remplace l'ancien DetailsProduct.
    //
    // Calquee sur WindowsPaiScreen, le seul ecran de l'app dont le rendu n'a
    // jamais pose probleme, et pour trois raisons precises reprises telles quelles :
    //
    // 1. LE FORMULAIRE PEINT SON PROPRE FOND (ControlStyles.UserPaint + OnPaint).
    //    Les Label a BackColor=Transparent se composent alors correctement.
    //    Poses sur un Guna2Panel "transparent", ils affichaient un rectangle de
    //    fond parasite derriere chaque titre.
    // 2. LES IMAGES PASSENT PAR Guna2PictureBox. Guna2Button se peint entierement
    //    lui-meme et IGNORE BackgroundImage -> les captures restaient invisibles.
    // 3. LA PAGE NE DEPASSE JAMAIS SON HOTE. Faire grandir le formulaire au-dela
    //    du panneau qui le contient ne fait que decaler le rognage : ici tout est
    //    calcule pour TENIR dans la place disponible, et le texte trop long
    //    defile dans sa propre zone.
    //
    // Les controles sont crees SANS position ; une seule methode (Layout) les
    // place a partir de la place disponible, rejouee a chaque onglet/redimension.
    public class ProductPage : Form
    {
        private const int Pad = 24;
        private const int Gap = 16;
        private const int ThumbGap = 8;
        private const int CardPad = 18;

        private readonly string _name, _description, _version, _lastUpdate, _linkDiscord, _videoUrl;
        private readonly Image _logo;
        private readonly Image[] _shots;
        private readonly Image _videoThumb;
        private readonly List<(string text, Image icon, Color color, string link)> _requirements;
        private readonly List<(string text, Image icon, Color color)> _features;
        private readonly Action _openProduct;
        private readonly bool _logoRounded;

        private readonly Utils _utils = new Utils();
        private readonly Images _images = new Images();

        private Guna2PictureBox _logoBox, _preview;
        private readonly List<Guna2PictureBox> _thumbs = new List<Guna2PictureBox>();
        private Label _title, _kicker;
        private Guna2CircleButton _expandBtn;

        private Guna2Panel _card;
        private readonly Guna2Button[] _tabBtns = new Guna2Button[3];
        private Guna2Panel _underline;
        private Panel _scrollArea;      // defile si le contenu depasse
        private Label _body;
        private Guna2Panel _listPanel;
        private Guna2Separator _metaLine;
        private Label _versionKey, _versionVal, _updateKey, _updateVal;
        private Guna2Button _launch;
        private Guna2HtmlLabel _report;
        private Label _lockNote;

        private Guna2Panel _overlay;
        private Guna2PictureBox _overlayImage;
        private Guna2CircleButton _overlayClose;

        private int _activeTab, _activeShot;

        // Nom de l'abonnement KeyAuth de ce produit : sert a savoir si LAUNCH doit
        // lancer, ou proposer de reclamer une cle.
        private readonly string _subscription;

        // Branche par la fenetre principale : ouvre le dialogue de claim.
        public Action ClaimRequested { get; set; }

        public ProductPage(
            string __subscriptionName,
            string productName,
            string productDescription,
            string __versionProduct,
            string __lastUpdate,
            string productVideoURL,
            bool logoRounded,
            Image productLogo,
            Image image1,
            Image image2,
            Image image3,
            Image image4,
            Image video1,
            List<(string requirementsText, Image icon, Color iconColor, string link)> requirements,
            List<(string featureText, Image icon, Color iconColor)> features,
            string linkDiscord,
            Action openProduct)
        {
            // Le catalogue met des retours a la ligne dans les noms
            // ("Rockstar: Grand\nTheft Auto V") -> on les aplatit, la mise en page
            // gere elle-meme le repli.
            _name = (productName ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
            _description = (productDescription ?? "").Replace("\r\n", "\n");
            _version = __versionProduct ?? "";
            _lastUpdate = __lastUpdate ?? "";
            _videoUrl = productVideoURL;
            _logo = productLogo;
            _logoRounded = logoRounded;
            _shots = new[] { image1, image2, image3, image4 }.Where(i => i != null).ToArray();
            _videoThumb = video1;
            _requirements = (requirements ?? new List<(string, Image, Color, string)>())
                .Select(r => (text: r.requirementsText, icon: r.icon, color: r.iconColor, link: r.link)).ToList();
            _features = (features ?? new List<(string, Image, Color)>())
                .Select(f => (text: f.featureText, icon: f.icon, color: f.iconColor)).ToList();
            _linkDiscord = linkDiscord;
            _openProduct = openProduct;
            _subscription = __subscriptionName ?? "";

            FormBorderStyle = FormBorderStyle.None;
            BackColor = Colors.bgColor;
            Size = new Size(880, 540);
            Location = new Point(0, 0);
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            Build();
            _utils.DisableSelectionInGuna2HtmlLabels(this);

            ParentChanged += (s, e) => Relayout();
            SizeChanged += (s, e) => Relayout();
        }

        // ---------- construction (aucune position ici) ----------
        private void Build()
        {
            _logoBox = new Guna2PictureBox
            {
                Parent = this,
                Size = new Size(54, 54),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = _logo,
                FillColor = Colors.scColor,
                BorderRadius = _logoRounded ? 27 : 12,
                BackColor = Color.Transparent,
            };

            _title = MakeLbl(_name, Color.White, new Font("Inter Semibold", 17f));
            _kicker = MakeLbl("PaiPai", Colors.mainColor, new Font("Inter Semibold", 8.5f));

            _preview = new Guna2PictureBox
            {
                Parent = this,
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = _shots.Length > 0 ? _shots[0] : _logo,
                FillColor = Colors.scColor,
                BorderRadius = 12,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
            };
            _preview.Click += (s, e) => ShowOverlay();
            AttachBorder(_preview, () => Color.FromArgb(38, 255, 255, 255), 12);

            _expandBtn = new Guna2CircleButton
            {
                Parent = this,
                Size = new Size(38, 38),
                FillColor = Color.FromArgb(190, 12, 8, 12),
                BorderThickness = 0,
                Image = Utils.ChangeIconsColor(new Bitmap(Images.ExpandedIcon), Color.White),
                ImageSize = new Size(16, 16),
                Cursor = Cursors.Hand,
                Animated = true,
            };
            _expandBtn.HoverState.FillColor = Color.FromArgb(225, Colors.mainColor);
            _expandBtn.Click += (s, e) => ShowOverlay();

            for (int i = 0; i < _shots.Length; i++)
            {
                int index = i;
                var t = MakeThumb(_shots[i]);
                t.Click += (s, e) => SelectShot(index);
                _thumbs.Add(t);
            }
            if (_videoThumb != null && !string.IsNullOrWhiteSpace(_videoUrl))
            {
                var v = MakeThumb(_videoThumb);
                v.Click += (s, e) => { try { _utils.OpenLink(_videoUrl); } catch { } };
                // Pastille "play" par-dessus -> on distingue la video des captures.
                v.Paint += (s, e) =>
                {
                    var g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    float d = Math.Min(30f, v.Height * 0.55f), cx = v.Width / 2f, cy = v.Height / 2f;
                    using (var b = new SolidBrush(Color.FromArgb(205, 12, 8, 12)))
                        g.FillEllipse(b, cx - d / 2, cy - d / 2, d, d);
                    using (var b = new SolidBrush(Color.White))
                        g.FillPolygon(b, new[]
                        {
                            new PointF(cx - d * 0.14f, cy - d * 0.22f),
                            new PointF(cx + d * 0.24f, cy),
                            new PointF(cx - d * 0.14f, cy + d * 0.22f),
                        });
                };
                _thumbs.Add(v);
            }

            _card = new Guna2Panel
            {
                Parent = this,
                FillColor = Color.FromArgb(165, 38, 26, 38),
                BorderColor = Color.FromArgb(52, 244, 114, 182),
                BorderThickness = 1,
                BorderRadius = 14,
                BackColor = Color.Transparent,
            };

            string[] tabs = { "About", "Requirements", "Features" };
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                var b = new Guna2Button
                {
                    Parent = _card,
                    Text = tabs[i],
                    Font = new Font("Inter Semibold", 9.5f),
                    FillColor = Color.Transparent,
                    BorderThickness = 0,
                    ForeColor = Colors.textMuted,
                    Cursor = Cursors.Hand,
                    Height = 30,
                    UseTransparentBackground = true,
                };
                b.HoverState.ForeColor = Color.White;
                b.Click += (s, e) => SelectTab(index);
                _tabBtns[i] = b;
            }

            _underline = new Guna2Panel
            {
                Parent = _card,
                Height = 2,
                FillColor = Colors.mainColor,
                BorderThickness = 0,
                BorderRadius = 1,
                BackColor = Color.Transparent,
            };

            // Zone defilante : garantit qu'une description longue ne pousse JAMAIS
            // le bouton hors de l'ecran, quelle que soit la taille de la fenetre.
            _scrollArea = new Panel
            {
                Parent = _card,
                AutoScroll = true,
                BackColor = Color.Transparent,
            };

            _body = new Label
            {
                Parent = _scrollArea,
                ForeColor = Colors.textSubtle,
                BackColor = Color.Transparent,
                Font = new Font("Inter Medium", 9.5f),
                AutoSize = false,
                UseCompatibleTextRendering = false,
                // Sans ceci, WinForms lit "&" comme prefixe de raccourci clavier et
                // l'AVALE : "Windows 10 & 11" s'affichait "Windows 10  11".
                UseMnemonic = false,
                Location = new Point(0, 0),
            };

            _listPanel = new Guna2Panel
            {
                Parent = _scrollArea,
                FillColor = Color.Transparent,
                BackColor = Color.Transparent,
                BorderThickness = 0,
                Location = new Point(0, 0),
                Visible = false,
            };

            _metaLine = new Guna2Separator { Parent = _card, FillColor = Colors.divider };
            _versionKey = MakeLbl("Version", Color.White, new Font("Inter Semibold", 9f), _card);
            _versionVal = MakeLbl(_version, Colors.textMuted, new Font("Inter Medium", 9f), _card);
            _updateKey = MakeLbl("Last update", Color.White, new Font("Inter Semibold", 9f), _card);
            _updateVal = MakeLbl(_lastUpdate, Colors.textMuted, new Font("Inter Medium", 9f), _card);

            _launch = new Guna2Button
            {
                Parent = _card,
                Text = "LAUNCH",
                Font = new Font("Inter Semibold", 10.5f),
                ForeColor = Color.White,
                FillColor = Colors.mainColor,
                BorderThickness = 0,
                BorderRadius = 10,
                Height = 44,
                Cursor = Cursors.Hand,
                Animated = true,
                Image = Utils.ChangeIconsColor(new Bitmap(_images.PlayIcon), Color.White),
                ImageSize = new Size(13, 14),
                // ImageAlign ET TextAlign du meme cote : Left/Right ferait chevaucher
                // l'icone et le texte.
                ImageAlign = HorizontalAlignment.Left,
                ImageOffset = new Point(14, 0),
            };
            _launch.HoverState.FillColor = ControlPaint.Light(Colors.mainColor, 0.22f);
            _launch.PressedColor = ControlPaint.Dark(Colors.mainColor, 0.05f);
            _launch.ShadowDecoration.Enabled = true;
            _launch.ShadowDecoration.Color = Color.FromArgb(120, Colors.mainColor);
            _launch.ShadowDecoration.Depth = 7;
            _launch.Click += (s, e) =>
            {
                // Le verrou vit ICI, pas a l'ouverture de la fiche : on veut que
                // tout le monde puisse consulter un produit, et que seul le
                // LANCEMENT exige une licence valide.
                if (HasAccess()) _openProduct?.Invoke();
                else ClaimRequested?.Invoke();
            };

            _report = new Guna2HtmlLabel
            {
                Parent = _card,
                Text = "<u>Report a bug</u>",
                ForeColor = Colors.mainColor,
                Font = new Font("Inter Semibold", 9f),
                BackColor = Color.Transparent,
                AutoSize = true,
                Cursor = Cursors.Hand,
                IsSelectionEnabled = false,
            };
            _report.Click += (s, e) => { try { _utils.OpenLink(_linkDiscord); } catch { } };

            _lockNote = MakeLbl("", Colors.textMuted, new Font("Inter Medium", 8.5f), _card);
            _lockNote.Visible = false;

            BuildOverlay();
            SelectTab(0);
            SelectShot(0);
            RefreshAccess();
        }

        private void BuildOverlay()
        {
            _overlay = new Guna2Panel
            {
                Parent = this,
                FillColor = Color.FromArgb(235, 10, 6, 10),
                BorderThickness = 0,
                Visible = false,
            };
            _overlayImage = new Guna2PictureBox
            {
                Parent = _overlay,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                FillColor = Color.Transparent,
                BorderRadius = 10,
            };
            _overlayClose = new Guna2CircleButton
            {
                Parent = _overlay,
                Size = new Size(40, 40),
                FillColor = Color.FromArgb(205, 20, 12, 20),
                BorderThickness = 0,
                Image = Utils.ChangeIconsColor(new Bitmap(_images.CloseIcon), Color.White),
                ImageSize = new Size(14, 14),
                Cursor = Cursors.Hand,
                Animated = true,
            };
            _overlayClose.HoverState.FillColor = Color.FromArgb(230, 255, 95, 87);
            _overlayClose.Click += (s, e) => _overlay.Visible = false;
            _overlay.Click += (s, e) => _overlay.Visible = false;
            _overlayImage.Click += (s, e) => _overlay.Visible = false;
        }

        // ---------- helpers ----------
        private Label MakeLbl(string text, Color color, Font font, Control parent = null)
            => new Label
            {
                Parent = parent ?? (Control)this,
                Text = text,
                ForeColor = color,
                BackColor = Color.Transparent,
                Font = font,
                AutoSize = true,
                UseMnemonic = false,
            };

        // Peint un liseré arrondi par-dessus un Guna2PictureBox. La couleur est
        // fournie par un callback pour qu'un simple Invalidate() reflète l'état
        // courant (vignette active) sans avoir à retoucher chaque contrôle.
        private static void AttachBorder(Guna2PictureBox box, Func<Color> color, int radius)
        {
            box.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(0, 0, box.Width - 1, box.Height - 1);
                using (var path = RoundedPath(r, radius))
                using (var pen = new Pen(color(), 1.6f))
                    e.Graphics.DrawPath(pen, path);
            };
        }

        private static GraphicsPath RoundedPath(Rectangle r, int radius)
        {
            int d = Math.Max(2, radius * 2);
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        private Guna2PictureBox MakeThumb(Image img)
        {
            var t = new Guna2PictureBox
            {
                Parent = this,
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = img,
                FillColor = Colors.scColor,
                BorderRadius = 8,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
            };
            // Guna2PictureBox n'expose NI BorderColor NI BorderThickness : le liseré
            // (et la mise en évidence de la vignette active) est peint a la main.
            AttachBorder(t, () => _thumbs.IndexOf(t) == _activeShot
                ? Colors.mainColor
                : Color.FromArgb(38, 255, 255, 255), 8);
            return t;
        }

        private bool HasAccess()
            => string.IsNullOrEmpty(_subscription) || LicenseGate.HasValidSubscription(_subscription);

        // Recalcule l'apparence du bouton d'action. Appelable de l'exterieur : la
        // licence peut etre reclamee pendant que la fiche est ouverte.
        public void RefreshAccess()
        {
            if (_launch == null) return;
            bool owned = HasAccess();

            _launch.Text = owned ? "LAUNCH" : "GET ACCESS";
            _launch.Image = Utils.ChangeIconsColor(
                new Bitmap(owned ? _images.PlayIcon : _images.KeyIcon), Color.White);
            _launch.FillColor = owned ? Colors.mainColor : Color.FromArgb(70, 52, 70);
            _launch.HoverState.FillColor = owned
                ? ControlPaint.Light(Colors.mainColor, 0.22f)
                : Color.FromArgb(96, 72, 96);
            _launch.ShadowDecoration.Enabled = owned;

            _lockNote.Text = owned ? "" : "Locked — claim a key to unlock this product.";
            _lockNote.Visible = !owned;
            Relayout();
        }

        private void SelectShot(int index)
        {
            if (_shots.Length == 0) return;
            index = Math.Max(0, Math.Min(index, _shots.Length - 1));
            _activeShot = index;
            _preview.Image = _shots[index];
            foreach (var t in _thumbs) t.Invalidate();
        }

        private void ShowOverlay()
        {
            if (_shots.Length == 0) return;
            _overlayImage.Image = _shots[Math.Min(_activeShot, _shots.Length - 1)];
            _overlay.Visible = true;
            _overlay.BringToFront();
            LayoutOverlay();
        }

        private void SelectTab(int index)
        {
            _activeTab = index;
            for (int i = 0; i < _tabBtns.Length; i++)
                _tabBtns[i].ForeColor = (i == index) ? Color.White : Colors.textMuted;

            bool isAbout = index == 0;
            _body.Visible = isAbout;
            _listPanel.Visible = !isAbout;
            if (!isAbout) FillList(index == 1);

            Relayout();
        }

        private void FillList(bool requirements)
        {
            foreach (Control c in _listPanel.Controls.Cast<Control>().ToList())
            {
                _listPanel.Controls.Remove(c);
                c.Dispose();
            }

            var rows = requirements
                ? _requirements
                : _features.Select(f => (text: f.text, icon: f.icon, color: f.color, link: (string)null)).ToList();

            int y = 0, w = Math.Max(60, _listPanel.Width);
            foreach (var row in rows)
            {
                var b = new Guna2Button
                {
                    Parent = _listPanel,
                    Text = "   " + row.text,
                    TextAlign = HorizontalAlignment.Left,
                    Font = new Font("Inter Medium", 9f),
                    ForeColor = Colors.textSubtle,
                    FillColor = Color.FromArgb(120, 46, 32, 46),
                    BorderThickness = 0,
                    BorderRadius = 8,
                    Height = 36,
                    Location = new Point(0, y),
                    Width = w,
                    Cursor = row.link != null ? Cursors.Hand : Cursors.Default,
                    ImageAlign = HorizontalAlignment.Left,
                    ImageOffset = new Point(10, 0),
                    ImageSize = new Size(15, 15),
                    Image = row.icon == null ? null : Utils.ChangeIconsColor(new Bitmap(row.icon), row.color),
                };
                b.HoverState.FillColor = Color.FromArgb(170, 60, 42, 60);
                if (row.link != null)
                {
                    string link = row.link;
                    b.Click += (s, e) => { try { _utils.OpenLink(link); } catch { } };
                }
                y += b.Height + 7;
            }
            _listPanel.Height = Math.Max(0, y - 7);
        }

        // ---------- mise en page ----------
        private void Relayout()
        {
            if (_card == null) return;

            // On TIENT dans l'hote, on ne le deborde jamais : le faire grandir ne
            // ferait que deplacer le rognage.
            int hostW = (Parent != null && Parent.Width > 400) ? Parent.Width : Width;
            int hostH = VisibleHeight();
            if (Width != hostW || Height != hostH) { Size = new Size(hostW, hostH); }

            int contentW = hostW - Pad * 2;
            int leftW = (int)(contentW * 0.55);
            int rightW = contentW - leftW - Gap;

            // --- en-tete ---
            int y = Pad;
            _logoBox.Location = new Point(Pad, y);
            _title.MaximumSize = new Size(leftW, 0);
            _title.Location = new Point(_logoBox.Right + 14, y + 4);
            _kicker.Location = new Point(_logoBox.Right + 14, _title.Bottom + 1);
            y = Math.Max(_logoBox.Bottom, _kicker.Bottom) + 18;

            int bodyTop = y;
            int bottomLimit = hostH - Pad;
            int available = Math.Max(200, bottomLimit - bodyTop);

            // --- colonne gauche : vignettes d'abord (hauteur connue), le reste
            // revient a l'apercu -> jamais de debordement vertical.
            int count = Math.Max(1, _thumbs.Count);
            int thumbW = (leftW - ThumbGap * (count - 1)) / count;
            int thumbH = Math.Max(38, (int)(thumbW * 9f / 16f));
            int previewH = Math.Min((int)(leftW * 9f / 16f), available - thumbH - ThumbGap);
            previewH = Math.Max(120, previewH);

            _preview.Location = new Point(Pad, bodyTop);
            _preview.Size = new Size(leftW, previewH);
            _expandBtn.Location = new Point(_preview.Right - _expandBtn.Width - 12, _preview.Bottom - _expandBtn.Height - 12);
            _expandBtn.BringToFront();

            for (int i = 0; i < _thumbs.Count; i++)
            {
                _thumbs[i].Location = new Point(Pad + i * (thumbW + ThumbGap), _preview.Bottom + ThumbGap);
                _thumbs[i].Size = new Size(thumbW, thumbH);
            }

            // --- colonne droite ---
            _card.Location = new Point(Pad + leftW + Gap, bodyTop);
            _card.Size = new Size(rightW, available);

            int innerW = rightW - CardPad * 2;
            int cx = CardPad, cy = CardPad;

            int tx = cx;
            foreach (var b in _tabBtns)
            {
                // +30 : Guna2Button applique sa propre marge interne, une largeur
                // calee sur la seule mesure du texte tronquait les libelles.
                int w = TextRenderer.MeasureText(b.Text, b.Font).Width + 30;
                b.Location = new Point(tx, cy);
                b.Width = w;
                tx += w + 2;
            }
            var act = _tabBtns[_activeTab];
            _underline.Location = new Point(act.Left + 8, act.Bottom + 3);
            _underline.Width = Math.Max(18, act.Width - 16);
            cy = _underline.Bottom + 14;

            // --- bloc bas (ancre au bas de la carte) : LAUNCH, meta, separateur ---
            int bottom = available - CardPad;

            _report.Location = new Point(cx + innerW - _report.Width, bottom - _report.Height);
            bottom = _report.Top - 8;

            _launch.Width = innerW;
            _launch.Location = new Point(cx, bottom - _launch.Height);
            bottom = _launch.Top - 8;

            if (_lockNote != null && _lockNote.Visible)
            {
                _lockNote.MaximumSize = new Size(innerW, 0);
                _lockNote.Location = new Point(cx, bottom - _lockNote.Height);
                bottom = _lockNote.Top - 8;
            }
            bottom -= 8;

            _updateKey.Location = new Point(cx, bottom - _updateKey.Height);
            _updateVal.Location = new Point(cx + innerW - _updateVal.Width, _updateKey.Top);
            bottom = _updateKey.Top - 6;

            _versionKey.Location = new Point(cx, bottom - _versionKey.Height);
            _versionVal.Location = new Point(cx + innerW - _versionVal.Width, _versionKey.Top);
            bottom = _versionKey.Top - 12;

            _metaLine.Width = innerW;
            _metaLine.Location = new Point(cx, bottom - _metaLine.Height);
            bottom = _metaLine.Top - 14;

            // --- zone defilante : tout l'espace restant entre onglets et bloc bas ---
            _scrollArea.Location = new Point(cx, cy);
            _scrollArea.Size = new Size(innerW, Math.Max(60, bottom - cy));

            int textW = innerW - 20;   // marge pour l'eventuelle barre de defilement
            if (_activeTab == 0)
            {
                _body.Width = textW;
                // On mesure sur une largeur legerement inferieure et on ajoute une
                // ligne : MeasureText et le rendu interne du Label ne replient pas
                // toujours au meme mot, et un mot d'ecart suffisait a faire
                // disparaitre la derniere ligne.
                Size m = TextRenderer.MeasureText(_description, _body.Font,
                    new Size(textW - 6, int.MaxValue),
                    TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
                _body.Height = m.Height + _body.Font.Height;
                _body.Text = _description;
            }
            else
            {
                _listPanel.Width = textW;
                foreach (Control row in _listPanel.Controls) row.Width = textW;
            }

            LayoutOverlay();
            Invalidate(true);
        }

        // Hauteur REELLEMENT visible, pas celle du panneau hote.
        //
        // _contentForDetailsForm a une hauteur fixe (600) et debute sous la barre
        // de navigation : dans une fenetre plus courte, son bas passe SOUS le bord
        // de l'application. Se caler sur Parent.Height ancrait donc le bouton
        // LAUNCH hors de l'ecran — invisible et incliquable.
        private int VisibleHeight()
        {
            if (Parent == null) return Height;
            int h = Parent.Height;
            var top = Parent.TopLevelControl;
            if (top != null)
            {
                try
                {
                    Point originInTop = top.PointToClient(Parent.PointToScreen(Point.Empty));
                    h = Math.Min(h, top.ClientSize.Height - originInTop.Y);
                }
                catch { /* handle pas encore cree : on garde la hauteur du parent */ }
            }
            return Math.Max(300, h);
        }

        private void LayoutOverlay()
        {
            if (_overlay == null) return;
            _overlay.Location = new Point(0, 0);
            _overlay.Size = ClientSize;

            int w = (int)(ClientSize.Width * 0.8), h = (int)(ClientSize.Height * 0.74);
            _overlayImage.Size = new Size(Math.Max(80, w), Math.Max(60, h));
            _overlayImage.Location = new Point((ClientSize.Width - _overlayImage.Width) / 2,
                                               (ClientSize.Height - _overlayImage.Height) / 2 + 12);
            _overlayClose.Location = new Point(_overlayImage.Right - _overlayClose.Width,
                                               _overlayImage.Top - _overlayClose.Height - 10);
            _overlayClose.BringToFront();
        }

        // Fond peint par le FORMULAIRE lui-meme (comme WindowsPaiScreen) : c'est ce
        // qui permet aux Label transparents de se composer sans rectangle parasite.
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width, Height);

            using (var bg = new LinearGradientBrush(rect, Colors.bgColor,
                       Color.FromArgb(44, 24, 40), LinearGradientMode.ForwardDiagonal))
                g.FillRectangle(bg, rect);

            using (var glow = new GraphicsPath())
            {
                glow.AddEllipse(Width - 320, -220, 460, 420);
                using (var pgb = new PathGradientBrush(glow))
                {
                    pgb.CenterColor = Color.FromArgb(48, Colors.mainColor);
                    pgb.SurroundColors = new[] { Color.FromArgb(0, Colors.mainColor) };
                    g.FillPath(pgb, glow);
                }
            }
        }
    }
}
