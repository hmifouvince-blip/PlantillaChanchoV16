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

        // Neutres du thème. Le template d'origine codait en dur des gris BLEUS
        // (#878BA6, #A2A5BE, #282A39, #23242D) qui juraient avec le fond prune :
        // les fiches produit paraissaient "froides" à côté du reste de l'app.
        // Ces trois nuances sont les mêmes gris, réchauffés vers le rose.
        public static Color textMuted = Color.FromArgb(154, 140, 154);   // #9A8C9A texte secondaire
        public static Color textSubtle = Color.FromArgb(184, 170, 184);  // #B8AAB8 texte tertiaire
        public static Color divider = Color.FromArgb(52, 38, 52);        // #342634 séparateurs / rails

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
