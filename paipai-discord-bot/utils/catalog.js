// Catalogue produit EFFECTIF du serveur : les produits integres
// (config/products.js) + ce que PaiPai a cree ou modifie depuis le Bot
// Manager. Toute commande/route qui a besoin de la liste des produits doit
// passer par ICI, jamais par config/products.js directement, sinon les
// produits crees depuis l'appli y sont invisibles.
//
// Pourquoi stocker les creations dans data/store.json plutot que de reecrire
// config/products.js : le bot tourne chez un hebergeur ou le code est
// redeploye depuis Git a chaque mise a jour. Un fichier de code modifie a
// chaud serait ecrase au prochain deploiement -- les donnees, non.
const baseProducts = require("../config/products");
const branding = require("../config/branding");
const store = require("./store");

// Champs modifiables depuis PaiPai. Volontairement PAS `key` ni `channelName`
// (renommer un salon casserait les liens deja publies), ni l'image (aucun
// upload de fichier dans l'API de controle -> les produits crees retombent
// sur le logo PaiPai).
const EDITABLE_FIELDS = ["name", "emoji", "tagline", "description", "prices", "delivery", "website", "note"];

// Limites CONSERVATRICES par rapport a Discord (titre d'embed 256, description
// 4096, nom de salon 100, nom de role 100) : un depassement fait echouer TOUT
// l'envoi du message, donc on refuse la saisie plutot que de decouvrir l'echec
// au moment de publier.
const LIMITS = {
  key: 24,
  name: 64,
  emoji: 8,
  tagline: 200,
  description: 3000,
  delivery: 100,
  website: 300,
  note: 400,
  priceLabel: 40,
  priceValue: 40,
  prices: 8,
};

const KEY_PATTERN = /^[a-z0-9][a-z0-9-]{1,23}$/;
const VALID_STATES = ["online", "maintenance", "offline"];

// Nom libre -> identifiant utilisable comme cle et comme nom de salon Discord
// (minuscules, sans accent, tirets).
function slugify(text) {
  return String(text || "")
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "") // accents decomposes par NFD
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .slice(0, LIMITS.key);
}

function customList(data) {
  return Array.isArray(data.customProducts) ? data.customProducts : [];
}

function overridesOf(data, key) {
  const all = data.productOverrides || {};
  return all[key] && typeof all[key] === "object" ? all[key] : {};
}

// Produit integre + ses modifications : on ne recopie que les champs
// reellement surcharges, pour qu'une valeur d'origine mise a jour dans le code
// reste visible tant que personne ne l'a editee depuis PaiPai.
function applyOverrides(product, override) {
  const merged = { ...product };
  for (const field of EDITABLE_FIELDS) {
    if (override[field] !== undefined) merged[field] = override[field];
  }
  return merged;
}

// Un produit cree depuis PaiPai n'a pas de visuel dedie (l'API de controle ne
// transporte pas de fichier) : il reutilise l'image du logo, mais sous un NOM
// D'ATTACHEMENT propre, comme le fait deja "windowspai". Reutiliser tel quel
// le nom du logo ferait pointer la vignette et la grande image de l'embed sur
// le meme attachement.
function hydrateCustom(record) {
  return {
    faq: baseProducts.COMMON_FAQ || [],
    ...record,
    imagePath: branding.logoPath,
    imageAttachmentName: `${record.key}.png`,
    custom: true,
  };
}

// Liste complete, produits integres d'abord (ordre du code), creations
// ensuite (ordre de creation).
function list() {
  const data = store.load();
  const builtins = baseProducts.map((p) => applyOverrides(p, overridesOf(data, p.key)));
  // Les produits crees depuis PaiPai n'ont pas de surcharge : leur edition
  // reecrit directement l'enregistrement, il n'y a pas de version "d'origine"
  // dans le code a preserver.
  const customs = customList(data).map(hydrateCustom);
  return [...builtins, ...customs];
}

function find(key) {
  return list().find((p) => p.key === key) || null;
}

function isBuiltin(key) {
  return baseProducts.some((p) => p.key === key);
}

// Forme envoyee a PaiPai : que du JSON serialisable (pas de chemin de fichier
// local, qui n'a aucun sens sur la machine de l'utilisateur).
function toPublic(product, productStatus) {
  const status = (productStatus || store.load().productStatus || {})[product.key];
  return {
    key: product.key,
    name: product.name,
    emoji: product.emoji || "",
    tagline: product.tagline || "",
    description: product.description || "",
    channelName: product.channelName,
    prices: Array.isArray(product.prices) ? product.prices : [],
    delivery: product.delivery || "",
    website: product.website || "",
    note: product.note || "",
    builtin: isBuiltin(product.key),
    status: status || product.defaultStatus || "online",
  };
}

function listPublic() {
  const productStatus = store.load().productStatus || {};
  return list().map((p) => toPublic(p, productStatus));
}

function tooLong(value, max) {
  return String(value).length > max;
}

// Renvoie { ok, patch } : chaque champ ABSENT du corps est laisse tel quel
// (edition partielle), chaque champ present est valide avant d'etre retenu.
function buildPatch(input) {
  const patch = {};

  for (const field of EDITABLE_FIELDS) {
    if (input[field] === undefined || input[field] === null) continue;

    if (field === "prices") {
      const raw = input.prices;
      if (!Array.isArray(raw)) return { ok: false, error: "Prices must be a list." };
      if (raw.length > LIMITS.prices) return { ok: false, error: `At most ${LIMITS.prices} price lines.` };
      const prices = [];
      for (const row of raw) {
        const label = String((row && row.label) || "").trim();
        const price = String((row && row.price) || "").trim();
        if (!label || !price) return { ok: false, error: "Each price line needs a label and a price." };
        if (tooLong(label, LIMITS.priceLabel) || tooLong(price, LIMITS.priceValue)) {
          return { ok: false, error: `Price lines are limited to ${LIMITS.priceLabel} characters per side.` };
        }
        prices.push({ label, price });
      }
      patch.prices = prices;
      continue;
    }

    const value = String(input[field]).trim();
    if (LIMITS[field] && tooLong(value, LIMITS[field])) {
      return { ok: false, error: `"${field}" is limited to ${LIMITS[field]} characters.` };
    }
    // Un bouton Discord de type Link exige une URL absolue : une valeur du
    // genre "paipai.fr" ferait echouer l'envoi de la fiche entiere.
    if (field === "website" && value && !/^https?:\/\/\S+$/.test(value)) {
      return { ok: false, error: "Website must start with http:// or https://" };
    }
    patch[field] = value;
  }

  return { ok: true, patch };
}

// Cree ou modifie un produit. `input.key` vide -> cle derivee du nom (cas
// "nouveau produit" depuis PaiPai, ou l'utilisateur ne saisit qu'un nom).
function upsert(input) {
  const name = String((input && input.name) || "").trim();
  const requestedKey = String((input && input.key) || "").trim().toLowerCase();
  const key = requestedKey || slugify(name);

  if (!key) return { ok: false, error: "Product name is required." };
  if (!KEY_PATTERN.test(key)) {
    return { ok: false, error: `Invalid product key "${key}" — use 2 to ${LIMITS.key} characters: a-z, 0-9, "-".` };
  }

  const data = store.load();
  const builtin = isBuiltin(key);
  const existingCustom = customList(data).find((p) => p.key === key) || null;
  const created = !builtin && !existingCustom;

  if (created && !name) return { ok: false, error: "Product name is required." };

  const built = buildPatch(input || {});
  if (!built.ok) return built;
  const patch = built.patch;

  if (created) {
    const channelName = slugify(input.channelName || key).slice(0, 90) || key;
    const status = VALID_STATES.includes(input.defaultStatus) ? input.defaultStatus : "online";
    store.update((d) => {
      if (!Array.isArray(d.customProducts)) d.customProducts = [];
      d.customProducts.push({
        key,
        name,
        channelName,
        emoji: patch.emoji || "🌸",
        tagline: patch.tagline || "",
        description: patch.description || "",
        prices: patch.prices || [],
        delivery: patch.delivery || "",
        website: patch.website || "",
        note: patch.note || "",
        defaultStatus: status,
        createdAt: Date.now(),
      });
    });
  } else if (builtin) {
    // Produit integre : on n'ecrit QUE la surcharge, le reste continue de
    // suivre le code.
    store.update((d) => {
      if (!d.productOverrides || typeof d.productOverrides !== "object") d.productOverrides = {};
      d.productOverrides[key] = { ...(d.productOverrides[key] || {}), ...patch };
    });
  } else {
    store.update((d) => {
      const record = customList(d).find((p) => p.key === key);
      if (record) Object.assign(record, patch);
    });
  }

  return { ok: true, created, product: find(key) };
}

module.exports = {
  EDITABLE_FIELDS,
  LIMITS,
  slugify,
  list,
  listPublic,
  toPublic,
  find,
  isBuiltin,
  upsert,
};
