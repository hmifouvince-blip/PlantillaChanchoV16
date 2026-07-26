using Guna.UI2.WinForms;
using PlantillaChanchoV16.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static PlantillaChanchoV16.Utilities.Utils;
using static System.Windows.Forms.AxHost;
using Timer = System.Windows.Forms.Timer;
using Microsoft.VisualBasic.Logging;
using System.Diagnostics;
using System.Net;
using KeyAuth;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using System.Text.RegularExpressions;

namespace PlantillaChanchoV16
{
    public partial class DetailsProduct : Form
    {




        private GlobalKeyHook globalKeyHook;

        Utils utils = new Utils();
        Images images = new Images();

        Guna2CircleButton videoPreview;

        Guna2CircleButton expandedImage;

        Guna2BorderlessForm border2;

        LoadingProduct loadingProduct;
        Action closeDetails;

        Guna2Panel containerHero;

        static Main main;


        public DetailsProduct(
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
            InitializeComponent();


            int sizeGeneral = 100;


            this.Location = new Point(0,15);

            this.Width = 928 - sizeGeneral;
            this.Height = 618 - sizeGeneral;

            containerHero = new Guna2Panel
            {
                Size = this.Size,
                BorderColor = Color.White,
                BorderThickness = 0,
            };

            this.Controls.Add(containerHero);

            border2 = new Guna2BorderlessForm
            {
                HasFormShadow = false,
                ContainerControl = this,
                BorderRadius = 12
            };
            AddCircularProgressBar();
            CreateDetailsProduct(
                _subscriptionName_: __subscriptionName,
                productName: productName,
                productDescription: productDescription,
                _versionProduct_: __versionProduct,
                _lastUpdate_: __lastUpdate,
                productVideoURL: productVideoURL,
                logoRounded: logoRounded,
                productLogo: productLogo,
                image1: image1,
                image2: image2,
                image3: image3,
                image4: image4,
                video1: video1,
                requirements: requirements,
                features: features,
                linkDiscord: linkDiscord,
                openProduct: openProduct
            );




            this.BackColor = Colors.bgColor;








            ConfigureImage(imageProduct1);
            ConfigureImage(imageProduct2);
            ConfigureImage(imageProduct3);
            ConfigureImage(imageProduct4);

            imageTransparency[imageProduct1] = 0f;
            imageTransparency[imageProduct2] = 0.6f;
            imageTransparency[imageProduct3] = 0.6f;
            imageTransparency[imageProduct4] = 0.6f;

            activeImage = imageProduct1;

            foreach (var button in imageTransparency.Keys)
            {
                button.Invalidate();
            }









            FormBorderStyle = FormBorderStyle.None;
            SetUpExpandedView();
            this.BringToFront();
            //this.Controls.Add(head);
            //globalKeyHook = new Utils.GlobalKeyHook(this);
            //utils.EnableDragControlInGuna2Panels(this);
            utils.DisableSelectionInGuna2HtmlLabels(this);
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
                e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                e.Graphics.FillPath(new SolidBrush(button.FillColor), path);
                e.Graphics.DrawPath(new Pen(button.BorderColor, button.BorderThickness), path);
            };
        }

        Guna2CircleProgressBar progressBar;

        private void AddCircularProgressBar()
        {
            progressBar = new Guna2CircleProgressBar
            {
                Size = new Size(100, 100),
                ProgressColor = Colors.mainColor,
                ProgressColor2 = Colors.mainColor,
                FillColor = Colors.bgColor,
                ProgressStartCap = LineCap.Round,
                ProgressEndCap = LineCap.Round,
                FillThickness = 6,
                ProgressThickness = 6,
                
                BackColor = Color.Transparent,
                Value = 70,
                Animated = true,
                AnimationSpeed = 3f,
            };
            progressBar.Location = new Point((this.Width - progressBar.Width) / 2, (this.Height - progressBar.Height - 20) / 2);

            containerHero.Controls.Add(progressBar);
        }




        // CONTAINER MAIN
        Guna2Panel containerMain;

        // CONTAINER LEFT
        Guna2Panel containerLeftArea;
        Guna2Button imageProductBig;

        Guna2Panel containerSmallImage;
        Guna2Button imageProduct1;
        Guna2Button imageProduct2;
        Guna2Button imageProduct3;
        Guna2Button imageProduct4;
        Guna2Button videoProduct1;

        // CONTAINER RIGHT
        Guna2Panel containerRightArea;

        private Action openProduct;



        private Guna2Panel expandedPanel;
        private Guna2PictureBox bigImage;
        private Guna2CircleButton closeButton;

        private void SetUpExpandedView()
        {
            if (expandedPanel == null)
            {
                expandedPanel = new Guna2Panel
                {
                    BackColor = Color.Transparent,
                    Size = new Size(this.Width, this.Height),
                    UseTransparentBackground = true,
                    FillColor = Color.FromArgb(190, 0, 0, 0),
                    Visible = false
                };

                int x = (this.Width - expandedPanel.Width) / 2;
                int y = (this.Height - expandedPanel.Height) / 2;
                expandedPanel.Location = new Point(x, y);
                this.Controls.Add(expandedPanel);

                bigImage = new Guna2PictureBox
                {
                    Image = imageProductBig.Image,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    UseTransparentBackground = false,
                    
                };

                int newWidth = this.Width - 100;
                int newHeight = (int)((float)bigImage.Image.Height / bigImage.Image.Width * newWidth);
                bigImage.Size = new Size(newWidth, newHeight);

                bigImage.Location = new Point(
                    (expandedPanel.Width - bigImage.Width) / 2,
                    (expandedPanel.Height - bigImage.Height) / 2 - 35
                );
               
                expandedPanel.Controls.Add(bigImage);

                closeButton = new Guna2CircleButton
                {
                    Image = Utils.ChangeIconsColor(new Bitmap(images.CloseIcon), Color.White),
                    Size = new Size(40, 40),
                    UseTransparentBackground = true,
                    FillColor = Color.FromArgb(200, 0, 0, 0),
                    BackColor = Color.Transparent,
                    Visible = false
                };

                closeButton.Animated = true;
                closeButton.Cursor = Cursors.Hand;
                closeButton.HoverState.FillColor = Color.FromArgb(230, 255, 95, 87);

                closeButton.Location = new Point(
                    bigImage.Right - closeButton.Width - 30,
                    bigImage.Top + 30
                );

                closeButton.Click += async (sender, e) =>
                {
                    //bigImage.Visible = false;
                    //await Task.Delay(100);
                    expandedPanel.Visible = false;
                    //transitionPanels.Hide(expandedPanel);
                };

                expandedPanel.Controls.Add(closeButton);
                expandedPanel.BringToFront();

                expandedPanel.Invalidate();
                expandedPanel.Refresh();
            }

        }



        private async void CreateDetailsProduct(string _subscriptionName_, string productName, string productDescription, string _versionProduct_, string _lastUpdate_, string productVideoURL, bool logoRounded, Image productLogo, Image image1, Image image2, Image image3, Image image4, Image video1, List<(string requirementsText, Image icon, Color iconColor, string link)> requirements, List<(string featureText, Image icon, Color iconColor)> features, string linkDiscord, Action openProduct)
        {
            //Random random = new Random();

            //int delay = random.Next(2500, 3501);

            //await Task.Delay(delay);

            int separationSmallImages = 12;

            containerMain = new Guna2Panel
            {
                FillColor = Color.Transparent,
                BackColor = Color.Transparent,
                AutoSize = true,
                BorderColor = Color.Wheat,
                BorderThickness = 0,
            };





            // LEFT AREA


            #region leftArea

            containerLeftArea = new Guna2Panel
            {
                BorderColor = Color.Green,
                BorderThickness = 0,
                Height = this.Height,
                FillColor = Colors.bgColor,
                BackColor = Colors.bgColor,
                UseTransparentBackground = false,
                Width = 415,
                AutoSize = false,
                Location = new Point(0, 0),
            };

            imageProductBig = new Guna2Button
            {
                Image = image1,
                Width = containerLeftArea.Width - 5,
                FillColor = Color.Transparent,
                BorderColor = Colors.bgColor,
                BorderThickness = 2,
                Height = 231, // ANTES 386
            };

            //PlantillaChanchoV16.Template.SakuraMessageBox.Show(imageProductBig.Width.ToString());

            imageProductBig.Location = new Point(0, imageProductBig.Location.Y);

            // Fin lisere d'accent sous l'image hero (meme langage visuel que les cartes
            // produits + le rail) : ancre le regard, coherent avec le reste du relook.
            imageProductBig.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var line = new Rectangle(0, imageProductBig.Height - 3, imageProductBig.Width, 3);
                using (var lg = new LinearGradientBrush(line,
                        Color.FromArgb(0, Colors.mainColor),
                        Color.FromArgb(150, Colors.mainColor),
                        LinearGradientMode.Horizontal))
                {
                    lg.SetSigmaBellShape(0.5f);
                    g.FillRectangle(lg, line);
                }
            };

            expandedImage = new Guna2CircleButton
            {
                Size = new Size(42, 42),
                Image = Utils.ChangeIconsColor(new Bitmap(Images.ExpandedIcon), Color.White),
                FillColor = Color.FromArgb(200, 0, 0, 0),
                BackColor = Color.Transparent,
                UseTransparentBackground = true,
                Location = new Point(200, 200),
                Visible = true,
            };

            int margin = 10;
            expandedImage.Location = new Point(
                imageProductBig.Width - expandedImage.Width - margin,
                imageProductBig.Height - expandedImage.Height - margin
            );



            int reduceSizeExpanedIcon = 20;

            expandedImage.Click += async (sender, e) =>
            {
                closeButton.Visible = false;
                await Task.Delay(100);
                //transitionPanels.Show(bigImage);
                expandedPanel.Visible = true;
                //await Task.Delay(100);
                bigImage.Visible = true;
                closeButton.BringToFront();

                int newWidth = this.Width - 200;
                int newHeight = (int)((float)bigImage.Image.Height / bigImage.Image.Width * newWidth);
                bigImage.Size = new Size(newWidth, newHeight);

                bigImage.Location = new Point(
                 bigImage.Location.X,
                 (expandedPanel.Height - newHeight) / 2 - 35
             );

                closeButton.Location = new Point(
                    bigImage.Right - closeButton.Width - 30,
                    bigImage.Top + 30
                );

                await Task.Delay(1000);

                closeButton.Visible = true;
                
            };



            expandedImage.ImageSize = new Size(expandedImage.Width - reduceSizeExpanedIcon, expandedImage.Height - reduceSizeExpanedIcon);
            expandedImage.BringToFront();


            imageProductBig.Paint += (s, e) =>
            {
                var pictureBox = (Guna2Button)s;
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











            containerSmallImage = new Guna2Panel
            {
                AutoSize = true,
                FillColor = Color.Transparent,
                BackColor = Color.Transparent,
                //BorderColor = Color.Red,
                BorderThickness = 1,
            };

            int adjustSizeImage = 5;
            int roundingSmallImage = 4;
            Size sizeSmallImage = new Size(133 - adjustSizeImage, 114 - adjustSizeImage);

            imageProduct1 = new Guna2Button
            {
                Image = image1,
                Size = sizeSmallImage,
                BorderRadius = roundingSmallImage,
                FillColor = Color.Transparent,
                BorderColor = Colors.bgColor,
                BorderThickness = 2,
                HoverState =
                {
                    BorderColor = Color.White,
                    
                }
                
            };

            imageProduct2 = new Guna2Button
            {
                Image = image2,
                Size = sizeSmallImage,
                BorderRadius = roundingSmallImage,
                FillColor = Color.Transparent,
                BorderColor = Colors.bgColor,
                BorderThickness = 2,
            };

            imageProduct3 = new Guna2Button
            {
                Image = image3,
                Size = sizeSmallImage,
                BorderRadius = roundingSmallImage,
                FillColor = Color.Transparent,
                BorderColor = Colors.bgColor,
                BorderThickness = 2,
            };
            imageProduct4 = new Guna2Button
            {
                Image = image4,
                Size = sizeSmallImage,
                BorderRadius = roundingSmallImage,
                FillColor = Color.Transparent,
                BorderColor = Colors.bgColor,
                BorderThickness = 2,
            };

            videoProduct1 = new Guna2Button
            {
                Image = video1,
                Size = new Size(sizeSmallImage.Width, (sizeSmallImage.Height*2) + separationSmallImages),
                BorderRadius = roundingSmallImage,
                FillColor = Color.Transparent,
                BorderColor = Colors.bgColor,
                BorderThickness = 3,
            };

            videoPreview = new Guna2CircleButton
            {
                Parent = videoProduct1,
                Size = new Size(45,45),
                Image = Utils.ChangeIconsColor(new Bitmap(images.PlayVideoIcon), Color.White),
                UseTransparentBackground = true,
                BackColor = Color.Transparent,
                FillColor = Color.FromArgb(200, 0, 0, 0),
                ImageSize = new Size(22, 22)
            };
            videoPreview.BringToFront();
            videoPreview.Location = new Point((videoProduct1.Width - videoPreview.Width) / 2, (videoProduct1.Height - videoPreview.Height) / 2);


            videoPreview.Click += (s, e) => { utils.OpenLink(productVideoURL); };










            imageProduct1.Paint += (s, e) =>
            {
                

                var pictureBox = (Guna2Button)s;
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





            imageProduct2.Paint += (s, e) =>
            {
                var pictureBox = (Guna2Button)s;
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







            imageProduct3.Paint += (s, e) =>
            {
                var pictureBox = (Guna2Button)s;
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

            imageProduct4.Paint += (s, e) =>
            {
                var pictureBox = (Guna2Button)s;
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

            videoProduct1.Paint += (s, e) =>
            {
                var pictureBox = (Guna2Button)s;
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

                    using (var brush = new SolidBrush(Color.FromArgb(128, 0, 0, 0)))
                    {
                        e.Graphics.FillRectangle(brush, pictureBox.ClientRectangle);
                    }
                }
            };















            imageProduct1.Location = new Point(0, 0);
            imageProduct2.Location = new Point(imageProduct1.Right + separationSmallImages, 0);
            imageProduct3.Location = new Point(imageProduct1.Left, imageProduct1.Bottom + separationSmallImages);
            imageProduct4.Location = new Point(imageProduct2.Left, imageProduct2.Bottom + separationSmallImages);
            videoProduct1.Location = new Point(imageProduct2.Right + separationSmallImages, imageProduct1.Top);


            containerSmallImage.Location = new Point(imageProductBig.Left, imageProductBig.Bottom + separationSmallImages);

            imageProduct1.Click += (sender, e) =>
            {
                imageProductBig.Image = image1;
                bigImage.Image = imageProductBig.Image;
                expandedPanel.Invalidate();
                expandedPanel.Refresh();
            };

            imageProduct2.Click += (sender, e) =>
            {
                imageProductBig.Image = image2;
                bigImage.Image = imageProductBig.Image;
                expandedPanel.Invalidate();
                expandedPanel.Refresh();
            };

            imageProduct3.Click += (sender, e) =>
            {
                imageProductBig.Image = image3;
                bigImage.Image = imageProductBig.Image;
                expandedPanel.Invalidate();
                expandedPanel.Refresh();
            };
            imageProduct4.Click += (sender, e) =>
            {
                imageProductBig.Image = image4;
                bigImage.Image = imageProductBig.Image;
                expandedPanel.Invalidate();
                expandedPanel.Refresh();
            };










            containerSmallImage.Controls.Add(imageProduct1);
            containerSmallImage.Controls.Add(imageProduct2);
            containerSmallImage.Controls.Add(imageProduct3);
            containerSmallImage.Controls.Add(imageProduct4);
            containerSmallImage.Controls.Add(videoProduct1);

            containerLeftArea.Controls.Add(imageProductBig);
            imageProductBig.Controls.Add(expandedImage);
            containerLeftArea.Controls.Add(containerSmallImage);

            containerHero.Controls.Add(containerMain);
            containerMain.Controls.Add(containerLeftArea);
            containerMain.Controls.Add(containerRightArea);



            #endregion leftArea




            #region rightArea



            containerRightArea = new Guna2Panel
            {
                //Dock = DockStyle.Right,
                Location = new Point(containerLeftArea.Right + 12, 0),
                Width = this.Width - containerLeftArea.Width - 125,
                BackColor = Colors.bgColor,
                BorderColor = Color.Yellow,
                BorderThickness = 0,
                UseTransparentBackground = false,
                Height = imageProductBig.Height + containerSmallImage.Height + separationSmallImages
            };


            // PRODUCT NAME AND LOGO

            containerProductName = new Guna2Panel
            {
                FillColor = Color.Transparent,
                BackColor = Color.Transparent,
                //BorderColor = Color.White,
                BorderThickness = 1,
                UseTransparentBackground = true,
                AutoSize = true,
                Width = containerRightArea.Width,
                Height = 60,
                Location = new Point(0,0),
            };


            logoProduct = new Guna2Button
            {
                Image = productLogo,
                Width = 50,
                Height = 50,
                UseTransparentBackground = true,
                //SizeMode = PictureBoxSizeMode.Zoom,
                //BorderStyle = BorderStyle.FixedSingle,
                //BorderRadius = 6,
                BorderColor = Colors.bgColor,
                BorderThickness = 1,
                FillColor = Color.Transparent,
            };
            logoProduct.ImageSize = new Size(logoProduct.Width, logoProduct.Height);

            logoProduct.HoverState.BorderColor = Color.Transparent;
            logoProduct.HoverState.FillColor = Color.Transparent;
            logoProduct.PressedColor = Color.Transparent;


            nameProduct = new Label
            {
                Text = productName,
                ForeColor = Color.White,
                Font = new Font("Inter Semibold", 16.4f, FontStyle.Regular),
                AutoSize = false,
                Height = logoProduct.Height,
                TextAlign = ContentAlignment.MiddleLeft,
                //BorderStyle = BorderStyle.FixedSingle,
            };
            nameProduct.Width = containerRightArea.Width - logoProduct.Width - 30;

            logoProduct.Location = new Point(1, (containerProductName.Height - logoProduct.Height) / 2);
            nameProduct.Location = new Point(logoProduct.Right + 5, (containerProductName.Height - nameProduct.Height) / 2);


            containerProductName.Controls.Add(logoProduct);
            containerProductName.Controls.Add(nameProduct);


            containerMain.Controls.Add(containerRightArea);
            containerRightArea.Controls.Add(containerProductName);






            // TAB BUTTONS PRODUCT [ ABOUT, REQUIREMENTS, FEATURES ]

            containerTabBtnProduct = new Guna2Panel
            {
                BackColor = Colors.bgColor,
                BorderColor = Color.Transparent,
                BorderThickness = 1,
                Height = 60,
                Width = containerRightArea.Width
            };
            containerTabBtnProduct.Location = new Point(0, containerProductName.Bottom + 5);

            InitializeIndicatorPanel();



            btnAbout = CreateButtonsTab(0, new Point(0,0), "About", () =>
            {
                containerTab2.Visible = false;
                containerTab3.Visible = false;
                containerTab1.Visible = true;
                RelayoutRightArea();
            });
            btnRequirements = CreateButtonsTab(1, new Point(0, 2), "Requirements", () =>
            {
                containerTab1.Visible = false;
                containerTab3.Visible = false;
                containerTab2.Visible = true;
                RelayoutRightArea();
            });
            btnFeatures = CreateButtonsTab(2, new Point(0, 0), "Features", () =>
            {
                containerTab1.Visible = false;
                containerTab2.Visible = false;
                containerTab3.Visible = true;
                RelayoutRightArea();
            });
            containerIndicatorPanel = new Guna2Panel
            {
                FillColor = Colors.divider,
                BorderColor = Colors.divider,
                BorderThickness = 0,
                Width = containerRightArea.Width,
                Height = 2,
            };
            containerIndicatorPanel.Location = new Point(0, 51);

            containerTab1 = new Guna2Panel
            {
                BackColor = Colors.bgColor,
                UseTransparentBackground = false,
                Visible = false,
                AutoSize = true,
                BorderColor = Color.Green,
                BorderThickness = 0,
                Width = containerRightArea.Width,
            };
            containerTab2 = new Guna2Panel
            {
                BackColor = Colors.bgColor,
                BorderColor = Color.Green,
                BorderThickness = 0,
                UseTransparentBackground = false,
                Visible = false,
                AutoSize = false,
                Height = 200,
                Width = containerRightArea.Width,
            };
            containerTab3 = new Guna2Panel
            {
                BackColor = Colors.bgColor,
                BorderColor = Color.Green,
                BorderThickness = 0,
                UseTransparentBackground = false,
                Visible = false,
                AutoSize = false,
                Height = 200,
                Width = containerRightArea.Width,
            };


            containerTab1.Location = new Point(0, containerTabBtnProduct.Bottom + 10);
            containerTab2.Location = new Point(0, containerTabBtnProduct.Bottom + 10);
            containerTab3.Location = new Point(0, containerTabBtnProduct.Bottom + 10);


            // INFO TAB 1

            descriptionProduct = new Guna2HtmlLabel
            {
                Text = $"<div style='line-height: 1.6;'>{productDescription}</div>", // PARAMETRO
                Font = new Font("Inter Medium", 10.5f, FontStyle.Regular),
                ForeColor = Colors.textMuted,
                AutoSizeHeightOnly = true,
                Width = containerTab1.Width,
                TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias,
                UseGdiPlusTextRendering = true,
                IsSelectionEnabled = false
            };

            descriptionProduct.Location = new Point(0, descriptionProduct.Location.Y);

           

            separatorUpdates = new Guna2Separator
            {
                Width = containerRightArea.Width,
                FillColor = Color.FromArgb(10, 255, 255, 255),
                UseTransparentBackground = true,

            };
            separatorUpdates.Location = new Point(0, descriptionProduct.Bottom + 46);



            versionProduct = CreateStyledLabel("Version:", Color.White, Point.Empty);
            versionProduct.Location = new Point(separatorUpdates.Left, separatorUpdates.Top - versionProduct.Height - 5);

            dateVersionProduct = CreateStyledLabel(_versionProduct_, Colors.textMuted, Point.Empty);
            dateVersionProduct.Location = new Point(separatorUpdates.Right - dateVersionProduct.Width - 5, separatorUpdates.Top - dateVersionProduct.Height);

            lastUpdateProduct = CreateStyledLabel("Last update:", Color.White, Point.Empty);
            lastUpdateProduct.Location = new Point(separatorUpdates.Left, separatorUpdates.Bottom + 5);

            dateLastUpdateProduct = CreateStyledLabel(_lastUpdate_, Colors.textMuted, Point.Empty);
            dateLastUpdateProduct.Location = new Point(separatorUpdates.Right - dateLastUpdateProduct.Width - 5, separatorUpdates.Bottom + 5);



   











            // INFO TAB 2 REQUIREMENTS

            containerTab2.Width = separatorUpdates.Width;
            containerTab2.Location = new Point(separatorUpdates.Left, containerTab2.Location.Y);



            containerListRequirements = new Guna2Panel
            {
                AutoScroll = false,
                Width = containerRightArea.Width,
                Location = new Point(0, 0),
                Height = containerTab2.Height,
                BorderColor = Color.Blue,
                BorderThickness = 1,
            };






            int verticalSpacing = 10; // Espacio entre los botones
            int currentYPosition = 0; // Posici�n inicial de Y para el primer bot�n

            AddRequirementsToContainer(requirements);


            containerTab2.Controls.Add(containerListRequirements);

            // INFO TAB 3 FEATURES 
            containerTab3.Width = separatorUpdates.Width;
            containerTab3.Location = new Point(separatorUpdates.Left, containerTab2.Location.Y);

            containerListFeatures = new Guna2Panel
            {
                AutoScroll = false,
                Width = containerRightArea.Width,
                Location = new Point(0, 0),
                Height = containerTab3.Height,
            };



            AddFeaturesToContainer(features);
            containerTab3.Controls.Add(containerListFeatures);






            // PAS de Dock.Bottom ici : containerRightArea a une hauteur FIGÉE sur la
            // colonne média de gauche, alors que l'onglet About est en AutoSize. Avec
            // une description longue (Spoofer et ses 6 puces), le contenu débordait
            // sous le bouton ancré en bas -> LAUNCH se retrouvait rogné hors du
            // viewport, donc incliquable. Il est désormais positionné explicitement
            // sous le contenu réel par RelayoutRightArea(), rappelé à chaque
            // changement d'onglet (les trois onglets n'ont pas la même hauteur).
            containerActivateProduct = new Guna2Panel
            {
                BorderColor = Color.Red,
                BorderThickness = 0,
                AutoSize = true,
                BackColor = Colors.bgColor,
                UseTransparentBackground = false,
            };

            int heightItemsActivate = 40;
            int radiusItemsActivate = 4;

            // Plus de saisie de clé ici : l'accès au produit est déjà verrouillé en amont
            // (il faut avoir "claim" la licence via "Add license" sur l'accueil pour même
            // pouvoir ouvrir cette fiche). LAUNCH lance donc directement le produit.


           







            btnActivateProduct = new Guna2Button
            {
                Text = AddSpaceBetweenLetters("LAUNCH"),
                Font = new Font("Inter Medium", 10.9f, FontStyle.Regular),
                Image = ChangeIconsColor(new Bitmap(images.PlayIcon), Color.White),
                ImageSize = new Size(13,14),
                ImageOffset = new Point(10,0),
                TextOffset = new Point(0,0),
                ImageAlign = HorizontalAlignment.Left,
                FillColor = Colors.mainColor,
                BorderColor = Colors.mainColor,
                BorderThickness = 1,
                UseTransparentBackground = true,
                Animated = true,
                Width = separatorUpdates.Width,
                BorderRadius = radiusItemsActivate,
                Height = heightItemsActivate,
            };
            btnActivateProduct.Location = new Point(0, 0);
            btnActivateProduct.BorderThickness = 0;
            btnActivateProduct.Cursor = Cursors.Hand;
            btnActivateProduct.HoverState.FillColor = System.Windows.Forms.ControlPaint.Light(Colors.mainColor, 0.25f);
            btnActivateProduct.PressedColor = System.Windows.Forms.ControlPaint.Dark(Colors.mainColor, 0.04f);
            btnActivateProduct.ShadowDecoration.Enabled = true;
            btnActivateProduct.ShadowDecoration.Color = Color.FromArgb(130, Colors.mainColor);
            btnActivateProduct.ShadowDecoration.Depth = 9;
            btnActivateProduct.ShadowDecoration.Shadow = new Padding(4);
            Utilities.UiStyle.AddGlossySheen(btnActivateProduct);






            btnActivateProduct.Click += (sender, e) => openProduct.Invoke();




            lbRequestLicense = new Guna2HtmlLabel
            {
                Text = "<u>Report a bug</u>",
                ForeColor = Colors.mainColor,
                Font = new Font("Inter Semibold", 10.6f, FontStyle.Regular),
            };


            lbRequestLicense.Click += (s, e) =>
            {
                utils.OpenLink(linkDiscord);
            };

            lbRequestLicense.Location = new Point(btnActivateProduct.Right - lbRequestLicense.Width, btnActivateProduct.Bottom + 10);







            containerRightArea.Controls.Add(containerActivateProduct);
            containerActivateProduct.Controls.Add(btnActivateProduct);
            containerActivateProduct.Controls.Add(lbRequestLicense);

            // Recalé APRÈS l'ajout : un Guna2HtmlLabel en AutoSize ne connaît sa
            // largeur réelle qu'une fois rattaché à un parent. Calculée avant, la
            // position ci-dessus utilisait une largeur encore nulle -> le lien
            // partait à gauche au lieu d'être aligné à droite du bouton.
            lbRequestLicense.Location = new Point(
                Math.Max(0, btnActivateProduct.Right - lbRequestLicense.Width),
                btnActivateProduct.Bottom + 10);


            containerTab1.Controls.Add(descriptionProduct);
            //containerTab1.Controls.Add(textForLink);
            //containerTab1.Controls.Add(linkPreviewProduct);
            containerTab1.Controls.Add(versionProduct);
            containerTab1.Controls.Add(dateVersionProduct);
            containerTab1.Controls.Add(lastUpdateProduct);
            containerTab1.Controls.Add(dateLastUpdateProduct);
            containerTab1.Controls.Add(developerProduct);
            containerTab1.Controls.Add(nameDeveloperProduct);
            containerTab1.Controls.Add(separatorUpdates);




            containerRightArea.Controls.Add(containerTab1);
            containerRightArea.Controls.Add(containerTab2);
            containerRightArea.Controls.Add(containerTab3);





            //containerActivateProduct.Location = new Point(0, containerTab1.Bottom + 30);
            //containerActivateProduct.Width = containerRightArea.Width;

            containerListRequirements.BorderThickness = 0;
            containerListRequirements.BorderColor = Color.Yellow;

            containerListFeatures.BorderThickness = 0;
            containerListFeatures.BorderColor = Color.Red;


            containerTab2.BorderThickness = 0;
            containerTab2.BorderColor = Color.White;


            containerTab3.BorderThickness = 0;
            containerTab3.BorderColor = Color.White;

            btnAbout.PerformClick();

            containerRightArea.Controls.Add(containerTabBtnProduct);
            containerTabBtnProduct.Controls.Add(containerIndicatorPanel);


            // Rayon 10 au lieu de 4 : le reste de l'app (cartes du carrousel, panneaux
            // sakura, boutons Guna) est nettement plus arrondi. A 4 px, la galerie
            // gardait l'angle sec du template d'origine et jurait avec l'ensemble.
            RoundGunaButtonCorners(imageProductBig, 10);
            RoundGunaButtonCorners(imageProduct1, 8);
            RoundGunaButtonCorners(imageProduct2, 8);
            RoundGunaButtonCorners(imageProduct3, 8);
            RoundGunaButtonCorners(imageProduct4, 8);
            RoundGunaButtonCorners(videoProduct1, 8);

            if (!logoRounded)
            {
                RoundGunaButtonCorners(logoProduct, 5);
            }


            progressBar.Visible = false;

            this.Height = containerMain.Height;

            RelayoutRightArea();

            // Fond sakura : la fiche etait un aplat prune uni, sans la profondeur
            // qu'ont l'accueil et les autres ecrans.
            Utilities.UiStyle.AttachContentBackdrop(containerHero);

            // Une fois l'arbre complet : sans double tampon, cette fiche (des dizaines
            // de panneaux imbriques) scintille et saccade au moindre survol ou
            // changement d'onglet.
            Utilities.UiStyle.EnableDoubleBuffer(this);

            #endregion rightArea
        }

        // Replace le bloc LAUNCH sous le contenu RÉELLEMENT visible et redimensionne
        // la colonne de droite en conséquence.
        //
        // Indispensable parce que les trois onglets ont des hauteurs très
        // différentes (About est en AutoSize et suit la longueur de la description,
        // Requirements/Features ont une hauteur propre) : une position calculée une
        // seule fois à la construction serait fausse dès le premier changement
        // d'onglet. Sans ça, le bouton finissait sous la ligne de flottaison et le
        // produit devenait impossible à lancer.
        private void RelayoutRightArea()
        {
            if (containerRightArea == null || containerActivateProduct == null) return;

            int contentBottom = containerTabBtnProduct?.Bottom ?? 0;
            foreach (var tab in new[] { containerTab1, containerTab2, containerTab3 })
            {
                if (tab != null && tab.Visible) contentBottom = Math.Max(contentBottom, tab.Bottom);
            }

            containerActivateProduct.Width = containerRightArea.Width;
            containerActivateProduct.Location = new Point(0, contentBottom + 24);
            containerActivateProduct.BringToFront();

            // La colonne de droite ne peut jamais être plus courte que la colonne
            // média de gauche (sinon le fond se coupe au milieu de la fiche).
            // On lit containerSmallImage.Bottom plutôt que de recalculer la somme :
            // separationSmallImages est une variable LOCALE à la construction.
            int mediaHeight = containerSmallImage?.Bottom ?? containerLeftArea?.Height ?? 0;
            containerRightArea.Height = Math.Max(mediaHeight, containerActivateProduct.Bottom + 16);

            // Le conteneur parent doit suivre, sinon la zone agrandie reste hors
            // du viewport défilable et on n'atteint toujours pas le bouton.
            int needed = containerRightArea.Bottom + 24;
            if (containerMain != null && containerMain.Height < needed) containerMain.Height = needed;
            if (this.Height < needed) this.Height = needed;
        }

        private string AddSpaceBetweenLetters(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            char[] characters = text.ToCharArray();
            return string.Join(" ", characters);
        }









        private Timer fadeTimer;
        private Guna2Button activeImage = null;
        private readonly Dictionary<Guna2Button, float> imageTransparency = new();


        private void ConfigureImage(Guna2Button button)
        {
            button.Paint += (s, e) => DrawOverlay(e.Graphics, imageTransparency[button]);
            button.Click += (s, e) => UpdateTransparency(button);
        }

        private void UpdateTransparency(Guna2Button clickedImage)
        {
            foreach (var button in imageTransparency.Keys)
            {
                if (button == clickedImage)
                {
                    imageTransparency[button] = 0;
                }
                else
                {
                    imageTransparency[button] = 0.6f;
                }

                button.Invalidate();
            }

            activeImage = clickedImage;
        }

        private void DrawOverlay(Graphics graphics, float alpha)
        {
            Color overlayColor = Color.FromArgb((int)(alpha * 255), Color.Black);
            using (Brush brush = new SolidBrush(overlayColor))
            {
                graphics.FillRectangle(brush, 0, 0, imageProduct1.Width, imageProduct1.Height);
            }
        }




































        private void AddRequirementsToContainer(List<(string requirementsText, Image icon, Color iconColor, string link)> requirements)
        {
            int verticalSpacing = 2; // Espaciado entre los elementos
            int currentYPosition = 0; // Posici�n inicial en Y

            containerListRequirements.Controls.Clear();

            Guna2Panel contentPanel = new Guna2Panel
            {
                Location = new Point(0, 0),
                Width = containerRightArea.Width - 10,
                AutoSize = false,
                BorderColor = Color.White,
                BorderThickness = 0,
            };
            containerListRequirements.Controls.Add(contentPanel);

            Guna2VScrollBar scrollBar = new Guna2VScrollBar
            {
                Width = 8,
                FillColor = Colors.scColor,
                BorderColor = Colors.bgColor,
                ThumbColor = Colors.mainColor,
                Minimum = 0,
                Maximum = 0,
                Visible = true,
                AutoRoundedCorners = true,
                Height = containerTab2.Height
            };
            containerListRequirements.Controls.Add(scrollBar);

            scrollBar.Location = new Point(contentPanel.Right, 0);
            scrollBar.BringToFront();

            foreach (var requirement in requirements)
            {
                var buttonPanel = CreateButtonPanelRequirements(requirement.requirementsText, requirement.icon, requirement.iconColor, requirement.link);
                buttonPanel.Location = new Point(0, currentYPosition);
                contentPanel.Controls.Add(buttonPanel);
                currentYPosition += buttonPanel.Height + verticalSpacing;
            }

            int totalHeight = currentYPosition;

            if (totalHeight > containerListRequirements.Height)
            {
                scrollBar.Visible = true;
                scrollBar.Maximum = totalHeight - containerListRequirements.Height;
                scrollBar.LargeChange = containerListRequirements.Height / 2;
                scrollBar.SmallChange = 20;

                scrollBar.ValueChanged += (sender, e) =>
                {
                    contentPanel.Top = -scrollBar.Value;
                    contentPanel.Invalidate();
                };

                containerListRequirements.MouseWheel += (object sender, MouseEventArgs e) =>
                {
                    int newValue = scrollBar.Value - e.Delta / 3;

                    scrollBar.Value = Math.Max(scrollBar.Minimum, Math.Min(scrollBar.Maximum, newValue));


                    contentPanel.Top = -scrollBar.Value;

                    contentPanel.Invalidate();
                };
            }

            contentPanel.Height = totalHeight;
            contentPanel.Width = containerRightArea.Width;
        }











        private Guna2Panel CreateButtonPanelRequirements(string text, Image icon, Color iconColor, string link)
        {
            Guna2Button button = CreateRequirements(text, icon, iconColor, link);

            Guna2Panel panel = new Guna2Panel
            {
                Width = containerRightArea.Width,
                Height = button.Height + 8,
                BorderColor = Colors.divider,
                BorderThickness = 0,
                BackColor = Color.Transparent
            };

            button.Location = new Point(0, 0);
            panel.Controls.Add(button);

            return panel;
        }


        private Guna2Button CreateRequirements(string text, Image icon, Color iconColor, string link = null, EventHandler clickEventHandler = null)
        {
            Bitmap coloredIcon = ChangeIconsColor(new Bitmap(icon), iconColor);

            Guna2Button button = new Guna2Button
            {
                Text = text,
                Image = coloredIcon,
                ForeColor = Colors.textSubtle,
                Font = new Font("Inter", 9f, FontStyle.Regular),
                ImageAlign = HorizontalAlignment.Right,
                TextAlign = HorizontalAlignment.Left,
                PressedColor = Color.Transparent,
                BackColor = Color.Transparent,
                Height = 30,
                ImageSize = new Size(20, 20),
                FillColor = Colors.divider,
                BorderColor = Colors.divider,
                BorderThickness = 1,
                BorderRadius = 6,
                Width = containerRightArea.Width - 20,
            };

            if (!string.IsNullOrEmpty(link))
            {
                button.Click += (sender, e) =>
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new ProcessStartInfo
                        {
                            FileName = link,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        PlantillaChanchoV16.Template.SakuraMessageBox.Show("Error al abrir el enlace: " + ex.Message);
                    }
                };
            }

            if (clickEventHandler != null)
            {
                button.Click += clickEventHandler;
            }

            return button;
        }










        private void AddFeaturesToContainer(List<(string featureText, Image icon, Color iconColor)> features)
        {
            int verticalSpacing = 2; // Espacio entre los paneles
            int currentYPosition = 0; // Posici�n inicial de Y para los elementos

            containerListFeatures.Controls.Clear();

            Panel contentPanel = new Panel
            {
                Location = new Point(0, 0),
                Width = containerRightArea.Width - 10,
                AutoSize = false,
            };
            containerListFeatures.Controls.Add(contentPanel);

            Guna2VScrollBar scrollBar = new Guna2VScrollBar
            {
                Width = 8,
                FillColor = Colors.scColor,
                BorderColor = Colors.bgColor,
                ThumbColor = Colors.mainColor,
                Minimum = 0,
                Maximum = 0,
                Visible = true,
                AutoRoundedCorners = true,
                Height = containerTab3.Height
            };
            containerListFeatures.Controls.Add(scrollBar);

            scrollBar.Location = new Point(contentPanel.Right, 0);
            scrollBar.BringToFront();

            foreach (var feature in features)
            {
                var buttonPanel = CreateButtonPanelFeatures(feature.featureText, feature.icon, feature.iconColor);
                buttonPanel.Location = new Point(0, currentYPosition);
                contentPanel.Controls.Add(buttonPanel);
                currentYPosition += buttonPanel.Height + verticalSpacing;
            }

            int totalHeight = currentYPosition;

            if (totalHeight > containerListFeatures.Height)
            {
                scrollBar.Visible = true;
                scrollBar.Maximum = totalHeight - containerListFeatures.Height;
                scrollBar.LargeChange = containerListFeatures.Height / 2;
                scrollBar.SmallChange = 20;

                scrollBar.ValueChanged += (sender, e) =>
                {
                    contentPanel.Top = -scrollBar.Value;
                    contentPanel.Invalidate();
                };

                containerListFeatures.MouseWheel += (object sender, MouseEventArgs e) =>
                {
                    int newValue = scrollBar.Value - e.Delta / 3;

                    scrollBar.Value = Math.Max(scrollBar.Minimum, Math.Min(scrollBar.Maximum, newValue));


                    contentPanel.Top = -scrollBar.Value;
                    contentPanel.Invalidate();
                };
            }

            contentPanel.Height = totalHeight;
            contentPanel.Width = containerRightArea.Width;
        }






        private Guna2Panel CreateButtonPanelFeatures(string text, Image icon, Color iconColor)
        {
            Guna2Button button = CreateFeatures(text, icon, iconColor);

            Guna2Panel panel = new Guna2Panel
            {
                Width = containerRightArea.Width,
                Height = button.Height + 8, 
                BorderColor = Colors.divider,
                BorderThickness = 0,
                BackColor = Color.Transparent
            };

            button.Location = new Point(0, 0);
            panel.Controls.Add(button);

            return panel;
        }

        private Guna2Button CreateFeatures(string text, Image icon, Color iconColor)
        {
            Bitmap coloredIcon = ChangeIconsColor(new Bitmap(icon), iconColor);

            return new Guna2Button
            {
                Text = text,
                Image = coloredIcon,
                ForeColor = Colors.textSubtle,
                Font = new Font("Inter", 9f, FontStyle.Regular),
                ImageAlign = HorizontalAlignment.Right,
                TextAlign = HorizontalAlignment.Left,
                PressedColor = Color.Transparent,
                BackColor = Color.Transparent,
                Height = 30,
                ImageSize = new Size(20, 20),
                FillColor = Colors.divider,
                BorderColor = Colors.divider,
                BorderThickness = 1,
                BorderRadius = 6,
                Width = containerRightArea.Width - 20,

            };
        }









        private Guna2HtmlLabel CreateStyledLabel(string text, Color foreColor, Point location)
        {
            return new Guna2HtmlLabel
            {
                Text = text,
                Font = new Font("Inter Medium", 11.2f, FontStyle.Regular),
                ForeColor = foreColor,
                AutoSize = true,
                Location = location
            };
        }

        private void InitializeIndicatorPanel()
        {
            indicatorPanel = new Guna2Panel
            {
                Height = 3,
                Width = 28,
                FillColor = Colors.mainColor,
                BorderColor = Colors.mainColor,
                BorderThickness = 1,

                BorderRadius = 2,
                Visible = true
            };
            indicatorPanel.Location = new Point(0, 50);
            containerTabBtnProduct.Controls.Add(indicatorPanel);

            animationTimer = new Timer();
            animationTimer.Interval = 16;
            animationTimer.Tick += AnimatePanel;
        }




        private Guna2Button TabButton;
        private Guna2Button btnTab1, btnTab2, btnTab3, btnTab4;

        Guna2Button lastClickedButton = null;

        private Guna2Button CreateButtonsTab(int index, Point txOffset, string textTab, Action methodToExecute)
        {
            Guna2Button TabButton = new Guna2Button
            {
                Text = textTab,
                FillColor = Color.Transparent,
                BorderColor = Color.Yellow,
                BorderThickness = 0,
                ForeColor = Colors.textMuted,
                AutoSize = true,
                Width = 100,
                TextOffset = txOffset,
                Font = new Font("Inter Medium", 9.8f, FontStyle.Regular),
                TextAlign = HorizontalAlignment.Center,
                ButtonMode = Guna.UI2.WinForms.Enums.ButtonMode.RadioButton,
            };
            TabButton.PressedColor = Color.Transparent;
            TabButton.CheckedState.FillColor = Color.Transparent;
            TabButton.CheckedState.BorderColor = Color.Transparent;
            TabButton.HoverState.FillColor = Color.Transparent;
            TabButton.HoverState.BorderColor = Color.Transparent;
            TabButton.Cursor = Cursors.Hand;

            // Survol : le texte s'éclaircit (sauf sur l'onglet actif).
            TabButton.MouseEnter += (s, e) => { if (TabButton != lastClickedButton) TabButton.ForeColor = Color.FromArgb(225, 225, 232); };
            TabButton.MouseLeave += (s, e) => { if (TabButton != lastClickedButton) TabButton.ForeColor = Colors.textMuted; };



            



            int currentXPosition = 0;

            foreach (Control ctrl in containerTabBtnProduct.Controls)
            {
                if (ctrl is Guna2Button)
                {
                    currentXPosition += ctrl.Width + 0;
                }
            }

            TabButton.Location = new Point(currentXPosition, 13);






            TabButton.Click += async (sender, e) =>
            {
                foreach (Control ctrl in containerTabBtnProduct.Controls)
                {
                    if (ctrl is Guna2Button button)
                    {
                        button.ForeColor = Colors.textMuted;

                    }
                }

                TabButton.ForeColor = Color.White;
                



                if (lastClickedButton != TabButton)
                {
                    if (lastClickedButton != null)
                    {
                        lastClickedButton.Checked = false;
                    }

                    methodToExecute();
                    MoveIndicatorPanel(TabButton);

                    lastClickedButton = TabButton;
                }
            };


           

            containerTabBtnProduct.Controls.Add(TabButton);
            return TabButton;
        }

        private Guna2Panel indicatorPanel;
        private Timer animationTimer;
        private int targetX;
        private int startX;
        private int animationDuration = 150;
        private int elapsedTime = 0;



        private int startWidth;
        private int targetWidth;


        private int startY;
        private int targetY;

        private void MoveIndicatorPanel(Guna2Button activeButton)
        {
            startX = indicatorPanel.Location.X;
            targetX = activeButton.Location.X;

            startY = indicatorPanel.Location.Y;
            targetY = activeButton.Location.Y + activeButton.Height + 10;

            startWidth = indicatorPanel.Width;
            targetWidth = activeButton.Width;

            elapsedTime = 0;
            animationTimer.Start();
        }

        private void AnimatePanel(object sender, EventArgs e)
        {
            elapsedTime += animationTimer.Interval;
            if (elapsedTime >= animationDuration)
            {
                animationTimer.Stop();
                indicatorPanel.Location = new Point(targetX, targetY);
                indicatorPanel.Width = targetWidth;
                return;
            }

            double progress = (double)elapsedTime / animationDuration;
            double easedProgress = EaseInOut(progress);

            int newX = (int)(startX + (targetX - startX) * easedProgress);

            int newY = (int)(startY + (targetY - startY) * easedProgress);

            indicatorPanel.Location = new Point(newX, newY);

            int newWidth = (int)(startWidth + (targetWidth - startWidth) * easedProgress);
            indicatorPanel.Width = newWidth;
        }


        private double EaseInOut(double t)
        {
            return t < 0.5 ? 2 * t * t : 1 - Math.Pow(-2 * t + 2, 2) / 2;
        }


















        // CONTAINER PRODUCT NAME AND LOGO

        Guna2Panel containerProductName;

        Guna2Button logoProduct;

        Label nameProduct;

        // TAB BUTTONS PRODUCT [ ABOUT, REQUIREMENTS, FEATURES ]

        Guna2Panel containerTabBtnProduct;

        Guna2Button btnAbout;

        Guna2Button btnRequirements;

        Guna2Button btnFeatures;

        Guna2Panel containerIndicatorPanel;

        Guna2Panel containerTab1;

        Guna2Panel containerUpdatesProduct;

        Guna2Panel containerTab2;

        Guna2Panel containerTab3;

        Guna2Panel containerActivateProduct;

        Guna2Button btnActivateProduct;

        Guna2HtmlLabel lbRequestLicense;

        // TAB 1

        Guna2HtmlLabel descriptionProduct;

        Guna2HtmlLabel versionProduct;

        Guna2HtmlLabel lastUpdateProduct;

        Guna2HtmlLabel dateVersionProduct;

        Guna2HtmlLabel dateLastUpdateProduct;

        Guna2HtmlLabel developerProduct;

        Guna2HtmlLabel nameDeveloperProduct;

        Guna2Separator separatorUpdates;


        // TAB 2

        Guna2Panel containerListRequirements;


        // TAB 3

        Guna2Panel containerListFeatures;





        private void ApplyRoundedCorners(Graphics graphics, Image image, Rectangle bounds, int cornerRadius)
        {
            if (image == null) return;

            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, cornerRadius * 2, cornerRadius * 2, 180, 90);
            path.AddArc(bounds.Right - cornerRadius * 2, bounds.Top, cornerRadius * 2, cornerRadius * 2, 270, 90);
            path.AddArc(bounds.Right - cornerRadius * 2, bounds.Bottom - cornerRadius * 2, cornerRadius * 2, cornerRadius * 2, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - cornerRadius * 2, cornerRadius * 2, cornerRadius * 2, 90, 90);
            path.CloseFigure();

            graphics.SetClip(path);

            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            graphics.DrawImage(image, bounds);

            graphics.ResetClip();
        }

    }

}
