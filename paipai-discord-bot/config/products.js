// Produits INTEGRES au bot -> base du catalogue. Le catalogue reellement
// utilise a l'execution est utils/catalog.js : il applique par-dessus les
// modifications et les creations faites depuis PaiPai (Bot Manager), qui
// vivent dans data/store.json. Ne jamais lire ce fichier directement dans
// une commande : les produits crees depuis l'appli y seraient invisibles.
//
// Descriptions alignees sur Products/ProductManager.cs (l'appli C#) pour que
// Discord et PaiPai racontent la meme chose.
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

const PRODUCTS = [
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
    // Le produit vendu est un PACK (le toolkit + l'emulateur sur lequel il
    // tourne) : la fiche doit le dire, sinon le nom « Val + Emulator » reste
    // incompris et la question revient dans chaque ticket -> d'ou la FAQ
    // dediee ci-dessous, en plus de la FAQ commune.
    description:
      "Take full control of every round. Precise aim assistance, lightning reflexes " +
      "and smart visual tools — all in a clean, undetected package built to help you " +
      "climb the ranks.\n\n" +
      "**What you get**\n" +
      "• PaiPai Val — aim assistance, faster reactions, smart visual tools\n" +
      "• The emulator it runs on, included — nothing extra to install\n" +
      "• One-click launch straight from the PaiPai app\n" +
      "• Always the latest build: the app downloads it for you at every launch\n" +
      "• Windows 10 & 11 • Intel / AMD",
    imagePath: path.join(ASSETS, "valorant.jpg"),
    imageAttachmentName: "valorant.jpg",
    defaultStatus: "online",
    prices: [],
    delivery: null,
    website: null,
    faq: [
      {
        q: "Do I need to set up the emulator myself?",
        a: "No. The emulator ships with the product — the PaiPai app downloads and launches everything for you.",
      },
      ...COMMON_FAQ,
    ],
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

module.exports = PRODUCTS;
// Attache a l'export pour que utils/catalog.js donne la meme FAQ aux produits
// crees depuis PaiPai (le processus d'achat est le meme pour tous), sans
// changer la forme de l'export (un simple tableau) attendue partout ailleurs.
module.exports.COMMON_FAQ = COMMON_FAQ;
