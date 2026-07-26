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
    .setDescription("Generates a code to control the bot from the PaiPai app.")
    .setDMPermission(false),

  async execute(interaction) {
    await interaction.deferReply({ flags: MessageFlags.Ephemeral });

    const member = await interaction.guild.members.fetch(interaction.user.id).catch(() => null);
    if (!member || !link.hasAdminRole(member)) {
      await interaction.editReply(
        `❌ Reserved for members with the **${branding.adminRoleNames.join("** or **")}** role.`
      );
      return;
    }

    const { code, expiresInMinutes } = link.createCode(interaction.user.id);

    await interaction.editReply(
      [
        "🔗 **Link your account to PaiPai**",
        "",
        `Your code: \`\`\`${code}\`\`\``,
        `⏱️ Valid for **${expiresInMinutes} minutes**, **single use**.`,
        "",
        "**In PaiPai** → *Bot Manager* → **Link Discord** → paste this code.",
        "",
        "⚠️ Do not share it — it grants control of the bot. " +
          "Generating a new one immediately invalidates this code.",
      ].join("\n")
    );
  },
};
