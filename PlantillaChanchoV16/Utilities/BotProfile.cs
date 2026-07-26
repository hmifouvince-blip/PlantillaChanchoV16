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
    }
}
