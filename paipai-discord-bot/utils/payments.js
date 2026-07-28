// Moyens de paiement affiches au client DANS SON TICKET (PayPal, adresses
// crypto...). Entierement pilotes depuis PaiPai (Bot Manager -> Payments) et
// stockes dans data/store.json, comme le catalogue produit : le code du bot
// est redeploye a chaque mise a jour chez l'hebergeur, une adresse ecrite en
// dur y serait perdue -- et surtout, changer une adresse ne doit demander
// AUCUN redeploiement.
//
// Ces adresses ne sont JAMAIS publiees dans un salon public : uniquement dans
// le ticket prive de l'acheteur. Une adresse affichee publiquement se fait
// recopier par les arnaqueurs qui se font passer pour le vendeur en MP.
const crypto = require("node:crypto");
const store = require("./store");

const KINDS = ["paypal", "crypto", "other"];

const LIMITS = {
  label: 60,
  address: 200,
  network: 40,
  note: 200,
  intro: 600,
  methods: 15,
};

// Avertissement anti-arnaque ajoute a CHAQUE affichage, non modifiable : c'est
// la seule ligne qui protege le client d'un faux vendeur en MP, elle ne doit
// pas pouvoir disparaitre d'un mauvais copier-coller dans l'intro.
const SCAM_WARNING =
  "⚠️ Staff will **never** DM you first. Only the addresses posted by the bot in this ticket are valid.";

const DEFAULT_INTRO =
  "Talk to Staff **before** sending anything — they confirm the amount and the method with you.";

function all() {
  const data = store.load();
  return Array.isArray(data.paymentMethods) ? data.paymentMethods : [];
}

function intro() {
  const data = store.load();
  const value = typeof data.paymentIntro === "string" ? data.paymentIntro.trim() : "";
  return value.length > 0 ? value : DEFAULT_INTRO;
}

// Ce que voit l'acheteur : uniquement les moyens actives, dans l'ordre de la
// liste (l'ordre est celui de creation, modifiable en supprimant/recreant).
function enabledList() {
  return all().filter((m) => m.enabled !== false);
}

function find(id) {
  return all().find((m) => m.id === id) || null;
}

function tooLong(value, max) {
  return String(value).length > max;
}

function clean(value, max, field) {
  const text = String(value == null ? "" : value).trim();
  if (max && tooLong(text, max)) return { ok: false, error: `"${field}" is limited to ${max} characters.` };
  return { ok: true, text };
}

// Cree ou modifie un moyen de paiement. Sans `id` -> creation.
function upsert(input) {
  const body = input || {};
  const id = String(body.id || "").trim();
  const existing = id ? find(id) : null;
  if (id && !existing) return { ok: false, error: `Unknown payment method: ${id}` };

  const kind = KINDS.includes(body.kind) ? body.kind : existing ? existing.kind : "crypto";

  const label = clean(body.label !== undefined ? body.label : existing?.label, LIMITS.label, "label");
  if (!label.ok) return label;
  const address = clean(body.address !== undefined ? body.address : existing?.address, LIMITS.address, "address");
  if (!address.ok) return address;
  const network = clean(body.network !== undefined ? body.network : existing?.network, LIMITS.network, "network");
  if (!network.ok) return network;
  const note = clean(body.note !== undefined ? body.note : existing?.note, LIMITS.note, "note");
  if (!note.ok) return note;

  if (!label.text) return { ok: false, error: "A label is required (e.g. \"Bitcoin (BTC)\")." };
  if (!address.text) return { ok: false, error: "An address is required." };
  // Une adresse crypto ne contient jamais d'espace : c'est le signe d'un
  // copier-coller qui a emporte du texte autour, et une adresse fausse = des
  // fonds envoyes dans le vide, irrecuperables.
  if (kind === "crypto" && /\s/.test(address.text)) {
    return { ok: false, error: "A crypto address cannot contain spaces — check the copy/paste." };
  }
  if (kind === "paypal" && address.text.includes(" ")) {
    return { ok: false, error: "The PayPal address cannot contain spaces." };
  }

  const enabled = body.enabled === undefined ? existing?.enabled !== false : body.enabled !== false;

  if (!existing && all().length >= LIMITS.methods) {
    return { ok: false, error: `At most ${LIMITS.methods} payment methods.` };
  }

  const record = {
    id: existing ? existing.id : crypto.randomBytes(4).toString("hex"),
    kind,
    label: label.text,
    address: address.text,
    network: network.text,
    note: note.text,
    enabled,
    updatedAt: Date.now(),
  };

  store.update((d) => {
    if (!Array.isArray(d.paymentMethods)) d.paymentMethods = [];
    const idx = d.paymentMethods.findIndex((m) => m.id === record.id);
    if (idx >= 0) d.paymentMethods[idx] = { ...d.paymentMethods[idx], ...record };
    else d.paymentMethods.push(record);
  });

  return { ok: true, created: !existing, method: find(record.id) };
}

function remove(id) {
  const target = find(String(id || ""));
  if (!target) return { ok: false, error: "Unknown payment method." };
  store.update((d) => {
    d.paymentMethods = (Array.isArray(d.paymentMethods) ? d.paymentMethods : []).filter((m) => m.id !== target.id);
  });
  return { ok: true, method: target };
}

function setIntro(text) {
  const value = clean(text, LIMITS.intro, "intro");
  if (!value.ok) return value;
  store.update((d) => {
    d.paymentIntro = value.text;
  });
  return { ok: true, intro: intro() };
}

module.exports = {
  KINDS,
  LIMITS,
  SCAM_WARNING,
  DEFAULT_INTRO,
  all,
  intro,
  enabledList,
  find,
  upsert,
  remove,
  setIntro,
};
