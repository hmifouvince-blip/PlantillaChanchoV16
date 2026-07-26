using System;
using System.ComponentModel;
using System.Diagnostics;

namespace PlantillaChanchoV16.Utilities
{
    // Lance/arrete le bot Discord (process Node.js "node index.js") et streame sa
    // sortie console en direct. NOUVEAU pattern pour ce projet : tout ce qui
    // existe deja (WindowsPaiTweaks.Run, WarpVpn) est BLOQUANT (ReadToEnd +
    // WaitForExit), adapte a une commande ponctuelle -> inutilisable ici puisque
    // le bot est cense tourner en continu. On utilise donc
    // OutputDataReceived/BeginOutputReadLine (asynchrone, non-bloquant).
    internal class BotProcessManager
    {
        private Process? _process;

        public event Action<string>? OutputReceived;
        public event Action? Exited;

        public bool IsRunning
        {
            get
            {
                try { return _process != null && !_process.HasExited; }
                catch { return false; }
            }
        }

        // Lance "node index.js" dans folderPath. Renvoie (true, null) si le
        // process a bien demarre, (false, message) sinon (ex: Node.js pas
        // installe/pas dans le PATH -> jamais d'exception qui remonte a l'UI).
        public (bool ok, string? error) Start(string folderPath)
        {
            if (IsRunning) return (false, "Le bot tourne déjà.");

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = "index.js",
                    WorkingDirectory = folderPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8,
                };

                _process = new Process { StartInfo = psi, EnableRaisingEvents = true };
                _process.OutputDataReceived += (s, e) => { if (e.Data != null) OutputReceived?.Invoke(e.Data); };
                _process.ErrorDataReceived += (s, e) => { if (e.Data != null) OutputReceived?.Invoke($"[erreur] {e.Data}"); };
                _process.Exited += (s, e) => Exited?.Invoke();

                _process.Start();
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();
                return (true, null);
            }
            catch (Win32Exception)
            {
                // Cas le plus frequent : "node" introuvable (Node.js pas installe
                // ou pas dans le PATH de l'utilisateur qui execute PaiPai).
                return (false, "Node.js est introuvable. Installe-le depuis nodejs.org puis réessaie.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public void Stop()
        {
            if (!IsRunning) return;
            try
            {
                _process!.Kill(entireProcessTree: true);
                _process.WaitForExit(5000);
            }
            catch { /* deja arrete ou impossible a tuer proprement -> pas bloquant */ }
            finally
            {
                _process?.Dispose();
                _process = null;
            }
        }

        public (bool ok, string? error) Restart(string folderPath)
        {
            Stop();
            return Start(folderPath);
        }
    }
}
