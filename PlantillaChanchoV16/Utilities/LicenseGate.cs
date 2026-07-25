using System;

namespace PlantillaChanchoV16.Utilities
{
    // Verrou d'accès partagé : un produit ne s'ouvre que si l'utilisateur possède déjà
    // l'abonnement KeyAuth correspondant (claim via "Add license" sur l'accueil) ET qu'il
    // n'a pas expiré. Même logique que Windows PaiPai, centralisée pour tous les produits.
    internal static class LicenseGate
    {
        public static bool HasValidSubscription(string subscriptionName)
        {
            return GetExpiry(subscriptionName).HasValue;
        }

        // Renvoie la date d'expiration locale si l'abonnement est possédé ET valide, sinon null.
        public static DateTime? GetExpiry(string subscriptionName)
        {
            var subs = Login.KeyAuthApp?.user_data?.subscriptions;
            if (subs == null) return null;

            foreach (var s in subs)
            {
                if (!string.Equals(s.subscription, subscriptionName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (long.TryParse(s.expiry, out long exp))
                {
                    DateTime expiry = DateTimeOffset.FromUnixTimeSeconds(exp).LocalDateTime;
                    if (expiry > DateTime.Now) return expiry;
                }
            }
            return null;
        }

        // "Lifetime" / "X day(s) left" / "X hour(s) left" / "X min left" / "Expired" (localisé).
        public static string FormatTimeLeft(DateTime expiry)
        {
            DateTime now = DateTime.Now;
            if (expiry <= now) return Localization.T("time.expired");

            TimeSpan span = expiry - now;
            if (span.TotalDays > 3650) return Localization.T("time.lifetime");           // > ~10 ans = à vie
            if (span.TotalDays >= 1) return Localization.T("time.days_left", (int)span.TotalDays);
            if (span.TotalHours >= 1) return Localization.T("time.hours_left", (int)span.TotalHours);
            return Localization.T("time.min_left", (int)span.TotalMinutes);
        }
    }
}
