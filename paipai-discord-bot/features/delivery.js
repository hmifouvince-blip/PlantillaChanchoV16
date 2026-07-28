// Livraison automatique d'une licence dans un ticket : le Staff confirme le
// paiement d'un clic, le bot genere la cle chez KeyAuth, la poste dans le
// ticket, l'envoie en MP, donne le role du produit et journalise la vente.
//
// POURQUOI UN CLIC ET PAS UNE DETECTION AUTOMATIQUE DU PAIEMENT : les moyens
// configures (PayPal Friends & Family, virements crypto) n'exposent AUCUNE
// notification fiable -- F&F n'a pas de webhook marchand, et un virement
// crypto demanderait de surveiller la chaine ET de faire correspondre un
// montant exact. Un "paiement recu" invente ferait livrer des cles gratuites.
// Le seul maillon humain restant est donc la confirmation ; tout le reste
// (generation, envoi, role, journal) est automatique.
const {
  ActionRowBuilder,
  ButtonBuilder,
  ButtonStyle,
  StringSelectMenuBuilder,
  MessageFlags,
} = require("discord.js");
const catalog = require("../utils/catalog");
const keyauth = require("../utils/keyauth");
const store = require("../utils/store");
const { licenseEmbed, durationLabel, logoAttachment } = require("../utils/embeds");
const { STAFF_ROLE_NAME } = require("../config/serverStructure");

const DELIVER_BUTTON_ID = "paipai_deliver";
const DELIVER_SELECT_ID = "paipai_deliver_pick";
// Separateur des valeurs du menu : ni "-" ni "_" (deja presents dans les cles
// de produit), et interdit dans une cle par catalog.slugify.
const VALUE_SEP = "::";
const MAX_SELECT_OPTIONS = 25;
const MAX_SALES_KEPT = 200;

function isStaff(interaction) {
  const role = interaction.guild?.roles.cache.find((r) => r.name === STAFF_ROLE_NAME);
  return Boolean(role && interaction.member?.roles.cache.has(role.id));
}

// Une offre n'est livrable que si elle porte une duree : sans elle, on ne
// saurait pas quelle licence generer.
function isDeliverable(offer) {
  return offer && offer.days !== undefined && offer.days !== null;
}

function deliverableOffers(product) {
  return (Array.isArray(product.offers) ? product.offers : []).filter(isDeliverable);
}

// Rangee de boutons ajoutee au message d'accueil du ticket. Le bouton reste
// visible meme sans offre livrable : le message d'erreur au clic explique quoi
// configurer, ce qu'un bouton absent ne ferait pas.
function deliverRow() {
  return new ActionRowBuilder().addComponents(
    new ButtonBuilder()
      .setCustomId(DELIVER_BUTTON_ID)
      .setLabel("Payment received → deliver")
      .setEmoji("🔑")
      .setStyle(ButtonStyle.Success)
  );
}

function optionsFor(products) {
  const options = [];
  for (const product of products) {
    for (const [index, offer] of (product.offers || []).entries()) {
      if (!isDeliverable(offer)) continue;
      if (options.length >= MAX_SELECT_OPTIONS) break;
      const duration = durationLabel(offer.days);
      const extra = duration && duration.toLowerCase() !== String(offer.label).toLowerCase() ? ` · ${duration}` : "";
      options.push({
        label: `${product.name} — ${offer.label}`.slice(0, 100),
        description: `${offer.price}${extra}`.slice(0, 100),
        value: `${product.key}${VALUE_SEP}${index}`,
      });
    }
  }
  return options;
}

// Clic sur « Payment received » -> menu des formules a livrer. Le ticket sait
// deja quel produit il concerne : on ne propose que ses formules, et on
// retombe sur le catalogue entier pour un ticket ouvert sans produit
// ("Other question").
async function handleDeliverButton(interaction) {
  if (!isStaff(interaction)) {
    return interaction.reply({ content: "⛔ Staff only.", flags: MessageFlags.Ephemeral });
  }

  if (!keyauth.isConfigured()) {
    return interaction.reply({
      content:
        "❌ Automatic delivery is not configured yet: add **KEYAUTH_SELLER_KEY** to the bot's environment " +
        "variables on the host, then restart the bot.",
      flags: MessageFlags.Ephemeral,
    });
  }

  const ticket = store.load().openTickets[interaction.channel.id];
  const product = ticket && ticket.productKey ? catalog.find(ticket.productKey) : null;
  const products = product ? [product] : catalog.list();
  const options = optionsFor(products);

  if (options.length === 0) {
    return interaction.reply({
      content:
        "❌ No deliverable offer yet. In PaiPai → Bot Manager → Products, give the product at least one " +
        "offer with a **duration** and a **KeyAuth level** (e.g. `1 month | 15 € | 30 | 1`).",
      flags: MessageFlags.Ephemeral,
    });
  }

  const menu = new StringSelectMenuBuilder()
    .setCustomId(DELIVER_SELECT_ID)
    .setPlaceholder("Which offer was paid for?")
    .addOptions(options);

  return interaction.reply({
    content: "Pick the offer to deliver — the key is generated and sent right away.",
    components: [new ActionRowBuilder().addComponents(menu)],
    flags: MessageFlags.Ephemeral,
  });
}

async function handleDeliverSelect(interaction) {
  if (!isStaff(interaction)) {
    return interaction.update({ content: "⛔ Staff only.", components: [] });
  }

  const [productKey, rawIndex] = String(interaction.values[0]).split(VALUE_SEP);
  const product = catalog.find(productKey);
  const offer = product ? (product.offers || [])[Number(rawIndex)] : null;

  if (!product || !isDeliverable(offer)) {
    return interaction.update({ content: "❌ That offer no longer exists — reopen the menu.", components: [] });
  }

  // La generation KeyAuth prend quelques secondes : sans cet accuse de
  // reception, Discord invalide l'interaction au bout de 3 s.
  await interaction.update({ content: "⏳ Generating the license…", components: [] });

  const result = await deliver(interaction.channel, {
    product,
    offer,
    deliveredBy: interaction.user,
  });

  await interaction.editReply({
    content: result.ok
      ? `✅ Delivered: **${product.name} — ${offer.label}**${result.dmSent ? " (also sent in DM)" : " (DM closed, the key is in the ticket)"}`
      : `❌ ${result.error}`,
  });
}

// Coeur de la livraison, reutilise par le bouton et par /deliver.
async function deliver(channel, { product, offer, deliveredBy, buyerId: forcedBuyerId }) {
  const data = store.load();
  const ticket = data.openTickets[channel.id];
  const buyerId = forcedBuyerId || (ticket ? ticket.userId : null);

  const license = await keyauth.createLicense({
    days: offer.days,
    level: offer.level || 1,
    // Note visible sur le dashboard KeyAuth : relie une cle a un acheteur et a
    // une formule sans avoir a fouiller les logs du bot.
    note: `${product.name} | ${offer.label} | discord:${buyerId || "?"}`,
  });

  if (!license.ok) return { ok: false, error: license.error };

  const duration = durationLabel(offer.days);
  const embed = licenseEmbed({
    product,
    offerLabel: offer.label,
    duration,
    key: license.key,
    buyerId,
  });

  await channel.send({ embeds: [embed], files: [logoAttachment()] });

  // MP « au mieux » : beaucoup de membres bloquent les MP du serveur. La cle
  // est deja dans le ticket, l'echec du MP ne doit donc jamais annuler la
  // vente ni relancer une generation.
  let dmSent = false;
  if (buyerId) {
    try {
      const user = await channel.client.users.fetch(buyerId);
      await user.send({ embeds: [embed] });
      dmSent = true;
    } catch {
      dmSent = false;
    }
  }

  // Role du produit : sert a reconnaitre les clients d'un produit donne.
  let roleGiven = false;
  if (buyerId) {
    try {
      const role = channel.guild.roles.cache.find((r) => r.name === product.name);
      const member = role ? await channel.guild.members.fetch(buyerId) : null;
      if (role && member) {
        await member.roles.add(role);
        roleGiven = true;
      }
    } catch {
      roleGiven = false;
    }
  }

  // Journal des ventes : la cle n'y est JAMAIS en clair (data/store.json est
  // lisible via l'API de controle) -- les 4 derniers caracteres suffisent a
  // rapprocher une ligne d'une reclamation client.
  store.update((d) => {
    if (!Array.isArray(d.sales)) d.sales = [];
    d.sales.push({
      at: Date.now(),
      productKey: product.key,
      productName: product.name,
      offerLabel: offer.label,
      price: offer.price,
      days: offer.days,
      level: offer.level || 1,
      buyerId: buyerId || null,
      channelId: channel.id,
      deliveredBy: deliveredBy ? deliveredBy.tag : "control",
      keyMasked: keyauth.maskKey(license.key),
      dmSent,
      roleGiven,
    });
    if (d.sales.length > MAX_SALES_KEPT) d.sales = d.sales.slice(-MAX_SALES_KEPT);
  });

  console.log(
    `[vente] ${product.name} — ${offer.label} (${offer.price}) livré par ` +
      `${deliveredBy ? deliveredBy.tag : "control"} à ${buyerId || "?"} — clé ${keyauth.maskKey(license.key)}.`
  );

  return { ok: true, dmSent, roleGiven };
}

module.exports = {
  DELIVER_BUTTON_ID,
  DELIVER_SELECT_ID,
  deliverRow,
  deliverableOffers,
  isDeliverable,
  handleDeliverButton,
  handleDeliverSelect,
  deliver,
};
