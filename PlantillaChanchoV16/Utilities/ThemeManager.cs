using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace PlantillaChanchoV16.Utilities
{
    internal class Theme
    {
        public string Name;
        public Color Main, Bg, Sc;
        public Color[] Petals;
    }

    // Gère les thèmes de couleurs. Le thème choisi est sauvegardé et appliqué au démarrage
    // (les couleurs sont capturées à la création des contrôles -> un changement de thème
    // demande un redémarrage de l'appli, géré par la fenêtre Settings via Application.Restart).
    internal static class ThemeManager
    {
        public static readonly List<Theme> Themes = new List<Theme>
        {
            new Theme { Name = "Sakura",  Main = Hex("F472B6"), Bg = Hex("1A121A"), Sc = Hex("261A26"),
                Petals = new[]{ Hex("FFD6E6"), Hex("FFB7C5"), Hex("F7A8C4"), Hex("F472B6") } },
            new Theme { Name = "Cyan",    Main = Hex("06B6D4"), Bg = Hex("0E1117"), Sc = Hex("161B22"),
                Petals = new[]{ Hex("CFFAFE"), Hex("A5F3FC"), Hex("67E8F9"), Hex("06B6D4") } },
            new Theme { Name = "Emerald", Main = Hex("10B981"), Bg = Hex("0F1115"), Sc = Hex("181B21"),
                Petals = new[]{ Hex("D1FAE5"), Hex("A7F3D0"), Hex("6EE7B7"), Hex("10B981") } },
            new Theme { Name = "Violet",  Main = Hex("8B5CF6"), Bg = Hex("14121C"), Sc = Hex("1E1B29"),
                Petals = new[]{ Hex("EDE9FE"), Hex("DDD6FE"), Hex("C4B5FD"), Hex("8B5CF6") } },
            new Theme { Name = "Crimson", Main = Hex("EF4444"), Bg = Hex("17110F"), Sc = Hex("241816"),
                Petals = new[]{ Hex("FEE2E2"), Hex("FECACA"), Hex("FCA5A5"), Hex("EF4444") } },
            new Theme { Name = "Gold",    Main = Hex("F59E0B"), Bg = Hex("14110A"), Sc = Hex("201A12"),
                Petals = new[]{ Hex("FEF3C7"), Hex("FDE68A"), Hex("FCD34D"), Hex("F59E0B") } },
            new Theme { Name = "Ocean",   Main = Hex("3B82F6"), Bg = Hex("0D1117"), Sc = Hex("161C27"),
                Petals = new[]{ Hex("DBEAFE"), Hex("BFDBFE"), Hex("93C5FD"), Hex("3B82F6") } },
        };

        public static Theme Current { get; private set; } = Themes[0];

        private static string FilePath
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PaiPai");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "theme.txt");
            }
        }

        // À appeler tout au début (Program.Main), avant toute création de fenêtre.
        public static void LoadAndApply()
        {
            string name = "Sakura";
            try { if (File.Exists(FilePath)) name = File.ReadAllText(FilePath).Trim(); } catch { }
            var t = Themes.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)) ?? Themes[0];
            Apply(t);
        }

        public static void Apply(Theme t)
        {
            Current = t;
            Colors.mainColor = t.Main;
            Colors.bgColor = t.Bg;
            Colors.scColor = t.Sc;
            Colors.sakuraPetals = t.Petals;
        }

        public static void Save(Theme t)
        {
            try { File.WriteAllText(FilePath, t.Name); } catch { }
            Apply(t);
        }

        private static Color Hex(string h) => ColorTranslator.FromHtml("#" + h);
    }
}
