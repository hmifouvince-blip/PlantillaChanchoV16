// API de controle HTTP du bot : permet a PaiPai (Bot Manager) de le piloter
// meme quand il tourne sur un hebergeur distant 24/7 -- cas ou le pilotage par
// process local (BotProcessManager cote C#) ne peut plus rien faire, puisqu'il
// n'y a plus aucun process sur la machine de l'utilisateur.
//
// node:http pur, AUCUNE dependance en plus : l'hebergement gratuit vise
// (256 Mo de RAM) ne laisse pas de marge pour un framework web, et discord.js
// consomme deja l'essentiel de l'enveloppe.
//
// Sans CONTROL_KEY dans l'environnement, le serveur ne demarre PAS : une API de
// controle ouverte laisserait n'importe qui redemarrer le bot ou lire ses logs.
const http = require("node:http");
const crypto = require("node:crypto");
const util = require("node:util");
const store = require("../utils/store");
const products = require("../config/products");
const branding = require("../config/branding");
const link = require("../features/link");
const { buildStatusEmbed, logoAttachment } = require("../commands/status-set");
const { postAnnounce } = require("../commands/announce");
const { postUpdate } = require("../commands/update-post");

const MAX_LINES = 300;
const MAX_BODY_BYTES = 64 * 1024;
const VALID_STATES = ["online", "maintenance", "offline"];

// Anti-force brute sur l'echange de code : cette route est la SEULE
// joignable sans authentification, elle merite donc son propre garde-fou.
const REDEEM_MAX_ATTEMPTS = 10;
const REDEEM_WINDOW_MS = 10 * 60 * 1000;
const redeemAttempts = new Map(); // ip -> { count, resetAt }

function tooManyRedeems(ip) {
  const now = Date.now();
  const entry = redeemAttempts.get(ip);
  if (!entry || entry.resetAt <= now) {
    redeemAttempts.set(ip, { count: 1, resetAt: now + REDEEM_WINDOW_MS });
    return false;
  }
  entry.count += 1;
  return entry.count > REDEEM_MAX_ATTEMPTS;
}

// Tampon circulaire des dernieres lignes de console. Chaque ligne porte un
// numero de sequence croissant : PaiPai demande "tout ce qui est apparu depuis
// seq N" et ne reaffiche donc jamais deux fois la meme ligne, meme si le
// tampon a deja evacue des lignes plus anciennes entre deux sondages.
const logLines = [];
let seq = 0;

function formatArg(arg) {
  return typeof arg === "string" ? arg : util.inspect(arg, { depth: 2, colors: false });
}

function record(prefix, args) {
  seq += 1;
  logLines.push({ seq, t: Date.now(), text: `${prefix}${args.map(formatArg).join(" ")}` });
  if (logLines.length > MAX_LINES) logLines.shift();
}

// Detourne console.* SANS le remplacer : la sortie continue d'aller au panel de
// l'hebergeur (indispensable pour diagnostiquer un crash au demarrage, quand
// l'API de controle n'est pas encore joignable).
function captureConsole() {
  for (const [method, prefix] of [
    ["log", ""],
    ["info", ""],
    ["warn", "[warn] "],
    ["error", "[erreur] "],
  ]) {
    const original = console[method].bind(console);
    console[method] = (...args) => {
      record(prefix, args);
      original(...args);
    };
  }
}

// Comparaison a temps constant. On hache les deux cotes avant de comparer :
// timingSafeEqual LEVE une exception si les longueurs different, ce qui
// trahirait a lui seul la longueur de la vraie cle.
function sameSecret(provided, expected) {
  if (typeof provided !== "string" || typeof expected !== "string") return false;
  const a = crypto.createHash("sha256").update(provided).digest();
  const b = crypto.createHash("sha256").update(expected).digest();
  return crypto.timingSafeEqual(a, b);
}

// Deux facons de s'authentifier, volontairement distinctes :
// - x-paipai-key    : la cle d'infrastructure (celui qui administre l'hebergeur)
// - x-paipai-token  : un jeton personnel obtenu via /link, adosse a un role
//                     Discord et revocable en retirant ce role
// Renvoie null si aucune des deux ne convient.
function identify(req) {
  if (sameSecret(req.headers["x-paipai-key"], process.env.CONTROL_KEY)) {
    return { kind: "key", label: "clé de contrôle", roles: [] };
  }

  const token = req.headers["x-paipai-token"];
  const session = link.verifyToken(token);
  if (session) {
    return { kind: "session", label: session.tag, roles: session.roles, token, session };
  }

  return null;
}

function readJsonBody(req) {
  return new Promise((resolve, reject) => {
    const chunks = [];
    let size = 0;
    req.on("data", (chunk) => {
      size += chunk.length;
      if (size > MAX_BODY_BYTES) {
        reject(new Error("Corps de requête trop volumineux."));
        req.destroy();
        return;
      }
      chunks.push(chunk);
    });
    req.on("end", () => {
      if (chunks.length === 0) return resolve({});
      try {
        resolve(JSON.parse(Buffer.concat(chunks).toString("utf8")));
      } catch {
        reject(new Error("JSON invalide."));
      }
    });
    req.on("error", reject);
  });
}

async function handle(req, res, client) {
  const url = new URL(req.url, "http://localhost");
  const send = (code, payload) => {
    const body = JSON.stringify(payload);
    res.writeHead(code, {
      "content-type": "application/json; charset=utf-8",
      "content-length": Buffer.byteLength(body),
    });
    res.end(body);
  };

  // Route non authentifiee : sert aux robots de keep-alive des hebergeurs
  // qui exigent du trafic HTTP. Ne divulgue rien.
  if (url.pathname === "/ping") return send(200, { ok: true });

  // Amorcage de la liaison : forcement accessible sans jeton, puisque son
  // but est justement d'en delivrer un. La protection vient du code (court,
  // a usage unique, 10 min) + du controle de role + du garde-fou anti-force
  // brute ci-dessous.
  if (req.method === "POST" && url.pathname === "/link/redeem") {
    const ip = req.socket.remoteAddress || "?";
    if (tooManyRedeems(ip)) {
      return send(429, { ok: false, error: "Trop de tentatives. Réessaie dans 10 minutes." });
    }
    const body = await readJsonBody(req);
    const result = await link.redeemCode(client, body.code);
    if (!result.ok) return send(403, result);
    console.log(`[link] ${result.tag} a lié une application PaiPai (${result.roles.join(", ")}).`);
    return send(200, result);
  }

  const caller = identify(req);
  if (!caller) {
    return send(401, {
      ok: false,
      error: "Non autorisé : clé de contrôle absente/invalide, ou lien Discord expiré (retape /link).",
    });
  }

  // Qui suis-je : permet a PaiPai d'afficher le compte lie et de detecter
  // qu'un jeton a ete revoque sans attendre l'echec d'une action.
  if (req.method === "GET" && url.pathname === "/me") {
    return send(200, {
      ok: true,
      kind: caller.kind,
      label: caller.label,
      roles: caller.roles,
      adminRoleNames: branding.adminRoleNames,
    });
  }

  if (req.method === "GET" && url.pathname === "/health") {
    const guild = client.guilds.cache.get(process.env.GUILD_ID) || null;
    return send(200, {
      ok: true,
      ready: client.isReady(),
      botTag: client.user ? client.user.tag : null,
      guildName: guild ? guild.name : null,
      memberCount: guild ? guild.memberCount : 0,
      // ws.ping vaut -1 tant que le premier heartbeat n'a pas eu lieu.
      pingMs: Math.max(0, Math.round(client.ws.ping)),
      uptimeSeconds: Math.round(process.uptime()),
    });
  }

  if (req.method === "GET" && url.pathname === "/logs") {
    const since = Number(url.searchParams.get("since")) || 0;
    return send(200, {
      ok: true,
      lines: logLines.filter((l) => l.seq > since),
      lastSeq: seq,
    });
  }

  // A partir d'ici, les routes PUBLIENT ou AGISSENT -> on recontrole les
  // roles Discord aupres du serveur. Le simple sondage d'etat (/health,
  // /logs) s'en dispense volontairement : il tourne toutes les 3 s et
  // n'expose rien de destructif.
  if (caller.kind === "session" && !(await link.revalidate(client, caller.token))) {
    return send(403, {
      ok: false,
      error: `Accès révoqué : tu n'as plus le rôle ${branding.adminRoleNames.join(" ou ")}.`,
    });
  }

  if (req.method === "POST" && url.pathname === "/announce") {
    const body = await readJsonBody(req);
    const title = String(body.title || "").trim();
    const message = String(body.message || "").trim();
    if (!title || !message) return send(400, { ok: false, error: "Titre et message obligatoires." });

    const guild = await client.guilds.fetch(process.env.GUILD_ID).catch(() => null);
    if (!guild) return send(409, { ok: false, error: "Serveur Discord introuvable." });

    const result = await postAnnounce(guild, { title, message, ping: body.ping === true });
    if (result.ok) console.log(`[control] Annonce publiée par ${caller.label} : « ${title} ».`);
    return send(result.ok ? 200 : 409, result);
  }

  if (req.method === "POST" && url.pathname === "/update") {
    const body = await readJsonBody(req);
    const title = String(body.title || "").trim();
    const changelog = String(body.changelog || "").trim();
    if (!title || !changelog) return send(400, { ok: false, error: "Titre et changelog obligatoires." });

    const guild = await client.guilds.fetch(process.env.GUILD_ID).catch(() => null);
    if (!guild) return send(409, { ok: false, error: "Serveur Discord introuvable." });

    const result = await postUpdate(guild, {
      title,
      changelog,
      productKey: body.productKey ? String(body.productKey) : null,
      version: body.version ? String(body.version) : null,
      note: body.note ? String(body.note) : null,
      ping: body.ping === true,
    });
    if (result.ok) console.log(`[control] Mise à jour publiée par ${caller.label} : « ${title} ».`);
    return send(result.ok ? 200 : 409, result);
  }

  if (req.method === "POST" && url.pathname === "/restart") {
    console.log(`[control] Redémarrage demandé par ${caller.label}.`);
    send(200, { ok: true, message: "Redémarrage en cours." });
    // On laisse la reponse partir avant de mourir. Sortie code 0 : tous les
    // hebergeurs relancent automatiquement le process.
    setTimeout(() => process.exit(0), 150);
    return;
  }

  if (req.method === "GET" && url.pathname === "/store") {
    return send(200, { ok: true, store: store.load() });
  }

  if (req.method === "POST" && url.pathname === "/product-status") {
    const body = await readJsonBody(req);
    const productKey = String(body.productKey || "");
    const state = String(body.state || "");

    if (!products.some((p) => p.key === productKey)) {
      return send(400, { ok: false, error: `Produit inconnu : ${productKey}` });
    }
    if (!VALID_STATES.includes(state)) {
      return send(400, { ok: false, error: `État invalide : ${state}` });
    }

    const data = store.update((d) => {
      d.productStatus[productKey] = state;
    });

    if (!data.statusMessage) {
      return send(409, { ok: false, error: "Aucune page de statut suivie — lance /setup-server d'abord." });
    }

    const channel = await client.channels.fetch(data.statusMessage.channelId).catch(() => null);
    const message = channel
      ? await channel.messages.fetch(data.statusMessage.messageId).catch(() => null)
      : null;

    if (!message) {
      return send(409, { ok: false, error: "Message de statut introuvable — relance /setup-server." });
    }

    await message.edit({ embeds: [buildStatusEmbed(data.productStatus)], files: [logoAttachment()] });
    return send(200, { ok: true });
  }

  return send(404, { ok: false, error: "Route inconnue." });
}

function start(client) {
  const key = process.env.CONTROL_KEY;
  if (!key || key.length < 16) {
    console.warn(
      "[control] CONTROL_KEY absente ou trop courte (< 16 caractères) -> API de contrôle " +
        "désactivée. PaiPai ne pourra pas piloter ce bot à distance."
    );
    return null;
  }

  captureConsole();

  // Chaque hebergeur impose sa propre variable : PORT chez la plupart des PaaS,
  // SERVER_PORT sur les panels Pterodactyl (Wispbyte, bot-hosting...).
  // CONTROL_PORT ne sert qu'en local, ou l'on choisit soi-meme.
  const port = Number(
    process.env.PORT || process.env.SERVER_PORT || process.env.CONTROL_PORT || 8080
  );

  const server = http.createServer((req, res) => {
    handle(req, res, client).catch((err) => {
      console.error("[control] Erreur de traitement:", err.message);
      if (!res.headersSent) {
        res.writeHead(500, { "content-type": "application/json; charset=utf-8" });
        res.end(JSON.stringify({ ok: false, error: err.message }));
      }
    });
  });

  // 0.0.0.0 et non localhost : sur un hebergeur, l'interface publique est la
  // seule joignable de l'exterieur.
  server.listen(port, "0.0.0.0", () => {
    console.log(`[control] API de contrôle à l'écoute sur le port ${port}.`);
  });
  server.on("error", (err) => {
    console.error(`[control] Impossible d'ouvrir le port ${port} : ${err.message}`);
  });

  return server;
}

module.exports = { start };
