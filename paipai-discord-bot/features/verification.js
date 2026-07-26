// "Basic" anti-multi-account verification: a button to click + minimum
// Discord account age (MIN_ACCOUNT_AGE_DAYS, 7 days by default). No
// silent block: an account that's too new gets a clear explanation
// instead of a plain refusal.
const {
  EmbedBuilder,
  ActionRowBuilder,
  ButtonBuilder,
  ButtonStyle,
  MessageFlags,
} = require("discord.js");
const branding = require("../config/branding");

const VERIFY_BUTTON_ID = "paipai_verify";
const ROLE_UNVERIFIED = "Unverified";
const ROLE_MEMBER = "Member";

function minAccountAgeDays() {
  const n = Number(process.env.MIN_ACCOUNT_AGE_DAYS);
  return Number.isFinite(n) && n > 0 ? n : 7;
}

// Posts (or reposts) the verification panel in #verification.
async function postVerificationPanel(channel) {
  const embed = new EmbedBuilder()
    .setColor(branding.colors.main)
    .setTitle("Welcome to PaiPai 🌸")
    .setDescription(
      "Before you can access the rest of the server, click the button below " +
        "to confirm you're a real person.\n\nThis check protects the " +
        "community from mass-created accounts."
    )
    .setFooter({ text: branding.footerText });

  const row = new ActionRowBuilder().addComponents(
    new ButtonBuilder()
      .setCustomId(VERIFY_BUTTON_ID)
      .setLabel("✅ I'm human, verify me")
      .setStyle(ButtonStyle.Success)
  );

  await channel.send({ embeds: [embed], components: [row] });
}

// Called by interactionCreate when the verification button is clicked.
async function handleVerifyButton(interaction) {
  const member = interaction.member;
  const guild = interaction.guild;

  const accountAgeDays =
    (Date.now() - member.user.createdTimestamp) / (1000 * 60 * 60 * 24);
  const minDays = minAccountAgeDays();

  if (accountAgeDays < minDays) {
    await interaction.reply({
      content:
        `❌ Your Discord account is too recent (created ` +
        `${Math.floor(accountAgeDays)} day(s) ago, minimum required: ${minDays} days).\n` +
        `If this is a mistake, open a ticket to contact Staff.`,
      flags: MessageFlags.Ephemeral,
    });
    return;
  }

  const unverifiedRole = guild.roles.cache.find((r) => r.name === ROLE_UNVERIFIED);
  const memberRole = guild.roles.cache.find((r) => r.name === ROLE_MEMBER);

  if (memberRole) await member.roles.add(memberRole).catch(() => {});
  if (unverifiedRole) await member.roles.remove(unverifiedRole).catch(() => {});

  await interaction.reply({
    content: "✅ Verification successful, welcome to the PaiPai server!",
    flags: MessageFlags.Ephemeral,
  });
}

module.exports = {
  VERIFY_BUTTON_ID,
  ROLE_UNVERIFIED,
  ROLE_MEMBER,
  postVerificationPanel,
  handleVerifyButton,
};
