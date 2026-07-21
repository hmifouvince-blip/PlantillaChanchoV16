using Microsoft.Win32;
using PlantillaChanchoV16.Utilities;
using PlantillaChanchoV16;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;
using System.Runtime.InteropServices;

namespace PlantillaChanchoV16
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static async Task Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);


            string namespaceDefault = typeof(Program).Namespace;

            using (Mutex mutex = new Mutex(false, namespaceDefault))
            {
                if (!mutex.WaitOne(TimeSpan.Zero, false))
                {
                    MessageBox.Show("The application is already running.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }


                var DefaultForm = new Default();
                if (!DefaultForm.TestMode)
                {
                    if (IsDebuggerAttached())
                    {
                        //MessageBox.Show("The application is being debugged.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                //await DisableWindowsDefender();

                if (!IsAdministrator())
                {
                    MessageBox.Show("The application must be run as administrator.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }



                string[] fontResourceNames = {
                    namespaceDefault + ".AllFonts.Inter-Black.ttf",
                    namespaceDefault + ".AllFonts.Inter-BlackItalic.ttf",
                    namespaceDefault + ".AllFonts.Inter-Bold.ttf",
                    namespaceDefault + ".AllFonts.Inter-BoldItalic.ttf",
                    namespaceDefault + ".AllFonts.Inter-ExtraBold.ttf",
                    namespaceDefault + ".AllFonts.Inter-ExtraBoldItalic.ttf",
                    namespaceDefault + ".AllFonts.Inter-ExtraLight.ttf",
                    namespaceDefault + ".AllFonts.Inter-ExtraLightItalic.ttf",
                    namespaceDefault + ".AllFonts.Inter-Italic.ttf",
                    namespaceDefault + ".AllFonts.Inter-Light.ttf",
                    namespaceDefault + ".AllFonts.Inter-LightItalic.ttf",
                    namespaceDefault + ".AllFonts.Inter-Medium.ttf",
                    namespaceDefault + ".AllFonts.Inter-MediumItalic.ttf",
                    namespaceDefault + ".AllFonts.Inter-Regular.ttf",
                    namespaceDefault + ".AllFonts.Inter-SemiBold.ttf",
                    namespaceDefault + ".AllFonts.Inter-SemiBoldItalic.ttf",
                    namespaceDefault + ".AllFonts.Inter-Thin.ttf",
                    namespaceDefault + ".AllFonts.Inter-ThinItalic.ttf",
                };
                ;

                Task[] installFontTasks = new Task[fontResourceNames.Length];
                for (int i = 0; i < fontResourceNames.Length; i++)
                {
                    installFontTasks[i] = InstallFontAsync(fontResourceNames[i]);
                }

                await Task.WhenAll(installFontTasks);



                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Login());
            }
        }




        private static bool IsDebuggerAttached()
        {
            if (Debugger.IsAttached)
            {
                return true;
            }
            bool isDebuggerPresent = false;
            WindowsImport.CheckRemoteDebuggerPresent(Process.GetCurrentProcess().Handle, ref isDebuggerPresent);
            return isDebuggerPresent;
        }

        private static async Task InstallFontAsync(string resourceFontName)
        {
            await Task.Run(() =>
            {
                var assembly = Assembly.GetExecutingAssembly();
                using (var fontStream = assembly.GetManifestResourceStream(resourceFontName))
                {
                    if (fontStream == null)
                        throw new FileNotFoundException("Fuente no encontrada en los recursos.", resourceFontName);

                    string fontName = Path.GetFileName(resourceFontName);
                    string destinationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), fontName);

                    if (!File.Exists(destinationPath))
                    {
                        using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
                        {
                            fontStream.CopyTo(fileStream);
                        }

                        using (RegistryKey regKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts", true))
                        {
                            regKey.SetValue($"{fontName} (TrueType)", fontName, RegistryValueKind.String);
                        }

                        WindowsImport.AddFontResource(destinationPath);
                        WindowsImport.SendMessage(WindowsImport.HWND_BROADCAST, WindowsImport.WM_FONTCHANGE, 0, 0);
                    }
                }
            });
        }


        private static bool IsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        private async static Task DisableWindowsDefender()
        {
            try
            {
                string command = "Set-MpPreference -DisableRealtimeMonitoring $true";

                Process process = new Process();
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.Arguments = $"-Command \"{command}\"";
                process.StartInfo.Verb = "runas";
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (!string.IsNullOrEmpty(output))
                {
                    Console.WriteLine("Output: " + output);
                }

                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine("Error: " + error);
                }

                Console.WriteLine("Protección en tiempo real de Windows Defender desactivada.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}