// Welcome: assigns the "Unverified" role on arrival (limited access, only
// #verification, until the member clicks the verify button).
const { ROLE_UNVERIFIED } = require("../features/verification");

module.exports = {
  name: "guildMemberAdd",
  async execute(member) {
    const role = member.guild.roles.cache.find((r) => r.name === ROLE_UNVERIFIED);
    if (!role) {
      console.warn(
        `Role "${ROLE_UNVERIFIED}" not found — run /setup-server once first.`
      );
      return;
    }
    await member.roles.add(role).catch((err) => {
      console.error("Could not assign the Unverified role:", err);
    });
  },
};
