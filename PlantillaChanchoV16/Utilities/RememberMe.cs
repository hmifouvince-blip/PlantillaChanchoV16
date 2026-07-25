using System;
using System.IO;
using System.Text;

namespace PlantillaChanchoV16.Utilities
{
    // Sauvegarde locale "Se souvenir de moi" (username + password), légèrement obfusquée
    // (XOR + Base64). Ce n'est pas du chiffrement fort, juste pour éviter le clair sur disque.
    internal static class RememberMe
    {
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("PaiPai-sakura-2025-remember-key");

        private static string FilePath
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PaiPai");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "creds.dat");
            }
        }

        public static void Save(string username, string password)
        {
            try
            {
                string payload = (username ?? "") + "\n" + (password ?? "");
                File.WriteAllText(FilePath, Obfuscate(payload));
            }
            catch { }
        }

        // Retourne (username, password) ou null si rien de sauvegardé.
        public static (string username, string password)? Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return null;
                string payload = Deobfuscate(File.ReadAllText(FilePath));
                string[] parts = payload.Split('\n');
                if (parts.Length < 2) return null;
                return (parts[0], parts[1]);
            }
            catch { return null; }
        }

        public static void Clear()
        {
            try { if (File.Exists(FilePath)) File.Delete(FilePath); }
            catch { }
        }

        private static string Obfuscate(string text)
        {
            byte[] data = Encoding.UTF8.GetBytes(text);
            for (int i = 0; i < data.Length; i++) data[i] ^= Key[i % Key.Length];
            return Convert.ToBase64String(data);
        }

        private static string Deobfuscate(string b64)
        {
            byte[] data = Convert.FromBase64String(b64);
            for (int i = 0; i < data.Length; i++) data[i] ^= Key[i % Key.Length];
            return Encoding.UTF8.GetString(data);
        }
    }
}
