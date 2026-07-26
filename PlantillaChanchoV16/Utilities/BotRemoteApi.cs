using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace PlantillaChanchoV16.Utilities
{
    // Client de l'API de controle exposee par le bot lui-meme
    // (paipai-discord-bot/control/server.js). Sert le cas "bot heberge 24/7
    // ailleurs" : BotProcessManager ne peut alors rien piloter (aucun process
    // local), tout passe par HTTP.
    //
    // Meme contrat que DiscordApi : jamais d'exception qui remonte a l'UI, un
    // message d'erreur lisible a la place. La cle est passee PAR APPEL (jamais
    // gardee comme etat) pour que plusieurs bots puissent etre pilotes avec la
    // meme instance sans melange.
    internal static class BotRemoteApi
    {
        // Timeout court : l'UI sonde /health toutes les 3 s. Une requete qui
        // traine plus longtemps que l'intervalle de sondage empilerait les
        // appels au lieu de simplement afficher "injoignable".
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(6) };

        public class Result
        {
            public bool Success { get; set; }
            public string? Error { get; set; }
            public JObject? Data { get; set; }
        }

        // Accepte ce que l'utilisateur colle depuis le panel de son hebergeur :
        // "1.2.3.4:25565", "http://host:port" ou une URL avec slash final.
        public static string NormalizeUrl(string url)
        {
            string u = (url ?? "").Trim().TrimEnd('/');
            if (u.Length == 0) return "";
            if (!u.Contains("://")) u = "http://" + u;
            return u;
        }

        private static async Task<Result> SendAsync(HttpMethod method, string baseUrl, string key, string path, JObject? body = null)
        {
            string root = NormalizeUrl(baseUrl);
            if (root.Length == 0) return new Result { Success = false, Error = "Aucune URL de contrôle configurée." };

            try
            {
                using var req = new HttpRequestMessage(method, root + path);
                req.Headers.Add("x-paipai-key", key);
                req.Headers.Add("User-Agent", "PaiPaiBotManager/1.0");
                if (body != null) req.Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");

                using var response = await Http.SendAsync(req);
                string raw = await response.Content.ReadAsStringAsync();

                JObject? data = null;
                try { data = string.IsNullOrWhiteSpace(raw) ? null : JObject.Parse(raw); } catch { }

                if (!response.IsSuccessStatusCode)
                {
                    // Le serveur de controle renvoie toujours {"ok":false,"error":"..."}
                    // -> ce message est bien plus parlant que le code HTTP seul.
                    string message = data?["error"]?.ToString() ?? $"{(int)response.StatusCode} {response.StatusCode}";
                    return new Result { Success = false, Error = message };
                }

                return new Result { Success = true, Data = data };
            }
            catch (TaskCanceledException)
            {
                // Timeout HttpClient : le cas le plus frequent quand l'hebergeur
                // est tombe ou que le port n'est pas ouvert.
                return new Result { Success = false, Error = "Bot injoignable (délai dépassé)." };
            }
            catch (HttpRequestException ex)
            {
                return new Result { Success = false, Error = $"Bot injoignable : {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new Result { Success = false, Error = ex.Message };
            }
        }

        public static Task<Result> Health(string baseUrl, string key)
            => SendAsync(HttpMethod.Get, baseUrl, key, "/health");

        // since = numero de la derniere ligne deja affichee -> le bot ne renvoie
        // que les nouvelles, aucune ligne dupliquee dans la console de PaiPai.
        public static Task<Result> Logs(string baseUrl, string key, long since)
            => SendAsync(HttpMethod.Get, baseUrl, key, $"/logs?since={since}");

        public static Task<Result> Restart(string baseUrl, string key)
            => SendAsync(HttpMethod.Post, baseUrl, key, "/restart");

        public static Task<Result> Store(string baseUrl, string key)
            => SendAsync(HttpMethod.Get, baseUrl, key, "/store");

        // C'est le bot qui edite son propre message de statut : lui seul connait
        // l'ID suivi dans data/store.json, fichier qui vit desormais chez
        // l'hebergeur et non sur la machine de l'utilisateur.
        public static Task<Result> SetProductStatus(string baseUrl, string key, string productKey, string state)
            => SendAsync(HttpMethod.Post, baseUrl, key, "/product-status",
                new JObject { ["productKey"] = productKey, ["state"] = state });
    }
}
