using PlantillaChanchoV16.Utilities;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;

namespace PlantillaChanchoV16.Template
{
    // Loader sakura qui s'exécute sur SON PROPRE thread UI (avec sa propre boucle de
    // messages). Comme il est indépendant du thread principal, son animation continue
    // même quand le thread principal est bloqué (ex: construction de Main / DetailsProduct)
    // -> ZÉRO freeze visible pendant les chargements.
    internal class SakuraLoaderThread
    {
        private Thread _thread;
        private Form _form;
        private SakuraLoadingScreen _screen;
        private readonly ManualResetEventSlim _ready = new ManualResetEventSlim(false);

        public void Show(Rectangle bounds, string message)
        {
            if (bounds.Width < 200 || bounds.Height < 150)
            {
                var scr = Screen.PrimaryScreen.Bounds;
                bounds = new Rectangle(scr.X + (scr.Width - 820) / 2, scr.Y + (scr.Height - 540) / 2, 820, 540);
            }

            var b = bounds;
            _thread = new Thread(() =>
            {
                _form = new Form
                {
                    FormBorderStyle = FormBorderStyle.None,
                    StartPosition = FormStartPosition.Manual,
                    Bounds = b,
                    ShowInTaskbar = false,
                    TopMost = true,
                    BackColor = Colors.bgColor
                };

                using (var path = Rounded(new Rectangle(0, 0, b.Width, b.Height), Default.borderForms))
                    _form.Region = new Region(path);

                _screen = new SakuraLoadingScreen(b.Width, b.Height, "PaiPai", message) { Dock = DockStyle.Fill };
                _form.Controls.Add(_screen);

                _form.Shown += (s, e) => _ready.Set();
                Application.Run(_form);
            });
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.IsBackground = true;
            _thread.Start();

            _ready.Wait(2000); // attend l'affichage (max 2s de sécurité)
        }

        public void UpdateBounds(Rectangle bounds)
        {
            try
            {
                if (_form != null && _form.IsHandleCreated && !_form.IsDisposed)
                    _form.BeginInvoke(new Action(() =>
                    {
                        _form.Bounds = bounds;
                        using (var path = Rounded(new Rectangle(0, 0, bounds.Width, bounds.Height), Default.borderForms))
                            _form.Region = new Region(path);
                    }));
            }
            catch { }
        }

        public void UpdateMessage(string message)
        {
            try
            {
                if (_screen != null && _screen.IsHandleCreated && !_screen.IsDisposed)
                    _screen.BeginInvoke(new Action(() => _screen.Message = message));
            }
            catch { }
        }

        public void Close()
        {
            try
            {
                if (_form != null && _form.IsHandleCreated && !_form.IsDisposed)
                    _form.BeginInvoke(new Action(() => _form.Close()));
            }
            catch { }
        }

        private static GraphicsPath Rounded(Rectangle r, int radius)
        {
            int d = Math.Max(2, radius * 2);
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
