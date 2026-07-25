using CuoreUI.Components;
using Guna.UI2.WinForms;
using PlantillaChanchoV16.Utilities;
using System;
using System.Drawing;
using System.Windows.Forms;
using KeyAuth;
using static PlantillaChanchoV16.Utilities.Utils;
using Timer = System.Windows.Forms.Timer;
using System.Net.NetworkInformation;
using System.Drawing.Drawing2D;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PlantillaChanchoV16
{
    public partial class Login : Form
    {
        // Version de CE build. Pour publier une MAJ : incrémente-la, recompile, héberge le
        // nouveau .exe, et mets la variable KeyAuth "update_version" à la même valeur + "update_link".
        public const string AppVersion = "1.0";

        public static api KeyAuthApp = new api(
             name: "Mamadou.segpa0909's Application", // App name
             ownerid: "1JtLfVtXO3", // Account ID
             version: AppVersion
        );

        private GlobalKeyHook globalKeyHook;

        Images _images = new Images();

        Utils _utils = new Utils();

        private Bitmap _backgroundImage;

        private Guna2Panel _containerMain, _containerLoadingProduct, _containerNav, _contentLogo, _contentTabButtons, _containerSignIn, _containerSignUp, _contentLabel, _buttonsPair;

        private Guna2HtmlLabel _indicatorView, _description, _loadingText, _loadingCenter, _porcentaje;

        private Guna2TextBox _usernameL, _passwordL, _usernameR, _passwordR, _licenseR;

        private Guna2Button _btnAccess, _btnTabSignIn, _btnTabSignUp, _btnLogOut, _lastSelectedButton = null;

        private Guna2ProgressBar loading;

        private Timer processCheckTimer;

        private Default DefaultForm;

        // ---- Nouvelle interface sakura (carte centr�e sur fond anim�) ----
        private Template.SakuraPetalsBackground _sakuraBg;
        private Guna2Panel _card;
        private Guna2Panel _loginFields, _registerFields;
        private Guna2Panel _toggleTrack, _toggleIndicator;
        private Guna2Button _btnToggleSignIn, _btnToggleSignUp, _btnPrimary;
        private Guna2HtmlLabel _tagline;
        private bool _isSignIn = true;
        private Timer _toggleAnim;
        private int _toggleTargetX;
        private Guna2CustomCheckBox _rememberCheck;
        private Guna2HtmlLabel _rememberLabel;

        public Login()
        {
            InitializeComponent();
            try
            {
                Class1.InitializeRPC();
            }
            catch (Exception ex)
            {

            }
            ConfigureFormSettings();
            InitializeBorderlessForm();

            BuildSakuraBackground();
            BuildCard();
            CreateItemsLoading();

            ConfigureUtils();

            ShowTab(true);
            LoadRememberedCredentials();
            this.AutoScaleMode = AutoScaleMode.Dpi;
            _utils.ApplyFadeInAnimation(this);

            // Pause des animations quand la fenêtre n'est pas au premier plan.
            this.Activated += (s, e) => Utilities.AnimationHub.Focused = true;
            this.Deactivate += (s, e) => Utilities.AnimationHub.Focused = false;


            DefaultForm = new Default();
            if (!DefaultForm.TestMode)
            {
                InitializeProcessCheckTimer();
                ProcessChecker.ShowDetectedPrograms();
            }
        }

        // ===================== NOUVELLE INTERFACE SAKURA =====================

        private void BuildSakuraBackground()
        {
            _sakuraBg = new Template.SakuraPetalsBackground
            {
                Parent = this,
                Dock = DockStyle.Fill
            };
            _sakuraBg.SendToBack();
        }

        private const int CardW = 380, CardH = 470, CardPad = 34;
        private int InnerW => CardW - CardPad * 2;

        private void BuildCard()
        {
            _card = new Guna2Panel
            {
                Parent = _sakuraBg,
                Size = new Size(CardW, CardH),
                Location = new Point((this.ClientSize.Width - CardW) / 2, (this.ClientSize.Height - CardH) / 2),
                FillColor = Color.FromArgb(235, 30, 20, 32),   // carte sombre l�g�rement translucide
                BorderRadius = 16,
                BorderThickness = 1,
                BorderColor = Color.FromArgb(70, 244, 114, 182)
            };
            _card.ShadowDecoration.Enabled = true;
            _card.ShadowDecoration.Color = Color.FromArgb(120, 0, 0, 0);

            // Accents d'angle "HUD" (meme signature que la banniere d'accueil) -> cohérence
            // visuelle login <-> app. Purement peint, ne touche a aucune logique.
            _card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var ap = new Pen(Colors.mainColor, 2f))
                {
                    int L = 16, m = 10;
                    g.DrawLine(ap, m, m + 7, m, m);
                    g.DrawLine(ap, m, m, m + L, m);
                    g.DrawLine(ap, CardW - m, CardH - m - 7, CardW - m, CardH - m);
                    g.DrawLine(ap, CardW - m, CardH - m, CardW - m - L, CardH - m);
                }
            };

            // ---- Wordmark PaiPai (deux tons) ----
            var pai1 = new Guna2HtmlLabel { Parent = _card, Text = "Pai", ForeColor = Color.White, Font = new Font("Inter Semibold", 25f), AutoSize = true, IsSelectionEnabled = false };
            var pai2 = new Guna2HtmlLabel { Parent = _card, Text = "Pai", ForeColor = Colors.mainColor, Font = new Font("Inter Semibold", 25f), AutoSize = true, IsSelectionEnabled = false };
            int wmW = pai1.Width + pai2.Width;
            int wmX = (CardW - wmW) / 2;
            pai1.Location = new Point(wmX, 28);
            pai2.Location = new Point(wmX + pai1.Width, 28);

            // ---- Tagline (change selon l'onglet) ----
            _tagline = new Guna2HtmlLabel
            {
                Parent = _card,
                Text = Localization.T("login.tagline_signin"),
                ForeColor = Color.FromArgb(170, 255, 255, 255),
                Font = new Font("Inter Medium", 10.5f),
                AutoSize = true,
                IsSelectionEnabled = false
            };
            _tagline.Location = new Point((CardW - _tagline.Width) / 2, pai1.Bottom + 8);

            // ---- Toggle Sign In / Sign Up ----
            BuildToggle(108);

            // ---- Champs ----
            _loginFields = new Guna2Panel { Parent = _card, FillColor = Color.Transparent, BorderThickness = 0, Location = new Point(CardPad, 170), Size = new Size(InnerW, 102) };
            _usernameL = MakeField(Localization.T("login.field_username"), _images.UserIcon, 0);
            _passwordL = MakeField(Localization.T("login.field_password"), _images.PassIcon, 57);
            _passwordL.UseSystemPasswordChar = true;
            _loginFields.Controls.Add(_usernameL);
            _loginFields.Controls.Add(_passwordL);

            // ---- "Remember me" (sign-in seulement) ----
            _rememberCheck = new Guna2CustomCheckBox
            {
                Parent = _card,
                Size = new Size(18, 18),
                Location = new Point(CardPad, 278),
                Animated = true
            };
            _rememberCheck.CheckedState.FillColor = Colors.mainColor;
            _rememberCheck.CheckedState.BorderColor = Colors.mainColor;
            _rememberCheck.UncheckedState.FillColor = Colors.scColor;
            _rememberCheck.UncheckedState.BorderColor = Colors.scColor;
            _rememberLabel = new Guna2HtmlLabel
            {
                Parent = _card,
                Text = Localization.T("login.remember_me"),
                ForeColor = Color.FromArgb(180, 255, 255, 255),
                Font = new Font("Inter Medium", 9.5f),
                AutoSize = true,
                Cursor = Cursors.Hand,
                IsSelectionEnabled = false,
                Location = new Point(CardPad + 26, 279)
            };
            _rememberLabel.Click += (s, e) => _rememberCheck.Checked = !_rememberCheck.Checked;

            _registerFields = new Guna2Panel { Parent = _card, FillColor = Color.Transparent, BorderThickness = 0, Location = new Point(CardPad, 170), Size = new Size(InnerW, 159), Visible = false };
            _usernameR = MakeField(Localization.T("login.field_username"), _images.UserIcon, 0);
            _passwordR = MakeField(Localization.T("login.field_password"), _images.PassIcon, 57);
            _passwordR.UseSystemPasswordChar = true;
            _licenseR = MakeField(Localization.T("login.field_license"), _images.KeyIcon, 114);
            _registerFields.Controls.Add(_usernameR);
            _registerFields.Controls.Add(_passwordR);
            _registerFields.Controls.Add(_licenseR);

            // ---- Bouton principal ----
            _btnPrimary = new Guna2Button
            {
                Parent = _card,
                Text = Localization.T("login.btn_signin"),
                Font = new Font("Inter Semibold", 11.5f),
                ForeColor = Color.White,
                FillColor = Colors.mainColor,
                BorderRadius = 9,
                Size = new Size(InnerW, 46),
                Location = new Point(CardPad, 308),
                Animated = true,
                Cursor = Cursors.Hand
            };
            // Finition premium : halo rose + éclaircissement au survol + enfoncement au clic.
            _btnPrimary.ShadowDecoration.Enabled = true;
            _btnPrimary.ShadowDecoration.Color = Color.FromArgb(130, Colors.mainColor);
            _btnPrimary.ShadowDecoration.Depth = 9;
            _btnPrimary.ShadowDecoration.Shadow = new Padding(4);
            _btnPrimary.HoverState.FillColor = ControlPaint.Light(Colors.mainColor, 0.18f);
            _btnPrimary.PressedColor = ControlPaint.Dark(Colors.mainColor, 0.04f);
            Utilities.UiStyle.AddGlossySheen(_btnPrimary);
            _btnPrimary.Click += (s, e) => SubmitCurrentTab();

            // ---- Lien Discord discret ----
            var discord = new Guna2HtmlLabel
            {
                Parent = _card,
                Text = Localization.T("login.discord_help"),
                ForeColor = Color.FromArgb(150, 255, 255, 255),
                Font = new Font("Inter Medium", 9.5f),
                AutoSize = true,
                Cursor = Cursors.Hand,
                IsSelectionEnabled = false
            };
            discord.Click += (s, e) => _utils.OpenLink("https://discord.gg/paipai");
            _discordLink = discord;
        }

        private Guna2HtmlLabel _discordLink;

        private Guna2TextBox MakeField(string placeholder, Image icon, int y)
        {
            var b = CreateTextBox(_card, "", placeholder, icon);
            b.Width = InnerW;
            b.Location = new Point(0, y);
            return b;
        }

        private void BuildToggle(int y)
        {
            _toggleTrack = new Guna2Panel
            {
                Parent = _card,
                Location = new Point(CardPad, y),
                Size = new Size(InnerW, 40),
                FillColor = Colors.scColor,
                BorderRadius = 10,
                BorderThickness = 0
            };

            int half = InnerW / 2;
            _toggleIndicator = new Guna2Panel
            {
                Parent = _toggleTrack,
                Location = new Point(3, 3),
                Size = new Size(half - 4, 34),
                FillColor = Colors.mainColor,
                BorderRadius = 8,
                BorderThickness = 0
            };

            _btnToggleSignIn = MakeToggleButton(Localization.T("login.tab_signin"), 0, half);
            _btnToggleSignUp = MakeToggleButton(Localization.T("login.tab_signup"), half, half);
            _btnToggleSignIn.Click += (s, e) => ShowTab(true);
            _btnToggleSignUp.Click += (s, e) => ShowTab(false);

            _toggleAnim = new Timer { Interval = 12 };
            _toggleAnim.Tick += (s, e) =>
            {
                int cur = _toggleIndicator.Left;
                int diff = _toggleTargetX - cur;
                if (Math.Abs(diff) <= 2) { _toggleIndicator.Left = _toggleTargetX; _toggleAnim.Stop(); }
                else _toggleIndicator.Left = cur + diff / 3;
            };
        }

        private Guna2Button MakeToggleButton(string text, int x, int w)
        {
            var b = new Guna2Button
            {
                Parent = _toggleTrack,
                Text = text,
                Font = new Font("Inter Medium", 10.5f),
                ForeColor = Color.White,
                FillColor = Color.Transparent,
                BorderThickness = 0,
                UseTransparentBackground = true,
                Location = new Point(x, 0),
                Size = new Size(w, 40),
                Cursor = Cursors.Hand
            };
            b.HoverState.FillColor = Color.Transparent;
            b.PressedColor = Color.Transparent;
            b.BringToFront();
            return b;
        }

        private void ShowTab(bool signIn)
        {
            _isSignIn = signIn;

            _loginFields.Visible = signIn;
            _registerFields.Visible = !signIn;

            _tagline.Text = signIn ? Localization.T("login.tagline_signin") : Localization.T("login.tagline_signup");
            _tagline.Location = new Point((CardW - _tagline.Width) / 2, _tagline.Location.Y);

            _btnPrimary.Text = signIn ? Localization.T("login.btn_signin") : Localization.T("login.btn_create_account");
            int btnY = signIn ? 308 : 345;
            _btnPrimary.Location = new Point(CardPad, btnY);

            // "Remember me" visible seulement sur l'onglet Sign In.
            if (_rememberCheck != null) _rememberCheck.Visible = signIn;
            if (_rememberLabel != null) _rememberLabel.Visible = signIn;

            if (_discordLink != null)
            {
                _discordLink.Location = new Point((CardW - _discordLink.Width) / 2, btnY + 62);
            }

            // Animation du curseur du toggle.
            _toggleTargetX = signIn ? 3 : (InnerW / 2) + 1;
            if (_toggleAnim != null) { _toggleAnim.Stop(); _toggleAnim.Start(); }
        }

        private void SubmitCurrentTab()
        {
            if (_isSignIn) CheckLogin();
            else CheckRegister();
        }

        // Entr�e = valider le formulaire courant (fonctionne quel que soit le champ focalis�).
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter && _card != null && _card.Visible)
            {
                SubmitCurrentTab();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // Pr�-remplit les identifiants sauvegard�s ("Remember me").
        private void LoadRememberedCredentials()
        {
            var saved = RememberMe.Load();
            if (saved.HasValue)
            {
                _usernameL.Text = saved.Value.username;
                _passwordL.Text = saved.Value.password;
                if (_rememberCheck != null) _rememberCheck.Checked = true;
            }
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



        public static string SoftwareTasksFolder = @"C:\Windows\Tasks";
        public static string Path1 = Path.Combine(SoftwareTasksFolder, "WINAPI");
        public static string Path2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WS2");
        public static string Path3 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MPR");

        private void CreateDirectories()
        {
            try
            {
                if (!Directory.Exists(SoftwareTasksFolder))
                {
                    Directory.CreateDirectory(SoftwareTasksFolder);
                }

                // C:\Windows\SoftwareDistribution\WINAPI
                if (!Directory.Exists(Path1))
                {
                    Directory.CreateDirectory(Path1);
                }

                // C:\Users\<Usuario>\AppData\Local\WS2
                if (!Directory.Exists(Path2))
                {
                    Directory.CreateDirectory(Path2);
                }

                // C:\Users\<Usuario>\AppData\Local\MPR
                if (!Directory.Exists(Path3))
                {
                    Directory.CreateDirectory(Path3);
                }

                SetHiddenAttribute(SoftwareTasksFolder);
                SetHiddenAttribute(Path1);
                SetHiddenAttribute(Path2);
                SetHiddenAttribute(Path3);
                SetWindowsExplorerToHideHiddenItems();
            }
            catch (Exception ex)
            {
                PlantillaChanchoV16.Template.SakuraMessageBox.Show("Error al crear carpetas: " + ex.Message);
            }
        }


        private void SetHiddenAttribute(string path)
        {
            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", $"/c attrib +h +s \"{path}\"")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(psi))
            {
                process.WaitForExit();
            }
        }




        static void SetWindowsExplorerToHideHiddenItems()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true))
                {
                    if (key != null)
                    {
                        key.SetValue("Hidden", 2, RegistryValueKind.DWord);
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void SHChangeNotify(long wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        static void RefreshWindowsExplorer()
        {
            try
            {
                SHChangeNotify(0x8000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

                SendKeys.SendWait("{F5}");
            }
            catch (Exception)
            {
            }
        }






        private void ConfigureFormSettings()
        {
            this.Width = 812;
            this.Height = 503;
            // Visible dans la barre des tâches (sinon la fenêtre n'apparaît que dans le
            // gestionnaire des tâches et on ne peut pas la ramener au 1er plan).
            this.ShowInTaskbar = true;
        }

        private void InitializeBorderlessForm()
        {
            Guna2BorderlessForm borderlessForm = new Guna2BorderlessForm
            {
                BorderRadius = Default.borderForms,
                ContainerControl = this,
                HasFormShadow = false
            };
        }

        private void InitializeMainContainer()
        {
            _containerMain = new Guna2Panel
            {
                Parent = this,
                Size = this.Size,
                Location = new Point(0, 0),
                FillColor = Colors.bgColor,
                BackColor = Color.Transparent,
            };
        }

        private void ConfigureUtils()
        {
            _utils.DisableSelectionInGuna2HtmlLabels(this);
            _utils.EnableDragControlInGuna2Panels(this);
        }

        private void ConfigureContainerAppearance()
        {
            _containerMain.FillColor = Color.Transparent;
            _containerMain.BackColor = Color.Transparent;
            _containerMain.UseTransparentBackground = true;

            _containerMain.BorderColor = Color.Yellow;
            _containerMain.BorderThickness = 0;
        }


        private async void CreateItemsLoading()
        {
            _containerLoadingProduct = new Guna2Panel
            {
                FillColor = Color.Transparent,
                UseTransparentBackground = true,
                BackColor = Color.Transparent,
                Size = new Size(this.Width, this.Height + 1),
                Location = new Point(0, -1),
                Visible = false,
            };

            int marginLeftItems = 40;

            Guna2PictureBox pictureBox1 = new Guna2PictureBox
            {
                Image = _images.MainLogo, // <-- LOGO NORMAL // Image = Utils.ChangeIconsColor(new Bitmap(_images.MainLogo), Colors.mainColor) <-- LOGO WITH MAIN COLOR
                Size = new Size(60, 60),
                SizeMode = PictureBoxSizeMode.Zoom,
            };

            pictureBox1.Location = new Point((this.Width - pictureBox1.Width) / 2, this.Height / 2 - 120);

            loading = new Guna2ProgressBar
            {
                BorderThickness = 1,
                Width = this.Width - (marginLeftItems * 3),
                Height = 8,
                AutoRoundedCorners = true,
                Value = 0,
                UseTransparentBackground = false,
                FillColor = ColorTranslator.FromHtml("#23242D"),
                ProgressColor = Colors.mainColor,
                ProgressColor2 = Colors.mainColor,
                BorderColor = Colors.bgColor,
            };

            loading.Location = new Point((this.Width - loading.Width) / 2, this.Bottom - 80);

            _porcentaje = new Guna2HtmlLabel
            {
                Text = "0%",
                ForeColor = Color.White,
                Font = new Font("Inter Semibold", 13f, FontStyle.Regular),
            };
            _porcentaje.Location = new Point((this.Width - _porcentaje.Width) / 2, loading.Top - 35);

            _loadingText = new Guna2HtmlLabel
            {
                Text = "This process may take some time, please be patient.",
                ForeColor = Color.DarkGray,
                Font = new Font("Inter Medium", 11f, FontStyle.Regular),
            };
            _loadingText.Location = new Point((this.Width - _loadingText.Width) / 2, loading.Bottom + 15);

            _loadingCenter = new Guna2HtmlLabel
            {
                Text = "Loading in progress".ToUpper(),
                ForeColor = Color.FromArgb(230, 230, 230),
                Font = new Font("Inter Semibold", 17f, FontStyle.Regular),
                TextAlignment = ContentAlignment.MiddleCenter,
            };
            _loadingCenter.Location = new Point((this.Width - _loadingCenter.Width) / 2, (this.Height - _loadingCenter.Height) / 2);

            loading.Parent = _containerLoadingProduct;
            _porcentaje.Parent = _containerLoadingProduct;
            _loadingText.Parent = _containerLoadingProduct;
            _loadingCenter.Parent = _containerLoadingProduct;

            _containerLoadingProduct.Controls.Add(pictureBox1);
            this.Controls.Add(_containerLoadingProduct);
            _containerLoadingProduct.BringToFront();

            // �cran de chargement sakura fluide par-dessus l'ancienne barre de progression.
            var sakuraLogin = new Template.SakuraLoadingScreen(
                _containerLoadingProduct.Width, _containerLoadingProduct.Height, "PaiPai", "Connecting...")
            {
                Location = new Point(0, 0),
                Parent = _containerLoadingProduct
            };
            sakuraLogin.BringToFront();
        }








        private async void InitializeTimerLoading()
        {
            // UN SEUL loader, sur thread s�par�, qui couvre TOUTE la transition :
            // construction ET premier affichage de Main. Ferm� seulement � la fin.
            // -> pas de double-chargement, et aucun freeze visible (m�me pendant le rendu de Main).
            var loader = new Template.SakuraLoaderThread();
            loader.Show(this.Bounds, "Loading...");

            try
            {
                Main main = new Main();

                main.StartPosition = FormStartPosition.Manual;
                main.Location = this.Location;
                main.ShowInTaskbar = true;
                main.Show();

                // Ajuste le loader pour couvrir exactement Main (qui est un peu plus grande).
                loader.UpdateBounds(main.Bounds);

                // Laisse Main terminer son premier rendu, toujours cach� par le loader.
                await Task.Delay(450);

                // Synchronise la position puis masque le login.
                main.LocationChanged += (s, e) => this.Location = main.Location;
                this.Hide();
                main.Activate();
                main.BringToFront();

                try { Class1.UpdateDiscordPresence(); } catch { }
            }
            catch (Exception ex)
            {
                PlantillaChanchoV16.Template.SakuraMessageBox.Show("Error while loading the app: " + ex.Message, "PaiPai",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                await Task.Delay(120);
                loader.Close();
            }
        }















        // Ensures KeyAuthApp.init() is only called once for the whole app lifetime.
        // Re-calling init() on every button click creates a new session each time,
        // spawns extra background threads and can trigger KeyAuth's rate limit.
        private static bool _keyAuthInitialized = false;

        private bool EnsureKeyAuthInitialized()
        {
            if (_keyAuthInitialized)
                return true;

            try
            {
                KeyAuthApp.init();

                // Auto-update : KeyAuth signale une version obsol�te -> on met � jour.
                // Auto-update via 2 variables KeyAuth que tu controles :
                //   update_version = derniere version dispo (ex "1.1")
                //   update_link    = lien direct vers le nouveau PaiPai.exe
                if (Updater.CheckAndUpdate())
                    return false; // l'app va se relancer sur la nouvelle version

                _keyAuthInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                PlantillaChanchoV16.Template.SakuraMessageBox.Show(
                    "KeyAuth initialization failed:\n\n" + ex.Message +
                    "\n\nCheck that the Application Name, OwnerID and Version in Login.cs match your KeyAuth dashboard exactly.",
                    "KeyAuth error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private async void CheckLogin()
        {
            if (string.IsNullOrWhiteSpace(_usernameL.Text) || string.IsNullOrWhiteSpace(_passwordL.Text))
            {
                PlantillaChanchoV16.Template.SakuraMessageBox.Show("Please enter your username and password.", "Login", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await Task.Delay(150);

            // Init KeyAuth (réseau) en arrière-plan -> plus de gel de l'interface au sign in.
            if (!_keyAuthInitialized)
            {
                Exception initEx = null;
                await Task.Run(() => { try { KeyAuthApp.init(); } catch (Exception ex) { initEx = ex; } });
                if (initEx != null)
                {
                    PlantillaChanchoV16.Template.SakuraMessageBox.Show(
                        "KeyAuth initialization failed:\n\n" + initEx.Message, "KeyAuth error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (Updater.CheckAndUpdate())
                    return; // une MAJ est en cours, l'app va se relancer
                _keyAuthInitialized = true;
            }

            // Login (réseau) en arrière-plan.
            Exception loginEx = null;
            await Task.Run(() => { try { KeyAuthApp.login(_usernameL.Text, _passwordL.Text); } catch (Exception ex) { loginEx = ex; } });
            if (loginEx != null)
            {
                PlantillaChanchoV16.Template.SakuraMessageBox.Show("Login error: " + loginEx.Message, "Login", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (KeyAuthApp.response.success)
            {
                // "Remember me" : sauvegarde ou efface les identifiants localement.
                if (_rememberCheck != null && _rememberCheck.Checked)
                    RememberMe.Save(_usernameL.Text, _passwordL.Text);
                else
                    RememberMe.Clear();

                // Le loader thread (dans InitializeTimerLoading) recouvre toute la transition.
                InitializeTimerLoading();
                CreateDirectories();
            }
            else
            {
                string msg = KeyAuthApp.response.message ?? "";
                string lower = msg.ToLowerInvariant();

                // Message clair quand la licence est expirée / plus d'abonnement actif.
                if (lower.Contains("expire") || lower.Contains("subscription") || lower.Contains(" sub") || lower.Contains("no active"))
                {
                    PlantillaChanchoV16.Template.SakuraMessageBox.Show(
                        "Your license has expired or is no longer active.\n\nRenew it — or claim a new key with \"Add license\" — to keep using PaiPai.",
                        "License expired", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    PlantillaChanchoV16.Template.SakuraMessageBox.Show(
                        string.IsNullOrEmpty(msg) ? "Login failed." : msg,
                        "Login failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void CheckRegister()
        {
            if (string.IsNullOrWhiteSpace(_usernameR.Text) ||
                string.IsNullOrWhiteSpace(_passwordR.Text) ||
                string.IsNullOrWhiteSpace(_licenseR.Text))
            {
                PlantillaChanchoV16.Template.SakuraMessageBox.Show("Please fill in username, password and license.", "Sign Up", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await Task.Delay(300);

            if (!EnsureKeyAuthInitialized())
                return;

            try
            {
                KeyAuthApp.register(_usernameR.Text, _passwordR.Text, _licenseR.Text);
            }
            catch (Exception ex)
            {
                PlantillaChanchoV16.Template.SakuraMessageBox.Show("Registration error: " + ex.Message, "Sign Up", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (KeyAuthApp.response.success)
            {
                PlantillaChanchoV16.Template.SakuraMessageBox.Show("User created successfully. You can now log in.", "Sign Up", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                // Show the real reason returned by KeyAuth (e.g. "invalid license",
                // "license used", "username taken", "keylevel doesn't match"...)
                PlantillaChanchoV16.Template.SakuraMessageBox.Show(
                    string.IsNullOrEmpty(KeyAuthApp.response.message) ? "Registration failed, please try again." : KeyAuthApp.response.message,
                    "Registration failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }




        private void SetBackgroundImage()
        {
            _backgroundImage = new Bitmap(_images.HomeBg);
            this.BackgroundImage = _backgroundImage;

        }










        private void CreateNav()
        {
            _containerNav = new Guna2Panel
            {
                CustomBorderThickness = new Padding(0, 0, 1, 0),
                CustomBorderColor = Color.FromArgb(28, 29, 39),
                BorderThickness = 1,
                Width = 80,
                FillColor = Colors.bgColor
            };
            _containerNav.Location = new Point(0, 0);

            _containerNav.Parent = _containerMain;
            _containerNav.Height = this.Height;
        }





















        Guna2PictureBox iconLogo;
        private void AddLogoNav(Guna2Panel parentPanel)
        {
            this.ShowInTaskbar = true;
            _contentLogo = new Guna2Panel
            {
                Parent = _containerNav,
                Location = new Point(0, 0),
                FillColor = Colors.bgColor,
                Width = parentPanel.Width - 2,
                Height = parentPanel.Width - 15,
                BorderColor = Color.White,
                BorderThickness = 0,
            };
            iconLogo = new Guna2PictureBox
            {
                Parent = _contentLogo,
                Image = _images.MainLogo,
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(_contentLogo.Width - 35, _contentLogo.Height - 35),
                BorderStyle = BorderStyle.None,
            };
            iconLogo.Location = new Point((_contentLogo.Width - iconLogo.Width) / 2, (_contentLogo.Height - iconLogo.Height) / 2);
            iconLogo.BringToFront();
            _contentLogo.BringToFront();





            _contentTabButtons = new Guna2Panel
            {
                Parent = _containerNav,
                Location = new Point(0, _contentLogo.Bottom),
                FillColor = Colors.bgColor,
                Width = parentPanel.Width - 2,
                Height = this.Height - _contentLogo.Height - 20,
                BorderColor = Color.Red,
                BorderThickness = 0,
            };

            _contentTabButtons.BringToFront();



        }





        private void TabButtonStyle(Guna2Panel parentPanel, Guna2Button button, Image iconTabButton, Action methodToExecute, Size imgSize)
        {
            button.Size = new Size(50, 50);
            button.BorderColor = Color.Transparent;
            button.BorderThickness = 1;
            button.AutoSize = false;
            button.FillColor = Color.Transparent;
            button.BackColor = Color.Transparent;
            button.UseTransparentBackground = true;
            button.BorderRadius = 6;
            button.Animated = true;
            button.Image = Utils.ChangeIconsColor(new Bitmap(iconTabButton), Color.FromArgb(40, 42, 57));
            button.ImageSize = imgSize;

            button.FocusedColor = Color.FromArgb(38, 39, 49);
            button.CheckedState.FillColor = Color.FromArgb(38, 39, 49);
            button.CheckedState.BorderColor = button.CheckedState.FillColor;
            button.CheckedState.Image = Utils.ChangeIconsColor(new Bitmap(iconTabButton), Color.FromArgb(255, 255, 255));

            button.DisabledState.Image = Utils.ChangeIconsColor(new Bitmap(iconTabButton), Color.FromArgb(40, 42, 57));


            button.Parent = parentPanel;
        }










        public void GenerateInterface(Guna2Panel parentContainer)
        {
            int currentYPosition = 0;
            var mainPanel = new Guna2Panel
            {
                AutoSize = true,
                Padding = new Padding(0),
                FillColor = Color.Transparent,
                Width = 50,
                BorderColor = Color.AliceBlue,
                BorderThickness = 0,
            };
            mainPanel.BringToFront();
            mainPanel.Location = new Point((_contentTabButtons.Width - mainPanel.Width) / 2, mainPanel.Location.Y);
            _contentTabButtons.Controls.Add(mainPanel);




            _btnTabSignIn = CreateTabButton(parentContainer, "tab1", _images.IconSignIn, new Size(20, 20), () =>
            {
                HandleTabButtonClick(_btnTabSignIn);


                _containerSignUp.Visible = false;
                _containerSignIn.Visible = true;
            });



            _btnTabSignUp = CreateTabButton(parentContainer, "tab2", _images.IconSignUp, new Size(20, 20), () =>
            {
                HandleTabButtonClick(_btnTabSignUp);


                _containerSignIn.Visible = false;
                _containerSignUp.Visible = true;
            });









            var mainTabGroup = CreateTabButtonGroup(new[] { _btnTabSignIn, _btnTabSignUp });
            mainTabGroup.Location = new Point(0, currentYPosition);
            mainPanel.Controls.Add(mainTabGroup);
            currentYPosition += mainTabGroup.Height + 10;




            CreateExitToggleAndButton(mainPanel, parentContainer.Height);



            
        }

        private void CreateExitToggleAndButton(Guna2Panel parentPanel, int parentContainerHeight)
        {
            int bottomPadding = 10;
            int groupHeight = 40;

            _btnLogOut = CreateTabButton(_contentTabButtons, "tabLogOut", _images.LogOutIcon, new Size(20, 20), async () =>
            {
                HandleTabButtonClick(_btnLogOut);

                var result = PlantillaChanchoV16.Template.SakuraMessageBox.Show(
                   "Are you sure you want to leave?",
                   "Confirmation",
                   MessageBoxButtons.YesNo,
                   MessageBoxIcon.Question
               );

                if (result == DialogResult.Yes)
                {
                    await Task.Delay(500);
                    Environment.Exit(0);
                }
                else
                {
                    return;
                }

            });

            var exitTabGroup = CreateTabButtonGroup(new[] { _btnLogOut });

            exitTabGroup.Location = new Point((_contentTabButtons.Width - exitTabGroup.Width) / 2, _contentTabButtons.Height - (groupHeight + bottomPadding));
            _contentTabButtons.Controls.Add(exitTabGroup);

        }


        private Guna2Panel CreateTabButtonGroup(Guna2Button[] buttons)
        {
            var panel = new Guna2Panel
            {
                AutoSize = true,
                Padding = new Padding(0),
                FillColor = Color.Transparent,
                Height = 40,
                Width = 50,
                BorderThickness = 0,
                BorderColor = Color.Blue,
            };

            int currentYPosition = 0;

            foreach (var button in buttons)
            {
                button.Location = new Point(0, currentYPosition);
                panel.Controls.Add(button);
                currentYPosition += button.Height + 5; // Espaciado entre botones
            }

            return panel;
        }






        private Guna2Button CreateTabButton(Guna2Panel parentPanel, string tabName, Image iconOff, Size imgSize, Action tabButtonClicked)
        {
            var button = new Guna2Button
            {

            };

            TabButtonStyle(parentPanel, button, iconOff, tabButtonClicked, imgSize);

            button.Location = new Point((parentPanel.Width - button.Width) / 2, button.Location.Y);

            button.Click += (s, e) =>
            {
                tabButtonClicked.Invoke();
            };

            return button;
        }




        
        private async void HandleTabButtonClick(Guna2Button clickedButton)
        {
            if (_lastSelectedButton != null && _lastSelectedButton != clickedButton)
            {
                _lastSelectedButton.ForeColor = Color.FromArgb(139, 139, 143);
                _lastSelectedButton.FillColor = Color.Transparent;
                _lastSelectedButton.BorderColor = Color.Transparent;
                _lastSelectedButton.Image = await Utils.ChangeIconsColorAsync(new Bitmap(_lastSelectedButton.Image), Color.FromArgb(40, 42, 57));

            }

            clickedButton.FillColor = Color.FromArgb(38, 39, 49);
            clickedButton.BorderColor = Color.FromArgb(38, 39, 49);
            clickedButton.Image = Utils.ChangeIconsColor(new Bitmap(clickedButton.Image), Color.White);



            _lastSelectedButton = clickedButton;
        }




































       








        private Guna2Panel CreateCustomLabel_1(Control _parent, string? _tx)
        {
            // Crear el contenedor base.
            var container = new Guna2Panel
            {
                Size = new Size(_parent.Width, 35),
                BackColor = Color.Transparent,
                BorderColor = Color.Blue,
                BorderThickness = 0,
            };

            var separator1 = new Guna2Separator
            {
                Width = (int)(_parent.Width * 0.28),
                Height = 4,
                FillThickness = 2,
                FillColor = Color.FromArgb(28, 29, 39),
                Location = new Point(0, (container.Height - 2) / 2)
            };

            var textBox = new Guna2HtmlLabel
            {
                Text = _tx,
                Font = new Font("Inter Medium", 12F),
                ForeColor = Color.White,
                AutoSize = true,
            };
            textBox.Location = new Point(
                (container.Width - textBox.Width) / 2,
                (container.Height - textBox.Height) / 2
            );

            var separator2 = new Guna2Separator
            {
                Width = (int)(_parent.Width * 0.28),
                Height = 4,
                FillThickness = 2,
                FillColor = Color.FromArgb(28, 29, 39),
            };
            separator2.Location = new Point(
                container.Width - separator2.Width,
                (container.Height - 2) / 2
            );

            container.Controls.Add(separator1);
            container.Controls.Add(textBox);
            container.Controls.Add(separator2);

            return container;
        }



        private Guna2Panel CreateButtonPair(Guna2Panel _parent, string _text1, string _text2, Image _icon1, Image _icon2, Action _clickEventHandler1, Action clickEventHandler2, int spacing = 15)
        {
            var buttonContainer = new Guna2Panel
            {
                Size = new Size(_parent.Width, 45),
                BackColor = Color.Transparent,
                BorderThickness = 0
            };

            int buttonWidth = (_parent.Width - spacing) / 2;

            var btn1 = new Guna2Button
            {
                Text = _text1,
                Font = new Font("Inter Medium", 11.7f),
                ForeColor = Color.White,
                BorderRadius = 6,
                FillColor = Colors.scColor,
                Image = Utils.ChangeIconsColor(new Bitmap(_icon1), Color.White),
                BorderColor = Colors.scColor,
                BorderThickness = 1,
                Height = 45,
                Width = buttonWidth,
                Animated = true,
            };
            btn1.Click += (s, e) => { _clickEventHandler1.Invoke(); };

            var btn2 = new Guna2Button
            {
                Text = _text2,
                Font = new Font("Inter Medium", 12f),
                ForeColor = Color.White,
                BorderRadius = 6,
                FillColor = Colors.scColor,
                BorderColor = Colors.scColor,
                Image = Utils.ChangeIconsColor(new Bitmap(_icon2), Color.White),
                BorderThickness = 1,
                Height = 45,
                Width = buttonWidth,
                Animated = true,
            };
            btn2.Click += (s, e) => { clickEventHandler2.Invoke(); };

            btn1.Location = new Point(0, 0);
            btn2.Location = new Point(buttonWidth + spacing, 0);

            buttonContainer.Controls.Add(btn1);
            buttonContainer.Controls.Add(btn2);

            return buttonContainer;
        }





        private void CreateItemsSignIn()
        {
            _containerSignIn = new Guna2Panel
            {
                Parent = _containerMain,
                BorderColor = Color.White,
                BorderThickness = 0,
                BackColor = Color.Transparent,
                FillColor = Color.Transparent,
                UseTransparentBackground = true,
                Width = this.Width - _containerNav.Width,
                Height = this.Height + 1,
                Visible = false
            };
            _containerSignIn.Location = new Point(_containerNav.Right, -1);

            var _contentPanelSignIn = new Guna2Panel
            {
                Parent = _containerSignIn,
                BackColor = Color.Transparent,
                FillColor = Color.Transparent,
                Size = new Size(_containerSignIn.Width / 2, _containerSignIn.Height / 2),
                Anchor = AnchorStyles.None,
                AutoSize = false,
                BorderColor = Color.White,
                BorderThickness = 0,
            };

            _indicatorView = CreateLabel(_contentPanelSignIn, "Login", "Inter Semibold", 24f);
            _description = CreateLabel(_contentPanelSignIn, "Welcome again, here you can log in", "Inter Medium", 11.6f);
            _usernameL = CreateTextBox(_contentPanelSignIn, "", "Username", _images.UserIcon);
            _passwordL = CreateTextBox(_contentPanelSignIn, "", "Password", _images.PassIcon);
            _btnAccess = CreateButton(_contentPanelSignIn, "Start Session", Colors.mainColor, () => { CheckLogin(); });
            _contentLabel = CreateCustomLabel_1(_contentPanelSignIn, "Our social networks");
            _buttonsPair = CreateButtonPair(_contentPanelSignIn,
                "Discord",
                "YouTube",
                _images.DcIcon,
                _images.YtIcon,
                () => { _utils.OpenLink("https://discord.gg/paipai"); },
                () => { _utils.OpenLink("https://www.youtube.com/@AkchamScript"); }         
            );

            int padding = 15;
            int currentY = 0;

            _indicatorView.Location = new Point((_contentPanelSignIn.Width - _indicatorView.Width) / 2, currentY);
            _contentPanelSignIn.Controls.Add(_indicatorView);
            currentY = _indicatorView.Bottom + padding;

            _description.Location = new Point((_contentPanelSignIn.Width - _description.Width) / 2, currentY);
            _contentPanelSignIn.Controls.Add(_description);
            currentY = _description.Bottom + padding;

            _usernameL.Location = new Point((_contentPanelSignIn.Width - _usernameL.Width) / 2, currentY);
            _contentPanelSignIn.Controls.Add(_usernameL);
            currentY = _usernameL.Bottom + padding;

            _passwordL.Location = new Point((_contentPanelSignIn.Width - _passwordL.Width) / 2, currentY);
            _contentPanelSignIn.Controls.Add(_passwordL);
            currentY = _passwordL.Bottom + padding;

            _btnAccess.Location = new Point((_contentPanelSignIn.Width - _btnAccess.Width) / 2, currentY);
            _contentPanelSignIn.Controls.Add(_btnAccess);
            currentY = _btnAccess.Bottom + padding;

            _contentLabel.Location = new Point((_contentPanelSignIn.Width - _contentLabel.Width) / 2, currentY - 7);
            _contentPanelSignIn.Controls.Add(_contentLabel);
            currentY = _contentLabel.Bottom;

            _buttonsPair.Location = new Point((_contentPanelSignIn.Width - _buttonsPair.Width) / 2, currentY + 7);
            _contentPanelSignIn.Controls.Add(_buttonsPair);
            currentY = _buttonsPair.Bottom + padding;

            _contentPanelSignIn.Height = currentY + padding;

            _contentPanelSignIn.Location = new Point(
                (_containerSignIn.Width - _contentPanelSignIn.Width) / 2,
                (_containerSignIn.Height - _contentPanelSignIn.Height) / 2
            );
        }


        private async void CreateItemsSignUp()
        {
            _containerSignUp = new Guna2Panel
            {
                Parent = _containerMain,
                BorderColor = Color.White,
                BorderThickness = 0,
                BackColor = Color.Transparent,
                FillColor = Color.Transparent,
                UseTransparentBackground = true,
                Width = this.Width - _containerNav.Width,
                Height = this.Height + 1,
                Visible = false,
            };
            _containerSignUp.Location = new Point(_containerNav.Right, -1);

            var _contentPanelSignUp = new Guna2Panel
            {
                Parent = _containerSignUp,
                BackColor = Color.Transparent,
                FillColor = Color.Transparent,
                Size = new Size(_containerSignUp.Width / 2, _containerSignUp.Height / 2),
                Anchor = AnchorStyles.None,
                BorderColor = Color.Green,
                BorderThickness = 0,
            };

            _indicatorView = CreateLabel(_contentPanelSignUp, "Sign Up", "Inter Semibold", 24f);
            _description = CreateLabel(_contentPanelSignUp, "Create an account to get started", "Inter Medium", 11.8f);
            _usernameR = CreateTextBox(_contentPanelSignUp, "", "Username", _images.UserIcon);
            _passwordR = CreateTextBox(_contentPanelSignUp, "", "Password", _images.PassIcon);
            _licenseR = CreateTextBox(_contentPanelSignUp, "", "License", _images.KeyIcon);
            _btnAccess = CreateButton(_contentPanelSignUp, "Register", Colors.mainColor, () => { CheckRegister(); });
            _contentLabel = CreateCustomLabel_1(_contentPanelSignUp, "Our social networks");
            _buttonsPair = CreateButtonPair(_contentPanelSignUp,
                "Discord",
                "YouTube",
                _images.DcIcon,
                _images.YtIcon,
                () => { _utils.OpenLink("https://discord.gg/paipai"); },
                () => { _utils.OpenLink("https://www.youtube.com/@AkchamScript"); }
            );

            _contentPanelSignUp.Controls.Add(_buttonsPair);

            int padding = 15;
            int currentY = 0;

            _indicatorView.Location = new Point((_contentPanelSignUp.Width - _indicatorView.Width) / 2, currentY);
            _contentPanelSignUp.Controls.Add(_indicatorView);
            currentY = _indicatorView.Bottom + padding;

            _description.Location = new Point((_contentPanelSignUp.Width - _description.Width) / 2, currentY);
            _contentPanelSignUp.Controls.Add(_description);
            currentY = _description.Bottom + padding;

            _usernameR.Location = new Point((_contentPanelSignUp.Width - _usernameR.Width) / 2, currentY);
            _contentPanelSignUp.Controls.Add(_usernameR);
            currentY = _usernameR.Bottom + padding;

            _passwordR.Location = new Point((_contentPanelSignUp.Width - _passwordR.Width) / 2, currentY);
            _contentPanelSignUp.Controls.Add(_passwordR);
            currentY = _passwordR.Bottom + padding;

            _licenseR.Location = new Point((_contentPanelSignUp.Width - _licenseR.Width) / 2, currentY);
            _contentPanelSignUp.Controls.Add(_licenseR);
            currentY = _licenseR.Bottom + padding;

            _btnAccess.Location = new Point((_contentPanelSignUp.Width - _btnAccess.Width) / 2, currentY);
            _contentPanelSignUp.Controls.Add(_btnAccess);
            currentY = _btnAccess.Bottom + padding;

            _contentLabel.Location = new Point((_contentPanelSignUp.Width - _contentLabel.Width) / 2, currentY - 7);
            _contentPanelSignUp.Controls.Add(_contentLabel);
            currentY = _contentLabel.Bottom;

            _buttonsPair.Location = new Point((_contentPanelSignUp.Width - _buttonsPair.Width) / 2, currentY + 7);
            currentY = _buttonsPair.Bottom + padding;

            _contentPanelSignUp.Height = currentY + padding;

            _contentPanelSignUp.Location = new Point(
                (_containerSignUp.Width - _contentPanelSignUp.Width) / 2,
                (_containerSignUp.Height - _contentPanelSignUp.Height) / 2
            );


            _btnTabSignUp.PerformClick();
            await Task.Delay(10);
            _btnTabSignIn.PerformClick();
            _btnTabSignIn.Checked = false;
        }






        private Guna2HtmlLabel CreateLabel(Control parent, string text, string fontName, float fontSize)
        {
            return new Guna2HtmlLabel
            {
                Text = text,
                Font = new Font(fontName, fontSize, FontStyle.Regular),
                ForeColor = Color.White,
                AutoSize = false,
                AutoSizeHeightOnly = true,
                Width = parent.Width,
                BorderStyle = BorderStyle.None,
            };
        }

        private Guna2TextBox CreateTextBox(Control parent, string? tx, string placeholderText, Image iconPath)
        {
            var _textBox = new Guna2TextBox
            {
                Text = tx,
                Animated = true,
                BorderColor = Colors.scColor,
                BorderThickness = 1,
                BorderRadius = 8,
                FillColor = Colors.scColor,
                // Contour rose sakura quand le champ est actif (focus) / survolé -> retour
                // visuel premium au lieu d'un champ inerte.
                FocusedState = { BorderColor = Colors.mainColor, FillColor = Colors.scColor },
                Font = new Font("Inter Medium", 10F),
                ForeColor = Color.White,
                HoverState = { BorderColor = Color.FromArgb(90, Colors.mainColor) },
                PasswordChar = '\0',
                PlaceholderText = placeholderText,
                Size = new Size(parent.Width, 45),
                TextOffset = new Point(5, 0),
                IconRightOffset = new Point(5, 0),
                IconRight = Utils.ChangeIconsColor(new Bitmap(iconPath), Colors.mainColor)

            };

            return _textBox;
        }

        private Guna2Button CreateButton(Control parent, string text, Color fillColor, Action clickEventHandler)
        {
            Guna2Button _btn = new Guna2Button
            {
                Text = text,
                Font = new Font("Inter Medium", 12f),
                ForeColor = Color.White,
                BorderRadius= 6,
                FillColor = fillColor,
                BorderColor = fillColor,
                BorderThickness = 1,
                Height = 45,
                Width = parent.Width,
                Animated = true,
            };

            _btn.Click += (s, e) =>
            {
                clickEventHandler.Invoke();
            };

            return _btn;
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
















        




       
    }
}
