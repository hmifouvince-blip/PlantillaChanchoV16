using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace PlantillaChanchoV16.Utilities
{
    // Auto-update : KeyAuth signale une version obsolète à l'init (response.message == "invalidver")
    // et fournit le lien du nouveau build (app_data.downloadLink). On télécharge et on relance.
    //
    // Côté dashboard KeyAuth : App Settings -> "Version" = nouvelle version, et "Download link"
    // = lien direct vers le nouveau .exe. Quand tu bumps la version, les clients se mettent à jour.
    internal static class Updater
    {
        // Vérifie les variables KeyAuth "update_version" / "update_link".
        // Renvoie true si une mise à jour a été lancée (l'appli va se relancer).
        // À publier une MAJ : bump la variable "update_version" + mets "update_link" (lien .exe direct).
        public static bool CheckAndUpdate()
        {
            try
            {
                string latest = PlantillaChanchoV16.Login.KeyAuthApp.var("update_version");
                if (string.IsNullOrWhiteSpace(latest)) return false;
                latest = latest.Trim();

                // 1re exécution (aucun marqueur) : on suppose que cet exe fraîchement téléchargé
                // EST déjà la dernière version -> on enregistre sans télécharger (aucun update inutile).
                if (!File.Exists(VersionFile))
                {
                    try { File.WriteAllText(VersionFile, latest); } catch { }
                    return false;
                }

                if (latest != CurrentVersion())
                {
                    string link = PlantillaChanchoV16.Login.KeyAuthApp.var("update_link");
                    return TryAutoUpdate(link, latest);
                }
            }
            catch { }
            return false;
        }

        // Fichier qui mémorise la version installée -> l'appli SAIT quelle version elle a,
        // donc tu n'as JAMAIS à toucher AppVersion : tu changes juste update_version/update_link
        // sur KeyAuth, et l'appli ne boucle pas. Écrit par le script seulement si la copie a réussi.
        private static string VersionFile
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PaiPai");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "installed_version.txt");
            }
        }

        public static string CurrentVersion()
        {
            try
            {
                if (File.Exists(VersionFile))
                {
                    string v = File.ReadAllText(VersionFile).Trim();
                    if (!string.IsNullOrWhiteSpace(v)) return v;
                }
            }
            catch { }
            return PlantillaChanchoV16.Login.AppVersion;
        }

        // Remplace l'exe ACTUEL par le nouveau, relance ce même chemin, et n'enregistre la
        // version installée QUE si la copie a réussi (sinon on réessaiera au prochain lancement).
        public static void ReplaceAndRelaunch(string newExeTemp, string version)
        {
            try
            {
                int pid = Process.GetCurrentProcess().Id;
                string currentExe = Process.GetCurrentProcess().MainModule.FileName;
                string marker = VersionFile;
                string bat = Path.Combine(Path.GetTempPath(), "paipai_relaunch.bat");
                string content =
                    "@echo off\r\n" +
                    ":wait\r\n" +
                    "tasklist /FI \"PID eq " + pid + "\" | find \"" + pid + "\" >nul 2>&1\r\n" +
                    "if not errorlevel 1 ( ping -n 2 127.0.0.1 >nul & goto wait )\r\n" +
                    "copy /Y \"" + newExeTemp + "\" \"" + currentExe + "\" >nul\r\n" +
                    "if not errorlevel 1 ( >\"" + marker + "\" echo " + version + " )\r\n" +
                    "start \"\" \"" + currentExe + "\"\r\n" +
                    "del \"" + newExeTemp + "\" >nul 2>&1\r\n" +
                    "del \"%~f0\"\r\n";
                File.WriteAllText(bat, content);

                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c \"" + bat + "\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
            }
            catch
            {
                // Fallback : lancement direct du nouvel exe téléchargé.
                try { Process.Start(new ProcessStartInfo(newExeTemp) { UseShellExecute = true }); } catch { }
            }
        }

        public static bool TryAutoUpdate(string downloadUrl, string toVersion = null)
        {
            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                PlantillaChanchoV16.Template.SakuraMessageBox.Show(
                    "A new version is available, but no download link is set.\nSet the KeyAuth variable \"update_link\" to your new PaiPai .exe.",
                    "Update", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // Type de fichier visé.
            string ext = ".exe";
            try { ext = Path.GetExtension(new Uri(downloadUrl).LocalPath); } catch { }
            bool isExe = string.Equals(ext, ".exe", StringComparison.OrdinalIgnoreCase);

            // Si ce n'est pas un .exe direct (zip/installeur...), on ouvre le lien dans le navigateur.
            if (!isExe)
            {
                PlantillaChanchoV16.Template.SakuraMessageBox.Show("A new version is available. Your browser will open to download it.",
                    "Update available", MessageBoxButtons.OK, MessageBoxIcon.Information);
                try { Process.Start(new ProcessStartInfo(downloadUrl) { UseShellExecute = true }); } catch { }
                return true;
            }

            // Écran de mise à jour sakura avec barre de progression réelle
            // (télécharge, puis relance sur la nouvelle version ; se ferme lui-même en cas d'erreur).
            // Nom UNIQUE : sinon on essaie d'écraser l'exe en cours d'exécution -> "Access denied".
            string tempPath = Path.Combine(Path.GetTempPath(), "PaiPai_update_" + DateTime.Now.Ticks + ".exe");
            using (var scr = new Template.SakuraUpdateScreen(downloadUrl, toVersion, tempPath))
                scr.ShowDialog();

            return true;
        }
    }
}
