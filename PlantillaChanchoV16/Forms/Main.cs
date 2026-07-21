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
        private Guna2Button _btnTabHome, _btnTabProducts, _btnTabUserData, _btnLogOut;
        private Guna2Panel containerAllProducts, _containerNav, _containerMain, _containerSpecialProduct, _containerFavProducts, _containerForUserAccount, _containerUserAccount;

        private Guna2HtmlLabel _titleView, _countAllProducts, _titleViewUserAccount, _btnViewAllProducts, _loadingLabelView;
        private Label viewLabel, _creditText;

        private Guna2CircleProgressBar _progressBar;
        private Guna2CircleButton _btnPrevious, _btnNext;

        private Guna2PictureBox _iconLogo;
        private Guna2Panel _contentLogo, _contentTabButtons;

        private Guna2Panel _pUsername, _pIP, _pHWID, _pCreateAt, _pLastLogin, _pExpiresAt, _pSubscriptionName;

        private AddProduct productManager;
        private DetailsProduct detailsForm;
        private ProductManager gameDetailsFactory = new ProductManager();
        private Guna2Panel _contentForDetailsForm;
    

        private static Guna2Panel overlayModal;
        private Form _currentForm = null;


        private Timer processCheckTimer;

        private Default DefaultForm;





        public Main()
        {
            InitializeComponent();
            ConfigureFormSettings();
            InitializeBorderlessForm();
            InitializeOverlayModal();

            // Inicialización de componentes principales
            CreateItemsView();
            CreateProductContainer(false);
            InitializeProductManager();
            SpecialProduct();
            AddCircularProgressBar();
            AddLogoNav(_containerNav);
            GenerateInterface(_containerNav);

            // Configuraciones de utilidades
            ConfigureUtils();

            AdjustContainerHeights();
            ManageOverlayOrder();

            DefaultForm = new Default();
            if (!DefaultForm.TestMode)
            {
                InitializeProcessCheckTimer();
                ProcessChecker.ShowDetectedPrograms();
            }
        }


        // SPECIAL PRODUCT
        private void SpecialProduct()
        {
            var _specialProductView = new SpecialProductView(
                parentPanel: _containerSpecialProduct,
                productUnderMaintenance: false,
                bgImage: images.Img1Valorant,
                productName: "Valorant",
                productDescription: "Conquer Valorant with precise aim, faster reflexes, and advanced features to dominate every match.",
                previewVideo: "https://images.savi.wtf/u/4Gn5w5.mp4",
                openDetails: () => OpenGameDetails("valorant")
            );

            _specialProductView.Parent = _containerSpecialProduct;
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
            int sizeGeneral = 100;
            this.Width = 928 - sizeGeneral;
            this.Height = 642 - sizeGeneral;
            this.BackColor = Colors.bgColor;
            this.FormBorderStyle = FormBorderStyle.None;
        }

        private void InitializeBorderlessForm()
        {
            Guna2BorderlessForm border = new Guna2BorderlessForm
            {
                BorderRadius = Default.borderForms,
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
            _containerNav.Height = this.Height;
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
        }






       

        

        private void ConfigureContainers(bool _isExpanded)
        {
            if (_isExpanded)
            {
               

                _containerMain.Location = new Point(_containerMain.Location.X, -1);
                _containerSpecialProduct.Visible = false;

                _btnNext.Visible = true;
                _btnPrevious.Visible = true;
                _btnViewAllProducts.Visible = false;

                _titleView.Location = new Point(0, 20);
                _countAllProducts.Location = new Point(_titleView.Right, _titleView.Top);

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
                _countAllProducts.Location = new Point(_titleView.Right, _titleView.Top);

                _btnNext.Location = new Point(_containerMain.Right - _btnNext.Width - 110, 11);

                _btnPrevious.Location = new Point(_btnNext.Left - _btnPrevious.Width - 10, _btnNext.Top);

            }
        }

        private void UpdateExpandedViewState()
        {
            if (_containerMain.Location.Y == _containerNav.Right)
            {
                _expandedViewProducts = true;
            }
            else if (_containerMain.Location.Y == _containerSpecialProduct.Bottom)
            {
                _expandedViewProducts = false;
            }
        }

























        private async void clickedBtnHome()
        {
            _progressBar.Visible = true;
            _loadingLabelView.Visible = true;
            _mainOverlay.BringToFront();
            _mainOverlay.Visible = true;

            //UpdateExpandedViewState();

            productManager.ShowView("view1");
            _containerSpecialProduct.Visible = true;
            _containerMain.Visible = true;
            _expandedViewProducts = false;
            ConfigureContainers(_expandedViewProducts);
            await Task.Delay(600);

            _btnNext.Visible = false;
            _btnPrevious.Visible = false;
            _contentForDetailsForm.Visible = false;
            _mainOverlay.BringToFront();
            await Task.Delay(400);
            _progressBar.Visible = false;
            _loadingLabelView.Visible = false;
            await Task.Delay(100);
            _mainOverlay.Visible = false;
        }

        private async void clicked_btnViewAllProducts()
        {
            _progressBar.Visible = true;
            _loadingLabelView.Visible = true;
            _mainOverlay.BringToFront();
            _mainOverlay.Visible = true;

            _expandedViewProducts = true;
            //productContainer.ShowView("initial");
            await Task.Delay(600);
            ConfigureContainers(_expandedViewProducts);
            _btnNext.Visible = true;
            _btnPrevious.Visible = true;

            _contentForDetailsForm.Visible = false;
            _mainOverlay.BringToFront();
            await Task.Delay(400);
            _progressBar.Visible = false;
            _loadingLabelView.Visible = false;
            await Task.Delay(100);
            _containerMain.Visible = true;
            _mainOverlay.Visible = false;
        }



        private async void clickedBtnViewUserData()
        {
            //_progressBar.Visible = true;
            //_loadingLabelView.Visible = true;
            //_mainOverlay.BringToFront();
            //_mainOverlay.Visible = true;

            //UpdateExpandedViewState();

            _containerSpecialProduct.Visible = false;
            _containerMain.Visible = false;
            _containerForUserAccount.Visible = true;
            _containerUserAccount.Visible = true;
            _expandedViewProducts = false;
            ConfigureContainers(_expandedViewProducts);
            //await Task.Delay(600);

            _btnNext.Visible = false;
            _btnPrevious.Visible = false;
            _contentForDetailsForm.Visible = false;
            _mainOverlay.BringToFront();
            await Task.Delay(400);
            _progressBar.Visible = false;
            _loadingLabelView.Visible = false;
            await Task.Delay(100);
            _mainOverlay.Visible = false;
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
                // Verificar que el Unix Timestamp es válido (mayor o igual a 0)
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













        private void AddLogoNav(Guna2Panel parentPanel)
        {
            _contentLogo = new Guna2Panel
            {
                Parent = _containerNav,
                Location = new Point(0,0),
                FillColor = Colors.bgColor,
                Width = parentPanel.Width -2,
                Height = parentPanel.Width - 15,
                BorderColor = Color.White,
                BorderThickness = 0,
            };

            _iconLogo = new Guna2PictureBox
            {
                Parent = _contentLogo,
                Image = images.MainLogo, // <-- LOGO NORMAL // Image = Utils.ChangeIconsColor(new Bitmap(_images.MainLogo), Colors.mainColor) <-- LOGO WITH MAIN COLOR
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(_contentLogo.Width - 35, _contentLogo.Height - 35),
            };
            _iconLogo.Location = new Point((_contentLogo.Width - _iconLogo.Width) / 2, (_contentLogo.Height - _iconLogo.Height) / 2);

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




            _btnTabHome = CreateTabButton(parentContainer, "tab1", images.IconTabHome, new Size(20, 20), () =>
            {
                try
                {
                    if (!_btnHomePressed)
                    {
                        _btnHomePressed = true;
                        _btnAllProductsPressed = false;
                        _btnUserDataPressed = false;

                        _containerFavProducts.Visible = false;
                        _containerForUserAccount.Visible = false;
                        _containerUserAccount.Visible = false;
                        _loadingLabelView.Text = "Preparing the main view, hold on.";
                        _loadingLabelView.Location = new Point(
                            _progressBar.Location.X + (_progressBar.Width - _loadingLabelView.Width) / 2,
                            _progressBar.Location.Y + _progressBar.Height + 20
                        );

                        clickedBtnHome();

                        HandleTabButtonClick(_btnTabHome);
                        containerAllProducts.Height = _containerMain.Height;
                    }
                }
                catch (Exception ex)
                {
                    string errorMessage = $"Error: {ex.Message}\nStackTrace: {ex.StackTrace}";
                    File.WriteAllText("error.log", errorMessage);
                    MessageBox.Show(
                        "An unexpected error occurred while processing the Home tab. Please try again later.",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            });

            _btnTabProducts = CreateTabButton(parentContainer, "tab2", images.IconTabProducts, new Size(20, 20), () =>
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

                        _loadingLabelView.Text = "Loading all products, please wait.";
                        _loadingLabelView.Location = new Point(
                            _progressBar.Location.X + (_progressBar.Width - _loadingLabelView.Width) / 2,
                            _progressBar.Location.Y + _progressBar.Height + 20
                        );

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
                    MessageBox.Show(
                        "An unexpected error occurred while processing the Products tab. Please try again later.",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            });

            _btnTabUserData = CreateTabButton(parentContainer, "tab4", images.IconTabUserData, new Size(20, 20), () =>
            {
                try
                {
                    if (!_btnUserDataPressed)
                    {
                        _btnHomePressed = false;
                        _btnAllProductsPressed = false;
                        _btnUserDataPressed = true;

                        _loadingLabelView.Text = "Loading account information, please wait.";
                        _loadingLabelView.Location = new Point(
                            _progressBar.Location.X + (_progressBar.Width - _loadingLabelView.Width) / 2,
                            _progressBar.Location.Y + _progressBar.Height + 20
                        );

                        clickedBtnViewUserData();
                        HandleTabButtonClick(_btnTabUserData);
                        _btnTabHome.Checked = false;

                        _containerMain.Visible = false;
                        _containerSpecialProduct.Visible = false;
                        _containerUserAccount.Visible = true;
                        _containerForUserAccount.Visible = true;
                    }
                }
                catch (Exception ex)
                {
                    string errorMessage = $"Error: {ex.Message}\nStackTrace: {ex.StackTrace}";
                    File.WriteAllText("error.log", errorMessage);
                    MessageBox.Show(
                        "An unexpected error occurred while processing the User Data tab. Please try again later.",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            });




            _btnTabHome.Checked = true;





            var mainTabGroup = CreateTabButtonGroup(new[] { _btnTabHome, _btnTabProducts, _btnTabUserData});
            mainTabGroup.Location = new Point(0, currentYPosition);
            mainPanel.Controls.Add(mainTabGroup);
            currentYPosition += mainTabGroup.Height + 10;


            

            CreateExitToggleAndButton(mainPanel, parentContainer.Height);


            //_btnTab1.PerformClick();
        }

        private void CreateExitToggleAndButton(Guna2Panel parentPanel, int parentContainerHeight)
        {
            int bottomPadding = 10;
            int groupHeight = 40;

            _btnLogOut = CreateTabButton(_contentTabButtons, "tabLogOut", images.LogOutIcon, new Size(20, 20), async () =>
            {
                HandleTabButtonClick(_btnLogOut);
                _btnTabHome.Checked = false;


                var result = MessageBox.Show(
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

            TabButtonStyle(parentPanel,button,iconOff, tabButtonClicked, imgSize );

            button.Location = new Point((parentPanel.Width - button.Width) / 2, button.Location.Y);

            button.Click += (s, e) =>
            {
                tabButtonClicked.Invoke();
            };

            return button;
        }



        private async void HandleTabButtonClick(Guna2Button clickedButton)
        {
            if (lastSelectedButton != null && lastSelectedButton != clickedButton)
            {
                lastSelectedButton.ForeColor = Color.FromArgb(139, 139, 143);
                lastSelectedButton.FillColor = Color.Transparent;
                lastSelectedButton.BorderColor = Color.Transparent;
                lastSelectedButton.Image = await Utils.ChangeIconsColorAsync(new Bitmap(lastSelectedButton.Image), Color.FromArgb(40, 42, 57));

            }

            clickedButton.FillColor = Color.FromArgb(38, 39, 49);
            clickedButton.BorderColor = Color.FromArgb(38, 39, 49);
            clickedButton.Image = Utils.ChangeIconsColor(new Bitmap(clickedButton.Image), Color.White);



            lastSelectedButton = clickedButton;
        }

























        private Guna2Panel CreateTextBoxPanel(Control parent, string? tx, string labelText, Point location)
        {
            var valueLabel = new Guna2HtmlLabel
            {
                Text = tx ?? string.Empty,
                Font = new Font("Inter Medium", 9.4F),
                ForeColor = Color.DarkCyan,
                AutoSize = true,
                TextAlignment = ContentAlignment.MiddleLeft, 
                Location = new Point(0, 10),
                BackColor = Color.Transparent,
                BorderStyle = BorderStyle.None,
            };

            var labelForPanel = new Guna2HtmlLabel
            {
                Text = labelText,
                Font = new Font("Inter Medium", 9.4F),
                ForeColor = Color.White,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(0, 0) 
            };

            var panelValueContainer = new Guna2Panel
            {
                Width = parent.Width - 32,
                Height = 45,
                Location = new Point(0, labelForPanel.Bottom +4),
                FillColor = Colors.scColor,
                BorderColor = Colors.scColor,
                BorderRadius = 4,
                BorderThickness = 1,
            };

            valueLabel.Location = new Point(10, (panelValueContainer.Height- valueLabel.Height) / 2);


            panelValueContainer.Controls.Add(valueLabel);

            var panel = new Guna2Panel
            {
                Size = new Size(parent.Width - 30, panelValueContainer.Height + 25),
                Location = location,
                BackColor = Color.Transparent
            };

            panel.Controls.Add(labelForPanel);
            panel.Controls.Add(panelValueContainer);

            return panel;
        }






        private void CreateInfoUser()
        {
            string username = Login.KeyAuthApp?.user_data?.username ?? "Chancho Gamer";
            string ip = Login.KeyAuthApp?.user_data?.ip ?? "192.168.1.100";
            string hwid = Login.KeyAuthApp?.user_data?.hwid ?? "B3F9-4A2C-71D8-839E";

            string createdAt = UnixTimeToDateTime(long.TryParse(Login.KeyAuthApp?.user_data?.createdate, out long created) ? created : 0).ToString() ?? "2023-01-01 12:00:00";
            string lastLogin = UnixTimeToDateTime(long.TryParse(Login.KeyAuthApp?.user_data?.lastlogin, out long last) ? last : 0).ToString() ?? "2023-05-20 08:30:45";
            string expiresAt = "No subscription expiration found";
            string subscriptionName = "No subscription name found";

            if (Login.KeyAuthApp?.user_data?.subscriptions != null && Login.KeyAuthApp.user_data.subscriptions.Count > 0)
            {
                string expiryUnix = Login.KeyAuthApp.user_data.subscriptions[0].expiry;  // Aquí puedes cambiar el índice si es necesario
                expiresAt = UnixTimeToDateTime(long.Parse(expiryUnix)).ToString();
                subscriptionName = Login.KeyAuthApp.user_data.subscriptions[0].subscription;
            }

         

            _pUsername = CreateTextBoxPanel(
                parent: _containerUserAccount,
                tx: username,
                labelText: "Username:",
                location: new Point(10, _titleViewUserAccount.Bottom + 15)
            );

            _pIP = CreateTextBoxPanel(
                parent: _containerUserAccount,
                tx: ip,
                labelText: "IP Address:",
                location: new Point(10, _pUsername.Bottom + 10)
            );

            _pHWID = CreateTextBoxPanel(
                parent: _containerUserAccount,
                tx: hwid,
                labelText: "HWID:",
                location: new Point(10, _pIP.Bottom + 10)
            );

            _pCreateAt = CreateTextBoxPanel(
                parent: _containerUserAccount,
                tx: createdAt,
                labelText: "Created At:",
                location: new Point(10, _pHWID.Bottom + 10)
            );

            _pLastLogin = CreateTextBoxPanel(
                parent: _containerUserAccount,
                tx: lastLogin,
                labelText: "Last Login:",
                location: new Point(10, _pCreateAt.Bottom + 10)
            );

            _pExpiresAt = CreateTextBoxPanel(
                parent: _containerUserAccount,
                tx: expiresAt,
                labelText: "Expires At:",
                location: new Point(10, _pLastLogin.Bottom + 10)
            );

            _pSubscriptionName = CreateTextBoxPanel(
                parent: _containerUserAccount,
                tx: subscriptionName,
                labelText: "Subscription:",
                location: new Point(10, _pExpiresAt.Bottom + 10)
            );

            foreach (var panel in new[] { _pUsername, _pCreateAt, _pLastLogin, _pIP, _pHWID, _pSubscriptionName, _pExpiresAt })
            {
                panel.Parent = _containerUserAccount;
            }
        }






        private void CreateItemsView()
        {

            _containerNav = new Guna2Panel
            {
                CustomBorderThickness = new Padding(0,0,1,0),
                CustomBorderColor = Color.FromArgb(28,29,39),
                BorderThickness = 1,
                Width = 80,
            };
            _containerNav.Location = new Point(0,0);

            _containerSpecialProduct = new Guna2Panel
            {
                Height = 307,
                Width = this.Width - _containerNav.Width + 1,
                FillColor = Colors.bgColor,
                BorderColor = Color.Green,
                BorderThickness = 0,
                UseTransparentBackground = false,
                Visible = false,
            };
            _containerSpecialProduct.Location = new Point(_containerNav.Right, -1);
            _containerSpecialProduct.Parent = this;



            _containerFavProducts = new Guna2Panel
            {
                Height = 500,
                Width = this.Width - _containerNav.Width + 1,
                FillColor = Colors.bgColor,
                BorderColor = Color.YellowGreen,
                BorderThickness = 2,
                UseTransparentBackground = false,
                Visible = false,
            };
            _containerFavProducts.Location = new Point(_containerNav.Right, -1);
            _containerFavProducts.Parent = this;



            _containerMain = new Guna2Panel
            {
                Width = this.Width - _containerNav.Width - 16,
                Height = this.Height,
                BackColor = Color.White,
                FillColor = Colors.bgColor,
                BorderThickness = 0,
                BorderColor = Color.White,
                Padding = new Padding(0),
                UseTransparentBackground = false,
                Visible = true,
            };


            _containerMain.Location = new Point(_containerNav.Right + 15, 0);
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
                FillColor = Colors.bgColor,
                BorderColor = Colors.bgColor,
                BorderThickness = 1,
                UseTransparentBackground= true,
                BackColor = Color.Transparent,
                Width = _containerForUserAccount.Width -30,
                AutoSize = true,
                Visible = false,
            };

            

            Guna2VScrollBar scrollBar = new Guna2VScrollBar
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
            scrollBar.Location = new Point(_containerUserAccount.Right, (_containerForUserAccount.Height - scrollBar.Height) / 2);
            _containerForUserAccount.Controls.Add(scrollBar);


            scrollBar.ValueChanged += (sender, e) =>
            {
                _containerUserAccount.Top = -scrollBar.Value;
                _containerUserAccount.Invalidate();
            };


            _containerUserAccount.MouseWheel += (object sender, MouseEventArgs e) =>
            {
                int newValue = scrollBar.Value - e.Delta / 3;

                scrollBar.Value = Math.Max(scrollBar.Minimum, Math.Min(scrollBar.Maximum, newValue));


                _containerUserAccount.Top = -scrollBar.Value;

                _containerUserAccount.Invalidate();
            };



            _mainOverlay = new Guna2Panel
            {
                FillColor = Colors.bgColor,
                UseTransparentBackground = false,
                BackColor = Color.Transparent,
                Size = new Size(this.Width - _containerNav.Width, this.Height),
                Visible = false,
            };

            _mainOverlay.Location = new Point(_containerNav.Right, 0);

            this.Controls.Add(_mainOverlay);
            




            _titleView = new Guna2HtmlLabel
            {
                ForeColor = Color.White,
                Font = new Font("Inter Medium", 14f, FontStyle.Regular),
                Text = "All our products",
            };
            _titleView.Location = new Point(0, 15);

            _countAllProducts = new Guna2HtmlLabel
            {
                ForeColor = Color.White,
                Font = new Font("Inter Medium", 14f, FontStyle.Regular),
                Text = "",
            };
            _countAllProducts.Location = new Point(_titleView.Right, _titleView.Top);

            _titleViewUserAccount = new Guna2HtmlLabel
            {
                ForeColor = Color.White,
                Font = new Font("Inter Medium", 14f, FontStyle.Regular),
                Text = "Account information",
                Parent = _containerUserAccount,
            };
            _titleViewUserAccount.Location = new Point(0, 18);


            CreateInfoUser();
            _btnViewAllProducts = new Guna2HtmlLabel
            {
                Text = "See more products",
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




            _creditText = new Label
            {
                Text = "Eternal Project - © 2025 All rights reserved. Powered by AKDASOY.",
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
                Width = this.Width - _containerNav.Width,
                Visible = false,
                Location = new Point(_containerNav.Right + 15, -1),
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
            utils.StartColorAnimation(_btnViewAllProducts, _btnViewAllProducts.ForeColor, Color.White);
        }

        private void _btnViewAllProducts_MouseLeave(object sender, EventArgs e)
        {
            utils.StartColorAnimation(_btnViewAllProducts, _btnViewAllProducts.ForeColor, Color.FromArgb(70, 70, 85));
        }












































        private async void OpenGameDetails(string gameName)
        {
            try
            {
                await Task.Delay(300);
                _btnHomePressed = false;
                _btnAllProductsPressed = false;

                _loadingLabelView.Text = "Obtaining product data, please wait.";
                _loadingLabelView.Location = new Point(
                    _progressBar.Location.X + (_progressBar.Width - _loadingLabelView.Width) / 2,
                    _progressBar.Location.Y + _progressBar.Height + 20
                );

                _progressBar.Visible = true;
                _loadingLabelView.Visible = true;
                _mainOverlay.BringToFront();
                _mainOverlay.Visible = true;

                UpdateExpandedViewState();

                // _containerSpecialProduct.Visible = false;
                // _containerMain.Visible = false;

                await Task.Delay(1000);
                DetailsProduct detailsForm = await gameDetailsFactory.GetGameDetails(this, gameName);
                LoadDetailsFormInContainer(detailsForm);
                detailsForm.BringToFront();
                _contentForDetailsForm.BringToFront();
                _mainOverlay.BringToFront();

                await Task.Delay(1500);
                _progressBar.Visible = false;
                _loadingLabelView.Visible = false;

                await Task.Delay(300);
                _mainOverlay.Visible = false;
            }
            catch (Exception ex)
            {
                // Log the error for debugging purposes
                string errorMessage = $"Error: {ex.Message}\nStackTrace: {ex.StackTrace}";
                File.WriteAllText("error.log", errorMessage);

                // Optionally, show a user-friendly error message
                MessageBox.Show(
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
                MessageBox.Show("El contenedor de paneles no está inicializado.");
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
                //MessageBox.Show("Error al eliminar carpetas: " + ex.Message);
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




        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            CloseAllOpenExe();
            DeleteDirectories();
            Environment.Exit(0);
        }
    }
}
