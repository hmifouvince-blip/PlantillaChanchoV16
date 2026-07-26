// /announce -> publie une annonce officielle dans #announcements.
const { SlashCommandBuilder, PermissionFlagsBits, ChannelType, MessageFlags } = require("discord.js");
const { announceEmbed, filesFor } = require("../utils/embeds");

// Logique reutilisable : appelee par la commande slash ET par l'API de
// controle (control/server.js), pour que l'application PaiPai publie
// exactement le meme rendu sans dupliquer la mise en page cote C#.
async function postAnnounce(guild, { title, message, ping }) {
  await guild.channels.fetch();
  const channel = guild.channels.cache.find(
    (c) => c.type === ChannelType.GuildText && c.name === "announcements"
  );
  if (!channel) return { ok: false, error: "#announcements channel not found — run /setup-server." };

  const sent = await channel.send({
    content: ping ? "@everyone" : undefined,
    embeds: [announceEmbed({ title, message })],
    files: filesFor(null),
  });

  return { ok: true, channelId: channel.id, messageId: sent.id };
}

module.exports = {
  postAnnounce,
  data: new SlashCommandBuilder()
    .setName("announce")
    .setDescription("Publishes an announcement in #announcements.")
    .setDefaultMemberPermissions(PermissionFlagsBits.ManageGuild)
    .addStringOption((opt) => opt.setName("title").setDescription("Announcement title").setRequired(true))
    .addStringOption((opt) => opt.setName("message").setDescription("Content").setRequired(true))
    .addBooleanOption((opt) =>
      opt.setName("ping").setDescription("Mention @everyone?").setRequired(false)
    ),

  async execute(interaction) {
    await interaction.deferReply({ flags: MessageFlags.Ephemeral });

    const result = await postAnnounce(interaction.guild, {
      title: interaction.options.getString("title"),
      message: interaction.options.getString("message"),
      ping: interaction.options.getBoolean("ping") ?? false,
    });

    await interaction.editReply(
      result.ok ? `✅ Announcement posted in <#${result.channelId}>.` : `❌ ${result.error}`
    );
  },
};
