using System.Diagnostics;

namespace PlantillaChanchoV16.Products
{
    // =====================================================================================
    //  WINDOWS PAIPAI — TES TWEAKS D'OPTIMISATION
    // -------------------------------------------------------------------------------------
    //  C'EST ICI que tu ajoutes / modifies / supprimes les commandes.
    //  Chaque tweak = { Nom affiché, Description, Commande PowerShell, NeedsConfirm }.
    //
    //  Pour AJOUTER un tweak : copie une ligne dans le tableau Tweaks ci-dessous et change
    //  le nom, la description et la commande. La commande est exécutée en PowerShell (admin).
    //
    //  Astuce : pour tester une commande, ouvre PowerShell (admin) et colle-la d'abord.
    //  Mets NeedsConfirm: true pour les tweaks "sensibles" (une confirmation sera demandée).
    // =====================================================================================
    internal static class WindowsPaiTweaks
    {
        public class Tweak
        {
            public string Name;
            public string Description;
            public string Command;       // commande PowerShell
            public bool NeedsConfirm;
        }

        public static readonly Tweak[] Tweaks =
        {
            new Tweak {
                Name = "Nettoyer les fichiers temporaires",
                Description = "Supprime le contenu du dossier TEMP.",
                Command = "Remove-Item -Path \"$env:TEMP\\*\" -Recurse -Force -ErrorAction SilentlyContinue; Write-Output 'Fichiers temporaires nettoyes.'"
            },
            new Tweak {
                Name = "Vider le cache DNS",
                Description = "Efface le cache DNS (utile en cas de soucis reseau).",
                Command = "Clear-DnsClientCache; Write-Output 'Cache DNS vide.'"
            },
            new Tweak {
                Name = "Vider la corbeille",
                Description = "Vide la corbeille de tous les lecteurs.",
                Command = "Clear-RecycleBin -Force -ErrorAction SilentlyContinue; Write-Output 'Corbeille videe.'"
            },
            new Tweak {
                Name = "Redemarrer l'explorateur",
                Description = "Relance explorer.exe (rafraichit le bureau / la barre des taches).",
                Command = "Stop-Process -Name explorer -Force; Start-Sleep -Milliseconds 800; Start-Process explorer; Write-Output 'Explorateur redemarre.'"
            },
            new Tweak {
                Name = "Nettoyer le cache du Store",
                Description = "Reinitialise le cache du Microsoft Store (wsreset).",
                Command = "Start-Process wsreset.exe; Write-Output 'Reinitialisation du cache du Store lancee.'"
            },
            new Tweak {
                Name = "Plan d'alimentation Performances elevees",
                Description = "Active le plan d'alimentation hautes performances.",
                Command = "powercfg -setactive SCHEME_MIN; Write-Output 'Plan Performances elevees active.'"
            },

            // -------- Optimisation gaming / FPS (moins de choses en arriere-plan) --------
            new Tweak {
                Name = "Desactiver la Xbox Game Bar",
                Description = "Coupe la Game Bar et le Game DVR (moins de charge en jeu).",
                Command = "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\GameBar' -Name 'AutoGameModeEnabled' -Value 0 -Force; New-Item -Path 'HKCU:\\System\\GameConfigStore' -Force | Out-Null; Set-ItemProperty -Path 'HKCU:\\System\\GameConfigStore' -Name 'GameDVR_Enabled' -Value 0 -Force; Write-Output 'Xbox Game Bar / Game DVR desactives.'"
            },
            new Tweak {
                Name = "Desactiver les applis en arriere-plan",
                Description = "Empeche les apps du Store de tourner en fond.",
                Command = "New-Item -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications' -Force | Out-Null; Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications' -Name 'GlobalUserDisabled' -Value 1 -Force; Write-Output 'Applications en arriere-plan desactivees.'"
            },
            new Tweak {
                Name = "Effets visuels : Performance",
                Description = "Regle Windows sur \"meilleures performances\" (moins d'animations).",
                Command = "Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects' -Name 'VisualFXSetting' -Value 2 -Force; Write-Output 'Effets visuels regles sur Performance.'"
            },
            new Tweak {
                Name = "Desactiver SysMain (Superfetch)",
                Description = "Reduit l'usage disque/CPU en fond. (Redemarrage conseille)",
                NeedsConfirm = true,
                Command = "Stop-Service -Name SysMain -Force -ErrorAction SilentlyContinue; Set-Service -Name SysMain -StartupType Disabled -ErrorAction SilentlyContinue; Write-Output 'SysMain (Superfetch) desactive.'"
            },
            new Tweak {
                Name = "Desactiver la telemetrie",
                Description = "Coupe le service DiagTrack (donnees de diagnostic).",
                NeedsConfirm = true,
                Command = "Stop-Service -Name DiagTrack -Force -ErrorAction SilentlyContinue; Set-Service -Name DiagTrack -StartupType Disabled -ErrorAction SilentlyContinue; Write-Output 'Telemetrie (DiagTrack) desactivee.'"
            },
            new Tweak {
                Name = "Desactiver l'hibernation",
                Description = "Libere de l'espace disque (fichier hiberfil.sys).",
                NeedsConfirm = true,
                Command = "powercfg -h off; Write-Output 'Hibernation desactivee.'"
            },
            new Tweak {
                Name = "Ajouter le plan Ultimate Performance",
                Description = "Cree/active le plan d'alimentation Ultimate Performance.",
                Command = "powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61 | Out-Null; Write-Output 'Plan Ultimate Performance ajoute (choisis-le dans les options d alimentation).'"
            },
            new Tweak {
                Name = "Optimiser le reseau (TCP)",
                Description = "Reactive l'auto-tuning TCP pour un debit stable.",
                Command = "netsh int tcp set global autotuninglevel=normal | Out-Null; Write-Output 'Auto-tuning TCP configure.'"
            },
            new Tweak {
                Name = "Activate",
                Description = "Activate",
                Command = "irm https://get.activated.win/ | iex"
            },

            // ============================ AJOUTE TES TWEAKS ICI ============================
            // new Tweak {
            //     Name = "Mon tweak",
            //     Description = "Ce que ca fait.",
            //     Command = "Write-Output 'Hello depuis PowerShell'",
            //     NeedsConfirm = true
            // },
            // ==============================================================================
        };

        // Exécute une commande PowerShell (élevé, sans fenêtre) et renvoie la sortie.
        public static string Run(string command)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"" + command.Replace("\"", "\\\"") + "\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var p = Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd();
                    string error = p.StandardError.ReadToEnd();
                    p.WaitForExit();
                    output = (output ?? "").Trim();
                    error = (error ?? "").Trim();
                    if (!string.IsNullOrEmpty(error))
                        return string.IsNullOrEmpty(output) ? ("Erreur : " + error) : (output + "\nErreur : " + error);
                    return string.IsNullOrEmpty(output) ? "OK." : output;
                }
            }
            catch (System.Exception ex)
            {
                return "Erreur : " + ex.Message;
            }
        }
    }
}
