// /update-post -> posts a changelog entry in #updates.
const { SlashCommandBuilder, PermissionFlagsBits, EmbedBuilder, ChannelType, MessageFlags } = require("discord.js");
const branding = require("../config/branding");

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

    const embed = new EmbedBuilder()
      .setColor(branding.colors.main)
      .setTitle(`🆕 ${title}`)
      .setDescription(description)
      .setFooter({ text: branding.footerText })
      .setTimestamp(Date.now());

    await channel.send({ embeds: [embed] });
    await interaction.reply({ content: `✅ Update posted in ${channel}.`, flags: MessageFlags.Ephemeral });
  },
};
