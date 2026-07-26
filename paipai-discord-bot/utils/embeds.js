// Habillage visuel commun a TOUTES les publications du bot (annonces, mises
// a jour, fiches produit, page de statut) -> un seul endroit a modifier pour
// changer le rendu partout, que la publication vienne d'une commande slash
// ou de l'application PaiPai via l'API de controle.
//
// Les textes affiches sont en ANGLAIS (langue du serveur Discord et de
// l'application) ; seuls les commentaires restent en francais.
const { EmbedBuilder, AttachmentBuilder, ActionRowBuilder, ButtonBuilder, ButtonStyle } = require("discord.js");
const branding = require("../config/branding");
const { renderChangelog } = require("./changelog");

const LOGO_URI = `attachment://${branding.logoAttachmentName}`;

const STATUS_LABELS = {
  online: "🟢 Online",
  maintenance: "🟡 Maintenance",
  offline: "🔴 Offline",
};

// A joindre (via `files:`) a CHAQUE message/edit qui utilise ces embeds,
// sinon `attachment://` ne resout rien et Discord affiche l'embed sans image.
function logoAttachment() {
  return new AttachmentBuilder(branding.logoPath, { name: branding.logoAttachmentName });
}

function productAttachment(product) {
  return new AttachmentBuilder(product.imagePath, { name: product.imageAttachmentName });
}

// Le logo suffit quand le produit reutilise deja logo.png comme visuel :
// joindre deux fois le meme fichier ferait rejeter le message par Discord.
function filesFor(product) {
  const files = [logoAttachment()];
  if (product && product.imageAttachmentName !== branding.logoAttachmentName) {
    files.push(productAttachment(product));
  }
  return files;
}

function imageUriFor(product) {
  return product ? `attachment://${product.imageAttachmentName}` : LOGO_URI;
}

function footerFor(suffix) {
  const site = branding.website ? ` • ${branding.website}` : "";
  return { text: `${branding.footerText} • ${suffix}${site}`, iconURL: LOGO_URI };
}

// Embed generique, conserve pour la page de statut et tout ce qui n'a pas
// de mise en page dediee.
function brandedEmbed({ kicker, title, description, footer, color }) {
  return new EmbedBuilder()
    .setColor(color ?? branding.colors.main)
    .setAuthor({ name: kicker, iconURL: LOGO_URI })
    .setTitle(title)
    .setDescription(description)
    .setImage(LOGO_URI)
    .setFooter(footer ? { text: footer, iconURL: LOGO_URI } : footerFor(branding.tagline))
    .setTimestamp(Date.now());
}

// ---- Annonce officielle ----
function announceEmbed({ title, message }) {
  return new EmbedBuilder()
    .setColor(branding.colors.main)
    .setAuthor({ name: "PaiPai — Official announcement", iconURL: LOGO_URI })
    .setTitle(`📢 ${title}`)
    .setDescription(message)
    .setThumbnail(LOGO_URI)
    .setFooter(footerFor("Announcement"))
    .setTimestamp(Date.now());
}

// ---- Mise a jour / changelog ----
// `changelog` est du texte libre : chaque ligne prefixee par +, -, ! ou *
// ressort coloree (voir utils/changelog.js). Sans prefixe -> ligne neutre.
// `productChannelId` rend le champ Product cliquable : le lecteur saute
// directement au salon du produit concerne au lieu de devoir le chercher.
function updateEmbed({ title, changelog, product, version, note, productChannelId }) {
  const block = renderChangelog(changelog);
  const parts = [];

  if (product) parts.push(`> *${product.tagline}*`);
  parts.push("**Changes**");
  // Un changelog vide ne doit pas produire un bloc de code vide (moche et
  // deroutant) : on retombe sur le texte brut tel qu'il a ete saisi.
  parts.push(block ?? `_${String(changelog || "").trim() || "No details provided."}_`);
  if (note) parts.push(`⚠️ ${note}`);

  const embed = new EmbedBuilder()
    .setColor(branding.colors.main)
    .setAuthor({ name: "PaiPai — Update", iconURL: LOGO_URI })
    .setTitle(version ? `🆕 ${title} • ${version}` : `🆕 ${title}`)
    .setDescription(parts.join("\n\n"))
    .setThumbnail(imageUriFor(product))
    .setFooter(footerFor(product ? product.name : branding.tagline))
    .setTimestamp(Date.now());

  const fields = [];
  if (product) {
    fields.push({
      name: "📦 Product",
      value: productChannelId ? `<#${productChannelId}>` : product.name,
      inline: true,
    });
  }
  if (version) fields.push({ name: "🏷️ Version", value: `\`${version}\``, inline: true });
  if (fields.length > 0) embed.addFields(fields);

  return embed;
}

// ---- Fiche produit ----
function priceBlock(prices) {
  if (!Array.isArray(prices) || prices.length === 0) return null;
  // Largeur fixe sur le libelle -> les prix s'alignent verticalement en
  // police a chasse fixe, ce qu'un simple "label — prix" ne fait pas.
  const width = Math.max(...prices.map((p) => p.label.length));
  return prices.map((p) => `\`${p.label.padEnd(width)}\`  **${p.price}**`).join("\n");
}

function productEmbed(product, state) {
  const status = STATUS_LABELS[state] || STATUS_LABELS.online;

  const description = [`> *${product.tagline}*`, product.description];
  if (product.note) description.push(`⚠️ **Good to know**\n${product.note}`);

  const embed = new EmbedBuilder()
    .setColor(branding.colors.main)
    .setAuthor({ name: "PaiPai", iconURL: LOGO_URI })
    .setTitle(`${product.emoji} ${product.name}`)
    .setDescription(description.join("\n\n"))
    .setThumbnail(LOGO_URI)
    .setImage(imageUriFor(product))
    .setFooter(footerFor(branding.tagline))
    .setTimestamp(Date.now());

  const prices = priceBlock(product.prices);
  if (prices) embed.addFields({ name: "💎 Pricing", value: prices, inline: false });

  embed.addFields({ name: "📊 Availability", value: status, inline: true });
  if (product.delivery) {
    embed.addFields({ name: "⚡ Delivery", value: product.delivery, inline: true });
  }

  for (const item of product.faq || []) {
    embed.addFields({ name: `❔ ${item.q}`, value: item.a, inline: false });
  }

  return embed;
}

// Ligne de boutons d'une fiche produit. `buyCustomId` vient de
// features/tickets.js -> le bouton ouvre directement un ticket pre-rempli
// avec le bon produit.
function productComponents(product, buyCustomId) {
  const row = new ActionRowBuilder().addComponents(
    new ButtonBuilder()
      .setCustomId(buyCustomId)
      .setLabel(`Buy ${product.name}`)
      .setEmoji("🛒")
      .setStyle(ButtonStyle.Success)
  );

  // Un ButtonBuilder de type Link exige une URL valide : sans site configure
  // (branding.website reste null tant que l'utilisateur ne l'a pas rempli),
  // ajouter le bouton ferait echouer TOUT l'envoi du message.
  const url = product.website || branding.website;
  if (url) {
    row.addComponents(
      new ButtonBuilder().setLabel("Website").setEmoji("🌐").setStyle(ButtonStyle.Link).setURL(url)
    );
  }

  return [row];
}

module.exports = {
  LOGO_URI,
  STATUS_LABELS,
  logoAttachment,
  productAttachment,
  filesFor,
  brandedEmbed,
  announceEmbed,
  updateEmbed,
  productEmbed,
  productComponents,
};
