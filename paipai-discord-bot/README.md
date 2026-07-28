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
| `GET /ping` | Sans authentification — pour les robots de keep-alive. Ne divulgue rien. |
| `POST /link/redeem` | Sans authentification — échange un code `/link` contre un jeton (voir 6.5) |
| `POST /signup-key` | Sans authentification — délivre une clé de compte gratuit (quotas, voir plus bas) |
| `GET /me` | Qui suis-je : clé de contrôle, ou compte Discord lié + rôles |
| `GET /health` | En ligne ou non, uptime, serveur, membres, latence |
| `GET /logs?since=N` | Les lignes de console apparues depuis la ligne N |
| `POST /restart` | Arrête le process ; l'hébergeur le relance |
| `GET /store` | Contenu de `data/store.json` (tickets, statuts) |
| `POST /product-status` | Change l'état d'un produit sur la page #statut |
| `GET /products` | Le catalogue produit complet (intégrés + créés depuis PaiPai) |
| `POST /product` | Crée ou modifie un produit, puis republie sa fiche |
| `GET /payments` | Les moyens de paiement (PayPal, adresses crypto) + le texte d'intro |
| `POST /payment` | Ajoute ou modifie un moyen de paiement |
| `POST /payment-delete` | Supprime un moyen de paiement |
| `POST /payment-intro` | Change la phrase affichée au-dessus des adresses |
| `GET /keyauth-status` | Dit si la clé vendeur KeyAuth est configurée (jamais la clé elle-même) |
| `GET /sales?limit=N` | Les dernières licences livrées (clés masquées) |
| `POST /announce` | Publie une annonce dans #announcements |
| `POST /update` | Publie un changelog dans #updates |

⚠️ Sur un hébergeur qui ne fournit qu'une adresse `http://` (pas de
`https://`), la clé de contrôle circule **en clair** sur le réseau. Le
token du bot, lui, ne quitte jamais l'hébergeur : le pire qu'un attaquant
qui intercepterait la clé pourrait faire, c'est redémarrer le bot ou
changer un statut. Utilise une clé longue et aléatoire, et change-la si tu
la soupçonnes exposée.

### 6.5 Donner l'accès à l'équipe sans partager de secret

La clé de contrôle est un secret d'**infrastructure** : la donner à quelqu'un,
c'est lui donner tout le pouvoir, et la retirer à une seule personne oblige à
la changer pour tout le monde. Pour l'équipe, on utilise donc la **liaison
Discord** :

1. Donne le rôle **PaiPai** ou **PeiPei** à la personne dans Discord.
2. Elle tape `/link` dans le serveur → le bot lui répond en privé avec un code
   de 8 caractères, valable 10 minutes et utilisable une seule fois.
3. Dans PaiPai → **Bot Manager** → **Link Discord** → elle colle l'URL de
   contrôle et le code.

Elle peut alors piloter le bot **sans jamais détenir le token du bot ni la clé
de contrôle**. Pour lui retirer l'accès : enlève-lui simplement le rôle — le
bot revalide les rôles à chaque publication et révoque le jeton sur-le-champ.
Elle peut aussi se déconnecter elle-même avec `/unlink`.

Les rôles acceptés se règlent dans `config/branding.js` (`adminRoleNames`).

## Personnaliser les publications

Toute la mise en page (annonces, changelogs, fiches produit, page de statut)
vit dans **`utils/embeds.js`** — un seul fichier à toucher pour changer le
rendu partout, que la publication vienne d'une commande slash ou de
l'application PaiPai.

- **Changelog** : chaque ligne préfixée par `+` (ajout), `-` (retrait) ou
  `!` (correction) ressort colorée dans Discord. Sans préfixe, la ligne reste
  neutre.
- **Tarifs, livraison, FAQ, site web** d'un produit : champs optionnels. Ils
  sont **vides par défaut** (ils s'affichent publiquement, ils doivent donc
  venir de toi) et la section correspondante est simplement omise tant
  qu'ils ne sont pas remplis. Remplis-les depuis PaiPai (Bot Manager →
  **Products**) ou dans `config/products.js` puis `/post-products` — voir
  « Modifier les produits présentés » plus bas.

## Moyens de paiement (PayPal, crypto)

Depuis PaiPai → **Bot Manager** → **Payments** : `+ New method` pour ajouter
un PayPal ou une adresse crypto, `Edit` pour corriger, `Delete` pour retirer,
et la case **Shown to buyers** pour en masquer un temporairement sans le
supprimer. Le champ **Intro** en haut est la phrase affichée au-dessus des
adresses. Tout part chez le bot immédiatement — **aucun redéploiement, aucun
redémarrage**.

Ce que voit l'acheteur : dès l'ouverture de son ticket, le bot poste (et
épingle) une carte « 💳 Payment » avec chaque moyen activé — libellé, adresse
dans un bloc de code (bouton copier), réseau et note. Le Staff confirme le
montant et encaisse à la main, comme avant : **aucun paiement n'est
automatisé**. Si tu changes une adresse en cours de discussion, `/payment-post`
republie la carte dans le ticket.

Trois garde-fous volontaires :

- **Jamais dans un salon public.** Les adresses ne sont postées que dans le
  ticket privé de l'acheteur : une adresse affichée publiquement se fait
  recopier par des arnaqueurs qui se font ensuite passer pour toi en MP.
- **Avertissement non modifiable** ajouté à chaque carte : « Staff will never
  DM you first ». Il ne peut pas disparaître d'un mauvais copier-coller.
- **Toute modification est journalisée** dans la console live (qui a changé
  quoi) : quelqu'un qui détournerait un accès pour remplacer ton adresse BTC
  par la sienne laisse une trace visible immédiatement.

⚠️ Rappel : PayPal **Friends & Family** est prévu pour un envoi entre proches,
pas pour une vente. L'utiliser pour encaisser un achat est contraire aux
conditions de PayPal (risque de limitation du compte) et prive l'acheteur de
toute protection. C'est ton choix commercial — le champ « note » de chaque
moyen est là pour dire clairement à l'acheteur ce que tu attends de lui.

## Livraison automatique des licences

**Ce qui est automatique :** génération de la clé chez KeyAuth, envoi dans le
ticket, envoi en MP, attribution du rôle du produit, journal des ventes.
**Ce qui ne l'est pas :** constater que l'argent est arrivé — voir plus bas.

### 1. Une fois : donner sa clé vendeur au bot

Dans le panel de ton hébergeur, ouvre le fichier `.env` du bot et ajoute :

```
KEYAUTH_SELLER_KEY=ta_cle_vendeur_keyauth
```

Elle se trouve sur le dashboard KeyAuth (*Seller Settings*). Redémarre le bot.
Dans PaiPai → Bot Manager → **Payments**, la ligne du haut passe alors à
« ⚡ Auto-delivery ready ».

⚠️ Cette clé est un secret **maître** : elle permet de créer *et de supprimer*
licences et comptes. Elle ne va que dans `.env` chez l'hébergeur — jamais dans
le dépôt, jamais dans PaiPai, jamais dans un salon Discord. Si tu la crois
exposée, régénère-la depuis KeyAuth.

### 2. Quand tu veux : tes prix et tes durées

PaiPai → Bot Manager → **Products** → `Edit` → champ **Offers**, une ligne par
formule :

```
1 week    | 8 €  | 7   | 102
1 month   | 20 € | 30  | 102
Lifetime  | 90 € | 0   | 102
Reseller  | ask staff
```

`libellé | prix | durée en jours | niveau KeyAuth`. `0` jour = à vie. Les deux
derniers champs sont **facultatifs** : sans eux l'offre s'affiche sur la fiche
produit mais ne peut pas être livrée automatiquement (pratique pour annoncer un
prix avant d'avoir tranché la durée).

⚠️ Le **niveau** est ce qui décide à quel produit la clé donne accès. Ce n'est
PAS un numéro d'ordre : c'est le niveau de l'abonnement KeyAuth, visible sur
le dashboard (*Subscriptions*). Pour cette application :

| Produit | Abonnement KeyAuth | Niveau à mettre |
|---|---|---|
| Woofer | `Spoofer` | **101** |
| PaiPai Val + Emulator | `Valorant` | **102** |
| Roblox | `Roblox` | **103** |
| Windows PaiPai | `WindowsPai` | **104** |

Un mauvais niveau = une clé qui débloque le mauvais produit (ou rien).

### 3. À chaque vente

Dans le ticket, un bouton **« Payment received → deliver »** (Staff uniquement,
ou `/deliver`). Un clic → menu des formules du produit → le bot génère la clé,
la poste dans le ticket, l'envoie en MP, donne le rôle du produit et
enregistre la vente. L'acheteur colle la clé dans PaiPai (*Add license*, ou à
l'inscription) et son produit se débloque.

L'historique est consultable depuis Bot Manager → **Payments** → **Sales**
(30 dernières livraisons). Les clés y sont **masquées** (4 derniers
caractères) : `data/store.json` est lisible via l'API de contrôle, une copie ne
doit pas fournir un stock de licences valides.

### Pourquoi un clic et pas zéro

PayPal **Friends & Family** n'émet aucune notification marchande exploitable
(pas de webhook, c'est un virement entre proches), et un paiement crypto
demanderait de surveiller la chaîne et de faire correspondre un montant exact
à un acheteur. Un « paiement reçu » deviné à tort, c'est une licence offerte.
La confirmation humaine est donc le seul maillon conservé — tout ce qui suit
est automatique. Pour une détection réelle, il faut un encaissement qui
prévient le vendeur (PayPal *Goods & Services* via son API, un processeur
crypto type NOWPayments/BTCPay avec webhook) : c'est une évolution possible,
pas un réglage.

## Créer un compte PaiPai sans clé

Sur l'onglet **Sign Up** de l'application, un lien « No license key? Create a
free account » crée le compte sans rien demander d'autre qu'un identifiant et
un mot de passe. Le produit reste verrouillé : le compte sert à se connecter,
et la licence achetée s'ajoute ensuite via **Add license**.

**Pourquoi ça passe par le bot :** KeyAuth n'accepte aucune inscription sans
licence (`register(user, pass, key)` exige toujours une clé). Générer une clé
demande la clé **vendeur** — impossible à mettre dans l'exe, elle serait
extraite et n'importe qui pourrait alors supprimer toutes tes licences. Le bot
la garde donc chez l'hébergeur et l'appli ne reçoit qu'une clé à usage unique
via `POST /signup-key`. Le mot de passe, lui, ne passe jamais par le bot : il
part directement de l'appli vers KeyAuth en HTTPS.

**À configurer une fois** (en plus de `KEYAUTH_SELLER_KEY`) dans le `.env` du
bot :

```
KEYAUTH_FREE_LEVEL=1
KEYAUTH_FREE_DAYS=0
```

`1` est le niveau de l'abonnement `default` de KeyAuth, qui n'est utilisé par
**aucun** produit PaiPai (les produits vivent aux niveaux 101 à 105) : un
compte gratuit peut donc se connecter sans rien débloquer, sans avoir à créer
quoi que ce soit sur le dashboard. `0` jour = clé à vie (le compte ne débloque
rien, autant ne pas le faire expirer).

⚠️ Si tu mets ici le niveau d'un vrai produit (101-104), la route publique
distribue ce produit gratuitement à qui la connaît. Sans ces variables,
l'inscription sans clé est simplement désactivée et l'appli l'explique à
l'utilisateur.

**Garde-fous :** la route est publique (celui qui s'inscrit n'a pas encore de
compte), donc elle est limitée à **3 comptes par heure et par IP** et
**100 par jour** au total, et chaque délivrance est journalisée. Dépassé, le
message invite simplement à réessayer plus tard.

## Commandes disponibles (réservées au Staff / Administrateur)

| Commande | Rôle requis | Effet |
|---|---|---|
| `/setup-server` | Administrateur | Crée/complète toute la structure du serveur |
| `/post-products` | Gérer le serveur | Reposte la présentation des produits |
| `/status-set produit: état:` | Gérer le serveur | Met à jour la page #statut |
| `/update-post title: changelog: [product:] [version:] [note:]` | Gérer le serveur | Publie une update dans #updates |
| `/announce titre: message: ping:` | Gérer le serveur | Publie une annonce dans #annonces |
| `/payment-post` | Gérer le serveur | Republie les moyens de paiement dans le salon courant (à utiliser dans un ticket) |
| `/deliver` | Gérer les messages + rôle Staff | Livre une licence dans le ticket courant (même menu que le bouton) |
| `/link` | Rôle PaiPai / PeiPei | Génère un code pour piloter le bot depuis PaiPai |
| `/unlink` | Rôle PaiPai / PeiPei | Révoque tous ses accès PaiPai |

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

Deux chemins, au choix :

**Depuis PaiPai (recommandé, aucun redéploiement).** Bot Manager →
**Products** : la liste des produits arrive en direct du bot. `Edit` pour
retoucher un texte (nom, tagline, description, tarifs, livraison, note,
site), `+ New product` pour en créer un. À l'enregistrement, le bot
republie la fiche du produit concerné, rafraîchit l'annuaire `#products`
et la page `#status` ; à la création il crée aussi le salon du produit et
le rôle correspondant. Demande un bot **hébergé** avec l'URL de contrôle
renseignée (c'est le bot qui détient le catalogue).

Ces modifications vivent dans `data/store.json` (`productOverrides` pour
les produits intégrés, `customProducts` pour les créations) : elles
**survivent à un redéploiement du code**, contrairement à un fichier de
config édité à chaud chez l'hébergeur.

⚠️ Un produit créé ici n'existe **que côté Discord** (fiche, salon, rôle,
ticket). Il ne crée aucun abonnement KeyAuth ni aucune carte dans
l'application PaiPai — ça reste à faire de ton côté.

**Dans le code.** Édite `config/products.js` (nom, description, image,
statut par défaut) puis relance `/post-products`. C'est le seul chemin
pour donner un **visuel dédié** à un produit : les produits créés depuis
PaiPai retombent sur le logo PaiPai, l'API de contrôle ne transporte pas
de fichier image. Les images vivent dans `assets/` (copiées depuis les
visuels de l'application pour rester cohérent).

Note : les menus déroulants des commandes slash (`/status-set`,
`/update-post`) sont figés à l'enregistrement des commandes — un produit
créé depuis PaiPai n'y apparaît qu'après un **redémarrage** du bot. Le
menu des tickets et le Bot Manager, eux, le voient immédiatement.

## Donner accès au Staff

Dans Discord, assigne le rôle **Staff** (créé par `/setup-server`) aux
personnes qui doivent gérer les tickets/annonces/statuts/updates.
