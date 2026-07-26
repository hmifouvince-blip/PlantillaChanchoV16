using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace PlantillaChanchoV16.Utilities
{
    // Wrapper HTTP minimal vers l'API REST Discord (v10), authentifie via le
    // token du bot ("Authorization: Bot <token>"). PREMIERE utilisation de
    // HttpClient dans ce projet (KeyAuth utilise l'ancien WebClient) -> isole
    // ici pour ne rien mélanger. Le token est passe PAR APPEL (jamais stocke
    // comme etat du client) : le Bot Manager peut piloter plusieurs bots avec
    // la meme instance sans risque de melange.
    internal static class DiscordApi
    {
        private const string BaseUrl = "https://discord.com/api/v10";

        // Un seul HttpClient partage pour toute l'appli (bonne pratique .NET :
        // en creer un par appel epuise les sockets a la longue).
        private static readonly HttpClient Http = new HttpClient();

        public class ApiResult
        {
            public bool Success { get; set; }
            public string? Error { get; set; }
            public JToken? Data { get; set; }
        }

        private static HttpRequestMessage NewRequest(HttpMethod method, string path, string token)
        {
            var req = new HttpRequestMessage(method, $"{BaseUrl}{path}");
            req.Headers.Add("Authorization", $"Bot {token}");
            req.Headers.Add("User-Agent", "PaiPaiBotManager/1.0");
            return req;
        }

        private static async Task<ApiResult> SendAsync(HttpRequestMessage request)
        {
            try
            {
                using var response = await Http.SendAsync(request);
                string body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    // Discord renvoie un JSON {"message": "...", "code": ...} sur erreur ->
                    // on remonte ce message tel quel, plus parlant qu'un simple code HTTP.
                    string message = body;
                    try { message = JObject.Parse(body)["message"]?.ToString() ?? body; } catch { }
                    return new ApiResult { Success = false, Error = $"{(int)response.StatusCode} {response.StatusCode}: {message}" };
                }

                JToken? data = string.IsNullOrWhiteSpace(body) ? null : JToken.Parse(body);
                return new ApiResult { Success = true, Data = data };
            }
            catch (Exception ex)
            {
                return new ApiResult { Success = false, Error = ex.Message };
            }
        }

        // Verifie que le token/guild ID d'un profil sont valides (utilise pour
        // afficher un etat clair dans l'UI du Bot Manager avant toute action).
        public static async Task<ApiResult> TestConnection(string token, string guildId)
        {
            var req = NewRequest(HttpMethod.Get, $"/guilds/{guildId}", token);
            return await SendAsync(req);
        }

        public static async Task<ApiResult> GetChannels(string token, string guildId)
        {
            var req = NewRequest(HttpMethod.Get, $"/guilds/{guildId}/channels", token);
            return await SendAsync(req);
        }

        // Cherche un salon par nom EXACT parmi les salons de la guilde (type=0 =
        // salon texte). Retourne l'ID du salon ou null si introuvable.
        public static async Task<string?> FindTextChannelIdByName(string token, string guildId, string channelName)
        {
            var result = await GetChannels(token, guildId);
            if (!result.Success || result.Data is not JArray channels) return null;

            foreach (var ch in channels)
            {
                if ((int?)ch["type"] == 0 && string.Equals((string?)ch["name"], channelName, StringComparison.Ordinal))
                    return (string?)ch["id"];
            }
            return null;
        }

        // Construit un embed simple (couleur PaiPai par defaut) -> reutilise pour
        // annonce/update/statut, meme visuel que celui poste par le bot Node.
        public static JObject BuildEmbed(string title, string description, int colorArgb, string footerText)
        {
            return new JObject
            {
                ["title"] = title,
                ["description"] = description,
                ["color"] = colorArgb,
                ["footer"] = new JObject { ["text"] = footerText },
            };
        }

        public static async Task<ApiResult> PostMessage(string token, string channelId, JObject embed, bool pingEveryone = false)
        {
            var payload = new JObject
            {
                ["embeds"] = new JArray { embed },
            };
            if (pingEveryone) payload["content"] = "@everyone";

            var req = NewRequest(HttpMethod.Post, $"/channels/{channelId}/messages", token);
            req.Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
            return await SendAsync(req);
        }

        public static async Task<ApiResult> EditMessage(string token, string channelId, string messageId, JObject embed)
        {
            var payload = new JObject { ["embeds"] = new JArray { embed } };
            var req = NewRequest(HttpMethod.Patch, $"/channels/{channelId}/messages/{messageId}", token);
            req.Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
            return await SendAsync(req);
        }
    }
}
