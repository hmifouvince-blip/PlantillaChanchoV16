// /announce -> posts an official announcement in #announcements.
const { SlashCommandBuilder, PermissionFlagsBits, EmbedBuilder, ChannelType, MessageFlags } = require("discord.js");
const branding = require("../config/branding");

module.exports = {
  data: new SlashCommandBuilder()
    .setName("announce")
    .setDescription("Publishes an announcement in #announcements.")
    .setDefaultMemberPermissions(PermissionFlagsBits.ManageGuild)
    .addStringOption((opt) => opt.setName("title").setDescription("Announcement title").setRequired(true))
    .addStringOption((opt) => opt.setName("message").setDescription("Announcement content").setRequired(true))
    .addBooleanOption((opt) =>
      opt.setName("ping").setDescription("Mention @everyone?").setRequired(false)
    ),

  async execute(interaction) {
    const title = interaction.options.getString("title");
    const message = interaction.options.getString("message");
    const ping = interaction.options.getBoolean("ping") ?? false;

    const channel = interaction.guild.channels.cache.find(
      (c) => c.type === ChannelType.GuildText && c.name === "announcements"
    );
    if (!channel) {
      await interaction.reply({ content: "❌ #announcements channel not found, run /setup-server.", flags: MessageFlags.Ephemeral });
      return;
    }

    const embed = new EmbedBuilder()
      .setColor(branding.colors.main)
      .setTitle(`📢 ${title}`)
      .setDescription(message)
      .setFooter({ text: branding.footerText })
      .setTimestamp(Date.now());

    await channel.send({ content: ping ? "@everyone" : undefined, embeds: [embed] });
    await interaction.reply({ content: `✅ Announcement posted in ${channel}.`, flags: MessageFlags.Ephemeral });
  },
};
