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

// ---- Moyens de paiement (affiches UNIQUEMENT dans un ticket prive) ----
// L'adresse est dans un bloc de code : Discord y ajoute un bouton « copier »
// et n'y applique aucune mise en forme -- un underscore au milieu d'une
// adresse ne peut donc pas la transformer en italique et la corrompre.
const PAYMENT_ICONS = { paypal: "🅿️", crypto: "🪙", other: "💳" };

function paymentEmbed({ methods, intro, warning, productName }) {
  const embed = new EmbedBuilder()
    .setColor(branding.colors.main)
    .setAuthor({ name: "PaiPai — Payment", iconURL: LOGO_URI })
    .setTitle(productName ? `💳 Payment — ${productName}` : "💳 Payment")
    .setDescription([intro, warning].filter(Boolean).join("\n\n"))
    .setThumbnail(LOGO_URI)
    .setFooter(footerFor("Payment"))
    .setTimestamp(Date.now());

  for (const method of methods) {
    const lines = [`\`\`\`\n${method.address}\n\`\`\``];
    if (method.network) lines.push(`Network: **${method.network}**`);
    if (method.note) lines.push(`_${method.note}_`);
    embed.addFields({
      name: `${PAYMENT_ICONS[method.kind] || PAYMENT_ICONS.other} ${method.label}`,
      value: lines.join("\n"),
      inline: false,
    });
  }

  return embed;
}

// ---- Livraison d'une licence ----
// La cle est dans un bloc de code : bouton « copier » cote Discord, et aucune
// mise en forme ne peut la deformer. Envoye dans le ticket ET en message
// prive -- si le ticket est ferme avant que l'acheteur revienne, il garde sa
// cle dans ses MP.
function licenseEmbed({ product, offerLabel, duration, key, buyerId }) {
  const lines = [];
  if (buyerId) lines.push(`<@${buyerId}>, thanks for your purchase!`);
  // « 1 month · 1 month » : quand le libelle de la formule EST deja la duree,
  // la repeter donne l'air d'un bug.
  const extra = duration && duration.toLowerCase() !== String(offerLabel).toLowerCase() ? ` · ${duration}` : "";
  lines.push(`**${product ? product.name : "PaiPai"}** — ${offerLabel}${extra}`);

  return new EmbedBuilder()
    .setColor(branding.colors.success)
    .setAuthor({ name: "PaiPai — Delivery", iconURL: LOGO_URI })
    .setTitle("🔑 Your license key")
    .setDescription(lines.join("\n\n"))
    .setThumbnail(LOGO_URI)
    .addFields(
      { name: "License", value: `\`\`\`\n${key}\n\`\`\``, inline: false },
      {
        name: "How to use it",
        value:
          "1. Open **PaiPai** and sign in (or create an account with this key)\n" +
          "2. Already signed in? Click **Add license** on the welcome banner and paste it\n" +
          "3. The product unlocks right away — no restart needed",
        inline: false,
      }
    )
    .setFooter(footerFor("Keep this key private"))
    .setTimestamp(Date.now());
}

// ---- Fiche produit ----
// Duree lisible : KeyAuth compte en jours, l'acheteur pense en mois.
function durationLabel(days) {
  if (days === undefined || days === null) return null;
  const n = Number(days);
  if (!Number.isFinite(n) || n <= 0 || n >= 3650) return "lifetime";
  if (n % 365 === 0) return `${n / 365} year${n / 365 > 1 ? "s" : ""}`;
  if (n % 30 === 0) return `${n / 30} month${n / 30 > 1 ? "s" : ""}`;
  return `${n} day${n > 1 ? "s" : ""}`;
}

// Formules vendues (libelle, prix, duree). Remplace `prices` des qu'un produit
// en a : c'est la meme information, en plus complet.
function offerBlock(offers) {
  if (!Array.isArray(offers) || offers.length === 0) return null;
  const width = Math.max(...offers.map((o) => o.label.length));
  return offers
    .map((o) => {
      const duration = durationLabel(o.days);
      const extra = duration && duration.toLowerCase() !== String(o.label).toLowerCase() ? ` · ${duration}` : "";
      return `\`${o.label.padEnd(width)}\`  **${o.price}**${extra}`;
    })
    .join("\n");
}

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

  const prices = offerBlock(product.offers) || priceBlock(product.prices);
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
  paymentEmbed,
  licenseEmbed,
  durationLabel,
  offerBlock,
  productEmbed,
  productComponents,
};
