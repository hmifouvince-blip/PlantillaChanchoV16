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

            // Applique le thème de couleurs et la langue sauvegardés AVANT toute création de fenêtre.
            Utilities.ThemeManager.LoadAndApply();
            Utilities.Localization.LoadAndApply();

            // Écran de démarrage visible DÈS LE DÉBUT : sinon, surtout juste après une mise à
            // jour (l'ancien processus a déjà fermé sa fenêtre ; le script de relance attend
            // ~1s+ qu'il disparaisse complètement, PUIS le nouveau processus doit encore
            // installer les polices et construire l'écran de connexion), l'utilisateur ne voit
            // RIEN pendant plusieurs secondes et croit que l'appli a planté ou met du temps à
            // se relancer. Tourne sur son propre thread -> reste fluide pendant que le thread
            // principal fait ce travail (potentiellement bloquant) juste en dessous.
            var splash = new Template.SakuraLoaderThread();
            splash.Show(Rectangle.Empty, "Starting PaiPai...");

            string namespaceDefault = typeof(Program).Namespace;

            using (Mutex mutex = new Mutex(false, namespaceDefault))
            {
                bool owned;
                try
                {
                    owned = mutex.WaitOne(TimeSpan.Zero, false);
                }
                catch (AbandonedMutexException)
                {
                    // L'instance précédente a été tuée sans libérer le mutex -> on récupère la main.
                    owned = true;
                }

                if (!owned)
                {
                    splash.Close();
                    PlantillaChanchoV16.Template.SakuraMessageBox.Show("The application is already running.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }


                var DefaultForm = new Default();
                if (!DefaultForm.TestMode)
                {
                    if (IsDebuggerAttached())
                    {
                        splash.Close();
                        //PlantillaChanchoV16.Template.SakuraMessageBox.Show("The application is being debugged.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                if (!IsAdministrator())
                {
                    splash.Close();
                    PlantillaChanchoV16.Template.SakuraMessageBox.Show("The application must be run as administrator.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                // Ne JAMAIS bloquer l'ouverture sur l'installation des polices : au 1er lancement
                // le broadcast WM_FONTCHANGE pouvait figer l'app (fenêtre invisible, uniquement
                // visible dans le gestionnaire des tâches). Max 3s, puis on ouvre quand même.
                await Task.WhenAny(Task.WhenAll(installFontTasks), Task.Delay(3000));



                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                var login = new Login();
                splash.Close();
                Application.Run(login);
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

    }
}