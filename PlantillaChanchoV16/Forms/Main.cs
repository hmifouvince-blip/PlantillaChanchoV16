using CuoreUI;
using CuoreUI.Components;
using Guna.UI2.WinForms;
using PlantillaChanchoV16.Products;
using PlantillaChanchoV16.Template;
using PlantillaChanchoV16.Utilities;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using static PlantillaChanchoV16.Utilities.Utils;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using Timer = System.Windows.Forms.Timer;
namespace PlantillaChanchoV16
{

    public partial class Main : Form
    {

        public Guna2Panel _mainOverlay;




        private bool _expandedViewProducts;
        public static bool _btnHomePressed, _btnAllProductsPressed, _btnUserDataPressed;

        private Images images = new Images();
        private Utils utils = new Utils();
        private GlobalKeyHook globalKeyHook;

        private List<Guna2Panel> views = new List<Guna2Panel>();
        private Guna2Button lastSelectedButton = null;
        private Guna2Button _btnTabHome, _btnTabProducts, _btnTabUserData, _btnClaimKey;
        // Icônes d'onglets déjà teintées (blanc actif / gris inactif), calculées UNE fois
        // par bouton -> plus d'allocation de 3 bitmaps (ni de fuite de l'ancienne image) à
        // chaque changement d'onglet.
        private readonly Dictionary<Guna2Button, (Image active, Image inactive)> _tabIconCache
            = new Dictionary<Guna2Button, (Image active, Image inactive)>();
        // La cascade d'apparition des produits ne se joue qu'à la 1re ouverture de la grille
        // (sur les visites suivantes : affichage instantané, pas de ré-animation perçue comme
        // une lenteur).
        private bool _productsEntrancePlayed;
        private Template.WelcomeBanner _welcomeBanner;
        private Guna2Panel containerAllProducts, _containerNav, _containerMain, _containerSpecialProduct, _containerFavProducts, _containerForUserAccount, _containerUserAccount;

        private Guna2HtmlLabel _titleView, _countAllProducts, _titleViewUserAccount, _btnViewAllProducts, _loadingLabelView;
        private Label viewLabel, _creditText;

        private Guna2CircleProgressBar _progressBar;
        private Template.SakuraLoadingScreen _sakuraOverlay;
        private Guna2CircleButton _btnPrevious, _btnNext;

        private Guna2PictureBox _iconLogo;
        private Guna2Panel _contentLogo, _contentTabButtons;

        private Guna2Panel _pIP, _pHWID, _pCurrentHwid, _pCreateAt, _pLastLogin, _pStatus;
        // Une ligne par produit du catalogue, montrant le temps restant ("Not claimed" si
        // l'utilisateur ne possède pas encore la licence) -> reflète le modèle multi-clés.
        private List<Guna2Panel> _pLicenseRows = new List<Guna2Panel>();
        private Guna2VScrollBar _userScrollBar;
        // Page compte "premium" : carte profil (avatar + nom + sous-titre) en tête.
        private Guna2Panel _profileHeader;
        private Guna2HtmlLabel _profileNameLabel;
        private Guna2HtmlLabel _detailsHeader;

        private AddProduct productManager;
        private DetailsProduct detailsForm;
        private ProductManager gameDetailsFactory = new ProductManager();
        private Guna2Panel _contentForDetailsForm;
    

        private static Guna2Panel overlayModal;
        private Form _currentForm = null;


        private Timer processCheckTimer;

        private Default DefaultForm;





        // Vrai si cette fenêtre doit s'ouvrir en plein écran (voir ToggleMaximize / la
        // fenêtre n'a pas de bordure redimensionnable : le "plein écran" construit une
        // fenêtre normale directement DIMENSIONNÉE à la zone de travail de l'écran, plutôt
        // que d'essayer de redimensionner après coup tout le contenu déjà construit -> tous
        // les calculs de mise en page existants (déjà basés sur this.Width/this.Height)
        // s'appliquent naturellement, sans code de "relayout" séparé et fragile à maintenir.
        private bool _startMaximized;

        public Main(bool startMaximized = false)
        {
            _startMaximized = startMaximized;

            // Ces indicateurs sont `static` -> ils persistent d'une instance de Main à
            // l'autre (RebuildForTheme crée un NOUVEAU Main à chaque changement de thème/
            // langue). Sans ce reset, une instance précédente qui avait déjà "pressé" Home
            // laisserait ActivateHomeTab() ci-dessous ne rien faire (son garde-fou
            // `if (_btnHomePressed) return;` bloquerait à tort l'initialisation de la
            // nouvelle fenêtre).
            _btnHomePressed = false;
            _btnAllProductsPressed = false;
            _btnUserDataPressed = false;

            InitializeComponent();
            ConfigureFormSettings();
            InitializeBorderlessForm();
            InitializeOverlayModal();

            // Inicializaci�n de componentes principales
            CreateItemsView();
            CreateProductContainer(false);
            InitializeProductManager();
            SpecialProduct();
            BuildHomeCarousel();
            AddCircularProgressBar();
            AddLogoNav(_containerNav);
            GenerateInterface(_containerNav);

            // Configuraciones de utilidades
            ConfigureUtils();

            AdjustContainerHeights();
            ManageOverlayOrder();

            AddWindowControls();

            // Relook "gaming hub" (Omen / Razer Cortex) : degrades sombres + halo d'accent.
            // 100% visuel, applique par-dessus les panneaux existants.
            ApplyOmenStyle();

            // Initialise l'accueil comme vue de départ (carrousel inclus) -> sans cet appel,
            // le premier affichage restait sur l'ancien état par défaut (grille) jusqu'à ce
            // que l'utilisateur change manuellement d'onglet.
            ActivateHomeTab();

            // (La cascade d'apparition des produits se joue à la 1re ouverture de l'onglet
            // "Products", pas au démarrage : au lancement l'accueil affiche le carrousel, la
            // grille est cachée -> animer une vue cachée ne servait à rien.)

            // Une fois la fenêtre affichée, préchauffe en arrière-plan le cache des fiches
            // produit (leur construction est lourde -> sans ça le 1er clic sur un produit
            // rame). Après ce préchauffage, toutes les ouvertures sont instantanées.
            this.Shown += (s, e) => WarmProductDetailsCache();

            // Met les animations en pause dès que PaiPai n'est pas au premier plan (ou minimisé)
            // -> zéro conso quand tu joues.
            this.Activated += (s, e) => Utilities.AnimationHub.Focused = true;
            this.Deactivate += (s, e) => { Utilities.AnimationHub.Focused = false; TrimMemory(); };

            DefaultForm = new Default();
            if (!DefaultForm.TestMode)
            {
                InitializeProcessCheckTimer();
                ProcessChecker.ShowDetectedPrograms();
            }
        }


        // SPECIAL PRODUCT  ->  Bannière d'accueil sakura (remplace le produit vedette Valorant)
        private void SpecialProduct()
        {
            string username = Login.KeyAuthApp?.user_data?.username;

            var welcome = new WelcomeBanner(
                width: _containerSpecialProduct.Width - 33,
                height: _containerSpecialProduct.Height - 17,
                username: username
            )
            {
                Location = new Point(15, 15),
                Parent = _containerSpecialProduct
            };
            _welcomeBanner = welcome; // pour que l'onglet "Claim Key" ouvre le même dialogue
            // Après un claim de clé réussi, la page User (temps restant par produit) doit
            // refléter le nouvel abonnement sans que l'utilisateur ait à se reconnecter.
            welcome.LicenseClaimed += () => RefreshUserInfo();
        }

        private Template.ProductCarousel _homeCarousel;

        // Carrousel "présentation rapide" (façon sekai.one) affiché sur l'accueil, sous la
        // bannière : cartes-affiches qui glissent, une présentation plus visuelle/pro que la
        // grille compacte (celle-ci reste utilisée par l'onglet "Products", avec défilement).
        private void BuildHomeCarousel()
        {
            int contentW = this.Width - ContentSideMargin * 2;

            _homeCarousel = new Template.ProductCarousel(
                width: contentW,
                titleAreaHeight: 32,
                cardW: 158, cardH: 182, gap: 14, visibleCount: 3,
                title: Localization.T("home.carousel_title"),
                arrowLeft: Utils.ChangeIconsColor(new Bitmap(Images.ArrowIcon), Color.White),
                arrowRight: Utils.ChangeIconsColor(new Bitmap(Images.ArrowRIcon), Color.White))
            {
                Parent = this,
                Visible = false,
            };
            _homeCarousel.Location = new Point(ContentSideMargin, _containerSpecialProduct.Bottom + ContentTopGap);

            _homeCarousel.AddCard(images.Img2Anydesk, "Spoofer", () => OpenGameDetails("anydesk"));
            _homeCarousel.AddCard(images.BgValorant, "Valorant", () => OpenGameDetails("valorant"));
            _homeCarousel.AddCard(images.BgRoblox, "Roblox", () => OpenGameDetails("roblox"));
            _homeCarousel.AddCard(images.Img2Anydesk, "Windows PaiPai", () => OpenGameDetails("windowspai"));
        }








        private void InitializeProcessCheckTimer()
        {
            processCheckTimer = new Timer();
            processCheckTimer.Interval = 10000;
            processCheckTimer.Tick += async (sender, e) => await ProcessCheckTimer_TickAsync();
            processCheckTimer.Start();
        }

        private async Task ProcessCheckTimer_TickAsync()
        {
            await Task.Run(() =>
            {
                ProcessChecker.CheckForProcesses();
            });
        }

        private void ConfigureFormSettings()
        {
            if (_startMaximized)
            {
                // Fenêtre normale (WindowState reste Normal) mais DIMENSIONNÉE à toute la
                // zone de travail de l'écran -> visuellement plein écran (respecte la barre
                // des tâches), sans les pièges de WindowState.Maximized sur une fenêtre sans
                // bordure. Tous les calculs de mise en page ci-dessous lisent this.Width/
                // this.Height, donc ils s'appliquent naturellement à cette taille.
                var wa = Screen.PrimaryScreen.WorkingArea;
                this.StartPosition = FormStartPosition.Manual;
                this.Location = wa.Location;
                this.Width = wa.Width;
                this.Height = wa.Height;
            }
            else
            {
                int sizeGeneral = 100;
                this.Width = 928 - sizeGeneral;
                this.Height = 642 - sizeGeneral;
            }
            this.BackColor = Colors.bgColor;
            this.FormBorderStyle = FormBorderStyle.None;
        }

        private Guna2BorderlessForm _borderlessForm;

        private void InitializeBorderlessForm()
        {
            _borderlessForm = new Guna2BorderlessForm
            {
                // Coins carrés en plein écran (comme la plupart des applis modernes qui
                // remplissent l'écran), arrondis en taille normale.
                BorderRadius = _startMaximized ? 0 : Default.borderForms,
                ContainerControl = this,
                HasFormShadow = false
            };
        }

        private void InitializeOverlayModal()
        {
            overlayModal = new Guna2Panel
            {
                Parent = this,
                BackColor = Color.Transparent,
                FillColor = Color.FromArgb(200, 0, 0, 0),
                UseTransparentBackground = true,
                Size = this.Size,
                Visible = false,
                Location = new Point(0, 0),
            };
        }

        private void InitializeProductManager()
        {
            productManager = new AddProduct(
                _containerMain,
                _containerSpecialProduct,
                _containerFavProducts,
                _countAllProducts,
                OpenGameDetails,
                images
            );
            productManager.InitializeProducts();
        }

        private void ConfigureUtils()
        {
            EnableDoubleBuffering(this);
            utils.EnableDragControlInGuna2Panels(this);
            utils.DisableSelectionInGuna2HtmlLabels(this);

            globalKeyHook = new Utils.GlobalKeyHook(this);
        }

        private void AdjustContainerHeights()
        {
            // Le rail est devenu une barre horizontale de hauteur fixe (NavBarHeight) : elle
            // ne s'étire plus sur toute la hauteur de la fenêtre, rien à ajuster ici.
        }

        // Applique l'habillage visuel facon Omen Gaming Hub / Razer Cortex :
        // - zone centrale + page compte : degrade sombre + halo d'accent
        // - barre laterale : degrade "rail" + lisere d'accent + glow logo
        // Purement graphique (aucune logique / donnee modifiee).
        private void ApplyOmenStyle()
        {
            UiStyle.AttachContentBackdrop(_containerMain);
            if (_containerForUserAccount != null)
                UiStyle.AttachContentBackdrop(_containerForUserAccount);

            // La barre laterale doit laisser voir son degrade : on rend ses deux
            // sous-panneaux transparents (ils etaient des aplats bgColor opaques).
            if (_contentLogo != null)
            {
                _contentLogo.FillColor = Color.Transparent;
                _contentLogo.UseTransparentBackground = true;
            }
            if (_contentTabButtons != null)
            {
                _contentTabButtons.FillColor = Color.Transparent;
                _contentTabButtons.UseTransparentBackground = true;
            }
            UiStyle.AttachTopBar(_containerNav);

            // Etat initial : Home est l'onglet actif au démarrage -> le premier changement
            // d'onglet réinitialise bien Home via le chemin lastSelectedButton.
            if (lastSelectedButton == null && _btnTabHome != null)
                lastSelectedButton = _btnTabHome;

            _containerNav.Invalidate();
            _containerMain.Invalidate();
        }

        private void ManageOverlayOrder()
        {
            _containerSpecialProduct.SendToBack();
            overlayModal.BringToFront();
        }


        








        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x84;
            const int HTCLIENT = 1;
            const int HTCAPTION = 2;
            const int WM_SYSCOMMAND = 0x112;
            const int SC_MAXIMIZE = 0xF030;

            if (m.Msg == WM_SYSCOMMAND && (m.WParam.ToInt32() & 0xFFF0) == SC_MAXIMIZE)
            {
                return;
            }

            base.WndProc(ref m);

            if (m.Msg == WM_NCHITTEST)
            {
                m.Result = (IntPtr)HTCAPTION;
            }
        }






        private void EnableDoubleBuffering(Control control)
        {
            if (control == null)
                return;

            control.GetType()
                .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(control, true, null);

            control.GetType()
                .GetMethod("SetStyle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(control, new object[]
                {
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer, true
                });

            foreach (Control child in control.Controls)
            {
                EnableDoubleBuffering(child);
            }
        }

        // Gèle le rendu de la fenêtre le temps d'une bascule de vue (plusieurs Visible/
        // BringToFront enchaînés), puis dégèle et repeint une seule fois -> zéro
        // scintillement, transition instantanée. Toujours appelé en try/finally pour ne
        // jamais laisser la fenêtre gelée en cas d'exception.
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int msg, bool wParam, int lParam);
        private const int WM_SETREDRAW = 0x000B;

        private void SuspendDrawing()
        {
            if (!IsHandleCreated) return;
            SendMessage(this.Handle, WM_SETREDRAW, false, 0);
        }

        private void ResumeDrawing()
        {
            if (!IsHandleCreated) return;
            SendMessage(this.Handle, WM_SETREDRAW, true, 0);
            this.Invalidate(true);
        }




        // Barre de boutons fenêtre (haut-droite) : Settings, Réduire, Plein écran, Fermer.
        private Template.WindowButton _maxBtn;
        private Guna2Panel _windowControlsHost;

        private void AddWindowControls()
        {
            var host = new Guna2Panel
            {
                Parent = this,
                Size = new Size(121, 28),
                FillColor = Color.Transparent,
                BackColor = Color.Transparent,
                BorderThickness = 0,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            // Centrés verticalement dans la barre de nav (désormais horizontale, en haut).
            host.Location = new Point(this.Width - host.Width - ContentSideMargin, (NavBarHeight - host.Height) / 2);
            _windowControlsHost = host;

            var settings = new Template.WindowButton(Template.WindowButton.Glyph.Settings, Colors.mainColor) { Parent = host, Location = new Point(0, 0) };
            var mini = new Template.WindowButton(Template.WindowButton.Glyph.Minimize, Colors.mainColor) { Parent = host, Location = new Point(31, 0) };
            _maxBtn = new Template.WindowButton(
                _startMaximized ? Template.WindowButton.Glyph.Restore : Template.WindowButton.Glyph.Maximize,
                Colors.mainColor)
            { Parent = host, Location = new Point(62, 0) };
            var close = new Template.WindowButton(Template.WindowButton.Glyph.Close, Color.FromArgb(255, 95, 87)) { Parent = host, Location = new Point(93, 0) };

            settings.Clicked += (s, e) =>
            {
                using (var dlg = new Template.SakuraSettingsDialog())
                {
                    dlg.ShowDialog(this);
                    if (dlg.ThemeChanged || dlg.LanguageChanged) RebuildForTheme();
                }
            };
            mini.Clicked += (s, e) => this.WindowState = FormWindowState.Minimized;
            _maxBtn.Clicked += (s, e) => ToggleMaximize();
            close.Clicked += (s, e) => Environment.Exit(0);

            host.BringToFront();

            AddVpnStatusChip(host);
        }

        // Bascule plein écran / taille normale. La fenêtre est sans bordure (pas de poignée
        // de redimensionnement) : plutôt que d'essayer de redimensionner APRÈS COUP tout le
        // contenu déjà construit (fragile, beaucoup de mise en page à retoucher à la main,
        // source de bugs), on reconstruit la fenêtre directement à la bonne taille -> tous
        // les calculs de mise en page (déjà basés sur this.Width/this.Height) s'appliquent
        // naturellement, exactement comme au premier lancement. Même mécanisme que
        // RebuildForTheme (session KeyAuth statique -> pas de reconnexion).
        private void ToggleMaximize()
        {
            bool goingMaximized = !_startMaximized;
            _rebuildingTheme = true;
            var nm = new Main(startMaximized: goingMaximized);
            nm.Show();
            this.Close();
        }

        private Guna2Panel _vpnDot;
        private Guna2HtmlLabel _vpnChipLabel;
        private Timer _vpnPollTimer;

        // Pastille de statut VPN dans le chrome de la fenêtre (façon "dashboard" pro) :
        // point vert/gris + "VPN", cliquable pour ouvrir Windows PaiPai (même verrou de
        // licence que le reste). Sondage léger, suspendu quand l'appli est en arrière-plan
        // (même philosophie perf que le reste de l'app).
        private void AddVpnStatusChip(Guna2Panel windowButtonsHost)
        {
            var chip = new Guna2Panel
            {
                Parent = this,
                Size = new Size(78, 26),
                FillColor = Colors.scColor,
                BorderColor = Color.FromArgb(40, 255, 255, 255),
                BorderThickness = 1,
                BorderRadius = 13,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                // Sans ça, les 4 coins du rectangle englobant (hors de l'arrondi) s'affichent
                // dans une couleur plate au lieu de laisser voir le degrade de la nav bar
                // derriere -> petits carres qui depassent de la pastille arrondie.
                UseTransparentBackground = true,
            };
            chip.Location = new Point(windowButtonsHost.Left - chip.Width - 10, windowButtonsHost.Top + 1);

            _vpnDot = new Guna2Panel
            {
                Parent = chip,
                Size = new Size(8, 8),
                BorderRadius = 4,
                FillColor = Color.FromArgb(140, 255, 255, 255),
                Location = new Point(12, (chip.Height - 8) / 2),
                UseTransparentBackground = true,
            };

            _vpnChipLabel = new Guna2HtmlLabel
            {
                Parent = chip,
                Text = Localization.T("main.vpn_label"),
                Font = new Font("Inter Semibold", 8.5f),
                ForeColor = Color.FromArgb(190, 255, 255, 255),
                AutoSize = true,
                BackColor = Color.Transparent,
            };
            _vpnChipLabel.Location = new Point(26, (chip.Height - _vpnChipLabel.Height) / 2);

            void OpenVpn()
            {
                if (!RequireLicense("windowspai")) return;
                using (var scr = new Template.WindowsPaiScreen())
                    scr.ShowDialog(this);
                PollVpnStatus();
            }
            chip.Click += (s, e) => OpenVpn();
            _vpnDot.Click += (s, e) => OpenVpn();
            _vpnChipLabel.Click += (s, e) => OpenVpn();

            chip.BringToFront();

            _vpnPollTimer = new Timer { Interval = 8000 };
            _vpnPollTimer.Tick += (s, e) => { if (Utilities.AnimationHub.Active) PollVpnStatus(); };
            _vpnPollTimer.Start();
            PollVpnStatus();
        }

        private void PollVpnStatus()
        {
            Task.Run(() =>
            {
                bool connected = Utilities.WarpVpn.IsInstalled() && Utilities.WarpVpn.IsConnected();
                try
                {
                    if (!IsHandleCreated || IsDisposed) return;
                    BeginInvoke(new Action(() =>
                    {
                        if (_vpnDot == null || _vpnDot.IsDisposed) return;
                        _vpnDot.FillColor = connected ? Color.FromArgb(255, 76, 217, 100) : Color.FromArgb(140, 255, 255, 255);
                        _vpnChipLabel.ForeColor = connected ? Color.FromArgb(230, 255, 255, 255) : Color.FromArgb(190, 255, 255, 255);
                        _vpnDot.Invalidate();
                    }));
                }
                catch { }
            });
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);

        // Vide le working set -> la RAM affichée chute quand PaiPai est en arrière-plan (jeu).
        // Windows recharge les pages à la demande au retour au premier plan.
        private static void TrimMemory()
        {
            try
            {
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
                SetProcessWorkingSetSize(System.Diagnostics.Process.GetCurrentProcess().Handle, -1, -1);
            }
            catch { }
        }

        private void AddCircularProgressBar()
        {
            _progressBar = new Guna2CircleProgressBar
            {
                Size = new Size(100, 100),
                ProgressColor = Colors.mainColor,
                ProgressColor2 = Colors.mainColor,
                FillColor = Colors.scColor,

                ProgressStartCap = LineCap.Round,
                ProgressEndCap = LineCap.Round,
                FillThickness = 6,
                ProgressThickness = 6,

                BackColor = Color.Transparent,
                Value = 20,
                Animated = true,
                AnimationSpeed = 2f,
            };
            _progressBar.Location = new Point((_mainOverlay.Width - _progressBar.Width) / 2, (_mainOverlay.Height - _progressBar.Height - 60) / 2);

            _loadingLabelView = new Guna2HtmlLabel
            {
                Text = "Loading Data...",
                ForeColor = ColorTranslator.FromHtml("#878BA6"),
                BackColor = Color.Transparent,
                Font = new Font("Inter Medium", 12f, FontStyle.Regular),
                AutoSize = true,
            };

            _loadingLabelView.Location = new Point(
                _progressBar.Location.X + (_progressBar.Width - _loadingLabelView.Width) / 2,
                _progressBar.Location.Y + _progressBar.Height + 20
            );

            _mainOverlay.Controls.Add(_progressBar);
            _mainOverlay.Controls.Add(_loadingLabelView);

            // Écran de chargement sakura (utilisé à l'ouverture d'un produit), masqué par défaut.
            _sakuraOverlay = new Template.SakuraLoadingScreen(_mainOverlay.Width, _mainOverlay.Height, "PaiPai", "Loading product...")
            {
                Location = new Point(0, 0),
                Visible = false
            };
            _mainOverlay.Controls.Add(_sakuraOverlay);
        }






       

        

        private void ConfigureContainers(bool _isExpanded)
        {
            if (_isExpanded)
            {
                // Vue "Products" : la bannière d'accueil est masquée, le contenu remplit la
                // zone SOUS la barre de nav (avant : y=-1, hérité du rail à gauche -> le
                // contenu passait DERRIÈRE la barre du haut = vue "cassée").
                _containerMain.Location = new Point(_containerMain.Location.X, _containerNav.Bottom + ContentTopGap);
                _containerSpecialProduct.Visible = false;

                _btnNext.Visible = true;
                _btnPrevious.Visible = true;
                _btnViewAllProducts.Visible = false;

                _titleView.Location = new Point(0, 20);
                _countAllProducts.Location = new Point(_titleView.Right + 10, _titleView.Top + 6);

                _btnNext.Location = new Point(_containerMain.Right - _btnNext.Width - 110, 13);

                _btnPrevious.Location = new Point(_btnNext.Left - _btnPrevious.Width - 10, _btnNext.Top);

                viewLabel.Location = new Point(_btnNext.Right - viewLabel.Width, 470 );

                _creditText.Location = new Point(_titleView.Left, 470);

                this.Invalidate();
            }
            else
            {
                containerAllProducts.Height = _containerMain.Height;

                _containerMain.Location = new Point(_containerMain.Location.X, _containerSpecialProduct.Bottom);
                _containerSpecialProduct.Visible = true;

                _btnNext.Visible = false;
                _btnPrevious.Visible = false;
                _btnViewAllProducts.Visible = true;

                _titleView.Location = new Point(0, 15);
                _countAllProducts.Location = new Point(_titleView.Right + 10, _titleView.Top + 6);

                _btnNext.Location = new Point(_containerMain.Right - _btnNext.Width - 110, 11);

                _btnPrevious.Location = new Point(_btnNext.Left - _btnPrevious.Width - 10, _btnNext.Top);

            }
        }

        private void UpdateExpandedViewState()
        {
            // Vue "Products" quand le contenu remplit la zone sous la barre de nav ; vue
            // "Home" quand il est sous la bannière d'accueil. (Avant : comparaison à
            // _containerNav.Right = une largeur, héritée du rail à gauche -> cassée.)
            if (_containerMain.Location.Y == _containerNav.Bottom + ContentTopGap)
                _expandedViewProducts = true;
            else if (_containerMain.Location.Y == _containerSpecialProduct.Bottom)
                _expandedViewProducts = false;
        }

























        private void clickedBtnHome()
        {
            // Bascule instantanée : toutes les vues sont déjà construites et mises en cache,
            // afficher l'accueil = juste basculer des Visible + repositionner. Aucun travail
            // bloquant -> ni overlay de chargement ni Task.Delay (c'était ~240 ms de latence
            // perçue à chaque clic d'onglet, pour rien). Peintures groupées via
            // Suspend/ResumeDrawing pour éviter tout scintillement.
            SuspendDrawing();
            try
            {
                // On cache d'abord ce qui doit disparaître (limite le scintillement).
                _containerMain.Visible = false;
                _contentForDetailsForm.Visible = false;
                _btnNext.Visible = false;
                _btnPrevious.Visible = false;

                // Sur l'accueil, le carrousel "présentation rapide" remplace la grille
                // compacte (celle-ci reste utilisée par l'onglet "Products", avec défilement).
                productManager.ShowView("view1");
                _expandedViewProducts = false;
                ConfigureContainers(_expandedViewProducts);
                _containerSpecialProduct.Visible = true;

                if (_homeCarousel != null)
                {
                    int carouselY = _containerSpecialProduct.Bottom + ContentTopGap;
                    _homeCarousel.Location = new Point(ContentSideMargin, carouselY);
                    // Adapte la taille des cartes à l'espace RÉELLEMENT disponible (largeur +
                    // hauteur, mesuré ici à l'affichage -> fiable quel que soit le facteur
                    // DPI) : petites dans la fenêtre réduite, GRANDES en plein écran.
                    int availW = this.Width - ContentSideMargin * 2;
                    _homeCarousel.FitTo(availW, this.Height - carouselY - 16);
                    _homeCarousel.Visible = true;
                    _homeCarousel.BringToFront();
                }
            }
            finally
            {
                ResumeDrawing();
            }
        }

        // Corps de l'onglet "Home" (état + bascule de vue), extrait pour pouvoir être
        // appelé à la fois par le clic sur l'onglet ET une fois au démarrage (sinon
        // l'accueil affiché au lancement reste l'ancienne grille tant qu'on n'a pas changé
        // d'onglet manuellement -> le carrousel ne s'applique jamais tout seul).
        private void ActivateHomeTab()
        {
            if (_btnHomePressed) return;
            _btnHomePressed = true;
            _btnAllProductsPressed = false;
            _btnUserDataPressed = false;

            _containerFavProducts.Visible = false;
            _containerForUserAccount.Visible = false;
            _containerUserAccount.Visible = false;

            clickedBtnHome();

            HandleTabButtonClick(_btnTabHome);
            containerAllProducts.Height = _containerMain.Height;
        }

        private void clicked_btnViewAllProducts()
        {
            // Bascule instantanée (voir clickedBtnHome) : plus d'overlay ni de Task.Delay.
            SuspendDrawing();
            try
            {
                if (_homeCarousel != null) _homeCarousel.Visible = false;
                _contentForDetailsForm.Visible = false;

                _expandedViewProducts = true;
                ConfigureContainers(_expandedViewProducts);
                // Espace réellement visible sous la nav (vue "Products", sans bannière).
                productManager.SetViewportHeight(this.Height - _containerMain.Location.Y - 16);

                _btnNext.Visible = true;
                _btnPrevious.Visible = true;
                _containerMain.Visible = true;
            }
            finally
            {
                ResumeDrawing();
            }

            // Cascade d'apparition : uniquement à la 1re ouverture de la grille. Sur les
            // visites suivantes -> affichage instantané (pas de ré-animation qui donnerait
            // une impression de lenteur à chaque retour sur l'onglet).
            if (!_productsEntrancePlayed)
            {
                _productsEntrancePlayed = true;
                productManager.PlayProductsEntrance();
            }
        }



        private void clickedBtnViewUserData()
        {
            // Bascule instantanée (voir clickedBtnHome) : plus d'overlay ni de Task.Delay.
            SuspendDrawing();
            try
            {
                _containerMain.Visible = false;
                if (_homeCarousel != null) _homeCarousel.Visible = false;
                _contentForDetailsForm.Visible = false;
                _btnNext.Visible = false;
                _btnPrevious.Visible = false;

                _expandedViewProducts = false;
                ConfigureContainers(_expandedViewProducts);

                // La page compte recouvre entièrement la bannière : on masque celle-ci APRÈS
                // ConfigureContainers (qui la remet visible dans sa branche "accueil").
                _containerSpecialProduct.Visible = false;
                _containerForUserAccount.Visible = true;
                _containerUserAccount.Visible = true;
                _containerForUserAccount.BringToFront();
            }
            finally
            {
                ResumeDrawing();
            }
        }

        private void NavigateToNextView()
        {
            productManager.NavigateToNextView();
            UpdateViewLabel();
        }

        private void NavigateToPreviousView()
        {
            productManager.NavigateToPreviousView();
            UpdateViewLabel();
        }

        private void UpdateViewLabel()
        {
            if (viewLabel != null)
            {
                viewLabel.Text = $"Viewing page {productManager.CurrentViewIndex + 1} of {productManager.ViewCount}";
            }
        }






        private Guna2Panel CreateProductContainer(bool visible = true)
        {
            containerAllProducts = new Guna2Panel
            {
                BackColor = Color.Transparent,
                FillColor = Color.Transparent,
                AutoScroll = false,
                BorderThickness = 0,
                Visible = visible,
                BorderColor = Color.White,
                Width = _containerMain.Width,
                Height = _containerMain.Height - 100,
                AutoSize = false,
            };

            containerAllProducts.Location = new Point(0, _titleView.Bottom + 25);

            containerAllProducts.GetType()
                .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(containerAllProducts, true, null);

            containerAllProducts.GetType()
                .GetMethod("SetStyle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(containerAllProducts, new object[] {
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer, true
                });

            _containerMain.Controls.Add(containerAllProducts);
            views.Add(containerAllProducts);

            return containerAllProducts;
        }





























        public DateTime UnixTimeToDateTime(long unixTime)
        {
            try
            {
                // Verificar que el Unix Timestamp es v�lido (mayor o igual a 0)
                if (unixTime < 0)
                    throw new ArgumentOutOfRangeException(nameof(unixTime), "El Unix Timestamp no puede ser negativo.");

                // Convertir timestamp a DateTime
                DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                return dtDateTime.AddSeconds(unixTime).ToLocalTime();
            }
            catch (Exception ex)
            {
                // Manejo de errores: Devuelve una fecha predeterminada en caso de fallo
                Console.WriteLine($"Error al convertir Unix Timestamp: {ex.Message}");
                return new DateTime(1970, 1, 1);
            }
        }













        // Logo compact aligné à gauche de la barre (aligné avec la marge du contenu en
        // dessous) + zone des onglets juste après, horizontale, centrée verticalement.
        private void AddLogoNav(Guna2Panel parentPanel)
        {
            int logoSize = 34;
            _contentLogo = new Guna2Panel
            {
                Parent = _containerNav,
                FillColor = Colors.bgColor,
                Size = new Size(logoSize + 14, NavBarHeight),
                Location = new Point(ContentSideMargin - 7, 0),
                BorderThickness = 0,
            };

            // Logo rond qui tourne (remplacera le logo PaiPai fourni plus tard).
            var spinLogo = new Template.SpinningLogo(images.MainLogo, logoSize)
            {
                Parent = _contentLogo,
                Location = new Point((_contentLogo.Width - logoSize) / 2, (_contentLogo.Height - logoSize) / 2)
            };
            spinLogo.BringToFront();

            _contentLogo.BringToFront();

            _contentTabButtons = new Guna2Panel
            {
                Parent = _containerNav,
                FillColor = Colors.bgColor,
                Location = new Point(_contentLogo.Right + 14, 0),
                Size = new Size(this.Width - _contentLogo.Right - 250, NavBarHeight),
                BorderThickness = 0,
            };

            _contentTabButtons.BringToFront();
        }







        // Pilule horizontale icône + libellé (la largeur s'adapte au texte -> robuste au
        // changement de langue). Repos : icône/texte gris discret. Actif : pilule rose pleine
        // + icône/texte blancs. Survol : léger halo rose.
        private void TabButtonStyle(Guna2Panel parentPanel, Guna2Button button, Image iconTabButton, string label, Size imgSize, int extraPad = 40)
        {
            var font = new Font("Inter Semibold", 10f);
            // Largeur confortable = icône + texte mesuré + marge (les libellés FR/ES plus
            // longs, ex. "Activer une clé", ne doivent jamais toucher le bord de la pilule).
            int textW = TextRenderer.MeasureText(label, font).Width;
            int width = imgSize.Width + 10 + textW + extraPad;

            button.Size = new Size(width, 40);
            button.Text = label;
            button.Font = font;
            button.ForeColor = Color.FromArgb(150, 152, 168);
            button.BorderColor = Color.Transparent;
            button.BorderThickness = 1;
            button.AutoSize = false;
            button.FillColor = Color.Transparent;
            button.BackColor = Color.Transparent;
            button.UseTransparentBackground = true;
            button.BorderRadius = 12;
            button.Animated = true;
            button.Cursor = Cursors.Hand;
            button.Image = Utils.ChangeIconsColor(new Bitmap(iconTabButton), Color.FromArgb(140, 142, 158));
            button.ImageSize = imgSize;
            // Icône ET texte alignés du MÊME côté (Left/Left) : Guna2Button les enchaîne
            // alors nativement icône -> texte. La combinaison Left/Right (bords opposés)
            // n'est pas fiable et fait chevaucher l'icône sur le texte.
            button.ImageAlign = HorizontalAlignment.Left;
            button.ImageOffset = new Point(12, 0);
            button.TextAlign = HorizontalAlignment.Left;
            button.TextOffset = new Point(6, 0);

            // Survol : léger fond rose sakura (n'affecte pas l'onglet déjà actif car son
            // FillColor plein rose reste au-dessus).
            button.HoverState.FillColor = Color.FromArgb(40, Colors.mainColor);
            button.HoverState.BorderColor = Color.Transparent;
            button.HoverState.ForeColor = Color.FromArgb(220, 255, 255, 255);

            // L'état actif est piloté explicitement par SetTabActive (FillColor du bouton).
            // On neutralise le fond "focus" pour qu'il n'interfère pas avec ça.
            button.FocusedColor = Color.Transparent;

            button.DisabledState.Image = Utils.ChangeIconsColor(new Bitmap(iconTabButton), Color.FromArgb(120, 122, 138));


            button.Parent = parentPanel;
        }


        





        public void GenerateInterface(Guna2Panel parentContainer)
        {
            _btnTabHome = CreateTabButton(parentContainer, "tab1", images.IconTabHome, Localization.T("nav.home"), new Size(18, 18), () =>
            {
                try { ActivateHomeTab(); }
                catch (Exception ex)
                {
                    string errorMessage = $"Error: {ex.Message}\nStackTrace: {ex.StackTrace}";
                    File.WriteAllText("error.log", errorMessage);
                    PlantillaChanchoV16.Template.SakuraMessageBox.Show(
                        "An unexpected error occurred while processing the Home tab. Please try again later.",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            });

            _btnTabProducts = CreateTabButton(parentContainer, "tab2", images.IconTabProducts, Localization.T("nav.products"), new Size(18, 18), () =>
            {
                try
                {
                    if (!_btnAllProductsPressed)
                    {
                        _btnHomePressed = false;
                        _btnAllProductsPressed = true;
                        _btnUserDataPressed = false;

                        _containerFavProducts.Visible = false;
                        _containerForUserAccount.Visible = false;
                        _containerUserAccount.Visible = false;
                        containerAllProducts.Height = this.Height - 100;

                        clicked_btnViewAllProducts();
                        UpdateViewLabel();

                        HandleTabButtonClick(_btnTabProducts);
                        _btnTabHome.Checked = false;

                        this.Invalidate();
                    }
                }
                catch (Exception ex)
                {
                    string errorMessage = $"Error: {ex.Message}\nStackTrace: {ex.StackTrace}";
                    File.WriteAllText("error.log", errorMessage);
                    PlantillaChanchoV16.Template.SakuraMessageBox.Show(
                        "An unexpected error occurred while processing the Products tab. Please try again later.",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            });

            _btnTabUserData = CreateTabButton(parentContainer, "tab4", images.IconTabUserData, Localization.T("nav.account"), new Size(18, 18), () =>
            {
                try
                {
                    if (!_btnUserDataPressed)
                    {
                        _btnHomePressed = false;
                        _btnAllProductsPressed = false;
                        _btnUserDataPressed = true;

                        clickedBtnViewUserData();
                        HandleTabButtonClick(_btnTabUserData);
                        _btnTabHome.Checked = false;
                    }
                }
                catch (Exception ex)
                {
                    string errorMessage = $"Error: {ex.Message}\nStackTrace: {ex.StackTrace}";
                    File.WriteAllText("error.log", errorMessage);
                    PlantillaChanchoV16.Template.SakuraMessageBox.Show(
                        "An unexpected error occurred while processing the User Data tab. Please try again later.",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            });




            // Onglet-action "Claim Key" : ouvre directement la fenêtre de saisie de clé
            // (même logique que le bouton "Add license" de la bannière d'accueil). Ce n'est
            // pas une vue -> il ne devient pas l'onglet actif.
            _btnClaimKey = CreateTabButton(parentContainer, "tabClaim", images.KeyIcon, Localization.T("nav.claim_key"), new Size(16, 16), () =>
            {
                _welcomeBanner?.ShowClaimDialog();
            });

            // Le groupe d'onglets est ajouté DIRECTEMENT à _contentTabButtons (plus de panneau
            // AutoSize intermédiaire qui pouvait se réduire à 0 et masquer les boutons).
            var tabButtons = new[] { _btnTabHome, _btnTabProducts, _btnTabUserData, _btnClaimKey };
            var mainTabGroup = CreateTabButtonGroup(tabButtons);

            // En FR/ES certains libellés (ex. "Activer une clé") sont bien plus longs qu'en
            // EN -> le groupe peut déborder au-delà de l'espace réservé à droite (pastille
            // VPN + boutons fenêtre) et se retrouver caché derrière (pastille VPN par-dessus).
            // On réduit alors la marge interne de chaque bouton (jamais en dessous d'un
            // minimum confortable) pour que tout rentre dans l'espace réellement disponible,
            // au lieu d'une marge fixe calée sur la largeur du texte anglais.
            if (mainTabGroup.Width > _contentTabButtons.Width)
            {
                int overflow = mainTabGroup.Width - _contentTabButtons.Width;
                int newPad = Math.Max(16, 40 - (overflow / tabButtons.Length) - 4);
                TabButtonStyle(parentContainer, _btnTabHome, images.IconTabHome, Localization.T("nav.home"), new Size(18, 18), newPad);
                TabButtonStyle(parentContainer, _btnTabProducts, images.IconTabProducts, Localization.T("nav.products"), new Size(18, 18), newPad);
                TabButtonStyle(parentContainer, _btnTabUserData, images.IconTabUserData, Localization.T("nav.account"), new Size(18, 18), newPad);
                TabButtonStyle(parentContainer, _btnClaimKey, images.KeyIcon, Localization.T("nav.claim_key"), new Size(16, 16), newPad);
                mainTabGroup = CreateTabButtonGroup(tabButtons);
            }

            mainTabGroup.Parent = _contentTabButtons;
            mainTabGroup.Location = new Point(0, Math.Max(0, (_contentTabButtons.Height - mainTabGroup.Height) / 2));
            mainTabGroup.BringToFront();

            // Etat initial : Home actif (pilule rose portée par le bouton lui-même).
            SetTabActive(_btnTabHome);
        }



        // Range les onglets côte à côte (horizontal) au lieu d'empilés -> barre du haut.
        private Guna2Panel CreateTabButtonGroup(Guna2Button[] buttons)
        {
            int spacing = 6;
            int totalW = 0, maxH = 0;
            foreach (var b in buttons) { totalW += b.Width + spacing; maxH = Math.Max(maxH, b.Height); }
            totalW = Math.Max(0, totalW - spacing);

            var panel = new Guna2Panel
            {
                AutoSize = false,
                Padding = new Padding(0),
                FillColor = Color.Transparent,
                Height = maxH,
                Width = totalW,
                BorderThickness = 0,
                BorderColor = Color.Blue,
            };

            int currentXPosition = 0;

            foreach (var button in buttons)
            {
                button.Location = new Point(currentXPosition, 0);
                panel.Controls.Add(button);
                currentXPosition += button.Width + spacing;
            }

            return panel;
        }

        




        private Guna2Button CreateTabButton(Guna2Panel parentPanel, string tabName, Image iconOff, string label, Size imgSize, Action tabButtonClicked)
        {
            var button = new Guna2Button
            {

            };

            TabButtonStyle(parentPanel, button, iconOff, label, imgSize);

            button.Click += (s, e) =>
            {
                tabButtonClicked.Invoke();
            };

            return button;
        }



        // Onglet actif : pilule rose portée par le bouton LUI-MÊME (fond + bordure + icône/
        // texte blancs). Simple et robuste — pas de panneau superposé ni de transparence.
        private void HandleTabButtonClick(Guna2Button clickedButton)
        {
            SetTabActive(clickedButton);
        }

        private void SetTabActive(Guna2Button clickedButton)
        {
            // "Claim Key" est une action (ouvre un dialogue), pas une vue -> jamais actif.
            if (clickedButton == _btnClaimKey) return;

            foreach (var b in new[] { _btnTabHome, _btnTabProducts, _btnTabUserData })
            {
                if (b == null) continue;
                bool active = b == clickedButton;
                b.FillColor = active ? Colors.mainColor : Color.Transparent;
                b.BorderColor = active ? Colors.mainColor : Color.Transparent;
                b.ForeColor = active ? Color.White : Color.FromArgb(150, 152, 168);
                var icons = GetTabIcons(b);
                b.Image = active ? icons.active : icons.inactive;

                // Halo rose "premium" sous l'onglet actif (éteint sur les autres).
                b.ShadowDecoration.Enabled = active;
                if (active)
                {
                    b.ShadowDecoration.Color = Color.FromArgb(140, Colors.mainColor);
                    b.ShadowDecoration.Depth = 9;
                    b.ShadowDecoration.Shadow = new Padding(4);
                }
            }
            lastSelectedButton = clickedButton;
        }

        // Renvoie (et met en cache la 1re fois) les deux variantes teintées de l'icône du
        // bouton : blanche (onglet actif) et grise (inactif). Évite de réallouer/teinter un
        // bitmap à chaque clic (et de fuir l'ancienne image).
        private (Image active, Image inactive) GetTabIcons(Guna2Button b)
        {
            if (!_tabIconCache.TryGetValue(b, out var pair))
            {
                // b.Image de départ = silhouette grise (posée par TabButtonStyle) : son canal
                // alpha (la forme) est préservé par ChangeIconsColor, on peut donc en dériver
                // les deux teintes sans perte.
                using (var baseImg = new Bitmap(b.Image))
                {
                    pair = (
                        Utils.ChangeIconsColor(new Bitmap(baseImg), Color.White),
                        Utils.ChangeIconsColor(new Bitmap(baseImg), Color.FromArgb(140, 142, 158))
                    );
                }
                _tabIconCache[b] = pair;
            }
            return pair;
        }

























        // Carte "libellé + valeur" (liseré rose à gauche). cardWidth < 0 => pleine largeur du
        // parent (comportement historique) ; sinon largeur explicite -> permet une grille à
        // 2 colonnes sur la page compte.
        private Guna2Panel CreateTextBoxPanel(Control parent, string? tx, string labelText, Point location, int cardWidth = -1)
        {
            int w = cardWidth > 0 ? cardWidth : parent.Width - 30;

            var valueLabel = new Guna2HtmlLabel
            {
                Text = tx ?? string.Empty,
                Font = new Font("Inter Semibold", 9.8F),
                ForeColor = Color.White,
                AutoSize = true,
                TextAlignment = ContentAlignment.MiddleLeft,
                Location = new Point(0, 10),
                BackColor = Color.Transparent,
                BorderStyle = BorderStyle.None,
            };

            var labelForPanel = new Guna2HtmlLabel
            {
                Text = (labelText ?? "").TrimEnd(':').ToUpper(),
                Font = new Font("Inter Semibold", 8F),
                ForeColor = Colors.mainColor,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(2, 0)
            };

            var panelValueContainer = new Guna2Panel
            {
                Width = w - 2,
                Height = 46,
                Location = new Point(0, labelForPanel.Bottom + 5),
                FillColor = Colors.scColor,
                BorderColor = Colors.scColor,
                BorderRadius = 10,
                BorderThickness = 1,
                // Liseré rose sakura à gauche.
                CustomBorderThickness = new Padding(3, 0, 0, 0),
                CustomBorderColor = Colors.mainColor,
            };

            valueLabel.Location = new Point(14, (panelValueContainer.Height - valueLabel.Height) / 2);

            panelValueContainer.Controls.Add(valueLabel);

            // Survol premium : la carte s'éclaircit légèrement (retour visuel discret).
            Color baseFill = Colors.scColor;
            Color hoverFill = System.Windows.Forms.ControlPaint.Light(Colors.scColor, 0.35f);
            void SetHover(bool on) { panelValueContainer.FillColor = on ? hoverFill : baseFill; }
            panelValueContainer.MouseEnter += (s, e) => SetHover(true);
            panelValueContainer.MouseLeave += (s, e) => SetHover(false);
            valueLabel.MouseEnter += (s, e) => SetHover(true);
            valueLabel.MouseLeave += (s, e) => SetHover(false);

            var panel = new Guna2Panel
            {
                Size = new Size(w, panelValueContainer.Height + 25),
                Location = location,
                BackColor = Color.Transparent
            };

            panel.Controls.Add(labelForPanel);
            panel.Controls.Add(panelValueContainer);

            panel.Tag = valueLabel; // pour pouvoir mettre à jour la valeur (Actualiser)
            return panel;
        }






        private void CreateInfoUser()
        {
            string username = Login.KeyAuthApp?.user_data?.username ?? "Chancho Gamer";
            string ip = Login.KeyAuthApp?.user_data?.ip ?? "192.168.1.100";
            string hwid = Login.KeyAuthApp?.user_data?.hwid ?? "B3F9-4A2C-71D8-839E";

            string createdAt = UnixTimeToDateTime(long.TryParse(Login.KeyAuthApp?.user_data?.createdate, out long created) ? created : 0).ToString() ?? "2023-01-01 12:00:00";
            string lastLogin = UnixTimeToDateTime(long.TryParse(Login.KeyAuthApp?.user_data?.lastlogin, out long last) ? last : 0).ToString() ?? "2023-05-20 08:30:45";

            int margin = 12;
            int gap = 12;
            int contentW = _containerUserAccount.Width;
            int fullW = contentW - margin * 2;
            int colW = (fullW - gap) / 2;
            int colLeftX = margin;
            int colRightX = margin + colW + gap;

            // --- Carte profil (avatar + nom + sous-titre) ---
            _profileHeader = BuildProfileHeader(username, margin, _titleViewUserAccount.Bottom + 16, fullW);
            _containerUserAccount.Controls.Add(_profileHeader);

            // --- Libellé de section "DÉTAILS" ---
            _detailsHeader = new Guna2HtmlLabel
            {
                Parent = _containerUserAccount,
                Text = Localization.T("account.details"),
                Font = new Font("Inter Semibold", 8.5f),
                ForeColor = Color.FromArgb(150, 255, 255, 255),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(margin + 2, _profileHeader.Bottom + 16)
            };

            // --- Grille 2 colonnes des infos courtes ---
            int gridTop = _detailsHeader.Bottom + 8;

            int activeCount = 0;
            foreach (var e in Utilities.ProductCatalog.All)
                if (Utilities.LicenseGate.HasValidSubscription(e.SubscriptionName)) activeCount++;
            string statusText = activeCount > 0
                ? Localization.T("account.licenses_active", activeCount)
                : Localization.T("account.no_active");

            _pIP = CreateTextBoxPanel(_containerUserAccount, ip, Localization.T("main.field_ip"),
                new Point(colLeftX, gridTop), colW);
            _pStatus = CreateTextBoxPanel(_containerUserAccount, statusText, Localization.T("account.status_label"),
                new Point(colRightX, gridTop), colW);
            if (_pStatus.Tag is Guna2HtmlLabel stLbl)
                stLbl.ForeColor = activeCount > 0 ? Color.FromArgb(80, 220, 150) : Color.FromArgb(150, 152, 168);

            int row2Top = Math.Max(_pIP.Bottom, _pStatus.Bottom) + 10;
            _pCreateAt = CreateTextBoxPanel(_containerUserAccount, createdAt, Localization.T("main.field_created"),
                new Point(colLeftX, row2Top), colW);
            _pLastLogin = CreateTextBoxPanel(_containerUserAccount, lastLogin, Localization.T("main.field_lastlogin"),
                new Point(colRightX, row2Top), colW);

            // --- HWID en pleine largeur (valeurs longues) ---
            int hwidTop = Math.Max(_pCreateAt.Bottom, _pLastLogin.Bottom) + 10;
            _pHWID = CreateTextBoxPanel(_containerUserAccount, hwid, Localization.T("main.field_hwid_reg"),
                new Point(margin, hwidTop), fullW);
            _pCurrentHwid = CreateTextBoxPanel(_containerUserAccount, GetCurrentHwid(), Localization.T("main.field_hwid_cur"),
                new Point(margin, _pHWID.Bottom + 10), fullW);

            foreach (var panel in new[] { _pIP, _pStatus, _pCreateAt, _pLastLogin, _pHWID, _pCurrentHwid })
                panel.Parent = _containerUserAccount;

            CreateLicensesSection();
        }

        // Carte profil premium en tête de la page compte : avatar circulaire (dégradé rose +
        // initiale), nom d'utilisateur en grand, sous-titre "Membre PaiPai".
        private Guna2Panel BuildProfileHeader(string username, int x, int y, int width)
        {
            var header = new Guna2Panel
            {
                Location = new Point(x, y),
                Size = new Size(width, 92),
                FillColor = Colors.scColor,
                BorderRadius = 14,
                BorderThickness = 0,
                BackColor = Color.Transparent,
            };

            string initial = string.IsNullOrWhiteSpace(username) ? "P" : username.Trim().Substring(0, 1).ToUpper();
            int av = 56;
            var avatar = new Guna2Panel
            {
                Parent = header,
                Size = new Size(av, av),
                Location = new Point(18, (header.Height - av) / 2),
                FillColor = Color.Transparent,
                BackColor = Color.Transparent,
                BorderThickness = 0,
            };
            var fInitial = new Font("Inter Semibold", 20f);
            avatar.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var rc = new Rectangle(0, 0, av - 1, av - 1);
                using (var path = new GraphicsPath())
                {
                    path.AddEllipse(rc);
                    using (var br = new LinearGradientBrush(rc,
                        Colors.mainColor,
                        ControlPaint.Dark(Colors.mainColor, 0.15f),
                        LinearGradientMode.ForwardDiagonal))
                        g.FillPath(br, path);
                }
                TextRenderer.DrawText(g, initial, fInitial, rc, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            _profileNameLabel = new Guna2HtmlLabel
            {
                Parent = header,
                Text = username,
                Font = new Font("Inter Semibold", 15f),
                ForeColor = Color.White,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(avatar.Right + 18, 24),
            };

            var sub = new Guna2HtmlLabel
            {
                Parent = header,
                Text = Localization.T("account.member"),
                Font = new Font("Inter Medium", 9.5f),
                ForeColor = Color.FromArgb(160, 162, 178),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(avatar.Right + 18, 50),
            };

            // Fine bordure d'accent (même langage HUD que la bannière/les cartes).
            header.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(0, 0, header.Width - 1, header.Height - 1);
                using (var path = RoundedRect(r, 14))
                using (var pen = new Pen(Color.FromArgb(55, Colors.mainColor), 1f))
                    e.Graphics.DrawPath(pen, path);
            };

            return header;
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
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

        // Recale la hauteur du panneau défilant sur le bas réel de son contenu (le nombre de
        // lignes de licences varie) -> le calcul d'étendue de la scrollbar reste juste.
        // NB : on n'utilise PAS `c.Visible` (qui renvoie false tant que le conteneur parent
        // est caché, ex. au démarrage avant le 1er clic sur l'onglet -> la page restait vide
        // jusqu'à un Refresh). La scrollbar n'est pas un enfant de ce panneau.
        private void LayoutUserAccountHeight()
        {
            int bottom = 0;
            foreach (Control c in _containerUserAccount.Controls)
                bottom = Math.Max(bottom, c.Bottom);
            _containerUserAccount.Height = bottom + 20;
        }

        private Guna2HtmlLabel _licensesHeader;

        // Une ligne par produit du catalogue (Spoofer / Valorant / Roblox / Windows PaiPai...) :
        // temps restant si possédé, "Not claimed" sinon. Reconstruite après chaque claim de
        // clé pour refléter le modèle multi-licences (un compte peut posséder plusieurs
        // produits en même temps).
        private void CreateLicensesSection()
        {
            foreach (var row in _pLicenseRows) { _containerUserAccount.Controls.Remove(row); row.Dispose(); }
            _pLicenseRows.Clear();
            if (_licensesHeader != null) { _containerUserAccount.Controls.Remove(_licensesHeader); _licensesHeader.Dispose(); _licensesHeader = null; }

            int margin = 12;
            int fullW = _containerUserAccount.Width - margin * 2;
            int anchorBottom = _pCurrentHwid?.Bottom ?? (_detailsHeader?.Bottom ?? _titleViewUserAccount.Bottom);

            _licensesHeader = new Guna2HtmlLabel
            {
                Parent = _containerUserAccount,
                Text = Localization.T("main.my_licenses"),
                Font = new Font("Inter Semibold", 8.5f),
                ForeColor = Color.FromArgb(150, 255, 255, 255),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(margin + 2, anchorBottom + 22)
            };

            Guna2Panel lastRow = null;
            foreach (var entry in Utilities.ProductCatalog.All)
            {
                int y = (lastRow?.Bottom ?? _licensesHeader.Bottom + 6) + 10;
                var card = MakeLicenseCard(entry, margin, y, fullW);
                _containerUserAccount.Controls.Add(card);
                _pLicenseRows.Add(card);
                lastRow = card;
            }
        }

        // Carte de licence premium (pleine largeur) : nom du produit + temps restant, et une
        // pastille de statut à droite (ACTIVE vert / EXPIRÉE rouge / VERROUILLÉE gris).
        private Guna2Panel MakeLicenseCard(Utilities.ProductCatalog.Entry entry, int x, int y, int width)
        {
            var expiry = Utilities.LicenseGate.GetExpiry(entry.SubscriptionName);
            bool owned = false;
            var subs = Login.KeyAuthApp?.user_data?.subscriptions;
            if (subs != null)
                foreach (var s in subs)
                    if (string.Equals(s.subscription, entry.SubscriptionName, StringComparison.OrdinalIgnoreCase)) { owned = true; break; }

            Color statusColor; string statusText; string timeText;
            if (expiry.HasValue)
            {
                statusColor = Color.FromArgb(80, 220, 150);
                statusText = Localization.T("account.status_active");
                timeText = Utilities.LicenseGate.FormatTimeLeft(expiry.Value);
            }
            else if (owned)
            {
                statusColor = Color.FromArgb(235, 110, 110);
                statusText = Localization.T("account.status_expired");
                timeText = Localization.T("time.expired");
            }
            else
            {
                statusColor = Color.FromArgb(150, 152, 168);
                statusText = Localization.T("account.status_locked");
                timeText = Localization.T("main.not_claimed");
            }

            var card = new Guna2Panel
            {
                Location = new Point(x, y),
                Size = new Size(width, 58),
                FillColor = Colors.scColor,
                BorderRadius = 10,
                BorderThickness = 1,
                BorderColor = Colors.scColor,
                CustomBorderThickness = new Padding(3, 0, 0, 0),
                CustomBorderColor = statusColor,
                BackColor = Color.Transparent,
            };

            var name = new Guna2HtmlLabel
            {
                Parent = card,
                Text = entry.DisplayName,
                Font = new Font("Inter Semibold", 10.5f),
                ForeColor = Color.White,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(16, 11),
            };
            var time = new Guna2HtmlLabel
            {
                Parent = card,
                Text = timeText,
                Font = new Font("Inter Medium", 8.8f),
                ForeColor = Color.FromArgb(165, 167, 182),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(16, 33),
            };

            // Pastille de statut (tag arrondi) à droite, centrée verticalement.
            var fPill = new Font("Inter Semibold", 8f);
            int pillW = TextRenderer.MeasureText(statusText, fPill).Width + 22;
            var pill = new Guna2Panel
            {
                Parent = card,
                Size = new Size(pillW, 22),
                Location = new Point(width - pillW - 16, (58 - 22) / 2),
                FillColor = Color.FromArgb(38, statusColor),
                BorderRadius = 11,
                BorderThickness = 0,
                BackColor = Color.Transparent,
            };
            var pillLbl = new Label
            {
                Parent = pill,
                Text = statusText,
                Font = fPill,
                ForeColor = statusColor,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
            };

            card.Tag = time; // pour un éventuel refresh de la valeur
            return card;
        }

        private string GetCurrentHwid()
        {
            try { return System.Security.Principal.WindowsIdentity.GetCurrent().User.Value; }
            catch { return "Unknown"; }
        }

        // Actualise les infos affichées (nom, HWID actuel, valeurs du compte, statut, licences).
        private void RefreshUserInfo()
        {
            SetPanelValue(_pCurrentHwid, GetCurrentHwid());

            var u = Login.KeyAuthApp?.user_data;
            if (u != null)
            {
                if (_profileNameLabel != null) _profileNameLabel.Text = u.username;
                SetPanelValue(_pIP, u.ip);
                SetPanelValue(_pHWID, u.hwid);
            }

            // Recompte les licences actives pour la carte "Statut".
            int activeCount = 0;
            foreach (var e in Utilities.ProductCatalog.All)
                if (Utilities.LicenseGate.HasValidSubscription(e.SubscriptionName)) activeCount++;
            SetPanelValue(_pStatus, activeCount > 0
                ? Localization.T("account.licenses_active", activeCount)
                : Localization.T("account.no_active"));
            if (_pStatus?.Tag is Guna2HtmlLabel stLbl)
                stLbl.ForeColor = activeCount > 0 ? Color.FromArgb(80, 220, 150) : Color.FromArgb(150, 152, 168);

            CreateLicensesSection();
            LayoutUserAccountHeight();
            RefreshUserScrollExtent();
        }

        // Active/étend le défilement de la page compte selon la hauteur RÉELLE du contenu
        // (recalée par LayoutUserAccountHeight). Modèle ÉPROUVÉ (identique à la grille
        // produits) : Maximum = débordement, la Value va de 0 à Maximum, Top = -Value. On NE
        // touche PAS à LargeChange (selon le modèle du Guna2VScrollBar, un LargeChange non nul
        // peut rendre le bas du contenu inatteignable) -> défilement sûr jusqu'en bas.
        private void RefreshUserScrollExtent()
        {
            if (_userScrollBar == null) return;
            int userVisibleH = _containerForUserAccount.Height;
            int overflow = Math.Max(0, _containerUserAccount.Height - userVisibleH);
            _userScrollBar.SmallChange = 40;
            _userScrollBar.Maximum = overflow;
            _userScrollBar.Visible = overflow > 0;
            // Si le contenu a rétréci (moins de licences), on borne le décalage courant.
            if (-_containerUserAccount.Top > overflow)
            {
                _userScrollBar.Value = overflow;
                _containerUserAccount.Top = -overflow;
            }
        }

        private void SetPanelValue(Guna2Panel panel, string value)
        {
            if (panel?.Tag is Guna2HtmlLabel lbl) lbl.Text = value ?? string.Empty;
        }





        // Nav horizontale en haut (plus à gauche) : hauteur de la barre + marge laterale du
        // contenu en dessous. Constantes utilisées par toute la mise en page de Main.
        private const int NavBarHeight = 66;
        private const int ContentSideMargin = 20;
        private const int ContentTopGap = 14;

        private void CreateItemsView()
        {

            _containerNav = new Guna2Panel
            {
                CustomBorderThickness = new Padding(0,0,0,1),
                CustomBorderColor = Color.FromArgb(28,29,39),
                BorderThickness = 1,
                Width = this.Width,
                Height = NavBarHeight,
            };
            _containerNav.Location = new Point(0,0);

            int contentX = ContentSideMargin;
            int contentTop = _containerNav.Bottom + ContentTopGap;
            int contentW = this.Width - ContentSideMargin * 2;

            _containerSpecialProduct = new Guna2Panel
            {
                // Réduite (était 307) : avec la nav désormais en haut (prend de la hauteur),
                // une bannière aussi haute ne laissait presque plus de place visible pour la
                // liste de produits en dessous. 210 contient tout son contenu (le badge
                // licence se termine vers y=178) avec de la marge.
                Height = 210,
                Width = contentW,
                FillColor = Colors.bgColor,
                BorderColor = Color.Green,
                BorderThickness = 0,
                UseTransparentBackground = false,
                Visible = false,
            };
            _containerSpecialProduct.Location = new Point(contentX, contentTop);
            _containerSpecialProduct.Parent = this;



            _containerFavProducts = new Guna2Panel
            {
                Height = 500,
                Width = contentW,
                FillColor = Colors.bgColor,
                BorderColor = Color.YellowGreen,
                BorderThickness = 2,
                UseTransparentBackground = false,
                Visible = false,
            };
            _containerFavProducts.Location = new Point(contentX, contentTop);
            _containerFavProducts.Parent = this;



            _containerMain = new Guna2Panel
            {
                Width = contentW,
                Height = this.Height,
                BackColor = Color.White,
                FillColor = Colors.bgColor,
                BorderThickness = 0,
                BorderColor = Color.White,
                Padding = new Padding(0),
                UseTransparentBackground = false,
                Visible = true,
            };


            _containerMain.Location = new Point(contentX, contentTop);
            _containerMain.Parent = this;


            _containerForUserAccount = new Guna2Panel
            {
                Parent = this,
                Location = _containerMain.Location,
                FillColor = Colors.bgColor,
                BorderColor = Colors.bgColor,
                BorderThickness = 1,
                UseTransparentBackground = true,
                BackColor = Color.Transparent,
                Size = new Size(_containerMain.Width, _containerMain.Height),
                Visible = false,
            };

            _containerUserAccount = new Guna2Panel
            {
                Parent = _containerForUserAccount,
                Location = new Point(0,0),
                // Fond OPAQUE (pas transparent) : déplacer un panneau à fond transparent force
                // WinForms à recomposer le fond du parent (dégradé) à CHAQUE frame de scroll =
                // très saccadé. Un fond plein se déplace d'un bloc = défilement fluide.
                FillColor = Colors.bgColor,
                BorderColor = Colors.bgColor,
                BorderThickness = 0,
                UseTransparentBackground = false,
                BackColor = Colors.bgColor,
                Width = _containerForUserAccount.Width -30,
                // Hauteur pilotée EXPLICITEMENT par LayoutUserAccountHeight (au lieu d'AutoSize) :
                // le défilement (déplacement de .Top) est alors déterministe et l'étendue de la
                // scrollbar exacte -> plus de saccades/bugs liés à l'AutoSize pendant le scroll.
                AutoSize = false,
                Height = 100,
                Visible = false,
            };

            // Double-buffering : le panneau entier se déplace à chaque cran de molette ->
            // sans ça, scintillement/à-coups. Rend le défilement fluide.
            EnableDoubleBuffering(_containerForUserAccount);
            EnableDoubleBuffering(_containerUserAccount);



            _userScrollBar = new Guna2VScrollBar
            {
                Width = 8,
                FillColor = ColorTranslator.FromHtml("#23242D"),
                BorderColor = Colors.bgColor,
                ThumbColor = Colors.mainColor,
                Minimum = 0,
                Maximum = 0,
                Visible = true,
                AutoRoundedCorners = true,
                Height = _containerForUserAccount.Height - 40,
            };
            _userScrollBar.Location = new Point(_containerUserAccount.Right, (_containerForUserAccount.Height - _userScrollBar.Height) / 2);
            _containerForUserAccount.Controls.Add(_userScrollBar);


            // Déplacement du contenu piloté UNIQUEMENT par la scrollbar (ValueChanged) -> une
            // seule source de vérité, pas de double mise à jour de .Top.
            _userScrollBar.ValueChanged += (sender, e) =>
            {
                _containerUserAccount.Top = -_userScrollBar.Value;
            };

            // Molette : ~40 px par cran (aligné sur SmallChange). On borne puis on laisse
            // ValueChanged déplacer le contenu.
            _containerUserAccount.MouseWheel += (object sender, MouseEventArgs e) =>
            {
                int step = (e.Delta / 120) * 40;
                int newValue = _userScrollBar.Value - step;
                _userScrollBar.Value = Math.Max(_userScrollBar.Minimum, Math.Min(_userScrollBar.Maximum, newValue));
            };



            _mainOverlay = new Guna2Panel
            {
                FillColor = Colors.bgColor,
                UseTransparentBackground = false,
                BackColor = Color.Transparent,
                Size = new Size(this.Width, this.Height - _containerNav.Bottom),
                Visible = false,
            };

            _mainOverlay.Location = new Point(0, _containerNav.Bottom);

            this.Controls.Add(_mainOverlay);
            




            _titleView = new Guna2HtmlLabel
            {
                ForeColor = Color.White,
                Font = new Font("Inter Semibold", 16f, FontStyle.Regular),
                Text = Localization.T("main.all_products"),
            };
            _titleView.Location = new Point(0, 15);

            // Compteur restylé "dashboard" (accent, discret, majuscules) au lieu d'un
            // simple "(N)" -> lit comme un vrai en-tête de page pro.
            _countAllProducts = new Guna2HtmlLabel
            {
                ForeColor = Color.FromArgb(190, Colors.mainColor.R, Colors.mainColor.G, Colors.mainColor.B),
                Font = new Font("Inter Semibold", 9.5f, FontStyle.Regular),
                Text = "",
            };
            _countAllProducts.Location = new Point(_titleView.Right + 10, _titleView.Top + 6);

            _titleViewUserAccount = new Guna2HtmlLabel
            {
                ForeColor = Color.White,
                Font = new Font("Inter Medium", 14f, FontStyle.Regular),
                Text = Localization.T("main.account_info"),
                Parent = _containerUserAccount,
            };
            _titleViewUserAccount.Location = new Point(0, 18);

            // Bouton "Refresh" pour actualiser les infos (HWID actuel, etc.).
            var btnRefresh = new Guna2Button
            {
                Parent = _containerUserAccount,
                Text = Localization.T("main.refresh"),
                Font = new Font("Inter Semibold", 9f),
                ForeColor = Color.White,
                FillColor = Colors.mainColor,
                BorderRadius = 8,
                BorderThickness = 0,
                Size = new Size(84, 30),
                Animated = true,
                Cursor = Cursors.Hand,
                UseTransparentBackground = true,
                // Positionné juste après le titre : en FR/ES le texte est plus long qu'en EN,
                // une position X fixe fait chevaucher le bouton sur le titre traduit.
                Location = new Point(_titleViewUserAccount.Right + 20, 14)
            };
            btnRefresh.HoverState.FillColor = System.Windows.Forms.ControlPaint.Light(Colors.mainColor, 0.2f);
            btnRefresh.PressedColor = System.Windows.Forms.ControlPaint.Dark(Colors.mainColor, 0.04f);
            UiStyle.AddGlossySheen(btnRefresh);
            btnRefresh.Click += (s, e) => RefreshUserInfo();

            CreateInfoUser();
            LayoutUserAccountHeight();
            RefreshUserScrollExtent();

            _btnViewAllProducts = new Guna2HtmlLabel
            {
                Text = Localization.T("main.see_more_products"),
                Font = new Font("Inter Medium", 10f, FontStyle.Regular),
                ForeColor = Color.FromArgb(80, 80, 95),
                BackColor = Color.Transparent,


            };



            _btnViewAllProducts.MouseEnter += _btnViewAllProducts_MouseEnter;
            _btnViewAllProducts.MouseLeave += _btnViewAllProducts_MouseLeave;

            _btnViewAllProducts.Location = new Point(
                _containerMain.Right - (_btnViewAllProducts.Width*2), 20);


            _btnViewAllProducts.Click += async (s, e) =>
            {
                _btnTabProducts.PerformClick();
            };




            _btnPrevious = new Guna2CircleButton
            {
                Image = Utils.ChangeIconsColor(new Bitmap(Images.ArrowIcon), ColorTranslator.FromHtml("#464856")),
                Size = new Size(33, 33),
                ImageSize = new Size(26, 26),
                FillColor = Color.Transparent,
                BorderColor = Color.Transparent,
                BorderThickness = 1,
                Visible = false,
                UseTransparentBackground = true,
            };

            _btnNext = new Guna2CircleButton
            {
                Image = Utils.ChangeIconsColor(new Bitmap(Images.ArrowRIcon), ColorTranslator.FromHtml("#464856")),
                Size = new Size(35, 35),
                ImageSize = new Size(26, 26),
                ImageOffset = new Point(1, 0),
                FillColor = Color.Transparent,
                BorderColor = Color.Transparent,
                BorderThickness = 1,
                Visible = false,
                UseTransparentBackground = true,
            };

            // Survol rose sakura sur les flèches de pagination.
            foreach (var arrow in new[] { _btnPrevious, _btnNext })
            {
                arrow.Animated = true;
                arrow.Cursor = Cursors.Hand;
                arrow.HoverState.FillColor = Color.FromArgb(55, Colors.mainColor);
                arrow.HoverState.BorderColor = Color.Transparent;
            }




            _creditText = new Label
            {
                Text = "PaiPai © 2025 - All rights reserved.",
                BackColor = Color.Transparent,
                Width = 350,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = ColorTranslator.FromHtml("#464856"),
                //BorderThickness = 1,
                //BorderColor = Color.Blue,
                AutoSize = false,
                Font = new Font("Inter Medium", 11f, FontStyle.Regular),
                Visible = true,
            };
            _creditText.Location = new Point(
               _btnNext.Left - _creditText.Width - 10,
               _btnNext.Top + (_btnNext.Height - _creditText.Height) / 2
           );

            viewLabel = new Label
            {
                Text = "",
                BackColor = Color.Transparent,
                Width = 200,
                ForeColor = ColorTranslator.FromHtml("#464856"),
                //BorderThickness = 1,
                //BorderColor = Color.Blue,
                AutoSize = false,
                BorderStyle = BorderStyle.None,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Inter Medium", 11f, FontStyle.Regular),
                Visible = true,
            };
            viewLabel.Location = new Point(
                _btnNext.Left - viewLabel.Width - 10,
                _btnNext.Top + (_btnNext.Height - viewLabel.Height) / 2
            );



            _btnNext.Location = new Point(_containerMain.Right - _btnNext.Width - 90, 11);

            _btnPrevious.Location = new Point(_btnNext.Left - _btnPrevious.Width - 10, _btnNext.Top);




            _btnNext.Click += (s, e) => NavigateToNextView();
            _btnPrevious.Click += (s, e) => NavigateToPreviousView();




            _contentForDetailsForm = new Guna2Panel
            {
                BackColor = Colors.bgColor,
                FillColor = Colors.bgColor,
                Dock = DockStyle.None,
                UseTransparentBackground = false,
                Width = contentW,
                Visible = false,
                Location = new Point(contentX, contentTop),
                Height = 600,
                BorderColor = Color.Green,
                BorderThickness = 0,
            };



            this.Controls.Add(_contentForDetailsForm);



            _containerNav.Parent = this;
            _containerNav.Parent = this;
            //head.Parent = _containerNav;
            viewLabel.Parent = _containerMain;
            _creditText.Parent = _containerMain;
            _btnViewAllProducts.Parent = _containerMain;
            _btnPrevious.Parent = _containerMain;
            _btnNext.Parent = _containerMain;
            _titleView.Parent = _containerMain;
            _countAllProducts.Parent = _containerMain;  

            _mainOverlay.BringToFront();
        }

        










        private void _btnViewAllProducts_MouseEnter(object sender, EventArgs e)
        {
            _btnViewAllProducts.Cursor = Cursors.Hand;
            utils.StartColorAnimation(_btnViewAllProducts, _btnViewAllProducts.ForeColor, Colors.mainColor);
        }

        private void _btnViewAllProducts_MouseLeave(object sender, EventArgs e)
        {
            utils.StartColorAnimation(_btnViewAllProducts, _btnViewAllProducts.ForeColor, Color.FromArgb(70, 70, 85));
        }












































        // Ouvre Windows PaiPai si l'abonnement "WindowsPai" est présent ET NON EXPIRÉ.
        // On revérifie l'expiration à CHAQUE ouverture (via le timestamp connu localement),
        // donc même si PaiPai est resté ouvert, une clé expirée bloque le lancement.
        private void OpenWindowsPai()
        {
            if (!RequireLicense("windowspai")) return;

            using (var scr = new Template.WindowsPaiScreen())
                scr.ShowDialog(this);
        }

        // Verrou d'accès partagé (même logique pour tous les produits) : bloque l'ouverture
        // si la clé correspondante n'a pas été "claim" (ou a expiré), avec le même message
        // d'invite que Windows PaiPai. Renvoie true si l'accès est autorisé.
        private bool RequireLicense(string gameKey)
        {
            var entry = Utilities.ProductCatalog.ByGameKey(gameKey);
            if (entry == null) return true; // produit non catalogué -> pas de verrou (legacy)

            if (!Utilities.LicenseGate.HasValidSubscription(entry.SubscriptionName))
            {
                Template.SakuraMessageBox.Show(
                    Localization.T("main.license_missing", entry.DisplayName),
                    entry.DisplayName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                // "Add license" n'existe plus sur l'accueil (bouton retiré, remplacé par
                // l'onglet nav "Claim Key") -> on ouvre directement le même dialogue de
                // claim ici, pour que le message reste actionnable au lieu de renvoyer
                // vers un bouton qui n'existe plus.
                _welcomeBanner?.ShowClaimDialog();
                return false;
            }
            return true;
        }

        // Préchauffe (construit + met en cache) les fiches produit sur le thread UI mais de
        // façon ÉTALÉE : une fiche à la fois, avec une pause entre chaque, pour ne pas figer
        // l'interface. Résultat : quand l'utilisateur clique sur un produit, la fiche est
        // déjà en cache -> ouverture instantanée (avant, la 1re construction ramait). Best-
        // effort : toute erreur (produit non catalogué, etc.) est ignorée silencieusement.
        private async void WarmProductDetailsCache()
        {
            // Laisse d'abord la fenêtre finir son premier rendu.
            await Task.Delay(400);

            foreach (var gameKey in new[] { "anydesk", "valorant", "roblox" })
            {
                if (IsDisposed || _rebuildingTheme) return;
                try
                {
                    await gameDetailsFactory.GetGameDetails(this, gameKey);
                }
                catch { /* préchauffage best-effort : on ignore */ }

                // Laisse respirer l'UI entre deux constructions (évite tout à-coup visible).
                await Task.Delay(200);
            }
        }

        private async void OpenGameDetails(string gameName)
        {
            // Windows PaiPai : écran custom (outils d'optimisation), verrouillé par la clé.
            if (gameName == "windowspai")
            {
                OpenWindowsPai();
                return;
            }

            // Tous les autres produits catalogués : même verrou (il faut avoir "claim" la clé
            // via "Add license" avant de pouvoir seulement ouvrir la fiche produit).
            if (!RequireLicense(gameName)) return;

            try
            {
                _btnHomePressed = false;
                _btnAllProductsPressed = false;
                if (_homeCarousel != null) _homeCarousel.Visible = false;

                // Loader sakura sur un thread séparé : il s'affiche IMMÉDIATEMENT et continue
                // d'animer même pendant la construction (bloquante) de DetailsProduct -> aucun
                // freeze. (Avant : un Task.Delay(300) précédait même l'affichage du loader =
                // 300 ms de latence pure au clic sur un produit, pour rien.)
                var loader = new Template.SakuraLoaderThread();
                loader.Show(this.Bounds, "Loading product...");
                try
                {
                    DetailsProduct detailsForm = await gameDetailsFactory.GetGameDetails(this, gameName);
                    LoadDetailsFormInContainer(detailsForm);
                    detailsForm.BringToFront();
                    _contentForDetailsForm.BringToFront();

                    UpdateExpandedViewState();

                    // Laisse la fiche produit peindre une frame avant de retirer le loader
                    // (évite un flash de contenu à moitié dessiné).
                    await Task.Delay(60);
                }
                finally
                {
                    loader.Close();
                }
            }
            catch (Exception ex)
            {
                // Log the error for debugging purposes
                string errorMessage = $"Error: {ex.Message}\nStackTrace: {ex.StackTrace}";
                File.WriteAllText("error.log", errorMessage);

                // Optionally, show a user-friendly error message
                PlantillaChanchoV16.Template.SakuraMessageBox.Show(
                    "An unexpected error occurred while loading game details. Please try again later.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }






        private async void LoadDetailsFormInContainer(Form _formToLoad)
        {
            _contentForDetailsForm.Visible = true;

            if (_currentForm != null)
            {
                _currentForm.Hide();
            }

            _formToLoad.TopLevel = false;

            _formToLoad.BringToFront();
            containerAllProducts.Visible = false;


            if (_contentForDetailsForm != null)
            {




                _contentForDetailsForm.Controls.Clear();
                _contentForDetailsForm.Controls.Add(_formToLoad);







                Guna2BorderlessForm borderForCuerrentForm = new Guna2BorderlessForm
                {
                    ContainerControl = _formToLoad,
                    BorderRadius = 5,
                    TransparentWhileDrag = false,
                    HasFormShadow = false,
                    DragForm = true,
                    DragMode = Guna.UI2.WinForms.Enums.DragMode.Form
                };

                _formToLoad.Show();
                //transition1.Show(_formToLoad);

            }
            else
            {
                PlantillaChanchoV16.Template.SakuraMessageBox.Show("El contenedor de paneles no est� inicializado.");
            }

            _currentForm = _formToLoad;
        }






















        private async Task CloseAllOpenExe()
        {
            var processesToClose = new[] { "PlantillaChanchoV5", "PlantillaChanchoV2" };

            await CloseProcesses(processesToClose);

            await Task.Delay(500);
            DeleteDirectories();
            await Task.Delay(500);
            Environment.Exit(0);
        }



        private void DeleteDirectories()
        {
            try
            {
                if (Directory.Exists(Login.Path1))
                {
                    Directory.Delete(Login.Path1, true);
                }
                if (Directory.Exists(Login.Path2))
                {
                    Directory.Delete(Login.Path2, true);
                }

                if (Directory.Exists(Login.Path3))
                {
                    Directory.Delete(Login.Path3, true);
                }
            }
            catch (Exception ex)
            {
                //PlantillaChanchoV16.Template.SakuraMessageBox.Show("Error al eliminar carpetas: " + ex.Message);
            }
        }


        private async Task CloseProcesses(string[] processNames)
        {
            foreach (var processName in processNames)
            {
                try
                {
                    var processes = Process.GetProcessesByName(processName);

                    foreach (var process in processes)
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                            Console.WriteLine($"Proceso {processName} cerrado.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al intentar cerrar el proceso {processName}: {ex.Message}");
                }
            }
        }




        private bool _rebuildingTheme = false;

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Arrête ET libère les timers de cette fenêtre (sinon ils fuient à chaque
            // reconstruction pour changement de thème / langue).
            _vpnPollTimer?.Stop(); _vpnPollTimer?.Dispose();
            processCheckTimer?.Stop(); processCheckTimer?.Dispose();
            globalKeyHook?.Dispose();
            if (_rebuildingTheme) return; // reconstruction pour changer de thème : ne pas quitter l'app
            CloseAllOpenExe();
            DeleteDirectories();
            Environment.Exit(0);
        }

        // Reconstruit la fenêtre principale pour appliquer un nouveau thème sans relancer l'appli
        // (la session KeyAuth est statique, donc conservée -> pas de reconnexion).
        public void RebuildForTheme()
        {
            _rebuildingTheme = true;
            var nm = new Main(startMaximized: _startMaximized);
            // Si on est en plein écran, ConfigureFormSettings a déjà positionné/dimensionné
            // la nouvelle fenêtre sur la zone de travail -> ne pas l'écraser ici.
            if (!_startMaximized)
            {
                nm.StartPosition = FormStartPosition.Manual;
                nm.Location = this.Location;
            }
            nm.Show();
            this.Close();
        }
    }
}
