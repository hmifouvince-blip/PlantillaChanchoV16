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

            Image = productImage;
            FillColor = Color.Transparent;
            UseTransparentBackground = true;
            Width = widthProduct;
            Height = heightProduct;
            BackColor = Color.Transparent;
            BorderRadius = 10;
            BorderThickness = 9;
            //this.BorderStyle = BorderStyle.None;
            //this.SizeMode = PictureBoxSizeMode.StretchImage;
            ImageSize = new Size(Width + 0, Height + 0);
            ImageOffset = new Point(0, 0);
            BorderColor = Colors.bgColor;


            RoundGunaButtonCorners(this, 10);

            CreateItemsProduct(productActive, productUnderMaintenance, productName, updateProduct, productLogo, productImage, openDetailsProduct);





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



        private void CreateItemsProduct(bool productActive, bool productUnderMaintenance, string productName, string updateProduct, Image productLogo, Image productImage, Action openDetailsProduct)
        {

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
                FillColor = Colors.mainColor,
                Image = Utils.ChangeIconsColor(new Bitmap(images.IconOpenDetails), Color.White),
                Size = new Size(34, 34),
                ImageSize = new Size(14, 14),
                ImageAlign = HorizontalAlignment.Center,
                ImageOffset = new Point(0, 0),
                BorderRadius = 4,
                BorderColor = Colors.mainColor,
                BorderThickness = 1,
                BackColor = Color.Transparent,
                UseTransparentBackground = true,
                Animated= true,
            };
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
