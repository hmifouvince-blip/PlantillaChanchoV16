using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace PlantillaChanchoV16.Utilities
{
    // Creation d'un compte SANS licence. KeyAuth n'a pas d'inscription sans
    // cle : register(user, pass, key) l'exige toujours. On demande donc au BOT
    // PaiPai de generer une cle "compte gratuit" (niveau qui ne debloque aucun
    // produit), puis l'application s'inscrit normalement avec.
    //
    // POURQUOI PASSER PAR LE BOT : generer une licence exige la cle VENDEUR
    // KeyAuth. La mettre dans l'exe reviendrait a la publier -- n'importe quel
    // acheteur pourrait alors creer, et surtout SUPPRIMER, toutes les licences
    // de la boutique. Elle reste donc chez l'hebergeur, et l'appli ne recoit
    // qu'une cle a usage unique.
    //
    // Le mot de passe, lui, ne transite JAMAIS par ici : il part directement
    // de l'appli vers KeyAuth en HTTPS (l'API de controle du bot, elle, est en
    // HTTP simple sur la plupart des hebergeurs).
    internal static class SignupApi
    {
        // Adresse par defaut du bot PaiPai. Si l'utilisateur a configure un
        // profil dans le Bot Manager, la sienne est prioritaire -> une equipe
        // qui deplace son bot n'a pas besoin d'une nouvelle version de l'appli.
        private const string DefaultControlUrl = "51.79.44.111:9697";

        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

        public static string ResolveUrl()
        {
            try
            {
                string? fromProfile = BotProfileStore.GetActive()?.RemoteUrl;
                if (!string.IsNullOrWhiteSpace(fromProfile)) return BotRemoteApi.NormalizeUrl(fromProfile);
            }
            catch { /* aucun profil lisible : on retombe sur l'adresse par defaut */ }
            return BotRemoteApi.NormalizeUrl(DefaultControlUrl);
        }

        public class Result
        {
            public bool Success { get; set; }
            public string Key { get; set; } = "";
            public string Error { get; set; } = "";
        }

        // Demande une cle de compte gratuit. Route publique cote bot, mais
        // limitee par IP et par jour : sans cela, un script pourrait fabriquer
        // des milliers de licences et polluer le dashboard KeyAuth.
        public static async Task<Result> RequestFreeKeyAsync()
        {
            string url = ResolveUrl();
            if (url.Length == 0) return new Result { Error = "No PaiPai server configured." };

            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, url + "/signup-key");
                req.Headers.Add("User-Agent", "PaiPaiApp/1.0");
                req.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

                using var response = await Http.SendAsync(req);
                string raw = await response.Content.ReadAsStringAsync();

                JObject? data = null;
                try { data = string.IsNullOrWhiteSpace(raw) ? null : JObject.Parse(raw); } catch { }

                if (!response.IsSuccessStatusCode)
                {
                    return new Result
                    {
                        Error = data?["error"]?.ToString() ?? $"{(int)response.StatusCode} {response.StatusCode}",
                    };
                }

                string key = data?["key"]?.ToString() ?? "";
                if (key.Length == 0) return new Result { Error = "The server answered without a key." };

                return new Result { Success = true, Key = key };
            }
            catch (TaskCanceledException)
            {
                return new Result { Error = "PaiPai server unreachable (timed out)." };
            }
            catch (Exception ex)
            {
                return new Result { Error = $"PaiPai server unreachable: {ex.Message}" };
            }
        }
    }
}
