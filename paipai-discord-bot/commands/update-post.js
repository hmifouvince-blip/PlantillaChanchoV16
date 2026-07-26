// /update-post -> posts a changelog entry in #updates.
const { SlashCommandBuilder, PermissionFlagsBits, ChannelType, MessageFlags } = require("discord.js");
const { brandedEmbed, logoAttachment } = require("../utils/embeds");

module.exports = {
  data: new SlashCommandBuilder()
    .setName("update-post")
    .setDescription("Publishes an update in #updates.")
    .setDefaultMemberPermissions(PermissionFlagsBits.ManageGuild)
    .addStringOption((opt) =>
      opt.setName("title").setDescription("Update title (e.g. PaiPai v3.6)").setRequired(true)
    )
    .addStringOption((opt) =>
      opt.setName("description").setDescription("Update details").setRequired(true)
    ),

  async execute(interaction) {
    const title = interaction.options.getString("title");
    const description = interaction.options.getString("description");

    const channel = interaction.guild.channels.cache.find(
      (c) => c.type === ChannelType.GuildText && c.name === "updates"
    );
    if (!channel) {
      await interaction.reply({ content: "❌ #updates channel not found, run /setup-server.", flags: MessageFlags.Ephemeral });
      return;
    }

    const embed = brandedEmbed({
      kicker: "🆕 Mise à jour",
      title,
      description,
      footer: "PaiPai • Update",
    });

    await channel.send({ embeds: [embed], files: [logoAttachment()] });
    await interaction.reply({ content: `✅ Update posted in ${channel}.`, flags: MessageFlags.Ephemeral });
  },
};
