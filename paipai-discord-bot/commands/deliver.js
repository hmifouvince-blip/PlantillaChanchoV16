// /deliver -> meme menu que le bouton « Payment received » du ticket.
// Utile quand le message d'accueil du ticket a ete supprime, ou pour livrer
// depuis un ticket ouvert avant l'arrivee de la livraison automatique.
//
// Aucune option de commande : la liste des formules change des que tu la
// modifies depuis PaiPai, alors que les choix d'une commande slash sont figes
// a l'enregistrement (donc au demarrage du bot).
const { SlashCommandBuilder, PermissionFlagsBits } = require("discord.js");
const delivery = require("../features/delivery");

module.exports = {
  data: new SlashCommandBuilder()
    .setName("deliver")
    .setDescription("Delivers a license in this ticket (Staff only).")
    .setDefaultMemberPermissions(PermissionFlagsBits.ManageMessages),

  async execute(interaction) {
    return delivery.handleDeliverButton(interaction);
  },
};
