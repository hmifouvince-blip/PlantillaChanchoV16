// /update-post -> publie une entree de changelog dans #updates.
const { SlashCommandBuilder, PermissionFlagsBits, ChannelType, MessageFlags } = require("discord.js");
const { updateEmbed, filesFor } = require("../utils/embeds");
const products = require("../config/products");

function findChannel(guild, name) {
  return guild.channels.cache.find((c) => c.type === ChannelType.GuildText && c.name === name) || null;
}

// Logique reutilisable : appelee par la commande slash ET par l'API de
// controle (control/server.js), pour que l'application PaiPai publie
// exactement le meme rendu sans dupliquer la mise en page cote C#.
async function postUpdate(guild, { title, changelog, productKey, version, note, ping }) {
  await guild.channels.fetch();
  const channel = findChannel(guild, "updates");
  if (!channel) return { ok: false, error: "Salon #updates introuvable — lance /setup-server." };

  const product = productKey ? products.find((p) => p.key === productKey) || null : null;
  if (productKey && !product) return { ok: false, error: `Produit inconnu : ${productKey}` };

  const embed = updateEmbed({ title, changelog, product, version, note });
  const message = await channel.send({
    content: ping ? "@everyone" : undefined,
    embeds: [embed],
    files: filesFor(product),
  });

  return { ok: true, channelId: channel.id, messageId: message.id };
}

module.exports = {
  postUpdate,
  data: new SlashCommandBuilder()
    .setName("update-post")
    .setDescription("Publie une mise à jour dans #updates.")
    .setDefaultMemberPermissions(PermissionFlagsBits.ManageGuild)
    .addStringOption((opt) =>
      opt.setName("title").setDescription("Titre (ex: PaiPai v3.9)").setRequired(true)
    )
    .addStringOption((opt) =>
      opt
        .setName("changelog")
        .setDescription("Une ligne par changement. Préfixe: + ajout, - retrait, ! correction.")
        .setRequired(true)
    )
    .addStringOption((opt) =>
      opt
        .setName("product")
        .setDescription("Produit concerné (optionnel)")
        .setRequired(false)
        .addChoices(...products.map((p) => ({ name: p.name, value: p.key })))
    )
    .addStringOption((opt) =>
      opt.setName("version").setDescription("Numéro de version (ex: v3.9)").setRequired(false)
    )
    .addStringOption((opt) =>
      opt.setName("note").setDescription("Avertissement affiché sous le changelog").setRequired(false)
    ),

  async execute(interaction) {
    await interaction.deferReply({ flags: MessageFlags.Ephemeral });

    const result = await postUpdate(interaction.guild, {
      title: interaction.options.getString("title"),
      changelog: interaction.options.getString("changelog"),
      productKey: interaction.options.getString("product"),
      version: interaction.options.getString("version"),
      note: interaction.options.getString("note"),
    });

    await interaction.editReply(
      result.ok ? `✅ Mise à jour publiée dans <#${result.channelId}>.` : `❌ ${result.error}`
    );
  },
};
