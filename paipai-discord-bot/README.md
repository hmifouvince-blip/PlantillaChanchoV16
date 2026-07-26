# Bot Discord PaiPai

Bot qui gère automatiquement le serveur Discord PaiPai : structure du
serveur, vérification anti-multi-compte, tickets d'achat/support,
présentation des produits, page de statut, annonces et updates.

Une fois configuré et lancé, **tout le reste se pilote depuis Discord**
(boutons + commandes slash) — tu n'as plus besoin de toucher au code.

---

## 1. Créer l'application bot Discord (à faire une seule fois)

1. Va sur https://discord.com/developers/applications
2. **New Application** → donne-lui un nom (ex: `PaiPai`) → **Create**
3. Dans le menu de gauche, va dans **Bot**
4. Clique **Reset Token** (ou **Copy** si un token est déjà visible) →
   **garde ce token précieusement, ne le partage à personne** (c'est
   l'équivalent d'un mot de passe complet pour ton bot)
5. Toujours dans **Bot**, descends jusqu'à **Privileged Gateway Intents**
   et active **Server Members Intent** (obligatoire — sans ça le bot ne
   peut pas détecter l'arrivée de nouveaux membres pour la vérification).
   Sauvegarde.
6. Va dans **Installation** (ou **OAuth2** → **URL Generator** selon la
   version du portail) :
   - Coche les scopes : `bot` et `applications.commands`
   - Dans les permissions du bot, coche **Administrator** (le plus simple
     et fiable pour que le bot puisse créer rôles/salons sans blocage de
     permissions)
   - Copie le lien généré en bas, ouvre-le dans ton navigateur, choisis
     ton serveur PaiPai, et clique **Autoriser**

## 2. Récupérer l'ID de ton serveur

1. Dans Discord (l'application, pas le site) : **Paramètres utilisateur**
   → **Avancés** → active **Mode développeur**
2. Clic droit sur l'icône de ton serveur → **Copier l'ID du serveur**

## 3. Configurer le bot

1. Dans ce dossier, copie `.env.example` en `.env`
2. Ouvre `.env` et remplis :
   ```
   BOT_TOKEN=le_token_copié_à_l'étape_1
   GUILD_ID=l'id_copié_à_l'étape_2
   ```

## 4. Installer et lancer

Il te faut Node.js (déjà installé sur cette machine, vérifié : v24).

```powershell
npm install
```

Puis, à chaque fois que tu veux démarrer le bot : double-clique
**`start.bat`** (ou `npm start` dans un terminal ouvert dans ce dossier).

⚠️ **Lancé comme ça, le bot ne tourne que tant que cette fenêtre reste
ouverte et que ton PC est allumé.** Pour qu'il reste en ligne 24h/24 sans
dépendre de ton PC, voir la **section 6 : hébergement 24/7**.

Tu dois voir dans la fenêtre : `Connecté en tant que PaiPai#XXXX` — si tu
vois une erreur à la place, relis le message, il explique quoi corriger
(token invalide, intent non activé, etc.).

## 5. Construire le serveur (une seule fois)

Dans un salon de ton serveur Discord, tape :
```
/setup-server
```
Le bot crée automatiquement les rôles, salons, le règlement, le panneau
de vérification, le panneau de tickets, la présentation des produits et
la page de statut. **Il ne touche jamais à ce qui existe déjà** — tu peux
relancer cette commande sans risque à tout moment (par exemple après
avoir ajouté un produit dans `config/products.js`).

## 6. Hébergement 24/7 + pilotage depuis PaiPai

Le bot embarque une petite **API de contrôle** (`control/server.js`) qui
permet au Bot Manager de PaiPai de suivre son état, lire sa console et le
redémarrer **même quand il tourne chez un hébergeur**, à l'autre bout du
monde. Sans elle, PaiPai ne saurait piloter qu'un bot lancé sur ta propre
machine.

### 6.1 Choisir l'hébergeur

Recommandé : **bot-hosting.net** — gratuit, sans carte bancaire, 24/7 sans
mise en veille, Node.js supporté, port + IP attribués (indispensable pour
l'API de contrôle). Alternative plus puissante mais plus technique :
**Oracle Cloud Always Free** (vraie VM gratuite à vie, carte bancaire
demandée pour la vérification).

À éviter : les offres gratuites de Render/Railway/Fly — soit le service
s'endort après 15 min d'inactivité (le bot passe hors ligne), soit le tier
gratuit n'existe plus, soit le disque est effacé à chaque redéploiement
(tu perdrais `data/store.json`, donc les tickets et la page de statut).

### 6.2 Déployer

1. Crée un serveur **Node.js** chez l'hébergeur.
2. Envoie-lui le contenu de ce dossier (upload, ou `git clone` du dépôt).
3. Commande de démarrage : `node index.js`. Installe les dépendances avec
   `npm install` au premier lancement.
4. Renseigne les variables d'environnement **dans le panel de
   l'hébergeur**, jamais dans un fichier envoyé au dépôt :
   - `BOT_TOKEN`, `GUILD_ID`, `MIN_ACCOUNT_AGE_DAYS`
   - `CONTROL_KEY` : un secret long et aléatoire (16 caractères minimum).
     **Sans cette variable, l'API de contrôle ne démarre pas du tout** —
     c'est volontaire : une API de contrôle ouverte laisserait n'importe
     qui redémarrer ton bot.
5. Note l'**IP et le port** attribués : c'est l'adresse à donner à PaiPai.
6. Démarre. La console doit afficher `[control] API de contrôle à l'écoute
   sur le port XXXX.` puis `Logged in as PaiPai#XXXX`.

### 6.3 Brancher PaiPai dessus

Dans PaiPai → **Bot Manager** → `Edit` sur le profil du bot :

- **Control URL** : `IP:PORT` de l'hébergeur (ex: `152.53.44.12:25565`)
- **Control key** : exactement la même valeur que `CONTROL_KEY`

Le volet du haut passe alors en mode distant : pastille en ligne/hors
ligne, nom du serveur, nombre de membres, latence, uptime, console live
rafraîchie toutes les 3 s, et bouton **Restart**. Les actions rapides
(annonce, update, statut, tickets) continuent de fonctionner et lisent
désormais les données **chez l'hébergeur**, plus sur ton disque.

Laisse ces deux champs vides pour revenir au pilotage d'un bot lancé
localement.

### 6.4 Ce que l'API expose

| Route | Effet |
|---|---|
| `GET /ping` | Seule route sans clé — pour les robots de keep-alive. Ne divulgue rien. |
| `GET /health` | En ligne ou non, uptime, serveur, membres, latence |
| `GET /logs?since=N` | Les lignes de console apparues depuis la ligne N |
| `POST /restart` | Arrête le process ; l'hébergeur le relance |
| `GET /store` | Contenu de `data/store.json` (tickets, statuts) |
| `POST /product-status` | Change l'état d'un produit sur la page #statut |

⚠️ Sur un hébergeur qui ne fournit qu'une adresse `http://` (pas de
`https://`), la clé de contrôle circule **en clair** sur le réseau. Le
token du bot, lui, ne quitte jamais l'hébergeur : le pire qu'un attaquant
qui intercepterait la clé pourrait faire, c'est redémarrer le bot ou
changer un statut. Utilise une clé longue et aléatoire, et change-la si tu
la soupçonnes exposée.

## Commandes disponibles (réservées au Staff / Administrateur)

| Commande | Rôle requis | Effet |
|---|---|---|
| `/setup-server` | Administrateur | Crée/complète toute la structure du serveur |
| `/post-products` | Gérer le serveur | Reposte la présentation des produits |
| `/status-set produit: état:` | Gérer le serveur | Met à jour la page #statut |
| `/update-post titre: description:` | Gérer le serveur | Publie une update dans #updates |
| `/announce titre: message: ping:` | Gérer le serveur | Publie une annonce dans #annonces |

## Fonctionnement automatique (aucune commande nécessaire)

- **Nouveau membre** → reçoit le rôle `Non Vérifié`, ne voit que
  `#règles` et `#vérification`.
- **Bouton "Je suis humain, vérifie-moi"** dans `#vérification` → vérifie
  l'ancienneté du compte Discord (7 jours minimum par défaut, réglable
  dans `.env` via `MIN_ACCOUNT_AGE_DAYS`) → passe au rôle `Membre` si OK.
- **Bouton "Ouvrir un ticket"** dans `#tickets` → menu déroulant du
  produit → crée un salon privé visible uniquement par le client et le
  `Staff`. Boutons `Réclamer` et `Fermer` dans chaque ticket. **Le
  paiement se gère entièrement à la main entre le Staff et le client dans
  le ticket** (aucune automatisation de paiement).

## Modifier les produits présentés

Édite `config/products.js` (nom, description, image, statut par défaut),
puis relance `/post-products` dans Discord pour republier les embeds à
jour. Les images utilisées sont dans `assets/` (copiées depuis les
visuels de l'application PaiPai pour rester cohérent).

## Donner accès au Staff

Dans Discord, assigne le rôle **Staff** (créé par `/setup-server`) aux
personnes qui doivent gérer les tickets/annonces/statuts/updates.
