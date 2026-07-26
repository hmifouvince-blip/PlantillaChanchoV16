// Au demarrage : enregistre les slash commands sur LE serveur configure
// (commandes de guilde = disponibles instantanement, contrairement aux
// commandes globales qui mettent jusqu'a 1h a se propager -> le bon choix
// pour un bot mono-serveur).
const { REST, Routes } = require("discord.js");

module.exports = {
  // discord.js v14.16+ renomme cet event "clientReady" (l'ancien nom "ready"
  // reste fonctionnel mais affiche un avertissement de depreciation avant
  // suppression en v15).
  name: "clientReady",
  once: true,
  async execute(client) {
    const commandsData = [...client.commands.values()].map((c) => c.data.toJSON());
    const rest = new REST().setToken(process.env.BOT_TOKEN);

    try {
      await rest.put(
        Routes.applicationGuildCommands(client.user.id, process.env.GUILD_ID),
        { body: commandsData }
      );
      console.log(`Slash commands registered (${commandsData.length}).`);
    } catch (err) {
      console.error("Failed to register slash commands:", err);
    }

    console.log(`Logged in as ${client.user.tag}`);
  },
};
