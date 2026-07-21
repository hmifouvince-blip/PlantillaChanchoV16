using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlantillaChanchoV16.Utilities;
using PlantillaChanchoV16;

namespace PlantillaChanchoV16.Utilities
{
    internal class ConfigLogo : Guna2Panel
    {
        Images images = new Images();
        Utils utils = new Utils();
        public static Guna2PictureBox logo;
        private Guna2HtmlLabel companyName1;
        private Guna2HtmlLabel companyName2;


        public ConfigLogo(Point locationLogo) 
        {
            FillColor = Color.Transparent;
            BackColor = Color.Transparent;
            
            //BorderColor = Color.Red;
            BorderThickness = 1;
            Location = locationLogo;


            logo = new Guna2PictureBox
            {
                Image = images.MainLogo, // <-- LOGO NORMAL // Image = Utils.ChangeIconsColor(new Bitmap(_images.MainLogo), Colors.mainColor) <-- LOGO WITH MAIN COLOR
                FillColor = Color.Transparent,
                BackColor = Color.Transparent,
                UseTransparentBackground = true,
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(27, 27),

            };


            companyName1 = new Guna2HtmlLabel
            {
                ForeColor = Color.White,
                Text = Default.companyName1,
                IsSelectionEnabled = false,
                Font = new Font("Inter Semibold", 15f, FontStyle.Regular),

            };
            companyName2 = new Guna2HtmlLabel
            {
                ForeColor = Colors.mainColor,
                Text = Default.companyName2,
                IsSelectionEnabled = false,
                Font = new Font("Inter Semibold", 15f, FontStyle.Regular),
            };

            this.Height = logo.Height;
            logo.Location = new Point(0, (this.Height - logo.Height) / 2);
            companyName1.Location = new Point(logo.Right + 5, (this.Height - companyName1.Height) / 2);
            companyName2.Location = new Point(companyName1.Right, (this.Height - companyName2.Height) / 2);


            this.Controls.Add(logo);
            this.Controls.Add(companyName1);
            this.Controls.Add(companyName2);

            this.Refresh();
            this.Update();
            this.Invalidate();
            this.Controls.Clear();

            this.Controls.Add(logo);
            this.Controls.Add(companyName1);
            this.Controls.Add(companyName2);

            //this.AutoSize = true;
            //this.BorderColor = Color.White;
            //this.BorderThickness = 1;
            this.Width = logo.Width + companyName1.Width + companyName2.Width + 20;
                
        }
    }
}
