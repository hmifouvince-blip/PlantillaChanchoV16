using System;
using System.Diagnostics;
using System.IO;

namespace PlantillaChanchoV16.Utilities
{
    // Pilote le VPN gratuit Cloudflare WARP via warp-cli.
    // Si WARP n'est pas installé, on l'installe automatiquement via winget (source officielle
    // Cloudflare) -> l'utilisateur n'a rien à installer à la main.
    internal static class WarpVpn
    {
        private const string CliFull = @"C:\Program Files\Cloudflare\Cloudflare WARP\warp-cli.exe";
        public const string OfficialUrl = "https://1.1.1.1/";

        public static bool IsInstalled()
        {
            try { return File.Exists(CliFull); } catch { return false; }
        }

        private static string Cli => File.Exists(CliFull) ? CliFull : "warp-cli";

        private static string Run(string exe, string args, int timeoutMs = 0)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using (var p = Process.Start(psi))
                {
                    string o = p.StandardOutput.ReadToEnd();
                    string e = p.StandardError.ReadToEnd();
                    if (timeoutMs > 0) p.WaitForExit(timeoutMs); else p.WaitForExit();
                    return ((o ?? "") + "\n" + (e ?? "")).Trim();
                }
            }
            catch (Exception ex) { return "ERR: " + ex.Message; }
        }

        // Installe WARP via winget. Renvoie true si installé au final.
        public static bool Install()
        {
            try
            {
                Run("winget", "install --id Cloudflare.Warp -e --silent --accept-source-agreements --accept-package-agreements");
            }
            catch { }
            return IsInstalled();
        }

        public static void EnsureRegistered()
        {
            // Nouvelle syntaxe (warp-cli récent) puis ancienne, on ignore les erreurs "déjà enregistré".
            Run(Cli, "--accept-tos registration new", 15000);
            Run(Cli, "--accept-tos register", 15000);
        }

        public static void Connect()
        {
            EnsureRegistered();
            Run(Cli, "--accept-tos connect", 15000);
            // Le tunnel met 1-3 s à s'établir : on attend qu'il soit réellement UP.
            for (int i = 0; i < 8; i++)
            {
                if (IsConnected()) return;
                System.Threading.Thread.Sleep(600);
            }
        }

        public static void Disconnect()
        {
            Run(Cli, "--accept-tos disconnect", 15000);
        }

        public static bool IsConnected()
        {
            // IMPORTANT : la sous-commande `status` n'accepte PAS `--accept-tos` sur les
            // versions récentes de warp-cli (elle renvoie une erreur d'argument). On lit
            // donc le statut SANS ce flag, avec repli sur l'ancienne syntaxe.
            string s = Run(Cli, "status", 8000);
            if (string.IsNullOrWhiteSpace(s) || s.StartsWith("ERR", StringComparison.OrdinalIgnoreCase)
                || s.IndexOf("unexpected", StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                s = Run(Cli, "--accept-tos status", 8000);
            }

            // "Disconnected" contient aussi "connected" -> on exclut d'abord les états négatifs.
            if (s.IndexOf("Disconnected", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (s.IndexOf("Unable", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            // "Connected" = connecté ; "Connecting" est transitoire -> on le considère non connecté.
            if (s.IndexOf("Connecting", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            return s.IndexOf("Connected", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
