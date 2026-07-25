using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Guna.UI2.WinForms;
using System.Threading.Tasks;
using WindowsInput;
using System.Runtime.InteropServices;
using Timer = System.Windows.Forms.Timer;
using PlantillaChanchoV16;

namespace PlantillaChanchoV16.Utilities
{
    public class Utils
    {
        public void ApplyFadeInAnimation(Form form)
        {
            form.Opacity = 0;
            form.Show();
            FadeAnimation fadeAnimation = new FadeAnimation(form, true);
            fadeAnimation.Start();
        }

        public void ApplyFadeOutAnimation(Form form, Action onCompleted)
        {
            FadeAnimation fadeAnimation = new FadeAnimation(form, false);
            fadeAnimation.Start(() =>
            {
                form.Hide();
                onCompleted?.Invoke();
            });
        }


        public class FadeAnimation
        {
            private readonly Form form;
            private readonly Timer timer;
            private readonly bool fadeIn;
            private Action onCompleted;

            // Higher frame rate (~66 fps) + smaller step = visibly smoother fade.
            private const int FrameIntervalMs = 15;
            private const double OpacityIncrement = 0.08;

            private readonly double opacityStep;

            public FadeAnimation(Form form, bool fadeIn, double opacityStep = OpacityIncrement)
            {
                this.form = form;
                this.fadeIn = fadeIn;
                this.opacityStep = opacityStep;

                timer = new Timer { Interval = FrameIntervalMs };
                timer.Tick += Timer_Tick;
            }

            public void Start(Action onCompleted = null)
            {
                this.onCompleted = onCompleted;

                if (fadeIn)
                {
                    form.Opacity = 0;
                    form.Show();
                }
                else
                {
                    form.Opacity = 1;
                }

                timer.Start();
            }

            private void Timer_Tick(object sender, EventArgs e)
            {
                if (fadeIn)
                {
                    double next = form.Opacity + EasedStep(form.Opacity);
                    if (next >= 1)
                    {
                        form.Opacity = 1;
                        timer.Stop();
                        onCompleted?.Invoke();
                    }
                    else
                    {
                        form.Opacity = next;
                    }
                }
                else
                {
                    double next = form.Opacity - EasedStep(form.Opacity);
                    if (next <= 0)
                    {
                        form.Opacity = 0;
                        timer.Stop();
                        form.Hide();
                        onCompleted?.Invoke();
                    }
                    else
                    {
                        form.Opacity = next;
                    }
                }
            }

            // Ease-out: move a bit faster in the middle, gently near the ends,
            // so the fade feels natural instead of perfectly linear.
            private double EasedStep(double currentOpacity)
            {
                double distanceToEdge = Math.Min(currentOpacity, 1 - currentOpacity);
                double factor = 0.5 + distanceToEdge;   // 0.5x near edges, up to 1x mid
                return opacityStep * factor;
            }
        }



























        private Timer colorTimer;
        private Color targetColor;
        private Color startColor;
        private int animationStep;
        private Control targetControl;

        public void StartColorAnimation(Control control, Color currentColor, Color newTargetColor)
        {
            if (currentColor == newTargetColor) return;

            colorTimer?.Stop();

            colorTimer = new Timer { Interval = 10 };
            colorTimer.Tick += ColorTimer_Tick;

            targetControl = control;
            startColor = currentColor;
            targetColor = newTargetColor;
            animationStep = 0;

            colorTimer.Start();
        }

        private void ColorTimer_Tick(object sender, EventArgs e)
        {
            animationStep += 5;

            if (animationStep >= 100)
            {
                animationStep = 100;
                colorTimer.Stop();
            }

            int r = startColor.R + (targetColor.R - startColor.R) * animationStep / 100;
            int g = startColor.G + (targetColor.G - startColor.G) * animationStep / 100;
            int b = startColor.B + (targetColor.B - startColor.B) * animationStep / 100;

            targetControl.ForeColor = Color.FromArgb(r, g, b);
        }

        public void EnableDragControlInGuna2Panels(Control container)
        {
            foreach (Control control in container.Controls)
            {
                if (control is Guna2Panel gunaPanel)
                {
                    Guna2DragControl dragControl = new Guna2DragControl
                    {
                        TargetControl = gunaPanel,
                        UseTransparentDrag = false
                    };


                }

                if (control is Guna2HtmlLabel gunaHtmlLabel)
                {
                    Guna2DragControl dragControl = new Guna2DragControl
                    {
                        TargetControl = gunaHtmlLabel,
                        UseTransparentDrag = false
                    };
                }

                if (control is Label label)
                {
                    Guna2DragControl dragControl = new Guna2DragControl
                    {
                        TargetControl = label,
                        UseTransparentDrag = false
                    };
                }

                if (control is Guna2Button Allbuttons)
                {
                    Guna2DragControl dragControl = new Guna2DragControl
                    {
                        TargetControl = Allbuttons,
                        UseTransparentDrag = false
                    };
                }

                if (control is Guna2PictureBox AllPictures)
                {
                    Guna2DragControl dragControl = new Guna2DragControl
                    {
                        TargetControl = AllPictures,
                        UseTransparentDrag = false
                    };
                }

                if (control is Guna2CircleProgressBar AllProgressBar)
                {
                    Guna2DragControl dragControl = new Guna2DragControl
                    {
                        TargetControl = AllProgressBar,
                        UseTransparentDrag = false
                    };
                }


                if (control.HasChildren)
                {
                    EnableDragControlInGuna2Panels(control);
                }
            }
        }
        public class GlobalKeyHook : IDisposable
        {
            private Main loadingForm;
            private InputSimulator inputSimulator;
            private const int WH_KEYBOARD_LL = 13;
            private LowLevelKeyboardProc keyboardProc;
            private IntPtr hookId = IntPtr.Zero;

            public GlobalKeyHook(Main mainForm1)
            {
                loadingForm = mainForm1;
                inputSimulator = new InputSimulator();
                keyboardProc = HookCallback;
                hookId = SetHook(keyboardProc);
            }

            public void UnhookKeyboard()
            {
                if (hookId != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(hookId);
                    hookId = IntPtr.Zero;
                }
            }

            // Retire le hook clavier système (sinon il fuit à chaque reconstruction de fenêtre).
            public void Dispose()
            {
                UnhookKeyboard();
            }

            private IntPtr SetHook(LowLevelKeyboardProc proc)
            {
                using (Process curProcess = Process.GetCurrentProcess())
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                        GetModuleHandle(curModule.ModuleName), 0);
                }
            }

            private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
            {
                if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
                {
                    _ = HookCallbackAsync(nCode, wParam, lParam);
                }
                return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
            }




            private async Task HookCallbackAsync(int nCode, IntPtr wParam, IntPtr lParam)
            {

                if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
                {
                    int vkCode = Marshal.ReadInt32(lParam);



                    //if ((Keys)vkCode == Keys.D1)
                    //{
                    //    Environment.Exit(0);
                    //}

                   


                }
            }




            private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool UnhookWindowsHookEx(IntPtr hhk);
            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr GetModuleHandle(string lpModuleName);
            private const int WM_KEYDOWN = 0x0100;
        }






































        public void DisableSelectionInGuna2HtmlLabels(Control container)
        {
            foreach (Control control in container.Controls)
            {
                if (control is Guna2HtmlLabel gunaHtmlLabel)
                {
                    gunaHtmlLabel.IsSelectionEnabled = false;
                    gunaHtmlLabel.UseGdiPlusTextRendering = true;
                    gunaHtmlLabel.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                }

                if (control.HasChildren)
                {
                    DisableSelectionInGuna2HtmlLabels(control);
                }
            }
        }

        public Image LoadEmbeddedImage(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new Exception($"No se pudo encontrar el recurso: {resourceName}");

                return Image.FromStream(stream);
            }
        }

        public void OpenLink(string url)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                PlantillaChanchoV16.Template.SakuraMessageBox.Show($"No se pudo abrir el enlace: {ex.Message}");
            }
        }


        // CAMBIAR COLOR A ICONOS
        // Recolore une icône : conserve l'alpha de chaque pixel, remplace le RGB par newColor.
        // Ancienne implémentation en GetPixel/SetPixel = très lente (verrou bitmap par pixel).
        // Version ColorMatrix = un seul DrawImage (résultat identique, bien plus rapide).
        public static Bitmap ChangeIconsColor(Bitmap originalImage, Color newColor)
        {
            var newImage = new Bitmap(originalImage.Width, originalImage.Height,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            // Sortie RGB = newColor (via la translation, dernière ligne), sortie A = A source.
            var matrix = new System.Drawing.Imaging.ColorMatrix(new float[][]
            {
                new float[] { 0, 0, 0, 0, 0 },
                new float[] { 0, 0, 0, 0, 0 },
                new float[] { 0, 0, 0, 0, 0 },
                new float[] { 0, 0, 0, 1, 0 },
                new float[] { newColor.R / 255f, newColor.G / 255f, newColor.B / 255f, 0, 1 }
            });

            using (var g = Graphics.FromImage(newImage))
            using (var attrs = new System.Drawing.Imaging.ImageAttributes())
            {
                attrs.SetColorMatrix(matrix);
                g.DrawImage(originalImage,
                    new Rectangle(0, 0, newImage.Width, newImage.Height),
                    0, 0, originalImage.Width, originalImage.Height,
                    GraphicsUnit.Pixel, attrs);
            }
            return newImage;
        }

        // Version asynchrone conservée pour compat (le recolorage est désormais rapide).
        public static Task<Bitmap> ChangeIconsColorAsync(Bitmap originalImage, Color newColor)
        {
            return Task.Run(() => ChangeIconsColor(originalImage, newColor));
        }







    }
}
