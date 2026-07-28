// /payment-post -> (re)publie la carte des moyens de paiement dans le salon
// courant. Sert au Staff quand une adresse a change en cours de discussion,
// ou quand le ticket a ete ouvert avant que les moyens de paiement soient
// configures depuis PaiPai.
//
// Volontairement SANS restriction de salon cote code, mais reserve a « Gerer
// le serveur » : c'est au Staff de ne pas la poster dans un salon public --
// une adresse publique se fait recopier par les arnaqueurs.
const { SlashCommandBuilder, PermissionFlagsBits, MessageFlags } = require("discord.js");
const { postPaymentCard } = require("../features/tickets");
const store = require("../utils/store");
const catalog = require("../utils/catalog");

module.exports = {
  data: new SlashCommandBuilder()
    .setName("payment-post")
    .setDescription("Posts the payment methods in this channel (use it inside a ticket).")
    .setDefaultMemberPermissions(PermissionFlagsBits.ManageGuild),

  async execute(interaction) {
    await interaction.deferReply({ flags: MessageFlags.Ephemeral });

    // Si la commande est lancee dans un ticket, on retrouve le produit
    // concerne pour titrer la carte comme a l'ouverture.
    const ticket = store.load().openTickets[interaction.channel.id];
    const product = ticket && ticket.productKey ? catalog.find(ticket.productKey) : null;
    const productName = product ? product.name : ticket ? ticket.reasonLabel : null;

    const message = await postPaymentCard(interaction.channel, { productName });
    await interaction.editReply(
      message
        ? "✅ Payment methods posted."
        : "❌ No payment method is enabled yet — add one from PaiPai (Bot Manager → Payments)."
    );
  },
};
