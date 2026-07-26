// Liaison Discord <-> application PaiPai.
//
// Probleme resolu : jusqu'ici, piloter le bot depuis PaiPai exigeait de
// connaitre CONTROL_KEY, un secret d'infrastructure qu'on ne peut pas
// distribuer a toute l'equipe (le donner, c'est donner TOUT le controle, et
// le retirer a une seule personne obligerait a le changer pour tout le monde).
//
// A la place : un membre qui porte l'un des roles de branding.adminRoleNames
// tape /link dans Discord, recupere un code court, le colle dans PaiPai, et
// obtient un jeton personnel. Retirer le role dans Discord suffit alors a lui
// couper l'acces (le jeton est revalide contre ses roles a chaque appel).
const crypto = require("node:crypto");
const branding = require("../config/branding");
const store = require("../utils/store");

// Alphabet sans caracteres ambigus : un code se lit a l'oral et se retape a
// la main, 0/O et 1/I/L provoqueraient des echecs incomprehensibles.
const ALPHABET = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
const CODE_LENGTH = 8;
const CODE_TTL_MS = 10 * 60 * 1000; // 10 min
const SESSION_TTL_MS = 30 * 24 * 60 * 60 * 1000; // 30 jours

function hashToken(token) {
  return crypto.createHash("sha256").update(String(token)).digest("hex");
}

function randomCode() {
  const bytes = crypto.randomBytes(CODE_LENGTH);
  let out = "";
  for (let i = 0; i < CODE_LENGTH; i += 1) out += ALPHABET[bytes[i] % ALPHABET.length];
  return out;
}

// Purge a chaque ecriture plutot que sur minuterie : sans process de fond,
// un bot redemarre souvent et un timer manquerait son tour.
function prune(data) {
  const now = Date.now();
  for (const [code, entry] of Object.entries(data.linkCodes)) {
    if (!entry || entry.expiresAt <= now) delete data.linkCodes[code];
  }
  for (const [hash, session] of Object.entries(data.sessions)) {
    if (!session || session.expiresAt <= now) delete data.sessions[hash];
  }
}

function hasAdminRole(member) {
  const allowed = branding.adminRoleNames.map((r) => r.toLowerCase());
  return member.roles.cache.some((role) => allowed.includes(role.name.toLowerCase()));
}

function matchedRoleNames(member) {
  const allowed = branding.adminRoleNames.map((r) => r.toLowerCase());
  return member.roles.cache.filter((r) => allowed.includes(r.name.toLowerCase())).map((r) => r.name);
}

// Genere (ou remplace) le code de liaison d'un membre. Un seul code vivant
// par personne : en regenerer un invalide le precedent, ce qui donne une
// sortie de secours si un code a ete colle dans le mauvais salon.
function createCode(userId) {
  const code = randomCode();
  store.update((data) => {
    prune(data);
    for (const [existing, entry] of Object.entries(data.linkCodes)) {
      if (entry.userId === userId) delete data.linkCodes[existing];
    }
    data.linkCodes[code] = { userId, expiresAt: Date.now() + CODE_TTL_MS };
  });
  return { code, expiresInMinutes: Math.round(CODE_TTL_MS / 60000) };
}

// Echange un code contre un jeton de session. Le code est consomme QUOI QU'IL
// ARRIVE (meme si les roles sont refuses) : sinon un code vole pourrait etre
// reessaye en boucle en attendant que la victime recoive le role.
async function redeemCode(client, rawCode) {
  const code = String(rawCode || "").trim().toUpperCase();
  if (!code) return { ok: false, error: "Code manquant." };

  let entry = null;
  store.update((data) => {
    prune(data);
    entry = data.linkCodes[code] || null;
    delete data.linkCodes[code];
  });

  if (!entry) return { ok: false, error: "Code invalide ou expiré. Retape /link dans Discord." };

  const guild = await client.guilds.fetch(process.env.GUILD_ID).catch(() => null);
  if (!guild) return { ok: false, error: "Serveur Discord introuvable." };

  const member = await guild.members.fetch(entry.userId).catch(() => null);
  if (!member) return { ok: false, error: "Tu n'es plus membre du serveur." };

  if (!hasAdminRole(member)) {
    return {
      ok: false,
      error: `Accès refusé : il faut le rôle ${branding.adminRoleNames.join(" ou ")}.`,
    };
  }

  const token = crypto.randomBytes(32).toString("hex");
  const roles = matchedRoleNames(member);
  const session = {
    userId: member.id,
    tag: member.user.tag,
    roles,
    createdAt: Date.now(),
    expiresAt: Date.now() + SESSION_TTL_MS,
  };

  store.update((data) => {
    prune(data);
    data.sessions[hashToken(token)] = session;
  });

  return { ok: true, token, tag: session.tag, roles, expiresAt: session.expiresAt };
}

// Verification a chaque requete. Deliberement SYNCHRONE et locale (aucun
// appel Discord) : elle est sur le chemin critique de /health, sonde toutes
// les 3 s par chaque PaiPai connecte. La revocation par retrait de role est
// assuree par revalidate() ci-dessous, appele beaucoup plus rarement.
function verifyToken(token) {
  if (typeof token !== "string" || token.length < 32) return null;
  const data = store.load();
  const session = data.sessions[hashToken(token)];
  if (!session || session.expiresAt <= Date.now()) return null;
  return session;
}

// Recontrole les roles aupres de Discord et revoque la session si le membre
// les a perdus. Appele sur les actions qui publient quelque chose, pas sur
// le simple sondage d'etat.
async function revalidate(client, token) {
  const session = verifyToken(token);
  if (!session) return null;

  const guild = await client.guilds.fetch(process.env.GUILD_ID).catch(() => null);
  const member = guild ? await guild.members.fetch(session.userId).catch(() => null) : null;

  // Discord injoignable -> on garde la session : couper l'acces sur un
  // incident reseau serait pire que le risque couvert.
  if (!member) return session;

  if (!hasAdminRole(member)) {
    store.update((data) => {
      delete data.sessions[hashToken(token)];
    });
    return null;
  }
  return session;
}

function revokeAllForUser(userId) {
  let removed = 0;
  store.update((data) => {
    for (const [hash, session] of Object.entries(data.sessions)) {
      if (session.userId === userId) {
        delete data.sessions[hash];
        removed += 1;
      }
    }
  });
  return removed;
}

module.exports = {
  createCode,
  redeemCode,
  verifyToken,
  revalidate,
  revokeAllForUser,
  hasAdminRole,
  matchedRoleNames,
};
