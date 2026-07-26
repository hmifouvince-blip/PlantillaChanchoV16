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
const { buildStatusEmbed } = require("../commands/status-set");

const MAX_LINES = 300;
const MAX_BODY_BYTES = 64 * 1024;
const VALID_STATES = ["online", "maintenance", "offline"];

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
function authorized(req) {
  const provided = req.headers["x-paipai-key"];
  if (typeof provided !== "string") return false;
  const a = crypto.createHash("sha256").update(provided).digest();
  const b = crypto.createHash("sha256").update(process.env.CONTROL_KEY).digest();
  return crypto.timingSafeEqual(a, b);
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

  // Seule route non authentifiee : sert aux robots de keep-alive des
  // hebergeurs qui exigent du trafic HTTP. Ne divulgue rien.
  if (url.pathname === "/ping") return send(200, { ok: true });

  if (!authorized(req)) return send(401, { ok: false, error: "Clé de contrôle invalide." });

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

  if (req.method === "POST" && url.pathname === "/restart") {
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

    await message.edit({ embeds: [buildStatusEmbed(data.productStatus)] });
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

  // PORT est impose par la plupart des hebergeurs ; CONTROL_PORT sert au test
  // en local ou l'on veut choisir soi-meme.
  const port = Number(process.env.PORT || process.env.CONTROL_PORT || 8080);

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
