using Guna.UI2.WinForms;
using PlantillaChanchoV16.Utilities;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PlantillaChanchoV16.Template
{
    internal class SpecialProductView : Guna2Button
    {
        Images images = new Images();
        Utils utils = new Utils();
        public SpecialProductView(Guna2Panel parentPanel, bool productUnderMaintenance, Image bgImage, string productName, string productDescription, string previewVideo, Action openDetails)
        {
            Width = parentPanel.Width - 33;
            FillColor = Color.Transparent;
            BackColor = Color.Transparent;
            Height = parentPanel.Height - 17;
            BorderRadius = 0;
            Size = Size;
            Image = bgImage;
            //this.SizeMode = PictureBoxSizeMode.Zoom;
            ImageSize = new Size(Width, Height);
            BorderThickness = 10;
            BorderColor = Color.White;

            Location = new Point(15, 15);

            RoundGunaButtonCorners(this, 4);

            InitializeSpecialProduct(productUnderMaintenance, productName, productDescription, previewVideo, openDetails);










            Paint += (s, e) =>
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

                    e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                    e.Graphics.DrawImage(img, pictureBox.ClientRectangle, cropRect, GraphicsUnit.Pixel);

                    using (var gradientBrush = new LinearGradientBrush(
                        new Rectangle(0, 0, pictureBox.Width, pictureBox.Height),
                        Color.Transparent,
                        Colors.bgColor,
                        LinearGradientMode.Vertical))
                    {
                        e.Graphics.FillRectangle(gradientBrush, new Rectangle(0, 0, pictureBox.Width, pictureBox.Height));
                    }

                    using (var gradientBrush = new LinearGradientBrush(
                       new Rectangle(0, 0, pictureBox.Width, pictureBox.Height),
                       Color.Transparent,
                       Colors.bgColor,
                       LinearGradientMode.Vertical))
                    {
                        e.Graphics.FillRectangle(gradientBrush, new Rectangle(0, 0, pictureBox.Width, pictureBox.Height));
                    }
                }
            };

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

        Guna2PictureBox logoProduct;
        Guna2HtmlLabel nameProduct;
        Guna2HtmlLabel descriptionProduct;
        Guna2Button BtnStart;
        Guna2Button BtnPreviewVideo;
        Action openDetails;

        private void InitializeSpecialProduct(bool productUnderMaintenance, string productName, string productDescription, string previewVideo, Action openDetails)
        {
            logoProduct = new Guna2PictureBox
            {
                Image = images.LogoValorant,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Size = new Size(78, 78),
                UseTransparentBackground = true,
                BorderRadius = 0,
            };

            nameProduct = new Guna2HtmlLabel
            {
                Text = productName,
                Font = new Font("Inter Semibold", 17f, FontStyle.Regular),
                ForeColor = Color.White
            };

            descriptionProduct = new Guna2HtmlLabel
            {
                Text = productDescription,
                Font = new Font("Inter Medium", 11f, FontStyle.Regular),
                ForeColor = Color.DarkCyan,
                AutoSizeHeightOnly = true,
                Width = 500,
                AutoSize = false,
            };

            BtnPreviewVideo = new Guna2Button
            {
                Image = Utils.ChangeIconsColor(new Bitmap(images.IconPrevSpecial), Color.White),
                ImageAlign = HorizontalAlignment.Right,
                ImageOffset = new Point(30, 0),
                TextOffset = new Point(-15, 0),
                ImageSize = new Size(20,20),
                Text = "Preview",
                Font = new Font("Inter Medium", 11.7f, FontStyle.Regular),
                FillColor = Color.FromArgb(40, 42, 57),
                BorderColor = Color.FromArgb(40, 42, 57),
                BorderThickness = 1,
                Height = 36,
                BorderRadius = 4,
                ForeColor = Color.White,
                UseTransparentBackground = true,
                Animated = true,
            };

            BtnPreviewVideo.Click += (s, e) =>
            {
                utils.OpenLink(previewVideo);
            };

            if(productUnderMaintenance == false)
            {
                BtnStart = new Guna2Button
                {
                    Image = Utils.ChangeIconsColor(new Bitmap(images.IconOpenDetails), Color.White),
                    ImageAlign = HorizontalAlignment.Right,
                    ImageOffset = new Point(35, 0),
                    TextOffset = new Point(-10, 0),
                    ImageSize = new Size(14, 14),
                    Text = "Launch",
                    Font = new Font("Inter Medium", 11.7f, FontStyle.Regular),
                    FillColor = Colors.mainColor,
                    BorderColor = Colors.mainColor,
                    BorderThickness = 1,
                    Height = 36,
                    BorderRadius = 4,
                    ForeColor = Color.White,
                    UseTransparentBackground = true,
                    Animated = true,
                };

                BtnStart.Click += (s, e) =>
                {
                    openDetails.Invoke();
                };
            }
            else
            {
                BtnStart = new Guna2Button
                {
                    Image = Utils.ChangeIconsColor(new Bitmap(images.UmIcon), Color.White),
                    ImageAlign = HorizontalAlignment.Right,
                    ImageOffset = new Point(20, 0),
                    TextOffset = new Point(-15, 0),
                    ImageSize = new Size(16, 16),
                    Text = "Not available",
                    Font = new Font("Inter Medium", 11.7f, FontStyle.Regular),
                    FillColor = Color.FromArgb(40, 42, 57),
                    BorderColor = Color.FromArgb(40, 42, 57),
                    BorderThickness = 1,
                    Height = 36,
                    BorderRadius = 4,
                    ForeColor = Color.White,
                    UseTransparentBackground = true,
                    Animated = true,
                    Width = 180,
                };

                BtnStart.Click += (s, e) =>
                {
                    MessageBox.Show("This product is currently under maintenance. Please try again later.",
                    "Maintenance Notice",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                };
            }







            int margin = 10;
            int bottomOffset = Height - 10;

            BtnPreviewVideo.Location = new Point(15, bottomOffset - BtnPreviewVideo.Height);
            BtnStart.Location = new Point(BtnPreviewVideo.Right + margin, bottomOffset - BtnStart.Height);

            descriptionProduct.Location = new Point(15, BtnStart.Top - descriptionProduct.Height - margin);
            nameProduct.Location = new Point(15, descriptionProduct.Top - nameProduct.Height - margin);
            logoProduct.Location = new Point(15, nameProduct.Top - logoProduct.Height - margin);

            Controls.Add(logoProduct);
            Controls.Add(nameProduct);
            Controls.Add(descriptionProduct);
            Controls.Add(BtnStart);
            Controls.Add(BtnPreviewVideo);
        }



    }
}
