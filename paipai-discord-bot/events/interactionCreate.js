// Central router: slash commands + buttons + select menus.
const { MessageFlags } = require("discord.js");
const verification = require("../features/verification");
const tickets = require("../features/tickets");

module.exports = {
  name: "interactionCreate",
  async execute(interaction) {
    try {
      if (interaction.isChatInputCommand()) {
        const command = interaction.client.commands.get(interaction.commandName);
        if (!command) return;
        await command.execute(interaction);
        return;
      }

      if (interaction.isButton()) {
        // Per-product "Buy Now" buttons carry a dynamic customId
        // ("paipai_buy_<productKey>") -> matched by prefix instead of an
        // exact case, since one exists per product in config/products.js.
        if (interaction.customId.startsWith(tickets.BUY_BUTTON_PREFIX)) {
          const productKey = interaction.customId.slice(tickets.BUY_BUTTON_PREFIX.length);
          return tickets.handleBuyProductButton(interaction, productKey);
        }

        switch (interaction.customId) {
          case verification.VERIFY_BUTTON_ID:
            return verification.handleVerifyButton(interaction);
          case tickets.OPEN_BUTTON_ID:
            return tickets.handleOpenTicketButton(interaction);
          case tickets.CLAIM_BUTTON_ID:
            return tickets.handleClaimButton(interaction);
          case tickets.CLOSE_BUTTON_ID:
            return tickets.handleCloseButton(interaction);
          case tickets.CLOSE_CONFIRM_ID:
            return tickets.handleCloseConfirm(interaction);
          case tickets.CLOSE_CANCEL_ID:
            return tickets.handleCloseCancel(interaction);
        }
        return;
      }

      if (interaction.isStringSelectMenu()) {
        if (interaction.customId === tickets.REASON_SELECT_ID) {
          return tickets.handleReasonSelect(interaction);
        }
        return;
      }
    } catch (err) {
      console.error("Interaction error:", err);
      const payload = { content: "❌ Something went wrong.", flags: MessageFlags.Ephemeral };
      if (interaction.replied || interaction.deferred) {
        await interaction.followUp(payload).catch(() => {});
      } else {
        await interaction.reply(payload).catch(() => {});
      }
    }
  },
};
