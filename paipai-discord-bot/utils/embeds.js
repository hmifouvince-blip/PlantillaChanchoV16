// Habillage visuel commun a /announce, /update-post et /status-set -> un
// seul endroit a modifier pour changer le rendu de TOUTES les publications
// du bot (logo sakura, couleur de marque, pied de page unifie).
const { EmbedBuilder, AttachmentBuilder } = require("discord.js");
const branding = require("../config/branding");

const LOGO_URI = `attachment://${branding.logoAttachmentName}`;

// A joindre (via `files:`) a CHAQUE message/edit qui utilise brandedEmbed,
// sinon `attachment://` ne resout rien et Discord affiche l'embed sans image.
function logoAttachment() {
  return new AttachmentBuilder(branding.logoPath, { name: branding.logoAttachmentName });
}

function brandedEmbed({ kicker, title, description, footer, color }) {
  return new EmbedBuilder()
    .setColor(color ?? branding.colors.main)
    .setAuthor({ name: kicker, iconURL: LOGO_URI })
    .setTitle(title)
    .setDescription(description)
    .setImage(LOGO_URI)
    .setFooter({ text: footer ?? branding.footerText, iconURL: LOGO_URI })
    .setTimestamp(Date.now());
}

module.exports = { logoAttachment, brandedEmbed, LOGO_URI };
