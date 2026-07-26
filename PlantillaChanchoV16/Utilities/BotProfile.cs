using System;

namespace PlantillaChanchoV16.Utilities
{
    // Un "profil" = un bot Discord que l'utilisateur veut piloter depuis PaiPai
    // (Bot Manager). Le token n'est JAMAIS stocké en clair : uniquement sous
    // forme chiffrée DPAPI (voir BotProfileStore). LocalFolderPath est optionnel
    // -> sans lui, seules les actions API directes (annonce/update/statut)
    // fonctionnent ; avec lui, le contrôle du process (démarrer/arrêter/logs)
    // et la lecture des tickets (data/store.json) deviennent aussi disponibles.
    public class BotProfile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = "";

        // Base64 du token chiffré via ProtectedData (scope CurrentUser) -> illisible
        // même si ce fichier est copié sur une autre machine ou ouvert par un autre
        // compte Windows.
        public string EncryptedTokenBase64 { get; set; } = "";

        public string GuildId { get; set; } = "";
        public string? LocalFolderPath { get; set; }

        // Bot heberge 24/7 ailleurs : URL de son API de controle
        // (paipai-discord-bot/control/server.js) + cle secrete partagee, chiffree
        // comme le token. Renseignes -> le Bot Manager bascule en mode distant
        // (etat/logs/redemarrage par HTTP) et ignore LocalFolderPath, qui ne
        // designe alors qu'une copie locale du code, pas le bot qui tourne.
        public string? RemoteUrl { get; set; }
        public string EncryptedControlKeyBase64 { get; set; } = "";

        // Liaison Discord (commande /link cote bot) : alternative a la cle de
        // controle pour les membres de l'equipe. La cle est un secret
        // d'infrastructure qu'on ne peut pas distribuer (la donner = tout
        // donner, la retirer a une personne = la changer pour tout le monde) ;
        // ce jeton-ci est personnel, adosse a un role Discord, et se revoque
        // en retirant simplement le role.
        public string EncryptedSessionTokenBase64 { get; set; } = "";

        // Compte Discord lie, affiche tel quel dans le Bot Manager. Purement
        // informatif : la source de verite reste le jeton, revalide par le bot.
        public string? LinkedDiscordTag { get; set; }
    }
}
