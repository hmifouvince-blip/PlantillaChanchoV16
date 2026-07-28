// Ticket system (payments are handled MANUALLY by Staff inside the
// channel -> no payment integration, as requested). Flow: "Open a ticket"
// button -> product dropdown -> private channel created -> Claim / Close
// buttons inside the channel.
const {
  EmbedBuilder,
  ActionRowBuilder,
  ButtonBuilder,
  ButtonStyle,
  StringSelectMenuBuilder,
  PermissionFlagsBits,
  ChannelType,
  MessageFlags,
} = require("discord.js");
const branding = require("../config/branding");
const catalog = require("../utils/catalog");
const payments = require("../utils/payments");
const { paymentEmbed, logoAttachment } = require("../utils/embeds");
const store = require("../utils/store");
const delivery = require("./delivery");
const { TICKETS_CATEGORY_NAME, STAFF_ROLE_NAME } = require("../config/serverStructure");

const OPEN_BUTTON_ID = "paipai_open_ticket";
const REASON_SELECT_ID = "paipai_ticket_reason";
const CLAIM_BUTTON_ID = "paipai_ticket_claim";
const CLOSE_BUTTON_ID = "paipai_ticket_close";
const CLOSE_CONFIRM_ID = "paipai_ticket_close_confirm";
const CLOSE_CANCEL_ID = "paipai_ticket_close_cancel";
// Prefix for the per-product "Buy Now" buttons on each showcase card
// (e.g. "paipai_buy_valorant") -> lets interactionCreate.js route any
// button matching this prefix without listing every product key.
const BUY_BUTTON_PREFIX = "paipai_buy_";

// Publie la carte des moyens de paiement DANS LE SALON DONNE. Reutilisee par
// l'ouverture de ticket et par /payment-post (quand le Staff veut la reposter
// apres avoir change une adresse en cours de discussion).
// Retourne null si aucun moyen n'est active : mieux vaut ne rien afficher
// qu'une carte vide qui donnerait l'impression d'un bug.
async function postPaymentCard(channel, { productName } = {}) {
  const methods = payments.enabledList();
  if (methods.length === 0) return null;

  const embed = paymentEmbed({
    methods,
    intro: payments.intro(),
    warning: payments.SCAM_WARNING,
    productName,
  });

  const message = await channel.send({ embeds: [embed], files: [logoAttachment()] });
  // Epinglee : dans un ticket qui s'allonge, l'acheteur retrouve les adresses
  // sans faire defiler. L'echec d'epinglage (permission manquante) ne doit pas
  // faire echouer l'ouverture du ticket.
  await message.pin().catch(() => {});
  return message;
}

// Posts the "Open a ticket" panel in #tickets.
async function postTicketPanel(channel) {
  const embed = new EmbedBuilder()
    .setColor(branding.colors.main)
    .setTitle("🎫 PaiPai Support & Purchases")
    .setDescription(
      "Want to buy a product or need help? Click the button below to open " +
        "a private ticket with Staff."
    )
    .setFooter({ text: branding.footerText });

  const row = new ActionRowBuilder().addComponents(
    new ButtonBuilder()
      .setCustomId(OPEN_BUTTON_ID)
      .setLabel("Open a ticket")
      .setEmoji("🎫")
      .setStyle(ButtonStyle.Primary)
  );

  await channel.send({ embeds: [embed], components: [row] });
}

// Click on "Open a ticket" -> shows a dropdown (product / other).
async function handleOpenTicketButton(interaction) {
  // Liste construite a CHAQUE ouverture : un produit cree depuis PaiPai
  // apparait aussitot dans le menu, sans redemarrer le bot.
  const options = catalog.list().map((p) => ({ label: p.name, value: p.key }));
  options.push({ label: "Other question", value: "other" });

  const menu = new StringSelectMenuBuilder()
    .setCustomId(REASON_SELECT_ID)
    .setPlaceholder("Choose a product or a reason")
    .addOptions(options);

  await interaction.reply({
    content: "Which product (or reason) is this ticket for?",
    components: [new ActionRowBuilder().addComponents(menu)],
    flags: MessageFlags.Ephemeral,
  });
}

// Shared logic: actually creates the private ticket channel + posts the
// instructions embed inside it. Used both by the dropdown flow
// (handleReasonSelect) and the direct "Buy Now" button on each product
// card (handleBuyProductButton) -> returns the created channel.
async function createTicketChannel(interaction, reasonKey) {
  const guild = interaction.guild;
  const product = catalog.find(reasonKey);
  const reasonLabel = product ? product.name : "Other question";

  const staffRole = guild.roles.cache.find((r) => r.name === STAFF_ROLE_NAME);
  let category = guild.channels.cache.find(
    (c) => c.type === ChannelType.GuildCategory && c.name === TICKETS_CATEGORY_NAME
  );
  if (!category) {
    category = await guild.channels.create({
      name: TICKETS_CATEGORY_NAME,
      type: ChannelType.GuildCategory,
    });
  }

  const data = store.update((d) => {
    d.ticketCounter += 1;
  });
  const ticketNumber = String(data.ticketCounter).padStart(4, "0");
  const safeUsername = interaction.user.username
    .toLowerCase()
    .replace(/[^a-z0-9]/g, "")
    .slice(0, 20) || "user";

  const overwrites = [
    { id: guild.roles.everyone.id, deny: [PermissionFlagsBits.ViewChannel] },
    {
      id: interaction.user.id,
      allow: [PermissionFlagsBits.ViewChannel, PermissionFlagsBits.SendMessages, PermissionFlagsBits.ReadMessageHistory],
    },
  ];
  if (staffRole) {
    overwrites.push({
      id: staffRole.id,
      allow: [PermissionFlagsBits.ViewChannel, PermissionFlagsBits.SendMessages, PermissionFlagsBits.ReadMessageHistory],
    });
  }

  const ticketChannel = await guild.channels.create({
    name: `ticket-${safeUsername}-${ticketNumber}`,
    type: ChannelType.GuildText,
    parent: category.id,
    permissionOverwrites: overwrites,
  });

  store.update((d) => {
    d.openTickets[ticketChannel.id] = {
      userId: interaction.user.id,
      // La CLE du produit en plus du libelle : c'est elle qui permet a la
      // livraison de retrouver les formules du produit sans deviner d'apres
      // un nom qui peut etre renomme entre-temps.
      productKey: product ? product.key : null,
      reasonLabel,
      claimedBy: null,
    };
  });

  const embed = new EmbedBuilder()
    .setColor(branding.colors.main)
    .setTitle(`Ticket — ${reasonLabel}`)
    .setDescription(
      `Welcome <@${interaction.user.id}>!\n\n` +
        "A Staff member will reply with payment instructions shortly. " +
        "Thanks for your patience.\n\n" +
        "⚠️ Payment only happens through the instructions given by Staff in this channel."
    )
    .setFooter({ text: branding.footerText });

  const controlRow = new ActionRowBuilder().addComponents(
    new ButtonBuilder()
      .setCustomId(CLAIM_BUTTON_ID)
      .setLabel("Claim")
      .setEmoji("🙋")
      .setStyle(ButtonStyle.Secondary),
    new ButtonBuilder()
      .setCustomId(CLOSE_BUTTON_ID)
      .setLabel("Close")
      .setEmoji("🔒")
      .setStyle(ButtonStyle.Danger)
  );

  await ticketChannel.send({
    content: staffRole ? `<@&${staffRole.id}>` : undefined,
    embeds: [embed],
    // 2e rangee : la livraison. Le bouton est visible par tous mais refuse
    // toute personne hors Staff au clic -- Discord ne sait pas masquer un
    // composant par role.
    components: [controlRow, delivery.deliverRow()],
  });

  // Les moyens de paiement arrivent tout de suite apres, dans le meme salon
  // prive : l'acheteur a tout sous les yeux sans attendre qu'un Staff soit
  // disponible. Le paiement reste confirme a la main par le Staff.
  await postPaymentCard(ticketChannel, { productName: product ? product.name : null }).catch((err) =>
    console.error("[tickets] Carte de paiement non publiée :", err.message)
  );

  return ticketChannel;
}

// Dropdown selection -> creates the ticket, then EDITS the ephemeral
// dropdown message to confirm (interaction was already replied to when
// the dropdown was shown).
async function handleReasonSelect(interaction) {
  const reasonKey = interaction.values[0];
  const ticketChannel = await createTicketChannel(interaction, reasonKey);

  await interaction.update({
    content: `✅ Your ticket has been created: ${ticketChannel}`,
    components: [],
  });
}

// Direct "Buy Now" button on a product card -> creates the ticket right
// away (skips the dropdown, one click from showcase to ticket). This
// interaction has NOT been replied to yet, so we REPLY instead of update.
async function handleBuyProductButton(interaction, productKey) {
  const ticketChannel = await createTicketChannel(interaction, productKey);

  await interaction.reply({
    content: `✅ Your ticket has been created: ${ticketChannel}`,
    flags: MessageFlags.Ephemeral,
  });
}

// "Claim" button (Staff only).
async function handleClaimButton(interaction) {
  const guild = interaction.guild;
  const staffRole = guild.roles.cache.find((r) => r.name === STAFF_ROLE_NAME);
  const isStaff = staffRole && interaction.member.roles.cache.has(staffRole.id);

  if (!isStaff) {
    await interaction.reply({ content: "⛔ Staff only.", flags: MessageFlags.Ephemeral });
    return;
  }

  store.update((d) => {
    const t = d.openTickets[interaction.channel.id];
    if (t) t.claimedBy = interaction.user.id;
  });

  await interaction.reply({
    content: `🙋 Ticket claimed by <@${interaction.user.id}>.`,
  });
}

// "Close" button -> asks for confirmation (avoids accidentally closing a
// channel tied to an ongoing purchase).
async function handleCloseButton(interaction) {
  const row = new ActionRowBuilder().addComponents(
    new ButtonBuilder()
      .setCustomId(CLOSE_CONFIRM_ID)
      .setLabel("Confirm close")
      .setStyle(ButtonStyle.Danger),
    new ButtonBuilder()
      .setCustomId(CLOSE_CANCEL_ID)
      .setLabel("Cancel")
      .setStyle(ButtonStyle.Secondary)
  );

  await interaction.reply({
    content: "⚠️ Are you sure you want to close this ticket?",
    components: [row],
    flags: MessageFlags.Ephemeral,
  });
}

async function handleCloseConfirm(interaction) {
  await interaction.update({ content: "🔒 Closing this ticket in 5 seconds...", components: [] });
  const channel = interaction.channel;
  setTimeout(async () => {
    store.update((d) => {
      delete d.openTickets[channel.id];
    });
    await channel.delete().catch(() => {});
  }, 5000);
}

async function handleCloseCancel(interaction) {
  await interaction.update({ content: "Close cancelled.", components: [] });
}

module.exports = {
  OPEN_BUTTON_ID,
  REASON_SELECT_ID,
  CLAIM_BUTTON_ID,
  CLOSE_BUTTON_ID,
  CLOSE_CONFIRM_ID,
  CLOSE_CANCEL_ID,
  BUY_BUTTON_PREFIX,
  postPaymentCard,
  postTicketPanel,
  handleOpenTicketButton,
  handleReasonSelect,
  handleBuyProductButton,
  handleClaimButton,
  handleCloseButton,
  handleCloseConfirm,
  handleCloseCancel,
};
