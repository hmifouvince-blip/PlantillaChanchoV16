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
  // id du message de la page de statut, pour l'EDITER au lieu d'en
  // reposter un nouveau a chaque /status-set.
  statusMessage: null, // { channelId, messageId }
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
