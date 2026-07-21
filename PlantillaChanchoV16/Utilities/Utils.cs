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
            private Form form;
            private Timer timer;
            private bool fadeIn;
            private double opacityStep;
            private const double OpacityIncrement = 0.05;

            public FadeAnimation(Form form, bool fadeIn, double opacityStep = OpacityIncrement)
            {
                this.form = form;
                this.fadeIn = fadeIn;
                this.opacityStep = opacityStep;

                timer = new Timer();
                timer.Interval = 20;
                timer.Tick += Timer_Tick;
            }

            public void Start(Action onCompleted = null)
            {
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

                if (!fadeIn)
                {
                    timer.Tick += (sender, e) =>
                    {
                        if (form.Opacity <= 0)
                        {
                            form.Opacity = 0;
                            timer.Stop();
                            form.Hide();
                            onCompleted?.Invoke();
                        }
                    };
                }
            }

            private void Timer_Tick(object sender, EventArgs e)
            {
                if (fadeIn)
                {
                    if (form.Opacity < 1)
                    {
                        form.Opacity += opacityStep;
                    }
                    else
                    {
                        form.Opacity = 1;
                        timer.Stop();
                    }
                }
                else
                {
                    if (form.Opacity > 0)
                    {
                        form.Opacity -= opacityStep;
                    }
                    else
                    {
                        form.Opacity = 0;
                        timer.Stop();
                        form.Hide();
                    }
                }
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
        public class GlobalKeyHook
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
                MessageBox.Show($"No se pudo abrir el enlace: {ex.Message}");
            }
        }


        // CAMBIAR COLOR A ICONOS
        public static async Task<Bitmap> ChangeIconsColorAsync(Bitmap originalImage, Color newColor)
        {
            return await Task.Run(() =>
            {
                Bitmap newImage = new Bitmap(originalImage.Width, originalImage.Height);

                for (int y = 0; y < originalImage.Height; y++)
                {
                    for (int x = 0; x < originalImage.Width; x++)
                    {
                        Color originalColor = originalImage.GetPixel(x, y);
                        if (originalColor.A > 0)
                        {
                            newImage.SetPixel(x, y, Color.FromArgb(originalColor.A, newColor.R, newColor.G, newColor.B));
                        }
                    }
                }

                return newImage;
            });
        }


        public static Bitmap ChangeIconsColor(Bitmap originalImage, Color newColor)
        {
            Bitmap newImage = new Bitmap(originalImage.Width, originalImage.Height);

            for (int y = 0; y < originalImage.Height; y++)
            {
                for (int x = 0; x < originalImage.Width; x++)
                {
                    Color originalColor = originalImage.GetPixel(x, y);
                    if (originalColor.A > 0)
                    {
                        newImage.SetPixel(x, y, Color.FromArgb(originalColor.A, newColor.R, newColor.G, newColor.B));
                    }
                }
            }

            return newImage;
        }







    }
}
