using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using DiscordRPC;
using PlantillaChanchoV16;
using Timer = System.Timers.Timer;

namespace PlantillaChanchoV16.Utilities
{
    public class Class1
    {
        public static DiscordRpcClient client;
        public static Timestamps rpctimestamp { get; set; }
        private static RichPresence presence;
        private static Timer updateTimer;
        private static DateTime startTime;

        public static string StatusText { get; set; }


        public static void InitializeRPC()
        {
            // Replace "YOUR_DISCORD_APP_ID_HERE" with your actual Discord application ID
            client = new DiscordRpcClient("1199192721252958270");
            client.Initialize();

            DiscordRPC.Button[] buttons = {
                new DiscordRPC.Button() { Label = "CUSTOM DESIGN", Url = "https://discord.gg/WHm7nezB87" },
                new DiscordRPC.Button() { Label = "BUY PROJECT", Url = "https://discord.gg/WHm7nezB87" },
            };

            startTime = DateTime.UtcNow;
            rpctimestamp = new Timestamps(startTime);

            presence = new RichPresence()
            {
                Buttons = buttons,
                Timestamps = rpctimestamp,

                Assets = new Assets()
                {
                    LargeImageKey = "panel_chancho_safe",
                    LargeImageText = "CHANCHO",
                    SmallImageKey = "https://i.pinimg.com/originals/6b/17/28/6b17287c0580c33894286a585bdd3f07.gif",
                    SmallImageText = ""
                }
            };

            client.SetPresence(presence);

            updateTimer = new Timer(60000);
            updateTimer.Elapsed += UpdateDiscordPresence;
            updateTimer.Start();
        }

        public static void SetState(string state, bool watching = false)
        {
            if (watching)
                state = "Looking at " + state;

            presence.State = state;
            client.SetPresence(presence);
        }

        public static void UpdateDiscordPresence(object sender = null, ElapsedEventArgs e = null)
        {
            if (Login.KeyAuthApp.response.success)
            {
                string username = Login.KeyAuthApp.user_data.username;
                string expiryString = Login.KeyAuthApp.user_data.subscriptions?.FirstOrDefault()?.expiry;

                if (!string.IsNullOrEmpty(expiryString) && long.TryParse(expiryString, out long expiryUnixTime))
                {
                    DateTime expiryDateTime = UnixTimeToDateTime(expiryUnixTime);

                    presence.Details = $"Username: {username}";
                    presence.State = $"Expiry: { expiryDateTime: dd - MM - yyyy}";
                }
                else
                {
                    presence.Details = $"Usuario: {username}";
                    presence.State = "No subscriptions";
                }
            }
            else
            {
                presence.Details = "USER";
                presence.State = StatusText;
            }

            presence.Timestamps = new Timestamps(startTime);
            client.SetPresence(presence);
        }


        private static DateTime UnixTimeToDateTime(long unixTime)
        {
            DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return unixStart.AddSeconds(unixTime).ToLocalTime();
        }
    }
}
