using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PlantillaChanchoV16;
using PlantillaChanchoV16.Utilities;
using Guna.UI2.WinForms;
using Microsoft.VisualBasic.Logging;
using System.Drawing.Drawing2D;
using static PlantillaChanchoV16.Utilities.Utils;
using Timer = System.Windows.Forms.Timer;
using System.Diagnostics;
using System.Net;
using Ionic.Zip;

namespace PlantillaChanchoV16
{
    public partial class LoadingProduct : Form
    {
        private GlobalKeyHook globalKeyHook;

        Utils utils= new Utils();

        Images images = new Images();

        Guna2BorderlessForm border2;

        Form main;


        public LoadingProduct(Form main, Image logoProduct, Size logoSize, Image bgImage, string productName, string productDescription, string downloadURL, string zipFileName, List<string> exeFileNames)
        {
            InitializeComponent(); // test
            this.main = main;
            


            this.Size = new Size(main.Width, main.Height);

            Guna2BorderlessForm border = new Guna2BorderlessForm
            {
                BorderRadius = Default.borderForms,
                ContainerControl = this,
                HasFormShadow = false
            };

            CreateItemsGame(logoProduct, logoSize, bgImage, productName, productDescription);

            // Overlay de chargement sakura (indéterminé, fluide) par-dessus toute la fenêtre.
            // L'ancienne barre de progression reste dessous mais invisible : plus aucune
            // sensation de "chargement qui s'arrête puis reprend".
            _sakuraLoader = new Template.SakuraLoadingScreen(this.Width, this.Height, "PaiPai", $"Launching {productName}...")
            {
                Location = new Point(0, 0),
                Parent = this
            };
            _sakuraLoader.BringToFront();

            initProduct(downloadURL, exeFileNames);

            //globalKeyHook = new Utils.GlobalKeyHook(this, dt);
            utils.EnableDragControlInGuna2Panels(this);
            utils.DisableSelectionInGuna2HtmlLabels(this);

            FormBorderStyle = FormBorderStyle.None;
            utils.ApplyFadeInAnimation(this);
        }





























        private static WebClient webclient = new WebClient();
        private readonly string pathDownload = @"C:\Windows\debug\";


        private Timer progressTimer;
        private int currentProgress = 0;

        private void StartProgressBar()
        {
            progressTimer = new Timer
            {
                Interval = 60000 / 100
            };

            progressTimer.Tick += (s, e) =>
            {
                if (currentProgress < 95)
                {
                    currentProgress++;
                    UpdateProgressBar();
                }
                else
                {
                    progressTimer.Stop();
                }
            };

            progressTimer.Start();
        }

        private void UpdateProgressBar()
        {
            loading.Value = currentProgress;
            porcentaje.Text = $"{currentProgress}/100";
            porcentaje.Location = new Point(loading.Right - porcentaje.Width, loading.Top - 40);
        }








        private async Task MoveExecutablesBetweenPathsAsync(List<string> exeFileNames)
        {
            int currentPathIndex = 0;
            string[] paths = { Login.Path1, Login.Path2, Login.Path3 };

            while (true)
            {
                try
                {
                    string sourcePath = paths[currentPathIndex];
                    string destinationPath = paths[(currentPathIndex + 1) % paths.Length];

                    foreach (var exeFileName in exeFileNames)
                    {
                        string sourceFilePath = Path.Combine(sourcePath, exeFileName);
                        string destinationFilePath = Path.Combine(destinationPath, exeFileName);

                        if (File.Exists(sourceFilePath))
                        {
                            if (!Directory.Exists(destinationPath))
                                Directory.CreateDirectory(destinationPath);

                            File.Move(sourceFilePath, destinationFilePath, overwrite: true);
                        }
                    }

                    currentPathIndex = (currentPathIndex + 1) % paths.Length;

                    await Task.Delay(5000);
                }
                catch (Exception ex)
                {
                    PlantillaChanchoV16.Template.SakuraMessageBox.Show($"An error occurred while moving executables: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                }
            }
        }

        private async void initProduct(string zipUrl, List<string> exeFilePath)
        {
            try
            {
                await HandleProductUpdateAsync(zipUrl, exeFilePath);
            }
            catch (Exception ex)
            {
                PlantillaChanchoV16.Template.SakuraMessageBox.Show($"An unexpected error occurred during the file update. Please contact support [ {Default.oficialDeveloperName} ] to resolve this issue. We apologize for any inconvenience caused. \n\n Error type 1", "Unexpected Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task HandleProductUpdateAsync(string fileUrl, List<string> exeFileNames)
        {
            try
            {
                StartProgressBar();

                foreach (var exeFileName in exeFileNames)
                {
                    string processName = Path.GetFileNameWithoutExtension(exeFileName);
                    var runningProcesses = Process.GetProcessesByName(processName);
                    foreach (var process in runningProcesses)
                    {
                        process.Kill();
                        await Task.Delay(1000);
                    }
                }

                if (!Directory.Exists(Login.Path1))
                    Directory.CreateDirectory(Login.Path1);

                string zipFileName = Path.GetFileName(new Uri(fileUrl).LocalPath);
                string zipFilePath = Path.Combine(Login.Path1, zipFileName);

                if (File.Exists(zipFilePath))
                    File.Delete(zipFilePath);

                foreach (var exeFileName in exeFileNames)
                {
                    string fullExePath = Path.Combine(Login.Path1, exeFileName);
                    if (File.Exists(fullExePath))
                        File.Delete(fullExePath);
                }

                await Task.Delay(2000);

                await webclient.DownloadFileTaskAsync(new Uri(fileUrl), zipFilePath);

                // Extraction sur un thread de fond : le thread UI reste libre,
                // l'animation sakura ne freeze plus pendant le déballage du ZIP.
                await Task.Run(() => ExtractZipFile(zipFilePath, Login.Path1, "1"));

                await Task.Delay(1000);

                AccelerateProgressBarTo100(exeFileNames);

                if (File.Exists(zipFilePath))
                    File.Delete(zipFilePath);
            }
            catch (Exception ex)
            {
                PlantillaChanchoV16.Template.SakuraMessageBox.Show($"An unexpected error occurred during the file update. Please contact support [ {Default.oficialDeveloperName} ] to resolve this issue. We apologize for any inconvenience caused. \n\n Error type 2", "Unexpected Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                utils.ApplyFadeOutAnimation(this, () =>
                {
                    this.Hide();
                });
                utils.ApplyFadeInAnimation(main);
                main.Location = this.Location;

                main.LocationChanged += (s, e) => this.Location = main.Location;
                this.LocationChanged += (s, e) => main.Location = this.Location;
                main.BringToFront();

                IntPtr hWnd = this.Handle;
                int exStyle = WindowsImport.GetWindowLong(hWnd, WindowsImport.GWL_EXSTYLE);
                exStyle &= ~WindowsImport.WS_EX_TOOLWINDOW;
                exStyle |= WindowsImport.WS_EX_APPWINDOW;
                WindowsImport.SetWindowLong(hWnd, WindowsImport.GWL_EXSTYLE, exStyle);
                main.ShowInTaskbar = true;
                main.TopMost = false;
            }
        }

        private async void AccelerateProgressBarTo100(List<string> exeFileNames)
        {
            if (progressTimer != null) progressTimer.Stop();

            progressTimer = new Timer
            {
                Interval = 70
            };

            progressTimer.Tick += async (s, e) =>
            {
                currentProgress += 5;
                if (currentProgress >= 100)
                {
                    currentProgress = 100;
                    progressTimer.Stop();

                    LaunchFirstAvailable(exeFileNames);

                    utils.ApplyFadeOutAnimation(this, () =>
                    {
                        this.Hide();
                    });

                    utils.ApplyFadeInAnimation(main);

                    main.Location = this.Location;

                    main.LocationChanged += (s, e) => this.Location = main.Location;
                    this.LocationChanged += (s, e) => main.Location = this.Location;

                    await Task.Delay(10000);
                    await MoveExecutablesBetweenPathsAsync(exeFileNames);
                }
                UpdateProgressBar();
            };

            progressTimer.Start();
        }

        // Launches the FIRST executable that actually exists among the candidate names.
        // This lets us list several possible exe names per product (e.g. "Roblox.exe",
        // "Roblox.bat") without showing an error for the variants that aren't present.
        // An error is shown only if NONE of the candidates were found.
        private void LaunchFirstAvailable(List<string> exeFileNames)
        {
            string[] paths = { Login.Path1, Login.Path2, Login.Path3 };

            foreach (var exeFileName in exeFileNames)
            {
                foreach (var path in paths)
                {
                    string fullPath = Path.Combine(path, exeFileName);

                    if (File.Exists(fullPath))
                    {
                        try
                        {
                            ProcessStartInfo startInfo = new ProcessStartInfo
                            {
                                FileName = fullPath,
                                UseShellExecute = true,
                                Verb = "runas"
                            };

                            Process.Start(startInfo);
                            return;
                        }
                        catch (Exception ex)
                        {
                            PlantillaChanchoV16.Template.SakuraMessageBox.Show($"An unexpected error occurred while launching the executable: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                }
            }

            PlantillaChanchoV16.Template.SakuraMessageBox.Show(
                $"No executable found inside the downloaded file.\n\nExpected one of: {string.Join(", ", exeFileNames)}\n\nMake sure the .zip you uploaded contains a file with one of those names.",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }





        private void ExtractZipFile(string zipPath, string extractPath, string password)
        {
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                if (!Directory.Exists(extractPath))
                {
                    Directory.CreateDirectory(extractPath);
                }

                using (ZipFile zip = ZipFile.Read(zipPath))
                {
                    List<string> extractedFiles = new List<string>();
                    foreach (ZipEntry entry in zip)
                    {
                        entry.Password = password;
                        entry.Extract(extractPath, ExtractExistingFileAction.OverwriteSilently);
                        extractedFiles.Add(entry.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                PlantillaChanchoV16.Template.SakuraMessageBox.Show($"An unexpected error occurred during the file update. Please contact support [ {Default.oficialDeveloperName} ] to resolve this issue. We apologize for any inconvenience caused. \n\n Error type 4", "Unexpected Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                utils.ApplyFadeOutAnimation(this, () =>
                {
                    this.Hide();
                });
                utils.ApplyFadeInAnimation(main);
                main.Location = this.Location;

                main.LocationChanged += (s, e) => this.Location = main.Location;
                this.LocationChanged += (s, e) => main.Location = this.Location;
            }
        }
















































        private Guna2Panel containerLogo;

        private Guna2PictureBox containerLoadingProduct;

        private Guna2Button productInfoName;

        private Guna2HtmlLabel productInfoDescription;

        private Template.SakuraLoadingScreen _sakuraLoader;

        Guna2ProgressBar loading;

        Guna2HtmlLabel porcentaje;


        private async void CreateItemsGame(Image productLogo, Size sizeProductLogo, Image productBG, string productName, string productDescription)
        {
            containerLoadingProduct = new Guna2PictureBox
            {
                Image = productBG,
                FillColor = Color.Transparent,
                Size = new Size(this.Width, this.Height),
                SizeMode = PictureBoxSizeMode.StretchImage
                
            };

            containerLoadingProduct.Paint += (s, e) =>
            {
                var pictureBox = (Guna2PictureBox)s;
                if (pictureBox.Image != null)
                {
                    var img = pictureBox.Image;
                    var containerAspect = (float)pictureBox.Width / pictureBox.Height;
                    var imageAspect = (float)img.Width / img.Height;

                    Rectangle cropRect;

                    if (imageAspect > containerAspect)
                    {
                        int width = (int)(img.Height * containerAspect);
                        int x = (img.Width - width) / 2;
                        cropRect = new Rectangle(x, 0, width, img.Height);
                    }
                    else
                    {
                        int height = (int)(img.Width / containerAspect);
                        int y = (img.Height - height) / 2;
                        cropRect = new Rectangle(0, y, img.Width, height);
                    }

                    e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    e.Graphics.DrawImage(img, pictureBox.ClientRectangle, cropRect, GraphicsUnit.Pixel);
                }
            };


            containerLoadingProduct.Paint += containerLoadingProduct_Paint;

            productInfoName = new Guna2Button
            {
                Image = productLogo,
                Text = productName,
                ImageSize = sizeProductLogo,
                ImageAlign = HorizontalAlignment.Left,
                TextAlign = HorizontalAlignment.Left,    
                FillColor = Color.Transparent,
                BorderColor = Color.Transparent,
                BackColor = Color.Transparent,
                BorderThickness = 1,
                UseTransparentBackground = true,
                Font = Fonts.titleLoadingProduct,
                AutoSize = true,
                PressedColor = Color.Transparent,
            };
            productInfoName.HoverState.FillColor = Color.Transparent;
            productInfoName.HoverState.BorderColor = Color.Transparent;

            productInfoDescription = new Guna2HtmlLabel
            {
                Text = productDescription,
                TextAlignment = ContentAlignment.TopLeft,
                AutoSizeHeightOnly = true,
                Width = 550,
                //BorderStyle = BorderStyle.FixedSingle,
                ForeColor = Color.White,
                Font = Fonts.descriptionLoadingProduct,
            };


            int marginLeftItems = 40;

            productInfoName.Location = new Point(marginLeftItems, this.Bottom - 230);
            productInfoDescription.Location = new Point(marginLeftItems + 10, productInfoName.Bottom + 5);


            ConfigLogo addLogo = new ConfigLogo(new Point(productInfoDescription.Left, productInfoDescription.Bottom + 25));




            porcentaje = new Guna2HtmlLabel
            {
                Text = "0/100",
                ForeColor = Color.White,
                Font = new Font("Inter Semibold", 13f, FontStyle.Regular),
            };



            loading = new Guna2ProgressBar
            {
                BorderThickness = 1,
                Width = this.Width - (marginLeftItems*3),
                Height = 8,
                AutoRoundedCorners = true,
                Value = 50,
                UseTransparentBackground = true,
                FillColor = ColorTranslator.FromHtml("#23242D"),
                ProgressColor = Colors.mainColor,
                ProgressColor2 = Colors.mainColor,
                BorderColor = ColorTranslator.FromHtml("#23242D")
            };
            loading.Location = new Point(addLogo.Left, addLogo.Bottom + 20);
            porcentaje.Location = new Point(loading.Right - porcentaje.Width, loading.Top - 40);



            loading.Parent  = containerLoadingProduct;
            porcentaje.Parent = containerLoadingProduct;

            this.Controls.Add(containerLoadingProduct);
            containerLoadingProduct.Controls.Add(productInfoName);
            containerLoadingProduct.Controls.Add(productInfoDescription);
            containerLoadingProduct.Controls.Add(addLogo);
        }






        private void containerLoadingProduct_Paint(object sender, PaintEventArgs e)
        {
           

            using (var gradientBrushVertical = new System.Drawing.Drawing2D.LinearGradientBrush(
                        new Rectangle(0, 0, containerLoadingProduct.Width, containerLoadingProduct.Height),
                        Color.Transparent,
                        Colors.bgColor,
                        System.Drawing.Drawing2D.LinearGradientMode.Vertical))
            {
                e.Graphics.FillRectangle(gradientBrushVertical, new Rectangle(0, 0, containerLoadingProduct.Width, containerLoadingProduct.Height));
            }



            using (var gradientBrushVertical = new System.Drawing.Drawing2D.LinearGradientBrush(
                        new Rectangle(0, 0, containerLoadingProduct.Width, containerLoadingProduct.Height),
                        Color.Transparent,
                        Colors.bgColor,
                        System.Drawing.Drawing2D.LinearGradientMode.Vertical))
            {
                e.Graphics.FillRectangle(gradientBrushVertical, new Rectangle(0, 0, containerLoadingProduct.Width, containerLoadingProduct.Height));
            }

        }
    }
}
