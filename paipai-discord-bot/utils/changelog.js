// Transforme un texte de changelog libre en bloc ```diff colore par Discord.
//
// Pourquoi ```diff : c'est le SEUL surlignage disponible dans un embed qui
// colore une ligne entiere selon son premier caractere. La grammaire
// highlight.js utilisee par Discord classe "+" ET "!" en `addition` (vert),
// "-" en `deletion` (rouge) -> on obtient un changelog lisible en un coup
// d'oeil sans aucune image.
const PREFIXES = {
  "+": "+", // ajout           -> vert
  "!": "!", // correction      -> vert
  "*": "!", // ameliration     -> vert (alias confortable a taper)
  "~": "!",
  "-": "-", // retrait         -> rouge
};

// Un embed Discord plafonne a 4096 caracteres de description. On garde une
// marge pour l'en-tete, le titre de section et les delimiteurs du bloc.
const MAX_BLOCK = 3400;

// Trois backticks dans le texte saisi FERMERAIENT le bloc de code et
// laisseraient le reste du changelog s'echapper en markdown brut (voire
// casser l'embed). On neutralise avec une espace fine insecable.
function neutralizeFences(text) {
  return text.replace(/```/g, "`​``");
}

// Accepte aussi bien "\n" reel que la sequence litterale "\n" : quand la
// ligne arrive d'une option de commande slash ou d'un champ WinForms, les
// retours a la ligne sont souvent tapes tels quels par l'utilisateur.
function splitLines(raw) {
  return String(raw || "")
    .replace(/\\n/g, "\n")
    .split(/\r?\n/)
    .map((l) => l.trim())
    .filter((l) => l.length > 0);
}

// Normalise une ligne : "Ajout du support" -> "+ Ajout du support" si elle
// porte deja un prefixe, sinon la laisse neutre (blanc) — utile pour une
// phrase de contexte au milieu du changelog.
function formatLine(line) {
  const head = line[0];
  const mapped = PREFIXES[head];
  if (!mapped) return `  ${line}`;
  return `${mapped} ${line.slice(1).trim()}`;
}

function renderChangelog(raw) {
  const lines = splitLines(raw).map(formatLine);
  if (lines.length === 0) return null;

  let body = neutralizeFences(lines.join("\n"));
  if (body.length > MAX_BLOCK) {
    body = `${body.slice(0, MAX_BLOCK)}\n… (changelog truncated)`;
  }
  return "```diff\n" + body + "\n```";
}

module.exports = { renderChangelog, splitLines };
