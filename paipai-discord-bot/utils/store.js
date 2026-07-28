// Persistance simple en JSON (pas de base de donnees : un seul serveur,
// tourne sur un PC -> inutile de complexifier). Ecriture ATOMIQUE
// (fichier temporaire + rename) pour eviter un fichier corrompu si le
// process est tue en plein milieu d'une ecriture.
const fs = require("node:fs");
const path = require("node:path");

const STORE_PATH = path.join(__dirname, "..", "data", "store.json");

const DEFAULTS = {
  ticketCounter: 0,
  // ticketChannelId -> { userId, productKey, claimedBy }
  openTickets: {},
  // productKey -> "online" | "maintenance" | "offline"
  productStatus: {},

  // Catalogue pilote depuis PaiPai (utils/catalog.js) :
  // - productOverrides : cle d'un produit INTEGRE -> champs reecrits depuis
  //   le Bot Manager (seuls les champs edites y figurent).
  // - customProducts   : produits CREES depuis le Bot Manager, avec la meme
  //   forme que config/products.js (sans image dediee).
  // Ils vivent ici et non dans le code car l'hebergeur redeploie le code
  // depuis Git a chaque mise a jour : un fichier de config modifie a chaud
  // serait perdu, pas les donnees.
  productOverrides: {},
  customProducts: [],

  // Moyens de paiement affiches dans les tickets (utils/payments.js), pilotes
  // depuis PaiPai : { id, kind, label, address, network, note, enabled }.
  // Ici et pas dans le code : changer une adresse ne doit jamais demander un
  // redeploiement, et un redeploiement ne doit jamais ecraser une adresse.
  paymentMethods: [],
  // Phrase d'introduction affichee au-dessus des moyens de paiement.
  paymentIntro: "",

  // Journal des livraisons (features/delivery.js). La cle n'y figure JAMAIS
  // en clair : ce fichier est lisible via l'API de controle, une copie ne doit
  // pas donner un stock de licences valides. Seuls les 4 derniers caracteres
  // sont conserves, de quoi rapprocher une ligne d'une reclamation client.
  sales: [],
  // id du message de la page de statut, pour l'EDITER au lieu d'en
  // reposter un nouveau a chaque /status-set.
  statusMessage: null, // { channelId, messageId }

  // Liaison Discord <-> application PaiPai (features/link.js).
  // code a usage unique -> { userId, expiresAt }
  linkCodes: {},
  // EMPREINTE SHA-256 du jeton -> { userId, tag, roles, createdAt, expiresAt }.
  // On ne stocke jamais le jeton lui-meme : ce fichier vit chez l'hebergeur,
  // une copie ne doit pas suffire a piloter le bot.
  sessions: {},
};

function load() {
  try {
    const raw = fs.readFileSync(STORE_PATH, "utf8");
    return { ...DEFAULTS, ...JSON.parse(raw) };
  } catch {
    return { ...DEFAULTS };
  }
}

function save(data) {
  const tmpPath = `${STORE_PATH}.tmp`;
  fs.mkdirSync(path.dirname(STORE_PATH), { recursive: true });
  fs.writeFileSync(tmpPath, JSON.stringify(data, null, 2), "utf8");
  fs.renameSync(tmpPath, STORE_PATH);
}

// Lit, modifie via `mutator(data)`, puis sauvegarde -> evite d'oublier
// un save() apres une modification.
function update(mutator) {
  const data = load();
  mutator(data);
  save(data);
  return data;
}

module.exports = { load, save, update };
