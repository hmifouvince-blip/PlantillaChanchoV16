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
  logoPath: path.join(__dirname, "..", "assets", "logo.png"),
  logoAttachmentName: "paipai-logo.png",
};
