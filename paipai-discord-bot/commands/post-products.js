// /post-products -> refreshes the whole product line: one rich
// announcement card in EACH product's own channel, plus a short directory
// in #products linking to all of them. Reused by /setup-server for
// automatic initialization.
const { SlashCommandBuilder, PermissionFlagsBits, ChannelType, MessageFlags } = require("discord.js");
const catalog = require("../utils/catalog");
const store = require("../utils/store");
const { BUY_BUTTON_PREFIX } = require("../features/tickets");
const { ensureProductChannel, ensureProductRole, ensureProductsCategory } = require("../features/productChannels");
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
  const products = catalog.list();

  const lines = products.map((p) => {
    const ch = channelsByProductKey[p.key];
    const state = statusMap[p.key] || p.defaultStatus;
    // La pastille de statut est le 1er "mot" du libelle.
    const dot = (STATUS_LABELS[state] || STATUS_LABELS.online).split(" ")[0];
    const target = ch ? `${ch}` : "_channel missing_";
    return `${dot} ${p.emoji} **${p.name}** — ${target}`;
  });

  const embed = brandedEmbed({
    kicker: "PaiPai",
    title: "🌸 Our products",
    description:
      "Every product has its own channel — full details, pricing and one-click purchase.\n\n" +
      lines.join("\n"),
    footer: `PaiPai • ${products.length} products`,
  });

  await directoryChannel.send({ embeds: [embed], files: [logoAttachment()] });
}

async function clearBotMessages(channel) {
  const messages = await channel.messages.fetch({ limit: 50 }).catch(() => null);
  if (!messages) return;
  const mine = messages.filter((m) => m.author.id === channel.client.user.id);
  for (const msg of mine.values()) await msg.delete().catch(() => {});
}

function findTextChannel(guild, name) {
  return guild.channels.cache.find((c) => c.type === ChannelType.GuildText && c.name === name) || null;
}

// Salon de chaque produit du catalogue, par cle -> sert a reconstruire
// l'annuaire #products avec de vraies mentions de salon.
function channelsByProductKey(guild) {
  const map = {};
  for (const product of catalog.list()) {
    const channel = findTextChannel(guild, product.channelName);
    if (channel) map[product.key] = channel;
  }
  return map;
}

async function refreshDirectory(guild) {
  const directory = findTextChannel(guild, "products");
  if (!directory) return false;
  await clearBotMessages(directory);
  await postDirectory(directory, channelsByProductKey(guild));
  return true;
}

// Core reusable flow: finds each product's channel by name (catalog
// channelName), refreshes its card, then refreshes the #products directory.
// Used both by /post-products and /setup-server. Channels that don't exist
// yet are skipped (setup-server creates them beforehand).
async function refreshAllProductChannels(guild) {
  await guild.channels.fetch();
  const found = {};

  for (const product of catalog.list()) {
    const channel = findTextChannel(guild, product.channelName);
    if (!channel) continue;
    found[product.key] = channel;
    await clearBotMessages(channel);
    await postProductCard(channel, product);
  }

  const directoryChannel = findTextChannel(guild, "products");
  if (directoryChannel) {
    await clearBotMessages(directoryChannel);
    await postDirectory(directoryChannel, found);
  }

  return found;
}

// Publication d'UN SEUL produit apres creation/edition depuis PaiPai : on ne
// republie pas toute la gamme (chaque republication supprime puis reposte une
// carte -> inutile de secouer les 4 autres salons pour une virgule).
// A la CREATION seulement, le salon et le role du produit sont crees ; a
// l'edition on n'y touche pas (renommer un role suivrait le nom du produit et
// laisserait un doublon derriere lui).
async function publishProduct(guild, product, { created = false } = {}) {
  const log = [];
  await guild.channels.fetch();

  const category = await ensureProductsCategory(guild, log);
  if (created) await ensureProductRole(guild, product, log);
  const channel = await ensureProductChannel(guild, product, log, null, category);

  await clearBotMessages(channel);
  await postProductCard(channel, product);
  log.push(`✅ Card posted in **#${product.channelName}**`);

  if (await refreshDirectory(guild)) log.push("✅ #products directory refreshed");
  else log.push("⚠️ #products directory not found — run /setup-server");

  return { channelId: channel.id, log };
}

module.exports = {
  postProductCard,
  postDirectory,
  refreshAllProductChannels,
  refreshDirectory,
  publishProduct,
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
