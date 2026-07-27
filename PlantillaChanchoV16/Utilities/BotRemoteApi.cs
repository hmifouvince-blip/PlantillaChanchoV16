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
    // message d'erreur lisible a la place. Les identifiants sont passes PAR
    // APPEL (jamais gardes comme etat) pour que plusieurs bots puissent etre
    // pilotes avec la meme instance sans melange.
    internal static class BotRemoteApi
    {
        // Timeout court : l'UI sonde /health toutes les 3 s. Une requete qui
        // traine plus longtemps que l'intervalle de sondage empilerait les
        // appels au lieu de simplement afficher "injoignable".
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(6) };

        // Deux facons de prouver son identite au bot, envoyees ensemble : le
        // serveur retient la premiere valide.
        // - Key   : secret d'infrastructure (CONTROL_KEY chez l'hebergeur)
        // - Token : jeton personnel obtenu via /link, adosse a un role Discord
        //           et revoque des que ce role est retire
        public sealed class Auth
        {
            public string Key { get; }
            public string Token { get; }

            public Auth(string? key, string? token)
            {
                Key = (key ?? "").Trim();
                Token = (token ?? "").Trim();
            }

            public bool IsEmpty => Key.Length == 0 && Token.Length == 0;
            public static Auth None => new Auth("", "");
        }

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

        private static async Task<Result> SendAsync(HttpMethod method, string baseUrl, Auth auth, string path, JObject? body = null)
        {
            string root = NormalizeUrl(baseUrl);
            if (root.Length == 0) return new Result { Success = false, Error = "Aucune URL de contrôle configurée." };

            try
            {
                using var req = new HttpRequestMessage(method, root + path);
                // En-tetes omis quand vides : un "x-paipai-token: " vide ferait
                // echouer la comparaison cote bot avant meme d'essayer la cle.
                if (auth.Key.Length > 0) req.Headers.Add("x-paipai-key", auth.Key);
                if (auth.Token.Length > 0) req.Headers.Add("x-paipai-token", auth.Token);
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

        public static Task<Result> Health(string baseUrl, Auth auth)
            => SendAsync(HttpMethod.Get, baseUrl, auth, "/health");

        // Identite reconnue par le bot (cle d'infra ou compte Discord lie).
        public static Task<Result> Me(string baseUrl, Auth auth)
            => SendAsync(HttpMethod.Get, baseUrl, auth, "/me");

        // since = numero de la derniere ligne deja affichee -> le bot ne renvoie
        // que les nouvelles, aucune ligne dupliquee dans la console de PaiPai.
        public static Task<Result> Logs(string baseUrl, Auth auth, long since)
            => SendAsync(HttpMethod.Get, baseUrl, auth, $"/logs?since={since}");

        public static Task<Result> Restart(string baseUrl, Auth auth)
            => SendAsync(HttpMethod.Post, baseUrl, auth, "/restart");

        public static Task<Result> Store(string baseUrl, Auth auth)
            => SendAsync(HttpMethod.Get, baseUrl, auth, "/store");

        // C'est le bot qui edite son propre message de statut : lui seul connait
        // l'ID suivi dans data/store.json, fichier qui vit desormais chez
        // l'hebergeur et non sur la machine de l'utilisateur.
        public static Task<Result> SetProductStatus(string baseUrl, Auth auth, string productKey, string state)
            => SendAsync(HttpMethod.Post, baseUrl, auth, "/product-status",
                new JObject { ["productKey"] = productKey, ["state"] = state });

        // Catalogue produit du bot. C'est LUI qui fait autorite : il melange
        // les produits ecrits dans son code et ceux crees depuis PaiPai, que
        // l'application n'a aucun moyen de connaitre autrement.
        public static Task<Result> Products(string baseUrl, Auth auth)
            => SendAsync(HttpMethod.Get, baseUrl, auth, "/products");

        // Creation ET edition : le bot distingue les deux a la cle. Les champs
        // absents de l'objet gardent leur valeur actuelle -> on peut n'envoyer
        // qu'une description sans effacer le reste.
        public static Task<Result> SaveProduct(string baseUrl, Auth auth, JObject product)
            => SendAsync(HttpMethod.Post, baseUrl, auth, "/product", product);

        // Publier VIA le bot (et non directement via l'API Discord) : la mise
        // en page riche vit dans paipai-discord-bot/utils/embeds.js, un seul
        // endroit. La reconstruire ici la ferait diverger au premier changement.
        public static Task<Result> Announce(string baseUrl, Auth auth, string title, string message, bool ping)
            => SendAsync(HttpMethod.Post, baseUrl, auth, "/announce",
                new JObject { ["title"] = title, ["message"] = message, ["ping"] = ping });

        public static Task<Result> Update(string baseUrl, Auth auth, string title, string changelog,
                                          string? productKey, string? version, string? note, bool ping)
            => SendAsync(HttpMethod.Post, baseUrl, auth, "/update",
                new JObject
                {
                    ["title"] = title,
                    ["changelog"] = changelog,
                    ["productKey"] = productKey,
                    ["version"] = version,
                    ["note"] = note,
                    ["ping"] = ping,
                });

        // Echange le code affiche par /link contre un jeton personnel.
        // Deliberement SANS identifiants : c'est la route d'amorcage, son but
        // est justement d'en delivrer.
        public static Task<Result> RedeemLink(string baseUrl, string code)
            => SendAsync(HttpMethod.Post, baseUrl, Auth.None, "/link/redeem",
                new JObject { ["code"] = code });
    }
}
