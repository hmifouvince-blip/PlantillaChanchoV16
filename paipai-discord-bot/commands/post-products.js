// /post-products -> refreshes the whole product line: one rich
// announcement card in EACH product's own channel, plus a short directory
// in #products linking to all of them. Reused by /setup-server for
// automatic initialization.
const {
  SlashCommandBuilder,
  PermissionFlagsBits,
  EmbedBuilder,
  AttachmentBuilder,
  ActionRowBuilder,
  ButtonBuilder,
  ButtonStyle,
  ChannelType,
  MessageFlags,
} = require("discord.js");
const branding = require("../config/branding");
const products = require("../config/products");
const store = require("../utils/store");
const { BUY_BUTTON_PREFIX } = require("../features/tickets");

const STATUS_LABELS = {
  online: "🟢 Available",
  maintenance: "🟡 Maintenance",
  offline: "🔴 Unavailable",
};

// Posts one product's rich announcement card into its OWN channel.
async function postProductCard(channel, product) {
  const statusMap = store.load().productStatus;
  const state = statusMap[product.key] || product.defaultStatus;

  const heroAttachment = new AttachmentBuilder(product.imagePath, {
    name: product.imageAttachmentName,
  });
  const logoAttachment = new AttachmentBuilder(branding.logoPath, {
    name: branding.logoAttachmentName,
  });

  const embed = new EmbedBuilder()
    .setColor(branding.colors.main)
    .setAuthor({ name: "PaiPai", iconURL: `attachment://${branding.logoAttachmentName}` })
    .setTitle(`${product.emoji} ${product.name}`)
    .setDescription(`*${product.tagline}*\n\n${product.description}`)
    .setImage(`attachment://${product.imageAttachmentName}`)
    .addFields(
      { name: "Status", value: STATUS_LABELS[state] || STATUS_LABELS.online, inline: true },
      { name: "How to get it", value: "Click **Buy Now** below 👇", inline: true }
    )
    .setFooter({
      text: `${branding.footerText} • Premium tools, premium experience`,
      iconURL: `attachment://${branding.logoAttachmentName}`,
    });

  const row = new ActionRowBuilder().addComponents(
    new ButtonBuilder()
      .setCustomId(`${BUY_BUTTON_PREFIX}${product.key}`)
      .setLabel(`Buy ${product.name}`)
      .setEmoji("🛒")
      .setStyle(ButtonStyle.Success)
  );

  await channel.send({ embeds: [embed], files: [heroAttachment, logoAttachment], components: [row] });
}

// Posts the short directory embed in #products, linking to each product's
// dedicated channel via a real channel mention.
async function postDirectory(directoryChannel, channelsByProductKey) {
  const logoAttachment = new AttachmentBuilder(branding.logoPath, {
    name: branding.logoAttachmentName,
  });

  const lines = products.map((p) => {
    const ch = channelsByProductKey[p.key];
    return `${p.emoji} **${p.name}** — ${ch ? `${ch}` : "_channel missing_"}`;
  });

  const embed = new EmbedBuilder()
    .setColor(branding.colors.main)
    .setTitle("🌸 PaiPai — Premium Tools")
    .setDescription(
      "Everything you need, built with care and refined for performance.\n" +
        "Head to a product's own channel below to learn more and buy it:\n\n" +
        lines.join("\n")
    )
    .setImage(`attachment://${branding.logoAttachmentName}`)
    .setFooter({ text: branding.footerText });

  await directoryChannel.send({ embeds: [embed], files: [logoAttachment] });
}

async function clearBotMessages(channel) {
  const messages = await channel.messages.fetch({ limit: 50 }).catch(() => null);
  if (!messages) return;
  const mine = messages.filter((m) => m.author.id === channel.client.user.id);
  for (const msg of mine.values()) await msg.delete().catch(() => {});
}

// Core reusable flow: finds each product's channel by name (config/products.js
// channelName), refreshes its card, then refreshes the #products directory.
// Used both by /post-products and /setup-server. Channels that don't exist
// yet are skipped (setup-server creates them beforehand).
async function refreshAllProductChannels(guild) {
  await guild.channels.fetch();
  const channelsByProductKey = {};

  for (const product of products) {
    const channel = guild.channels.cache.find(
      (c) => c.type === ChannelType.GuildText && c.name === product.channelName
    );
    if (!channel) continue;
    channelsByProductKey[product.key] = channel;
    await clearBotMessages(channel);
    await postProductCard(channel, product);
  }

  const directoryChannel = guild.channels.cache.find(
    (c) => c.type === ChannelType.GuildText && c.name === "products"
  );
  if (directoryChannel) {
    await clearBotMessages(directoryChannel);
    await postDirectory(directoryChannel, channelsByProductKey);
  }

  return channelsByProductKey;
}

module.exports = {
  postProductCard,
  postDirectory,
  refreshAllProductChannels,
  data: new SlashCommandBuilder()
    .setName("post-products")
    .setDescription("Refreshes every product's channel and the #products directory.")
    .setDefaultMemberPermissions(PermissionFlagsBits.ManageGuild),

  async execute(interaction) {
    await interaction.deferReply({ flags: MessageFlags.Ephemeral });
    await refreshAllProductChannels(interaction.guild);
    await interaction.editReply("✅ All product channels refreshed.");
  },
};
