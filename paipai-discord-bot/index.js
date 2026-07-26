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

// Demarre AVANT le login : le detournement de console.* doit etre en place
// pour que les lignes de demarrage (et un eventuel echec de connexion)
// apparaissent dans la console live de PaiPai, pas seulement dans le panel de
// l'hebergeur. Sans CONTROL_KEY, la fonction ne fait rien.
require("./control/server").start(client);

// Sans ces ecouteurs, une connexion a la passerelle qui echoue en boucle reste
// TOTALEMENT silencieuse : discord.js reessaie en interne et n'ecrit rien sur
// la console. Symptome vecu en hebergement : le process tourne, aucun message,
// et le bot reste hors ligne sans qu'on sache pourquoi.
client.on("error", (err) => console.error("[discord] erreur client :", err.message));
client.on("shardError", (err) => console.error("[discord] erreur passerelle :", err.message));
client.on("warn", (msg) => console.warn("[discord]", msg));
client.rest.on("rateLimited", (info) =>
  console.warn(`[discord] limite de debit sur ${info.route} — attente ${info.timeToReset} ms`)
);

// Filet de securite : si la passerelle n'est pas prete au bout de 25 s, on dit
// ou on en est plutot que de laisser l'utilisateur devant une console muette.
const readyWatchdog = setTimeout(() => {
  if (!client.isReady()) {
    console.error(
      "[discord] Toujours pas connecte apres 25 s. Causes probables : token invalide ou " +
        "regenere, intent « Server Members » non active dans le portail developpeur, ou " +
        "sortie reseau bloquee par l'hebergeur."
    );
  }
}, 25000);
client.once("clientReady", () => clearTimeout(readyWatchdog));
client.once("ready", () => clearTimeout(readyWatchdog));

const token = (process.env.BOT_TOKEN || "").trim();
// Un token colle depuis un panel web arrive souvent avec des guillemets ou des
// espaces : ils rendent le token invalide sans que le message d'erreur le dise.
console.log(`[discord] Connexion avec un token de ${token.length} caracteres...`);

client.login(token).catch((err) => {
  console.error("❌ Connexion à Discord échouée — vérifie BOT_TOKEN dans .env.", err.message);
  process.exit(1);
});
