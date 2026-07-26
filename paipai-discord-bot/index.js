// Point d'entree du bot PaiPai. Charge la config, les commandes et les
// evenements, puis se connecte a Discord.
require("dotenv").config();
const fs = require("node:fs");
const path = require("node:path");
const { Client, GatewayIntentBits, Collection } = require("discord.js");

if (!process.env.BOT_TOKEN || !process.env.GUILD_ID) {
  console.error(
    "❌ BOT_TOKEN et/ou GUILD_ID manquants dans .env — copie .env.example en .env " +
      "et remplis les valeurs (voir README.md)."
  );
  process.exit(1);
}

const client = new Client({
  intents: [
    GatewayIntentBits.Guilds,
    // Intent PRIVILEGIE : doit etre active manuellement dans le portail
    // developpeur Discord (Bot -> Privileged Gateway Intents -> Server
    // Members Intent), sinon le bot plante au login. Necessaire pour
    // detecter l'arrivee de nouveaux membres (accueil + verification).
    GatewayIntentBits.GuildMembers,
  ],
});

client.commands = new Collection();
const commandsPath = path.join(__dirname, "commands");
for (const file of fs.readdirSync(commandsPath).filter((f) => f.endsWith(".js"))) {
  const command = require(path.join(commandsPath, file));
  if (command.data && command.execute) {
    client.commands.set(command.data.name, command);
  }
}

// Chaque event recoit deja ce dont il a besoin nativement (ready -> le
// client lui-meme ; guildMemberAdd -> le membre, qui expose .client ;
// interactionCreate -> l'interaction, qui expose .client) -> pas besoin
// de rajouter client manuellement.
const eventsPath = path.join(__dirname, "events");
for (const file of fs.readdirSync(eventsPath).filter((f) => f.endsWith(".js"))) {
  const event = require(path.join(eventsPath, file));
  if (event.once) {
    client.once(event.name, (...args) => event.execute(...args));
  } else {
    client.on(event.name, (...args) => event.execute(...args));
  }
}

client.login(process.env.BOT_TOKEN).catch((err) => {
  console.error("❌ Connexion à Discord échouée — vérifie BOT_TOKEN dans .env.", err);
  process.exit(1);
});
