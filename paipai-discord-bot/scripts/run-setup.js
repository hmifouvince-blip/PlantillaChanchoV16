// Execute /setup-server SANS passer par une interaction Discord (le bot n'a
// pas besoin qu'un humain tape la commande dans un salon). runSetup() est
// deliberement exportee par commands/setup-server.js pour ce cas exact :
// "called by the slash command (through a real Discord interaction) AND by
// one-off scripts (direct execution, no interaction needed)".
require("dotenv").config();
const { Client, GatewayIntentBits } = require("discord.js");
const { runSetup } = require("../commands/setup-server");

if (!process.env.BOT_TOKEN || !process.env.GUILD_ID) {
  console.error("❌ BOT_TOKEN et/ou GUILD_ID manquants dans .env.");
  process.exit(1);
}

const client = new Client({
  intents: [GatewayIntentBits.Guilds, GatewayIntentBits.GuildMembers],
});

client.once("clientReady", async () => {
  console.log(`Connecte en tant que ${client.user.tag}. Lancement de la configuration...`);
  try {
    const guild = await client.guilds.fetch(process.env.GUILD_ID);
    const log = await runSetup(guild);
    console.log("\n=== Resultat ===");
    console.log(log.join("\n"));
  } catch (err) {
    console.error("❌ Echec de la configuration :", err.message);
  } finally {
    client.destroy();
    process.exit(0);
  }
});

client.login(process.env.BOT_TOKEN.trim()).catch((err) => {
  console.error("❌ Connexion a Discord echouee :", err.message);
  process.exit(1);
});
