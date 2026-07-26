using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace PlantillaChanchoV16.Utilities
{
    // Persistance des profils de bot du Bot Manager (%LOCALAPPDATA%\PaiPai\bots.json,
    // même dossier que RememberMe.cs/Updater.cs). Le token de chaque profil est
    // chiffré via DPAPI avant d'être écrit -> jamais en clair sur le disque, et
    // le fichier reste illisible même copié ailleurs (scope CurrentUser = lié à
    // la session Windows courante).
    internal static class BotProfileStore
    {
        private class FileModel
        {
            public List<BotProfile> Profiles { get; set; } = new List<BotProfile>();
            public string? ActiveProfileId { get; set; }
        }

        private static string FilePath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PaiPai", "bots.json");

        // Entropie additionnelle pour ne pas dependre uniquement du scope DPAPI par
        // defaut (defense en profondeur, cout nul).
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("PaiPai.BotManager.v1");

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return "";
            byte[] bytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encrypted = ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encrypted);
        }

        public static string Decrypt(string encryptedBase64)
        {
            if (string.IsNullOrEmpty(encryptedBase64)) return "";
            try
            {
                byte[] encrypted = Convert.FromBase64String(encryptedBase64);
                byte[] bytes = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                // Fichier corrompu, ou chiffré par un autre compte Windows -> on ne
                // plante jamais l'appli pour ça, juste un token vide (l'utilisateur
                // devra le ressaisir).
                return "";
            }
        }

        private static FileModel Load()
        {
            try
            {
                string json = File.ReadAllText(FilePath);
                return JsonConvert.DeserializeObject<FileModel>(json) ?? new FileModel();
            }
            catch
            {
                return new FileModel();
            }
        }

        private static void Save(FileModel model)
        {
            string dir = Path.GetDirectoryName(FilePath)!;
            Directory.CreateDirectory(dir);
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(model, Formatting.Indented));
        }

        public static List<BotProfile> GetAll() => Load().Profiles;

        public static BotProfile? GetActive()
        {
            var model = Load();
            if (model.ActiveProfileId == null) return model.Profiles.FirstOrDefault();
            return model.Profiles.FirstOrDefault(p => p.Id == model.ActiveProfileId) ?? model.Profiles.FirstOrDefault();
        }

        public static void SetActive(string profileId)
        {
            var model = Load();
            model.ActiveProfileId = profileId;
            Save(model);
        }

        public static void AddOrUpdate(BotProfile profile)
        {
            var model = Load();
            int idx = model.Profiles.FindIndex(p => p.Id == profile.Id);
            if (idx >= 0) model.Profiles[idx] = profile;
            else model.Profiles.Add(profile);

            // Premier profil ajouté -> devient automatiquement l'actif.
            if (model.ActiveProfileId == null) model.ActiveProfileId = profile.Id;
            Save(model);
        }

        public static void Delete(string profileId)
        {
            var model = Load();
            model.Profiles.RemoveAll(p => p.Id == profileId);
            if (model.ActiveProfileId == profileId)
                model.ActiveProfileId = model.Profiles.FirstOrDefault()?.Id;
            Save(model);
        }
    }
}
