// Declarative target structure: roles, categories, channels, permissions.
// commands/setup-server.js reads this file and creates ONLY what's
// missing (checked by name) -> never destructive towards an existing
// server/role/channel.
//
// Permissions are expressed by role NAME (resolved to an ID at runtime,
// since roles don't exist yet before the first /setup-server run).

const ROLES = [
  {
    name: "Staff",
    color: 0xf472b6,
    hoist: true,
    mentionable: true,
    permissions: ["ManageChannels", "ManageRoles", "ManageMessages", "KickMembers", "ManageGuild"],
  },
  {
    name: "Member",
    color: 0x50c878,
    hoist: false,
    mentionable: false,
    permissions: [],
  },
  {
    name: "Unverified",
    color: 0x99a1af,
    hoist: false,
    mentionable: false,
    permissions: [],
  },
];

// "everyone" refers to the server's @everyone role (special-cased in
// setup-server.js). ViewChannel=false + SendMessages=false = channel
// hidden for that role.
const CATEGORIES = [
  {
    name: "📋 INFO",
    channels: [
      {
        name: "rules",
        topic: "PaiPai server rules — read before anything else.",
        overwrites: [
          { role: "everyone", deny: ["SendMessages"] },
          { role: "Unverified", allow: ["ViewChannel"], deny: ["SendMessages"] },
        ],
      },
      {
        name: "announcements",
        topic: "Official PaiPai announcements.",
        overwrites: [
          { role: "everyone", deny: ["SendMessages"] },
          { role: "Unverified", allow: ["ViewChannel"], deny: ["SendMessages"] },
          { role: "Staff", allow: ["SendMessages"] },
        ],
      },
      {
        name: "verification",
        topic: "Click the button to verify your account and unlock the server.",
        overwrites: [
          { role: "everyone", deny: ["ViewChannel"] },
          { role: "Unverified", allow: ["ViewChannel"], deny: ["SendMessages"] },
          { role: "Staff", allow: ["ViewChannel"] },
        ],
      },
    ],
  },
  {
    name: "🌸 PAIPAI",
    channels: [
      {
        // Directory/index channel: short pointer to each product's own
        // dedicated channel (see PRODUCTS_CATEGORY_NAME below) rather than
        // holding the full showcase itself.
        name: "products",
        topic: "Find every PaiPai product's dedicated channel.",
        overwrites: [
          { role: "everyone", deny: ["ViewChannel", "SendMessages"] },
          { role: "Unverified", deny: ["ViewChannel"] },
          { role: "Member", allow: ["ViewChannel"], deny: ["SendMessages"] },
        ],
      },
      {
        name: "status",
        topic: "Live status of every product.",
        overwrites: [
          { role: "everyone", deny: ["ViewChannel", "SendMessages"] },
          { role: "Unverified", deny: ["ViewChannel"] },
          { role: "Member", allow: ["ViewChannel"], deny: ["SendMessages"] },
        ],
      },
      {
        name: "updates",
        topic: "PaiPai changelog.",
        overwrites: [
          { role: "everyone", deny: ["ViewChannel", "SendMessages"] },
          { role: "Unverified", deny: ["ViewChannel"] },
          { role: "Member", allow: ["ViewChannel"], deny: ["SendMessages"] },
          { role: "Staff", allow: ["SendMessages"] },
        ],
      },
    ],
  },
  {
    name: "🎫 SUPPORT",
    channels: [
      {
        name: "tickets",
        topic: "Open a ticket to buy a product or ask a question.",
        overwrites: [
          { role: "everyone", deny: ["ViewChannel", "SendMessages"] },
          { role: "Unverified", deny: ["ViewChannel"] },
          { role: "Member", allow: ["ViewChannel"], deny: ["SendMessages"] },
        ],
      },
    ],
  },
];

// Dedicated category where individual ticket channels are created on the fly
// (it has no channel of its own, just a container).
const TICKETS_CATEGORY_NAME = "🎫 Tickets";

// Dedicated category holding ONE channel per product (config/products.js
// drives which channels get created here — see setup-server.js
// ensureProductChannels()). Kept separate from "🌸 PAIPAI" so brand-level
// info (status/updates) stays distinct from individual product pages.
const PRODUCTS_CATEGORY_NAME = "🛍️ PRODUCTS";

module.exports = { ROLES, CATEGORIES, TICKETS_CATEGORY_NAME, PRODUCTS_CATEGORY_NAME };
