using System;
using System.Collections.Generic;
using System.IO;

namespace PlantillaChanchoV16.Utilities
{
    public enum Lang { EN, FR, ES }

    // Système de langue léger : EN par défaut, FR/ES au choix (Settings). Même principe que
    // ThemeManager -> le choix est sauvegardé sur disque et appliqué au démarrage ; changer
    // de langue après le login reconstruit Main (RebuildForTheme) pour re-rendre les textes
    // (ils sont fixés à la construction des contrôles, comme les couleurs de thème).
    internal static class Localization
    {
        public static Lang Current { get; private set; } = Lang.EN;

        private static string FilePath
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PaiPai");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "language.txt");
            }
        }

        // À appeler tout au début (Program.Main), avant toute création de fenêtre.
        public static void LoadAndApply()
        {
            try
            {
                if (File.Exists(FilePath) && Enum.TryParse(File.ReadAllText(FilePath).Trim(), true, out Lang l))
                    Current = l;
            }
            catch { }
        }

        public static void Set(Lang lang)
        {
            Current = lang;
            try { File.WriteAllText(FilePath, lang.ToString()); } catch { }
        }

        // Traduit "key" dans la langue courante (repli sur l'anglais, puis sur la clé
        // elle-même si jamais absente). Supporte les paramètres façon string.Format.
        public static string T(string key, params object[] args)
        {
            string raw = Lookup(key);
            if (args == null || args.Length == 0) return raw;
            try { return string.Format(raw, args); } catch { return raw; }
        }

        private static string Lookup(string key)
        {
            if (!Strings.TryGetValue(key, out var entry)) return key;
            string s = Current switch
            {
                Lang.FR => entry.fr,
                Lang.ES => entry.es,
                _ => entry.en
            };
            return string.IsNullOrEmpty(s) ? entry.en : s;
        }

        // (en, fr, es) — clé courte + regroupée par écran pour rester lisible.
        private static readonly Dictionary<string, (string en, string fr, string es)> Strings = new()
        {
            // ---- Login ----
            ["login.tagline_signin"] = ("Welcome back, sign in to continue", "Content de te revoir, connecte-toi pour continuer", "Bienvenido de nuevo, inicia sesión para continuar"),
            ["login.tagline_signup"] = ("Create your PaiPai account", "Crée ton compte PaiPai", "Crea tu cuenta de PaiPai"),
            ["login.tab_signin"] = ("Sign In", "Connexion", "Iniciar sesión"),
            ["login.tab_signup"] = ("Sign Up", "Créer un compte", "Registrarse"),
            ["login.field_username"] = ("Username", "Nom d'utilisateur", "Usuario"),
            ["login.field_password"] = ("Password", "Mot de passe", "Contraseña"),
            ["login.field_license"] = ("License key", "Clé de licence", "Clave de licencia"),
            ["login.remember_me"] = ("Remember me", "Se souvenir de moi", "Recordarme"),
            ["login.btn_signin"] = ("Sign In", "Connexion", "Iniciar sesión"),
            ["login.btn_create_account"] = ("Create account", "Créer le compte", "Crear cuenta"),
            ["login.discord_help"] = ("Need help? Join our Discord", "Besoin d'aide ? Rejoins le Discord", "¿Necesitas ayuda? Únete a Discord"),

            // ---- Nav ----
            ["nav.home"] = ("Home", "Accueil", "Inicio"),
            ["nav.products"] = ("Products", "Produits", "Productos"),
            ["nav.account"] = ("Account", "Compte", "Cuenta"),
            ["nav.claim_key"] = ("Claim Key", "Activer une clé", "Canjear clave"),

            ["home.carousel_title"] = ("Our Products", "Nos Produits", "Nuestros Productos"),

            // ---- Chrome principal (Main) ----
            ["main.all_products"] = ("All our products", "Tous nos produits", "Todos nuestros productos"),
            ["main.account_info"] = ("Account information", "Informations du compte", "Información de la cuenta"),
            ["main.my_licenses"] = ("MY LICENSES", "MES LICENCES", "MIS LICENCIAS"),
            ["main.not_claimed"] = ("Not claimed", "Non réclamée", "No reclamada"),
            ["main.refresh"] = ("Refresh", "Actualiser", "Actualizar"),
            ["account.details"] = ("DETAILS", "DÉTAILS", "DETALLES"),
            ["account.status_label"] = ("Status:", "Statut :", "Estado:"),
            ["account.member"] = ("PaiPai member", "Membre PaiPai", "Miembro PaiPai"),
            ["account.licenses_active"] = ("{0} active license(s)", "{0} licence(s) active(s)", "{0} licencia(s) activa(s)"),
            ["account.no_active"] = ("No active license", "Aucune licence active", "Sin licencia activa"),
            ["account.status_active"] = ("ACTIVE", "ACTIVE", "ACTIVA"),
            ["account.status_expired"] = ("EXPIRED", "EXPIRÉE", "EXPIRADA"),
            ["account.status_locked"] = ("LOCKED", "VERROUILLÉE", "BLOQUEADA"),
            ["account.copied"] = ("Copied to clipboard", "Copié dans le presse-papiers", "Copiado al portapapeles"),
            ["main.see_more_products"] = ("See more products", "Voir plus de produits", "Ver más productos"),
            ["main.field_username"] = ("Username:", "Nom d'utilisateur :", "Usuario:"),
            ["main.field_ip"] = ("IP Address:", "Adresse IP :", "Dirección IP:"),
            ["main.field_hwid_reg"] = ("HWID (registration):", "HWID (inscription) :", "HWID (registro):"),
            ["main.field_hwid_cur"] = ("HWID (current):", "HWID (actuel) :", "HWID (actual):"),
            ["main.field_created"] = ("Created At:", "Créé le :", "Creado el:"),
            ["main.field_lastlogin"] = ("Last Login:", "Dernière connexion :", "Último inicio de sesión:"),
            ["main.vpn_label"] = ("VPN", "VPN", "VPN"),
            ["main.vpn_not_connected"] = ("Not connected", "Non connecté", "No conectado"),
            ["main.vpn_connected"] = ("PaiPai is connected to VPN", "PaiPai est connecté au VPN", "PaiPai está conectado a la VPN"),
            ["main.license_missing"] = ("Your {0} license is missing or has expired.\nClick OK to claim a key and unlock it.",
                                        "Ta licence {0} est manquante ou a expiré.\nClique sur OK pour activer une clé et la débloquer.",
                                        "Tu licencia de {0} falta o ha expirado.\nHaz clic en OK para activar una clave y desbloquearla."),

            // ---- Temps restant (LicenseGate / WelcomeBanner) ----
            ["time.lifetime"] = ("Lifetime", "À vie", "De por vida"),
            ["time.days_left"] = ("{0} day(s) left", "{0} jour(s) restant(s)", "{0} día(s) restante(s)"),
            ["time.hours_left"] = ("{0} hour(s) left", "{0} heure(s) restante(s)", "{0} hora(s) restante(s)"),
            ["time.min_left"] = ("{0} min left", "{0} min restantes", "{0} min restantes"),
            ["time.expired"] = ("Expired", "Expirée", "Caducada"),
            ["time.no_active_key"] = ("No active key", "Aucune clé active", "Sin clave activa"),
            ["time.unknown"] = ("Unknown", "Inconnu", "Desconocido"),

            // ---- Bannière d'accueil ----
            ["banner.welcome_back"] = ("WELCOME BACK", "CONTENT DE TE REVOIR", "BIENVENIDO DE NUEVO"),
            ["banner.subtitle"] = ("Choose a product below to get started.", "Choisis un produit ci-dessous pour commencer.", "Elige un producto abajo para empezar."),
            ["banner.license_label"] = ("License", "Licence", "Licencia"),
            ["banner.add_license"] = ("Add license", "Ajouter une licence", "Añadir licencia"),
            ["banner.claim_title"] = ("Add license", "Ajouter une licence", "Añadir licencia"),
            ["banner.claim_body"] = ("Enter a license key to add to your account. You can claim several keys to unlock different products or extend your time.",
                                      "Entre une clé de licence à ajouter à ton compte. Tu peux réclamer plusieurs clés pour débloquer différents produits ou prolonger ton temps.",
                                      "Introduce una clave de licencia para añadirla a tu cuenta. Puedes canjear varias claves para desbloquear distintos productos o ampliar tu tiempo."),
            ["banner.claim_added_title"] = ("License added", "Licence ajoutée", "Licencia añadida"),
            ["banner.claim_added_body"] = ("🌸 {0} has been added to your account!\n\nTime remaining: {1}",
                                            "🌸 {0} a été ajouté à ton compte !\n\nTemps restant : {1}",
                                            "🌸 ¡Se ha añadido {0} a tu cuenta!\n\nTiempo restante: {1}"),
            ["banner.claim_failed"] = ("This key could not be redeemed.", "Cette clé n'a pas pu être activée.", "No se pudo canjear esta clave."),
            ["banner.claim_error"] = ("Recharge error: {0}", "Erreur de recharge : {0}", "Error al recargar: {0}"),

            // ---- Settings ----
            ["settings.title"] = ("Settings", "Paramètres", "Ajustes"),
            ["settings.theme"] = ("THEME", "THÈME", "TEMA"),
            ["settings.theme_hint"] = ("Pick a color theme (applies instantly, no restart).", "Choisis un thème (appliqué instantanément, sans redémarrage).", "Elige un tema (se aplica al instante, sin reiniciar)."),
            ["settings.language"] = ("LANGUAGE", "LANGUE", "IDIOMA"),
            ["settings.updates"] = ("UPDATES", "MISES À JOUR", "ACTUALIZACIONES"),
            ["settings.updates_title"] = ("Updates", "Mises à jour", "Actualizaciones"),
            ["settings.check_updates"] = ("Check for updates", "Vérifier les mises à jour", "Buscar actualizaciones"),
            ["settings.performance"] = ("PERFORMANCE", "PERFORMANCE", "RENDIMIENTO"),
            ["settings.low_power"] = ("Reduce animations (save FPS / resources)", "Réduire les animations (gagner en FPS)", "Reducir animaciones (ahorra FPS)"),
            ["settings.account"] = ("ACCOUNT", "COMPTE", "CUENTA"),
            ["settings.account_title"] = ("Account", "Compte", "Cuenta"),
            ["settings.clear_login"] = ("Clear saved login", "Oublier la connexion", "Olvidar sesión guardada"),
            ["settings.close"] = ("Close", "Fermer", "Cerrar"),
            ["settings.up_to_date"] = ("PaiPai is up to date (v{0}).", "PaiPai est à jour (v{0}).", "PaiPai está actualizado (v{0})."),
            ["settings.forget_login_confirm"] = ("Forget the saved login on this PC?", "Oublier la connexion enregistrée sur ce PC ?", "¿Olvidar la sesión guardada en este PC?"),
            ["settings.login_cleared"] = ("Saved login cleared.", "Connexion enregistrée oubliée.", "Sesión guardada olvidada."),
        };
    }
}
