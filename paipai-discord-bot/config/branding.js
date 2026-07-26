// Identite visuelle PaiPai partagee par tous les embeds du bot -> reprend
// exactement les memes valeurs que Utilities/Colors.cs dans l'appli C#
// (theme "Sakura"), pour que Discord et l'appli aient le meme look.
const path = require("node:path");

module.exports = {
  colors: {
    main: 0xf472b6, // rose sakura (accent)
    success: 0x50c878,
    warning: 0xf4b942,
    danger: 0xe55353,
    dark: 0x1a121a, // fond prune (utilise pour les embeds "sombres")
  },
  footerText: "PaiPai",
  tagline: "Premium tools, premium experience",
  logoPath: path.join(__dirname, "..", "assets", "logo.png"),
  logoAttachmentName: "paipai-logo.png",

  // Site officiel affiche en pied d'embed et sur le bouton "Site web".
  // null -> le bouton et la mention sont simplement omis (jamais de lien
  // invente : un lien mort dans une annonce publique fait plus de mal que
  // pas de lien du tout).
  website: null,

  // Roles Discord qui donnent le droit de piloter le bot depuis PaiPai
  // (voir features/link.js). Compares au NOM du role, insensible a la casse.
  // Un membre sans l'un de ces roles peut generer un code mais sera refuse
  // a l'echange -> il n'obtient aucun jeton.
  adminRoleNames: ["PaiPai", "PeiPei"],
};
