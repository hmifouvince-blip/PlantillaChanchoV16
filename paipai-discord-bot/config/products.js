// Product catalog -> single source used by the per-product channels, the
// ticket dropdown, and the status page. Descriptions reused as-is from
// Products/ProductManager.cs (the C# app) to stay consistent with what's
// already shown inside PaiPai itself.
//
// Champs OPTIONNELS de mise en forme (utils/embeds.js les omet proprement
// s'ils sont absents -> rien ne casse tant qu'ils ne sont pas remplis) :
//   prices   : [{ label: "1 mois", price: "29 €" }, ...]  -> section « Tarifs »
//   delivery : "Instantanée dans le ticket"                -> champ « Livraison »
//   website  : "https://..."                               -> bouton « Site web »
//   note     : phrase d'avertissement affichee en gras
//   faq      : [{ q: "...", a: "..." }]                    -> un champ par question
//
// ⚠️ Les tarifs sont volontairement VIDES : ils s'affichent publiquement sur
// le serveur Discord, donc ils doivent venir de toi, pas d'une valeur
// d'exemple qui serait fausse. Remplis `prices` puis relance /post-products.
const path = require("node:path");

const ASSETS = path.join(__dirname, "..", "assets");

// FAQ commune a tous les produits : le processus d'achat est identique
// partout (paiement gere a la main dans le ticket, cf. README).
const COMMON_FAQ = [
  {
    q: "How do I buy?",
    a: "Click **Buy** below — a private ticket opens with the Staff, who will guide you through to delivery.",
  },
];

module.exports = [
  {
    key: "woofer",
    name: "Woofer",
    channelName: "woofer",
    emoji: "🛡️",
    tagline: "Stay undetected. Stay in the game.",
    description:
      "The most reliable HWID spoofer to bypass hardware bans and stay undetected.\n\n" +
      "• Windows 10 & 11 support\n" +
      "• All Intel / AMD processors\n" +
      "• Bypasses EAC, BattlEye, Ricochet & Vanguard\n" +
      "• Spoofs SSD/NVMe, CPU, GPU, motherboard, TPM & MAC\n" +
      "• TPM bypass ready for Valorant\n" +
      "• One-click, fully automatic",
    imagePath: path.join(ASSETS, "spoofer.png"),
    imageAttachmentName: "woofer.png",
    defaultStatus: "online",
    prices: [],
    delivery: null,
    website: null,
    faq: COMMON_FAQ,
  },
  {
    key: "valorant",
    name: "PaiPai Val + Emulator",
    channelName: "valorant",
    emoji: "🎯",
    tagline: "Every round, under control.",
    description:
      "Take full control of every round. Precise aim assistance, lightning reflexes " +
      "and smart visual tools — all in a clean, undetected package built to help you " +
      "climb the ranks.",
    imagePath: path.join(ASSETS, "valorant.jpg"),
    imageAttachmentName: "valorant.jpg",
    defaultStatus: "online",
    prices: [],
    delivery: null,
    website: null,
    faq: COMMON_FAQ,
  },
  {
    key: "roblox",
    name: "Roblox",
    channelName: "roblox",
    emoji: "🧱",
    tagline: "Built for creators and explorers.",
    description:
      "A powerful Roblox toolkit for creators and explorers. Run advanced scripts, " +
      "unlock visual enhancements and enjoy a smooth, stable and undetected experience " +
      "across all your favorite games.",
    imagePath: path.join(ASSETS, "roblox.jpg"),
    imageAttachmentName: "roblox.jpg",
    defaultStatus: "online",
    prices: [],
    delivery: null,
    website: null,
    faq: COMMON_FAQ,
  },
  {
    key: "windowspai",
    name: "Windows PaiPai",
    channelName: "windows-paipai",
    emoji: "🖥️",
    tagline: "A faster, cleaner Windows in one click.",
    description:
      "A legitimate Windows optimization suite: cleaning, performance and privacy " +
      "tweaks in just a few clicks, built directly into the PaiPai app.",
    imagePath: path.join(ASSETS, "logo.png"),
    imageAttachmentName: "windowspai.png",
    defaultStatus: "online",
    prices: [],
    delivery: null,
    website: null,
    faq: COMMON_FAQ,
  },
];
