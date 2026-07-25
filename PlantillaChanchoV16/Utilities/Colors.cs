using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantillaChanchoV16.Utilities
{
    internal class Colors
    {
        // ===== Thème Sakura (fleur de cerisier) =====
        // Accent rose sakura sur fond prune sombre.
        public static Color mainColor = Color.FromArgb(244, 114, 182); // #F472B6 rose sakura

        public static Color bgColor = Color.FromArgb(26, 18, 26);      // #1A121A prune très sombre

        public static Color scColor = Color.FromArgb(38, 26, 38);      // #261A26 panneaux / champs

        // Nuances de pétales utilisées par l'animation sakura.
        public static Color[] sakuraPetals = new Color[]
        {
            Color.FromArgb(255, 214, 230), // #FFD6E6 rose très clair
            Color.FromArgb(255, 183, 197), // #FFB7C5 rose cerisier
            Color.FromArgb(247, 168, 196), // #F7A8C4
            Color.FromArgb(244, 114, 182), // #F472B6 accent
        };
    }
}
