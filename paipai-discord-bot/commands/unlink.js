// /unlink -> revoque toutes les sessions PaiPai de celui qui tape la
// commande. Filet de securite si une machine liee est perdue ou partagee.
const { SlashCommandBuilder, MessageFlags } = require("discord.js");
const link = require("../features/link");

module.exports = {
  data: new SlashCommandBuilder()
    .setName("unlink")
    .setDescription("Revokes access for all your linked PaiPai apps.")
    .setDMPermission(false),

  async execute(interaction) {
    await interaction.deferReply({ flags: MessageFlags.Ephemeral });

    const removed = link.revokeAllForUser(interaction.user.id);
    await interaction.editReply(
      removed === 0
        ? "ℹ️ No PaiPai app is linked to your account."
        : `✅ ${removed} access revoked. Those PaiPai apps can no longer control the bot.`
    );
  },
};
