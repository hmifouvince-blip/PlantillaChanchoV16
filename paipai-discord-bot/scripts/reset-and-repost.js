// Script a usage UNIQUE : supprime tous les messages postes par un bot
// (l'ancienne application supprimee ET la nouvelle) dans TOUS les salons
// texte du serveur, puis relance runSetup() pour tout reposter proprement
// avec le style actuel (voir utils/embeds.js). Ne touche jamais un message
// ecrit par un humain (filtre sur author.bot), ni les salons/roles eux-memes.
//
// A executer UNE FOIS (voir startup command temporaire dans le panel de
// l'hebergeur), jamais a chaque demarrage : sinon chaque restart reviderait
// #announcements/#updates de leur historique.
require("dotenv").config();
const { Client, GatewayIntentBits, ChannelType } = require("discord.js");
const { runSetup } = require("../commands/setup-server");
const store = require("../utils/store");

if (!process.env.BOT_TOKEN || !process.env.GUILD_ID) {
  console.error("❌ BOT_TOKEN et/ou GUILD_ID manquants dans .env.");
  process.exit(1);
}

const client = new Client({
  intents: [GatewayIntentBits.Guilds, GatewayIntentBits.GuildMembers],
});

async function purgeBotMessages(channel) {
  let total = 0;
  for (;;) {
    const batch = await channel.messages.fetch({ limit: 100 }).catch(() => null);
    if (!batch || batch.size === 0) break;
    const botMessages = [...batch.filter((m) => m.author.bot).values()];
    for (const msg of botMessages) {
      await msg.delete().catch(() => {});
      total += 1;
    }
    if (batch.size < 100) break;
  }
  return total;
}

client.once("clientReady", async () => {
  console.log(`Connecte en tant que ${client.user.tag}. Nettoyage en cours...`);
  try {
    const guild = await client.guilds.fetch(process.env.GUILD_ID);
    await guild.channels.fetch();

    let totalDeleted = 0;
    for (const channel of guild.channels.cache.values()) {
      if (channel.type !== ChannelType.GuildText) continue;
      const deleted = await purgeBotMessages(channel);
      totalDeleted += deleted;
      if (deleted > 0) console.log(`- #${channel.name} : ${deleted} message(s) supprime(s).`);
    }
    console.log(`Total supprime : ${totalDeleted}.`);

    // Le message de statut vient d'etre supprime -> sans ce reset, runSetup
    // croirait a tort qu'il existe encore (store.json garde son ancien ID).
    store.update((d) => {
      d.statusMessage = null;
    });

    console.log("Republication...");
    const log = await runSetup(guild);
    console.log("\n=== Resultat ===");
    console.log(log.join("\n"));
  } catch (err) {
    console.error("❌ Echec :", err.message);
  } finally {
    client.destroy();
    process.exit(0);
  }
});

client.login(process.env.BOT_TOKEN.trim()).catch((err) => {
  console.error("❌ Connexion a Discord echouee :", err.message);
  process.exit(1);
});
