// /setup-server -> builds (or completes) the whole server structure from
// config/serverStructure.js. ALWAYS additive: checks for existence by
// NAME before creating anything, never deletes or renames an existing
// role/channel. Safe to re-run at any time.
const {
  SlashCommandBuilder,
  PermissionFlagsBits,
  ChannelType,
  EmbedBuilder,
  MessageFlags,
} = require("discord.js");
const { ROLES, CATEGORIES, TICKETS_CATEGORY_NAME } = require("../config/serverStructure");
const branding = require("../config/branding");
const catalog = require("../utils/catalog");
const verification = require("../features/verification");
const tickets = require("../features/tickets");
const {
  resolvePermValue,
  buildOverwrites,
  ensureProductsCategory,
  ensureProductChannel,
  ensureProductRole,
} = require("../features/productChannels");
const { postProductCard, postDirectory } = require("./post-products");
const { ensureStatusMessage } = require("./status-set");

async function ensureRoles(guild, log) {
  const created = {};
  for (const roleDef of ROLES) {
    let role = guild.roles.cache.find((r) => r.name === roleDef.name);
    if (!role) {
      role = await guild.roles.create({
        name: roleDef.name,
        // discord.js v14.16+: "color" (singular) is deprecated in favor of
        // "colors.primaryColor" (upcoming multi-color role support).
        colors: { primaryColor: roleDef.color },
        hoist: roleDef.hoist,
        mentionable: roleDef.mentionable,
        permissions: roleDef.permissions.map(resolvePermValue),
      });
      log.push(`✅ Role created: **${roleDef.name}**`);
    } else {
      log.push(`↪️ Role already present: **${roleDef.name}**`);
    }
    created[roleDef.name] = role;
  }
  return created;
}

async function ensureCategoriesAndChannels(guild, roles, log) {
  const channelsByName = {};
  for (const catDef of CATEGORIES) {
    let category = guild.channels.cache.find(
      (c) => c.type === ChannelType.GuildCategory && c.name === catDef.name
    );
    if (!category) {
      category = await guild.channels.create({ name: catDef.name, type: ChannelType.GuildCategory });
      log.push(`✅ Category created: **${catDef.name}**`);
    } else {
      log.push(`↪️ Category already present: **${catDef.name}**`);
    }

    for (const chanDef of catDef.channels) {
      let channel = guild.channels.cache.find(
        (c) => c.type === ChannelType.GuildText && c.name === chanDef.name && c.parentId === category.id
      );
      if (!channel) {
        channel = await guild.channels.create({
          name: chanDef.name,
          type: ChannelType.GuildText,
          parent: category.id,
          topic: chanDef.topic,
          permissionOverwrites: buildOverwrites(guild, roles, chanDef.overwrites),
        });
        log.push(`✅ Channel created: **#${chanDef.name}**`);
      } else {
        log.push(`↪️ Channel already present: **#${chanDef.name}**`);
      }
      channelsByName[chanDef.name] = channel;
    }
  }

  // Container category for individually-created ticket channels.
  let ticketsCategory = guild.channels.cache.find(
    (c) => c.type === ChannelType.GuildCategory && c.name === TICKETS_CATEGORY_NAME
  );
  if (!ticketsCategory) {
    await guild.channels.create({ name: TICKETS_CATEGORY_NAME, type: ChannelType.GuildCategory });
    log.push(`✅ Category created: **${TICKETS_CATEGORY_NAME}**`);
  } else {
    log.push(`↪️ Category already present: **${TICKETS_CATEGORY_NAME}**`);
  }

  return channelsByName;
}

// Un salon par produit du CATALOGUE (produits integres + ceux crees depuis
// PaiPai), dans la categorie "🛍️ PRODUCTS". Retourne { productKey: channel }.
async function ensureProductChannels(guild, roles, log) {
  const category = await ensureProductsCategory(guild, log);

  const channelsByProductKey = {};
  for (const product of catalog.list()) {
    channelsByProductKey[product.key] = await ensureProductChannel(guild, product, log, roles, category);
  }
  return channelsByProductKey;
}

// Un role par produit (nomme comme le produit, ex. "Woofer") : sert a reperer
// les acheteurs. Aucune permission particuliere.
async function ensureProductRoles(guild, log) {
  for (const product of catalog.list()) await ensureProductRole(guild, product, log);
}

// A channel is considered "empty" (never initialized by the bot) if it has
// no message posted by the bot itself -> avoids reposting the same panel
// every time /setup-server is re-run.
async function hasBotMessage(channel) {
  const messages = await channel.messages.fetch({ limit: 20 }).catch(() => null);
  if (!messages) return false;
  return messages.some((m) => m.author.id === channel.client.user.id);
}

const DEFAULT_RULES = [
  "1️⃣ Respect every member, no insults or harassment.",
  "2️⃣ No spam, advertising, or unsolicited links.",
  "3️⃣ Purchases happen ONLY through the ticket system (#tickets).",
  "4️⃣ Never share your banking information outside of a ticket.",
  "5️⃣ Staff has the final word in case of a dispute.",
].join("\n");

// Reusable logic: called by the slash command (through a real Discord
// interaction) AND by one-off scripts (direct execution, no interaction
// needed). Returns the action log.
async function runSetup(guild) {
  const log = [];

  const roles = await ensureRoles(guild, log);
  await ensureProductRoles(guild, log);
  const channels = await ensureCategoriesAndChannels(guild, roles, log);

  if (channels["rules"] && !(await hasBotMessage(channels["rules"]))) {
    const embed = new EmbedBuilder()
      .setColor(branding.colors.main)
      .setTitle("📋 PaiPai Rules")
      .setDescription(DEFAULT_RULES)
      .setFooter({ text: branding.footerText });
    await channels["rules"].send({ embeds: [embed] });
    log.push("✅ Rules posted (feel free to edit them in #rules).");
  }

  if (channels["verification"] && !(await hasBotMessage(channels["verification"]))) {
    await verification.postVerificationPanel(channels["verification"]);
    log.push("✅ Verification panel posted.");
  }

  if (channels["tickets"] && !(await hasBotMessage(channels["tickets"]))) {
    await tickets.postTicketPanel(channels["tickets"]);
    log.push("✅ Ticket panel posted.");
  }

  const productChannels = await ensureProductChannels(guild, roles, log);
  for (const product of catalog.list()) {
    const productChannel = productChannels[product.key];
    if (productChannel && !(await hasBotMessage(productChannel))) {
      await postProductCard(productChannel, product);
      log.push(`✅ ${product.name} announcement posted in #${product.channelName}.`);
    }
  }

  if (channels["products"] && !(await hasBotMessage(channels["products"]))) {
    await postDirectory(channels["products"], productChannels);
    log.push("✅ Product directory posted in #products.");
  }

  if (channels["status"]) {
    const wasCreated = await ensureStatusMessage(channels["status"]);
    log.push(wasCreated ? "✅ Status page initialized." : "↪️ Status page already in place.");
  }

  return log;
}

module.exports = {
  runSetup,
  data: new SlashCommandBuilder()
    .setName("setup-server")
    .setDescription("Creates or completes the PaiPai server structure (never deletes anything).")
    .setDefaultMemberPermissions(PermissionFlagsBits.Administrator),

  async execute(interaction) {
    await interaction.deferReply({ flags: MessageFlags.Ephemeral });
    const log = await runSetup(interaction.guild);

    const summary = new EmbedBuilder()
      .setColor(branding.colors.main)
      .setTitle("Server setup")
      .setDescription(log.join("\n"))
      .setFooter({ text: branding.footerText });

    await interaction.editReply({ embeds: [summary] });
  },
};
