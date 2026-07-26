// /status-set -> updates a product's status on the #status page. The bot
// always EDITS the same message (never reposts) — its ID is tracked in
// data/store.json (store.statusMessage).
const { SlashCommandBuilder, PermissionFlagsBits, MessageFlags } = require("discord.js");
const { brandedEmbed, logoAttachment } = require("../utils/embeds");
const products = require("../config/products");
const store = require("../utils/store");

const STATUS_LABELS = {
  online: "🟢 Online",
  maintenance: "🟡 Maintenance",
  offline: "🔴 Offline",
};

function buildStatusEmbed(productStatus) {
  const lines = products.map((p) => {
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

module.exports = {
  buildStatusEmbed,
  ensureStatusMessage,
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
        .addChoices(...products.map((p) => ({ name: p.name, value: p.key })))
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
