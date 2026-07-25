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
                new DiscordRPC.Button() { Label = "Join PaiPai", Url = "https://discord.gg/paipai" },
            };

            startTime = DateTime.UtcNow;
            rpctimestamp = new Timestamps(startTime);

            presence = new RichPresence()
            {
                Buttons = buttons,
                Timestamps = rpctimestamp,
                Details = "Cooking",
                State = "PaiPai",

                Assets = new Assets()
                {
                    LargeImageKey = "paipai",   // asset "paipai" à uploader dans le Discord Dev Portal (le logo)
                    LargeImageText = "PaiPai",
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
            presence.Details = "Cooking";
            if (Login.KeyAuthApp.response.success)
                presence.State = "PaiPai · " + Login.KeyAuthApp.user_data.username;
            else
                presence.State = "PaiPai";

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
