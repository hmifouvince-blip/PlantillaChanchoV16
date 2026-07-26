// /link -> genere un code a coller dans l'application PaiPai pour lier son
// compte Discord et piloter le bot depuis l'appli.
//
// Volontairement SANS setDefaultMemberPermissions : le droit d'usage vient du
// role PaiPai/PeiPei, pas des permissions Discord. Restreindre la commande a
// « Gerer le serveur » cacherait la commande a un membre qui a pourtant le
// bon role. Le controle reel se fait dans execute() puis, a nouveau, a
// l'echange du code cote API.
const { SlashCommandBuilder, MessageFlags } = require("discord.js");
const branding = require("../config/branding");
const link = require("../features/link");

module.exports = {
  data: new SlashCommandBuilder()
    .setName("link")
    .setDescription("Génère un code pour piloter le bot depuis l'application PaiPai.")
    .setDMPermission(false),

  async execute(interaction) {
    await interaction.deferReply({ flags: MessageFlags.Ephemeral });

    const member = await interaction.guild.members.fetch(interaction.user.id).catch(() => null);
    if (!member || !link.hasAdminRole(member)) {
      await interaction.editReply(
        `❌ Réservé aux porteurs du rôle **${branding.adminRoleNames.join("** ou **")}**.`
      );
      return;
    }

    const { code, expiresInMinutes } = link.createCode(interaction.user.id);

    await interaction.editReply(
      [
        "🔗 **Lier ton compte à PaiPai**",
        "",
        `Ton code : \`\`\`${code}\`\`\``,
        `⏱️ Valable **${expiresInMinutes} minutes**, utilisable **une seule fois**.`,
        "",
        "**Dans PaiPai** → *Bot Manager* → **Link Discord** → colle ce code.",
        "",
        "⚠️ Ne le partage à personne : il donne le contrôle du bot. " +
          "Si tu le regénères, l'ancien code est immédiatement invalidé.",
      ].join("\n")
    );
  },
};
