using System;
using System.IO;

namespace PlantillaChanchoV16.Utilities
{
    // Interrupteur global des animations "ambiance" (pétales, logo rotatif, bannière).
    //  - Focused = false quand PaiPai n'est pas au premier plan / minimisé  -> pause (zéro conso).
    //  - LowPower = mode "faible conso" choisi dans les Settings              -> animations coupées.
    // Les contrôles n'animent que si Active == (Focused && !LowPower).
    internal static class AnimationHub
    {
        private static bool _focused = true;
        private static bool _lowPower;
        private static bool _loaded;

        public static event Action ActiveChanged;

        public static bool Active => _focused && !LowPower;

        public static bool Focused
        {
            set
            {
                if (_focused == value) return;
                _focused = value;
                ActiveChanged?.Invoke();
            }
        }

        public static bool LowPower
        {
            get { EnsureLoaded(); return _lowPower; }
            set
            {
                if (_lowPower == value) return;
                _lowPower = value;
                Save();
                ActiveChanged?.Invoke();
            }
        }

        private static string FilePath
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PaiPai");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "lowpower.txt");
            }
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            try { if (File.Exists(FilePath)) _lowPower = File.ReadAllText(FilePath).Trim() == "1"; }
            catch { }
        }

        private static void Save()
        {
            try { File.WriteAllText(FilePath, _lowPower ? "1" : "0"); } catch { }
        }
    }
}
