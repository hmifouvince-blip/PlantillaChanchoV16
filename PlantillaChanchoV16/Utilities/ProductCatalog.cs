using System.Collections.Generic;

namespace PlantillaChanchoV16.Utilities
{
    // Source unique de vérité : nom d'abonnement KeyAuth <-> nom de produit affiché <-> clé
    // de routage interne (gameName utilisé par OpenGameDetails). Utilisé pour le verrou de
    // licence, le popup de claim et l'affichage du temps restant par produit dans User.
    internal static class ProductCatalog
    {
        public class Entry
        {
            public string SubscriptionName;   // nom exact sur le dashboard KeyAuth
            public string DisplayName;        // nom affiché à l'utilisateur
            public string GameKey;            // clé utilisée par OpenGameDetails / LoadGame
        }

        public static readonly List<Entry> All = new List<Entry>
        {
            new Entry { SubscriptionName = "Spoofer",    DisplayName = "Spoofer",        GameKey = "anydesk" },
            new Entry { SubscriptionName = "Valorant",   DisplayName = "Valorant",       GameKey = "valorant" },
            new Entry { SubscriptionName = "Roblox",     DisplayName = "Roblox",         GameKey = "roblox" },
            new Entry { SubscriptionName = "WindowsPai", DisplayName = "Windows PaiPai", GameKey = "windowspai" },
        };

        public static Entry BySubscription(string subscriptionName)
        {
            foreach (var e in All)
                if (string.Equals(e.SubscriptionName, subscriptionName, System.StringComparison.OrdinalIgnoreCase))
                    return e;
            return null;
        }

        public static Entry ByGameKey(string gameKey)
        {
            foreach (var e in All)
                if (string.Equals(e.GameKey, gameKey, System.StringComparison.OrdinalIgnoreCase))
                    return e;
            return null;
        }

        // Nom affiché pour un abonnement inconnu (produits legacy/désactivés) : renvoie tel quel.
        public static string DisplayNameOf(string subscriptionName)
        {
            return BySubscription(subscriptionName)?.DisplayName ?? subscriptionName;
        }
    }
}
