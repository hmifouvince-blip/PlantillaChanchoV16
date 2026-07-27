// Creation/mise a jour du salon et du role d'UN produit. Extrait de
// commands/setup-server.js pour etre reutilisable par commands/post-products.js
// (publication d'un produit cree depuis PaiPai) SANS dependance circulaire
// entre ces deux fichiers.
//
// Toujours ADDITIF : on cherche par nom avant de creer, on ne supprime ni ne
// renomme jamais un salon ou un role existant.
const { ChannelType, PermissionFlagsBits } = require("discord.js");
const { PRODUCTS_CATEGORY_NAME } = require("../config/serverStructure");
const branding = require("../config/branding");

// Visibilite d'un salon produit : reserve aux membres verifies, en lecture
// seule (les achats passent par les tickets).
const PRODUCT_OVERWRITES = [
  { role: "everyone", deny: ["ViewChannel", "SendMessages"] },
  { role: "Unverified", deny: ["ViewChannel"] },
  { role: "Member", allow: ["ViewChannel"], deny: ["SendMessages"] },
];

function resolvePermValue(name) {
  const v = PermissionFlagsBits[name];
  if (v === undefined) throw new Error(`Unknown permission: ${name}`);
  return v;
}

function rolesByName(guild) {
  const map = {};
  for (const role of guild.roles.cache.values()) map[role.name] = role;
  return map;
}

// Une regle visant un role INEXISTANT est ignoree plutot que transmise avec
// un id `undefined` : Discord rejetterait alors la creation du salon entiere.
function buildOverwrites(guild, roles, overwriteDefs) {
  return overwriteDefs
    .map((ow) => {
      const id = ow.role === "everyone" ? guild.roles.everyone.id : roles[ow.role]?.id;
      if (!id) return null;
      return {
        id,
        allow: (ow.allow || []).map(resolvePermValue),
        deny: (ow.deny || []).map(resolvePermValue),
      };
    })
    .filter(Boolean);
}

async function ensureProductsCategory(guild, log = []) {
  let category = guild.channels.cache.find(
    (c) => c.type === ChannelType.GuildCategory && c.name === PRODUCTS_CATEGORY_NAME
  );
  if (!category) {
    category = await guild.channels.create({ name: PRODUCTS_CATEGORY_NAME, type: ChannelType.GuildCategory });
    log.push(`✅ Category created: **${PRODUCTS_CATEGORY_NAME}**`);
  } else {
    log.push(`↪️ Category already present: **${PRODUCTS_CATEGORY_NAME}**`);
  }
  return category;
}

function topicFor(product) {
  return product.tagline ? `${product.name} — ${product.tagline}` : product.name;
}

// Cree le salon du produit s'il manque, sinon reajuste seulement son sujet
// (le nom du salon n'est JAMAIS renomme : des liens publies pointent dessus).
async function ensureProductChannel(guild, product, log = [], roles = null, category = null) {
  const resolvedRoles = roles || rolesByName(guild);
  const parent = category || (await ensureProductsCategory(guild, log));

  let channel = guild.channels.cache.find(
    (c) => c.type === ChannelType.GuildText && c.name === product.channelName && c.parentId === parent.id
  );

  if (!channel) {
    channel = await guild.channels.create({
      name: product.channelName,
      type: ChannelType.GuildText,
      parent: parent.id,
      topic: topicFor(product),
      permissionOverwrites: buildOverwrites(guild, resolvedRoles, PRODUCT_OVERWRITES),
    });
    log.push(`✅ Channel created: **#${product.channelName}**`);
    return channel;
  }

  log.push(`↪️ Channel already present: **#${product.channelName}**`);
  const wanted = topicFor(product);
  if (channel.topic !== wanted) await channel.setTopic(wanted).catch(() => {});
  return channel;
}

// Role de reconnaissance porte le nom du produit (utile pour reperer les
// acheteurs) : aucune permission particuliere.
async function ensureProductRole(guild, product, log = []) {
  let role = guild.roles.cache.find((r) => r.name === product.name);
  if (!role) {
    role = await guild.roles.create({
      name: product.name,
      colors: { primaryColor: branding.colors.main },
      hoist: false,
      mentionable: false,
      permissions: [],
    });
    log.push(`✅ Role created: **${product.name}**`);
  } else {
    log.push(`↪️ Role already present: **${product.name}**`);
  }
  return role;
}

module.exports = {
  PRODUCT_OVERWRITES,
  resolvePermValue,
  rolesByName,
  buildOverwrites,
  ensureProductsCategory,
  ensureProductChannel,
  ensureProductRole,
};
