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
    // Fiche produit — reecriture complete de l'ancien DetailsProduct.
    //
    // POURQUOI une reecriture : l'ancien ecran calculait chaque position en
    // lisant le .Bottom du controle precedent AU MOMENT de sa creation, avant
    // meme qu'il soit rattache a un parent (donc avant qu'il connaisse sa vraie
    // taille), melangeait des panneaux AutoSize et des hauteurs figees, et se
    // fiait a Control.Visible — qui renvoie la visibilite EFFECTIVE et vaut donc
    // false tant que la fenetre n'est pas affichee. Resultat : bouton LAUNCH
    // par-dessus le texte, colonne rognee, largeurs mortes.
    //
    // PRINCIPE ICI : les controles sont crees SANS position, puis UNE SEULE
    // methode (Layout) les place, de haut en bas, a partir de la largeur
    // disponible. Elle est rejouee a chaque changement d'onglet et de taille.
    // Aucun AutoSize, aucune lecture de Visible, aucune position calculee a la
    // construction -> le rendu ne depend plus de l'ordre du code.
    public class ProductPage : Form
    {
        private const int Pad = 26;      // marge exterieure
        private const int Gap = 18;      // espace entre les deux colonnes
        private const int ThumbGap = 10;

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

        // --- controles ---
        private Guna2Panel _root;
        private Guna2Button _logoBox;
        private Label _title, _kicker;
        private Guna2Button _preview;
        private readonly List<Guna2Button> _thumbs = new List<Guna2Button>();
        private Guna2CircleButton _expandBtn;
        private Guna2Panel _infoCard;
        private readonly Guna2Button[] _tabBtns = new Guna2Button[3];
        private Guna2Panel _tabUnderline;
        private Label _body;
        private Guna2Panel _listPanel;
        private Label _versionKey, _versionVal, _updateKey, _updateVal;
        private Guna2Separator _metaLine;
        private Guna2Button _launch;
        private Guna2HtmlLabel _report;

        private Guna2Panel _overlay;
        private Guna2PictureBox _overlayImage;

        private int _activeTab;          // 0 About, 1 Requirements, 2 Features
        private int _activeShot;

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
            _name = productName ?? "";
            _description = (productDescription ?? "").Replace("\r\n", "\n");
            _version = __versionProduct ?? "";
            _lastUpdate = __lastUpdate ?? "";
            _videoUrl = productVideoURL;
            _logo = productLogo;
            _logoRounded = logoRounded;
            _shots = new[] { image1, image2, image3, image4 }.Where(i => i != null).ToArray();
            _videoThumb = video1;
            _requirements = (requirements ?? new List<(string, Image, Color, string)>())
                .Select(r => (r.requirementsText, r.icon, r.iconColor, r.link)).ToList();
            _features = (features ?? new List<(string, Image, Color)>())
                .Select(f => (f.featureText, f.icon, f.iconColor)).ToList();
            _linkDiscord = linkDiscord;
            _openProduct = openProduct;

            FormBorderStyle = FormBorderStyle.None;
            BackColor = Colors.bgColor;
            Size = new Size(900, 560);
            Location = new Point(0, 15);

            Build();
            UiStyle.EnableDoubleBuffer(this);
            _utils.DisableSelectionInGuna2HtmlLabels(this);

            // Le parent n'existe pas encore a la construction : on remet en page
            // des qu'on y est rattache, puis a chaque redimensionnement.
            ParentChanged += (s, e) => Layout();
            SizeChanged += (s, e) => { if (_root != null) Layout(); };
        }

        // ---------- construction (aucune position ici) ----------
        private void Build()
        {
            _root = new Guna2Panel
            {
                Parent = this,
                FillColor = Color.Transparent,
                BackColor = Color.Transparent,
                BorderThickness = 0,
                UseTransparentBackground = true,
                Location = new Point(0, 0),
            };
            UiStyle.AttachContentBackdrop(_root);

            // --- en-tete ---
            _logoBox = new Guna2Button
            {
                Parent = _root,
                Size = new Size(58, 58),
                FillColor = Colors.scColor,
                BorderThickness = 0,
                BorderRadius = _logoRounded ? 29 : 14,
                Image = _logo,
                ImageSize = new Size(38, 38),
                Text = "",
                Enabled = false,
                UseTransparentBackground = true,
            };
            _logoBox.DisabledState.FillColor = Colors.scColor;
            _logoBox.DisabledState.CustomBorderColor = Color.Transparent;

            _title = MakeLabel(_name, Color.White, new Font("Inter Semibold", 19f), _root);
            _kicker = MakeLabel("PaiPai", Colors.mainColor, new Font("Inter Semibold", 9f), _root);

            // --- galerie ---
            _preview = MakeImageButton(_root, _shots.Length > 0 ? _shots[0] : _logo, 12);
            _preview.Click += (s, e) => ShowOverlay();

            _expandBtn = new Guna2CircleButton
            {
                Parent = _root,
                Size = new Size(40, 40),
                FillColor = Color.FromArgb(190, 12, 8, 12),
                BorderThickness = 0,
                Image = Utils.ChangeIconsColor(new Bitmap(Images.ExpandedIcon), Color.White),
                ImageSize = new Size(17, 17),
                Cursor = Cursors.Hand,
                Animated = true,
            };
            _expandBtn.HoverState.FillColor = Color.FromArgb(220, Colors.mainColor);
            _expandBtn.Click += (s, e) => ShowOverlay();

            // Vignettes : les captures, puis la video si elle existe.
            for (int i = 0; i < _shots.Length; i++)
            {
                int index = i;
                var t = MakeImageButton(_root, _shots[i], 9);
                t.Cursor = Cursors.Hand;
                t.Click += (s, e) => SelectShot(index);
                _thumbs.Add(t);
            }
            if (_videoThumb != null && !string.IsNullOrWhiteSpace(_videoUrl))
            {
                var v = MakeImageButton(_root, _videoThumb, 9);
                v.Cursor = Cursors.Hand;
                v.Image = _videoThumb;
                v.Click += (s, e) => { try { _utils.OpenLink(_videoUrl); } catch { } };
                // Pastille "play" dessinee par-dessus -> on distingue la video des captures.
                v.Paint += (s, e) =>
                {
                    var g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    float d = 34f, cx = v.Width / 2f, cy = v.Height / 2f;
                    using (var b = new SolidBrush(Color.FromArgb(200, 12, 8, 12)))
                        g.FillEllipse(b, cx - d / 2, cy - d / 2, d, d);
                    using (var b = new SolidBrush(Color.White))
                        g.FillPolygon(b, new[]
                        {
                            new PointF(cx - 5, cy - 8), new PointF(cx + 9, cy), new PointF(cx - 5, cy + 8),
                        });
                };
                _thumbs.Add(v);
            }

            // --- carte d'informations ---
            _infoCard = new Guna2Panel
            {
                Parent = _root,
                FillColor = Color.FromArgb(150, 38, 26, 38),
                BorderColor = Color.FromArgb(46, 244, 114, 182),
                BorderThickness = 1,
                BorderRadius = 14,
                UseTransparentBackground = true,
            };

            string[] tabs = { "About", "Requirements", "Features" };
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                var b = new Guna2Button
                {
                    Parent = _infoCard,
                    Text = tabs[i],
                    Font = new Font("Inter Semibold", 10f),
                    FillColor = Color.Transparent,
                    BorderThickness = 0,
                    ForeColor = Colors.textMuted,
                    Cursor = Cursors.Hand,
                    UseTransparentBackground = true,
                    Height = 34,
                };
                b.HoverState.ForeColor = Color.White;
                b.Click += (s, e) => SelectTab(index);
                _tabBtns[i] = b;
            }

            _tabUnderline = new Guna2Panel
            {
                Parent = _infoCard,
                Height = 2,
                FillColor = Colors.mainColor,
                BorderThickness = 0,
                BorderRadius = 1,
                UseTransparentBackground = true,
            };

            _body = new Label
            {
                Parent = _infoCard,
                ForeColor = Colors.textSubtle,
                BackColor = Color.Transparent,
                Font = new Font("Inter Medium", 10f),
                AutoSize = false,
                UseCompatibleTextRendering = false,
                // Sans ceci, WinForms lit "&" comme un prefixe de raccourci clavier
                // et l'AVALE : "Windows 10 & 11" s'affichait "Windows 10  11", et
                // "EAC, BattlEye, Ricochet & Vanguard" perdait son "&".
                UseMnemonic = false,
            };

            _listPanel = new Guna2Panel
            {
                Parent = _infoCard,
                FillColor = Color.Transparent,
                BackColor = Color.Transparent,
                BorderThickness = 0,
                UseTransparentBackground = true,
                Visible = false,
            };

            _metaLine = new Guna2Separator
            {
                Parent = _infoCard,
                FillColor = Colors.divider,
                UseTransparentBackground = true,
            };
            _versionKey = MakeLabel("Version", Color.White, new Font("Inter Semibold", 9.5f), _infoCard);
            _versionVal = MakeLabel(_version, Colors.textMuted, new Font("Inter Medium", 9.5f), _infoCard);
            _updateKey = MakeLabel("Last update", Color.White, new Font("Inter Semibold", 9.5f), _infoCard);
            _updateVal = MakeLabel(_lastUpdate, Colors.textMuted, new Font("Inter Medium", 9.5f), _infoCard);

            _launch = new Guna2Button
            {
                Parent = _infoCard,
                Text = "LAUNCH",
                Font = new Font("Inter Semibold", 11f),
                ForeColor = Color.White,
                FillColor = Colors.mainColor,
                BorderThickness = 0,
                BorderRadius = 10,
                Height = 46,
                Cursor = Cursors.Hand,
                Animated = true,
                UseTransparentBackground = true,
                Image = Utils.ChangeIconsColor(new Bitmap(_images.PlayIcon), Color.White),
                ImageSize = new Size(14, 15),
                ImageAlign = HorizontalAlignment.Left,
                ImageOffset = new Point(14, 0),
            };
            _launch.HoverState.FillColor = ControlPaint.Light(Colors.mainColor, 0.22f);
            _launch.PressedColor = ControlPaint.Dark(Colors.mainColor, 0.05f);
            _launch.ShadowDecoration.Enabled = true;
            _launch.ShadowDecoration.Color = Color.FromArgb(120, Colors.mainColor);
            _launch.ShadowDecoration.Depth = 8;
            _launch.Click += (s, e) => _openProduct?.Invoke();

            _report = new Guna2HtmlLabel
            {
                Parent = _infoCard,
                Text = "<u>Report a bug</u>",
                ForeColor = Colors.mainColor,
                Font = new Font("Inter Semibold", 9.5f),
                BackColor = Color.Transparent,
                AutoSize = true,
                Cursor = Cursors.Hand,
                IsSelectionEnabled = false,
            };
            _report.Click += (s, e) => { try { _utils.OpenLink(_linkDiscord); } catch { } };

            BuildOverlay();
            SelectTab(0);
        }

        private void BuildOverlay()
        {
            _overlay = new Guna2Panel
            {
                Parent = this,
                FillColor = Color.FromArgb(228, 10, 6, 10),
                BorderThickness = 0,
                UseTransparentBackground = false,
                Visible = false,
            };
            _overlayImage = new Guna2PictureBox
            {
                Parent = _overlay,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
            };
            var close = new Guna2CircleButton
            {
                Parent = _overlay,
                Size = new Size(42, 42),
                FillColor = Color.FromArgb(200, 20, 12, 20),
                BorderThickness = 0,
                Image = Utils.ChangeIconsColor(new Bitmap(_images.CloseIcon), Color.White),
                ImageSize = new Size(15, 15),
                Cursor = Cursors.Hand,
                Animated = true,
            };
            close.HoverState.FillColor = Color.FromArgb(230, 255, 95, 87);
            close.Click += (s, e) => _overlay.Visible = false;
            _overlay.Tag = close;
            _overlay.Click += (s, e) => _overlay.Visible = false;
        }

        // ---------- helpers ----------
        private static Label MakeLabel(string text, Color color, Font font, Control parent)
            => new Label
            {
                Parent = parent,
                Text = text,
                ForeColor = color,
                BackColor = Color.Transparent,
                Font = font,
                AutoSize = true,
            };

        private static Guna2Button MakeImageButton(Control parent, Image img, int radius)
        {
            var b = new Guna2Button
            {
                Parent = parent,
                Text = "",
                FillColor = Colors.scColor,
                BorderThickness = 1,
                BorderColor = Color.FromArgb(34, 255, 255, 255),
                BorderRadius = radius,
                BackgroundImage = img,
                BackgroundImageLayout = ImageLayout.Zoom,
                UseTransparentBackground = true,
                Animated = true,
            };
            b.HoverState.BorderColor = Color.FromArgb(150, Colors.mainColor);
            b.DisabledState.FillColor = Colors.scColor;
            return b;
        }

        private void SelectShot(int index)
        {
            if (index < 0 || index >= _shots.Length) return;
            _activeShot = index;
            _preview.BackgroundImage = _shots[index];
            for (int i = 0; i < _thumbs.Count; i++)
                _thumbs[i].BorderColor = (i == index)
                    ? Colors.mainColor
                    : Color.FromArgb(34, 255, 255, 255);
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
            if (_root != null) Layout();
        }

        // Reconstruit la liste affichee (prerequis ou fonctionnalites). On la
        // recree a chaque bascule plutot que de garder deux panneaux vivants :
        // moins de controles en memoire, et aucune position a resynchroniser.
        private void FillList(bool requirements)
        {
            foreach (Control c in _listPanel.Controls.Cast<Control>().ToList()) { _listPanel.Controls.Remove(c); c.Dispose(); }

            // Le 4e element doit etre NOMME des deux cotes : sans `link:`, le tuple
            // des fonctionnalites devient (string, Image, Color, string) et perd le
            // nom, ce qui rend les deux branches incompatibles.
            var rows = requirements
                ? _requirements.Select(r => (text: r.text, icon: r.icon, color: r.color, link: r.link)).ToList()
                : _features.Select(f => (text: f.text, icon: f.icon, color: f.color, link: (string)null)).ToList();

            int y = 0;
            foreach (var row in rows)
            {
                var b = new Guna2Button
                {
                    Parent = _listPanel,
                    Text = "   " + row.text,
                    TextAlign = HorizontalAlignment.Left,
                    Font = new Font("Inter Medium", 9.5f),
                    ForeColor = Colors.textSubtle,
                    FillColor = Color.FromArgb(120, 38, 26, 38),
                    BorderThickness = 0,
                    BorderRadius = 9,
                    Height = 38,
                    Location = new Point(0, y),
                    Width = Math.Max(60, _listPanel.Width),
                    Cursor = row.link != null ? Cursors.Hand : Cursors.Default,
                    UseTransparentBackground = true,
                    // ImageAlign ET TextAlign du meme cote : Left/Right ferait
                    // chevaucher l'icone et le texte.
                    ImageAlign = HorizontalAlignment.Left,
                    ImageOffset = new Point(10, 0),
                    ImageSize = new Size(16, 16),
                    Image = row.icon == null ? null : Utils.ChangeIconsColor(new Bitmap(row.icon), row.color),
                };
                b.HoverState.FillColor = Color.FromArgb(170, 52, 36, 52);
                if (row.link != null)
                {
                    string link = row.link;
                    b.Click += (s, e) => { try { _utils.OpenLink(link); } catch { } };
                }
                y += b.Height + 8;
            }
            _listPanel.Height = Math.Max(0, y - 8);
        }

        // ---------- mise en page (UNE seule source de verite) ----------
        private new void Layout()
        {
            if (_root == null || _infoCard == null) return;

            int hostW = (Parent != null && Parent.Width > 320) ? Parent.Width : Width;
            int contentW = Math.Max(560, hostW) - Pad * 2;

            // Colonne media un peu plus large que la colonne texte : la galerie
            // porte l'attrait visuel, le texte se lit tres bien plus etroit.
            int leftW = (int)(contentW * 0.56);
            int rightW = contentW - leftW - Gap;

            _root.Location = new Point(0, 0);
            _root.Width = Math.Max(hostW, contentW + Pad * 2);

            // --- en-tete ---
            int y = Pad;
            _logoBox.Location = new Point(Pad, y);
            _title.Location = new Point(_logoBox.Right + 16, y + 6);
            _kicker.Location = new Point(_logoBox.Right + 16, y + 6 + _title.Height + 1);
            int headerBottom = Math.Max(_logoBox.Bottom, _kicker.Bottom);

            y = headerBottom + 22;

            // --- colonne gauche : preview 16/9 + vignettes ---
            int previewH = (int)(leftW * 9f / 16f);
            _preview.Location = new Point(Pad, y);
            _preview.Size = new Size(leftW, previewH);
            _expandBtn.Location = new Point(_preview.Right - _expandBtn.Width - 14, _preview.Bottom - _expandBtn.Height - 14);
            _expandBtn.BringToFront();

            int count = Math.Max(1, _thumbs.Count);
            int thumbW = (leftW - ThumbGap * (count - 1)) / count;
            int thumbH = (int)(thumbW * 9f / 16f);
            for (int i = 0; i < _thumbs.Count; i++)
            {
                _thumbs[i].Location = new Point(Pad + i * (thumbW + ThumbGap), _preview.Bottom + ThumbGap);
                _thumbs[i].Size = new Size(thumbW, thumbH);
            }
            int leftBottom = _thumbs.Count > 0 ? _thumbs[0].Bottom : _preview.Bottom;

            // --- colonne droite : carte d'infos ---
            int cardX = Pad + leftW + Gap;
            int inner = 18;
            int innerW = rightW - inner * 2;

            _infoCard.Location = new Point(cardX, y);
            _infoCard.Width = rightW;

            // onglets
            int tx = inner;
            foreach (var b in _tabBtns)
            {
                // +34 et non +18 : Guna2Button applique sa propre marge interne, une
                // largeur calee sur la seule mesure du texte tronquait les libelles
                // ("About" -> "Abou", "Features" -> "Feature").
                int w = TextRenderer.MeasureText(b.Text, b.Font).Width + 34;
                b.Location = new Point(tx, inner);
                b.Width = w;
                tx += w + 6;
            }
            var active = _tabBtns[_activeTab];
            _tabUnderline.Location = new Point(active.Left + 6, active.Bottom + 4);
            _tabUnderline.Width = Math.Max(20, active.Width - 12);

            int cy = _tabUnderline.Bottom + 18;

            // corps : About = texte mesure, sinon liste
            if (_activeTab == 0)
            {
                _body.Location = new Point(inner, cy);
                _body.Width = innerW;
                // Mesure REELLE du texte replie : c'est ce qui garantit qu'aucun
                // element place dessous ne viendra le chevaucher.
                //
                // On mesure sur une largeur legerement INFERIEURE a celle du Label
                // et on ajoute une ligne de marge : MeasureText et le rendu interne
                // du Label ne replient pas toujours au meme mot, et un ecart d'un
                // seul mot suffisait a faire disparaitre la derniere puce.
                Size measured = TextRenderer.MeasureText(
                    _description, _body.Font, new Size(innerW - 6, int.MaxValue),
                    TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
                _body.Height = measured.Height + _body.Font.Height + 4;
                _body.Text = _description;
                cy = _body.Bottom;
            }
            else
            {
                _listPanel.Location = new Point(inner, cy);
                _listPanel.Width = innerW;
                foreach (Control row in _listPanel.Controls) row.Width = innerW;
                cy = _listPanel.Bottom;
            }

            // meta
            cy += 20;
            _metaLine.Location = new Point(inner, cy);
            _metaLine.Width = innerW;
            cy = _metaLine.Bottom + 12;

            _versionKey.Location = new Point(inner, cy);
            _versionVal.Location = new Point(inner + innerW - _versionVal.Width, cy);
            cy += Math.Max(_versionKey.Height, _versionVal.Height) + 8;

            _updateKey.Location = new Point(inner, cy);
            _updateVal.Location = new Point(inner + innerW - _updateVal.Width, cy);
            cy += Math.Max(_updateKey.Height, _updateVal.Height) + 20;

            // action
            _launch.Location = new Point(inner, cy);
            _launch.Width = innerW;
            cy = _launch.Bottom + 10;

            _report.Location = new Point(inner + innerW - _report.Width, cy);
            cy = _report.Bottom + inner;

            _infoCard.Height = cy;

            // --- hauteurs globales ---
            int totalH = Math.Max(leftBottom, _infoCard.Bottom) + Pad;
            _root.Height = totalH;
            if (Height != totalH) Height = totalH;
            int totalW = Math.Max(hostW, _infoCard.Right + Pad);
            if (Width != totalW) Width = totalW;
            _root.Size = new Size(totalW, totalH);

            LayoutOverlay();
        }

        private void LayoutOverlay()
        {
            if (_overlay == null) return;
            _overlay.Location = new Point(0, 0);
            _overlay.Size = new Size(Width, Height);

            int w = (int)(Width * 0.82), h = (int)(Height * 0.78);
            _overlayImage.Size = new Size(w, h);
            _overlayImage.Location = new Point((Width - w) / 2, (Height - h) / 2);
            if (_overlay.Tag is Guna2CircleButton close)
            {
                close.Location = new Point(_overlayImage.Right - close.Width, _overlayImage.Top - close.Height - 8);
                close.BringToFront();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
        }
    }
}
