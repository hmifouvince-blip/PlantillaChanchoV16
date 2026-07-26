// /unlink -> revoque toutes les sessions PaiPai de celui qui tape la
// commande. Filet de securite si une machine liee est perdue ou partagee.
const { SlashCommandBuilder, MessageFlags } = require("discord.js");
const link = require("../features/link");

module.exports = {
  data: new SlashCommandBuilder()
    .setName("unlink")
    .setDescription("Révoque l'accès de toutes tes applications PaiPai liées.")
    .setDMPermission(false),

  async execute(interaction) {
    await interaction.deferReply({ flags: MessageFlags.Ephemeral });

    const removed = link.revokeAllForUser(interaction.user.id);
    await interaction.editReply(
      removed === 0
        ? "ℹ️ Aucune application liée à ton compte."
        : `✅ ${removed} accès révoqué(s). Ces PaiPai ne peuvent plus piloter le bot.`
    );
  },
};
