// /post-products -> refreshes the whole product line: one rich
// announcement card in EACH product's own channel, plus a short directory
// in #products linking to all of them. Reused by /setup-server for
// automatic initialization.
const { SlashCommandBuilder, PermissionFlagsBits, ChannelType, MessageFlags } = require("discord.js");
const products = require("../config/products");
const store = require("../utils/store");
const { BUY_BUTTON_PREFIX } = require("../features/tickets");
const {
  productEmbed,
  productComponents,
  brandedEmbed,
  logoAttachment,
  filesFor,
  STATUS_LABELS,
} = require("../utils/embeds");

// Posts one product's rich announcement card into its OWN channel.
async function postProductCard(channel, product) {
  const statusMap = store.load().productStatus;
  const state = statusMap[product.key] || product.defaultStatus;

  await channel.send({
    embeds: [productEmbed(product, state)],
    files: filesFor(product),
    components: productComponents(product, `${BUY_BUTTON_PREFIX}${product.key}`),
  });
}

// Posts the short directory embed in #products, linking to each product's
// dedicated channel via a real channel mention.
async function postDirectory(directoryChannel, channelsByProductKey) {
  const statusMap = store.load().productStatus;

  const lines = products.map((p) => {
    const ch = channelsByProductKey[p.key];
    const state = statusMap[p.key] || p.defaultStatus;
    // La pastille de statut est le 1er "mot" du libelle.
    const dot = (STATUS_LABELS[state] || STATUS_LABELS.online).split(" ")[0];
    const target = ch ? `${ch}` : "_salon manquant_";
    return `${dot} ${p.emoji} **${p.name}** — ${target}`;
  });

  const embed = brandedEmbed({
    kicker: "PaiPai",
    title: "🌸 Nos produits",
    description:
      "Chaque produit a son propre salon : présentation complète, tarifs et achat en un clic.\n\n" +
      lines.join("\n"),
    footer: `PaiPai • ${products.length} produits`,
  });

  await directoryChannel.send({ embeds: [embed], files: [logoAttachment()] });
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
