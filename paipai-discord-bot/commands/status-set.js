// /status-set -> updates a product's status on the #status page. The bot
// always EDITS the same message (never reposts) — its ID is tracked in
// data/store.json (store.statusMessage).
const { SlashCommandBuilder, PermissionFlagsBits, MessageFlags } = require("discord.js");
const { brandedEmbed, logoAttachment } = require("../utils/embeds");
const catalog = require("../utils/catalog");
const store = require("../utils/store");

const STATUS_LABELS = {
  online: "🟢 Online",
  maintenance: "🟡 Maintenance",
  offline: "🔴 Offline",
};

function buildStatusEmbed(productStatus) {
  const lines = catalog.list().map((p) => {
    const state = productStatus[p.key] || p.defaultStatus;
    return `**${p.name}** — ${STATUS_LABELS[state] || STATUS_LABELS.online}`;
  });

  return brandedEmbed({
    kicker: "📊 Service status",
    title: "PaiPai — Product status",
    description: lines.join("\n"),
    footer: "PaiPai • Last updated",
  });
}

// Posts the status message if it doesn't exist yet (or if the previous one
// was deleted); returns true if a NEW message was created.
async function ensureStatusMessage(channel) {
  const data = store.load();

  if (data.statusMessage && data.statusMessage.channelId === channel.id) {
    const existing = await channel.messages.fetch(data.statusMessage.messageId).catch(() => null);
    if (existing) return false;
  }

  const embed = buildStatusEmbed(data.productStatus);
  const message = await channel.send({ embeds: [embed], files: [logoAttachment()] });
  store.update((d) => {
    d.statusMessage = { channelId: channel.id, messageId: message.id };
  });
  return true;
}

// Reecrit la page #status a partir du catalogue COURANT (sans changer aucun
// etat) : indispensable apres une creation de produit depuis PaiPai, sinon le
// nouveau produit n'apparaitrait sur la page qu'au prochain changement d'etat.
// Renvoie null si tout s'est bien passe, sinon le message d'erreur a afficher.
async function refreshStatusMessage(client) {
  const data = store.load();
  if (!data.statusMessage) return "No tracked status page — run /setup-server first.";

  const channel = await client.channels.fetch(data.statusMessage.channelId).catch(() => null);
  const message = channel
    ? await channel.messages.fetch(data.statusMessage.messageId).catch(() => null)
    : null;
  if (!message) return "Status message not found — run /setup-server again.";

  await message.edit({ embeds: [buildStatusEmbed(data.productStatus)], files: [logoAttachment()] });
  return null;
}

module.exports = {
  buildStatusEmbed,
  ensureStatusMessage,
  refreshStatusMessage,
  logoAttachment,
  data: new SlashCommandBuilder()
    .setName("status-set")
    .setDescription("Updates a product's status on the #status page.")
    .setDefaultMemberPermissions(PermissionFlagsBits.ManageGuild)
    .addStringOption((opt) =>
      opt
        .setName("product")
        .setDescription("Which product")
        .setRequired(true)
        // Les choix d'une commande slash sont figes a l'ENREGISTREMENT (au
        // demarrage) : un produit cree depuis PaiPai n'apparait ici qu'apres
        // un redemarrage du bot. Le Bot Manager, lui, lit le catalogue en
        // direct -> il voit le nouveau produit immediatement.
        .addChoices(...catalog.list().map((p) => ({ name: p.name, value: p.key })))
    )
    .addStringOption((opt) =>
      opt
        .setName("state")
        .setDescription("New status")
        .setRequired(true)
        .addChoices(
          { name: "🟢 Online", value: "online" },
          { name: "🟡 Maintenance", value: "maintenance" },
          { name: "🔴 Offline", value: "offline" }
        )
    ),

  async execute(interaction) {
    await interaction.deferReply({ flags: MessageFlags.Ephemeral });
    const productKey = interaction.options.getString("product");
    const state = interaction.options.getString("state");

    const data = store.update((d) => {
      d.productStatus[productKey] = state;
    });

    if (!data.statusMessage) {
      await interaction.editReply(
        "❌ No status page found. Run /setup-server first, or use this command inside #status."
      );
      return;
    }

    const channel = await interaction.guild.channels.fetch(data.statusMessage.channelId).catch(() => null);
    const message = channel
      ? await channel.messages.fetch(data.statusMessage.messageId).catch(() => null)
      : null;

    if (!message) {
      await interaction.editReply("❌ Status message not found, run /setup-server again.");
      return;
    }

    await message.edit({ embeds: [buildStatusEmbed(data.productStatus)], files: [logoAttachment()] });
    await interaction.editReply("✅ Status updated.");
  },
};
