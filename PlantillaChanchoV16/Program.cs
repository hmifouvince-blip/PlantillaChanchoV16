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

            // Élévation AVANT le mutex et AVANT le splash : le processus relancé
            // doit pouvoir prendre le mutex (sinon il se croirait en double) et
            // deux écrans de démarrage superposés clignoteraient.
            // En temps normal on ne passe jamais ici : app.manifest demande déjà
            // l'élévation à Windows. Ce relais sert aux cas où le manifeste n'est
            // pas appliqué (exécution via "dotnet PaiPai.dll", exe recompilé sans
            // le manifeste), pour relancer au lieu de refuser de démarrer.
            if (!IsAdministrator() && RelaunchAsAdmin())
                return;

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

                // Dernier filet : on n'arrive ici que si l'élévation a échoué ET
                // que la relance était impossible (exe introuvable, stratégie de
                // groupe qui bloque UAC...).
                if (!IsAdministrator())
                {
                    splash.Close();
                    PlantillaChanchoV16.Template.SakuraMessageBox.Show(
                        "PaiPai needs administrator rights to start.\nRight-click PaiPai.exe and choose \"Run as administrator\".",
                        "Administrator required", MessageBoxButtons.OK, MessageBoxIcon.Error);
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


        // Relance PaiPai avec le verbe "runas" (= invite UAC). Renvoie true quand
        // l'instance courante doit s'arrêter : soit la relance est partie, soit
        // l'utilisateur a refusé l'élévation — dans les deux cas, continuer sans
        // les droits ne servirait à rien (installation des polices et tweaks
        // système échoueraient silencieusement).
        private static bool RelaunchAsAdmin()
        {
            // ProcessPath pointe l'exe réel, y compris en publication single-file
            // (où Assembly.Location est vide).
            string exePath = Environment.ProcessPath ?? "";
            if (exePath.Length == 0 || !exePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                return false;

            try
            {
                var psi = new ProcessStartInfo(exePath)
                {
                    // Obligatoire pour "runas" : sans UseShellExecute, Windows ne
                    // sait pas afficher l'invite UAC.
                    UseShellExecute = true,
                    Verb = "runas",
                    WorkingDirectory = Path.GetDirectoryName(exePath) ?? Environment.CurrentDirectory,
                };
                Process.Start(psi);
                return true;
            }
            catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                // 1223 = ERROR_CANCELLED : l'utilisateur a cliqué "Non" sur l'UAC.
                PlantillaChanchoV16.Template.SakuraMessageBox.Show(
                    "PaiPai needs administrator rights to install its fonts and run system tweaks.\nStart it again and accept the Windows prompt.",
                    "Administrator required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return true;
            }
            catch
            {
                // Relance impossible : on laisse la suite du démarrage afficher le
                // message d'aide plutôt que de mourir sans explication.
                return false;
            }
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