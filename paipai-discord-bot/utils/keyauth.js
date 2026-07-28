// Generation de licences via l'API VENDEUR de KeyAuth. C'est ce qui rend la
// livraison automatique : le bot cree la cle au moment de la vente, personne
// n'ouvre le dashboard KeyAuth.
//
// La cle vendeur (KEYAUTH_SELLER_KEY) est un secret MAITRE : elle permet de
// creer ET de supprimer des licences et des comptes. Elle vit donc uniquement
// dans les variables d'environnement de l'hebergeur -- jamais dans le depot,
// jamais dans data/store.json (fichier lisible par l'API de controle), jamais
// renvoyee par une route, jamais ecrite dans la console.
const SELLER_API = process.env.KEYAUTH_SELLER_API || "https://keyauth.win/api/seller/";

// Format des cles generees. Chaque `*` = un caractere aleatoire.
const DEFAULT_MASK = "******-******-******-******";

// KeyAuth compte l'expiration en JOURS. Une licence "a vie" n'existe pas dans
// l'API : on utilise 10 ans, ce que l'appli affiche deja comme "Lifetime"
// (Template/WelcomeBanner.cs traite > 3650 jours comme illimite).
const LIFETIME_DAYS = 3650;

function isConfigured() {
  return String(process.env.KEYAUTH_SELLER_KEY || "").trim().length > 0;
}

// Etat expose a PaiPai : dit SI c'est configure et sur quel hote, jamais la
// cle elle-meme.
function status() {
  let host = SELLER_API;
  try {
    host = new URL(SELLER_API).host;
  } catch {
    /* URL invalide -> on renvoie la valeur brute, c'est deja un diagnostic */
  }
  return { configured: isConfigured(), api: host, lifetimeDays: LIFETIME_DAYS };
}

// Ne montre jamais une cle en clair dans un journal : les 4 derniers
// caracteres suffisent a rapprocher une ligne de journal d'une vente.
function maskKey(key) {
  const text = String(key || "");
  return text.length <= 4 ? "****" : `****${text.slice(-4)}`;
}

function normalizeDays(days) {
  const n = Number(days);
  if (!Number.isFinite(n) || n <= 0) return LIFETIME_DAYS;
  return Math.min(Math.round(n), LIFETIME_DAYS);
}

// Cree UNE licence et la renvoie. `level` doit correspondre au niveau de
// l'abonnement KeyAuth du produit vendu : c'est lui, et lui seul, qui decide
// a quel produit la cle donne acces.
async function createLicense({ days, level, note, mask }) {
  if (!isConfigured()) {
    return { ok: false, error: "KeyAuth seller key missing — set KEYAUTH_SELLER_KEY in the host's environment." };
  }

  const params = new URLSearchParams({
    sellerkey: String(process.env.KEYAUTH_SELLER_KEY).trim(),
    type: "add",
    format: "JSON",
    expiry: String(normalizeDays(days)),
    mask: mask || process.env.KEYAUTH_KEY_MASK || DEFAULT_MASK,
    level: String(level || 1),
    amount: "1",
    owner: "PaiPaiBot",
    character: "1",
  });
  if (note) params.set("note", String(note).slice(0, 120));

  let response;
  try {
    response = await fetch(`${SELLER_API}?${params.toString()}`, {
      // 20 s : la generation KeyAuth est rapide, mais un Staff qui clique
      // "livrer" prefere attendre que devoir recommencer.
      signal: AbortSignal.timeout(20000),
      headers: { "User-Agent": "PaiPaiBot/1.0" },
    });
  } catch (err) {
    return { ok: false, error: `KeyAuth unreachable: ${err.message}` };
  }

  const raw = await response.text();
  let data = null;
  try {
    data = JSON.parse(raw);
  } catch {
    /* KeyAuth renvoie parfois du texte brut : traite plus bas */
  }

  if (!data) {
    // Repli sur le format texte (`format=text`) : la reponse EST la cle.
    const candidate = raw.trim();
    if (response.ok && candidate.length > 0 && !/error|invalid|not found/i.test(candidate)) {
      return { ok: true, key: candidate };
    }
    return { ok: false, error: `KeyAuth answered: ${candidate.slice(0, 200) || response.status}` };
  }

  if (data.success === false) {
    return { ok: false, error: data.message || "KeyAuth refused the request." };
  }

  const key = data.key || (Array.isArray(data.keys) ? data.keys[0] : null);
  if (!key) return { ok: false, error: data.message || "KeyAuth answered without a key." };

  return { ok: true, key: String(key).trim() };
}

module.exports = {
  DEFAULT_MASK,
  LIFETIME_DAYS,
  isConfigured,
  status,
  maskKey,
  normalizeDays,
  createLicense,
};
