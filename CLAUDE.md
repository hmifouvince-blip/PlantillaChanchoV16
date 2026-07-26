# Règles du projet PaiPai

Ce projet (`PlantillaChanchoV16/`) est développé en parallèle par plusieurs
personnes, chacune avec son propre Claude Code sur sa machine. Ces règles
s'appliquent à **toute session Claude Code** travaillant sur ce dépôt, quel
que soit qui l'utilise.

## 1. Workflow de collaboration Git

- **Au début de CHAQUE session**, avant de lire ou modifier quoi que ce soit :
  `git pull origin master` (un autre développeur a pu pousser des changements
  entre-temps).
- Si le pull ramène des commits : résume ce qui a changé (quels fichiers, quel
  comportement), relance `dotnet build` pour vérifier que le projet compile
  toujours, et signale immédiatement tout conflit avec le travail en cours.
- **Après** une modification à livrer : `git add -A`, `git commit` (message
  clair), `git push origin master`.
- Après un push réussi : dire explicitement à l'utilisateur ce qui vient d'être
  poussé, pour qu'il puisse prévenir l'autre développeur qu'il y a du nouveau à
  récupérer.
- Si le push est refusé (rejected/non-fast-forward) : `git pull` d'abord,
  résous un éventuel conflit, puis repousse.
- Ne jamais committer `bin/` ou `obj/` (déjà exclus par `.gitignore`).
- Prévenir l'utilisateur avant tout `git push`.
- Travailler sur `master` (branche partagée). Un travail resté sur une branche
  locale ou non committé est invisible pour l'autre développeur.

## 2. Identité de l'application (à chaque mise à jour livrée)

Avant de pousser une mise à jour de code (pas pour une simple correction de
typo/doc), dans `PlantillaChanchoV16/PlantillaChanchoV16.csproj` :

1. **Incrémente la version** de +0.1 sur les trois propriétés EN MÊME TEMPS
   (ex: `3.5.0.0` -> `3.6.0.0`) :
   ```xml
   <Version>X.Y.0.0</Version>
   <AssemblyVersion>X.Y.0.0</AssemblyVersion>
   <FileVersion>X.Y.0.0</FileVersion>
   ```
   Ne touche jamais le premier chiffre (X) sans demande explicite.

2. **Vérifie que ces propriétés existent toujours et valent "PaiPai"** (ne
   les supprime jamais) :
   ```xml
   <AssemblyTitle>PaiPai</AssemblyTitle>
   <Product>PaiPai</Product>
   <Company>PaiPai</Company>
   <Description>PaiPai</Description>
   ```
   ⚠️ **Piège déjà rencontré** : le champ "Description du fichier" que
   Windows affiche (clic droit sur l'exe → Propriétés → Détails) est rempli
   par `AssemblyTitle`, **pas** par `Description` seule. Sans `AssemblyTitle`,
   Windows affiche le nom brut du projet ("PlantillaChanchoV16") au lieu de
   "PaiPai".

3. **Recompile ET republie les DEUX configurations** (jamais une seule,
   sinon l'autre reste périmée avec l'ancienne version/icône) :
   ```
   dotnet build PlantillaChanchoV16/PlantillaChanchoV16.csproj
   dotnet publish PlantillaChanchoV16/PlantillaChanchoV16.csproj -c Release -r win-x64 -p:PublishSingleFile=true
   ```

4. **Vérifie le résultat** avant de committer (doit afficher "PaiPai"
   partout et le bon numéro de version, sans hash Git en suffixe) :
   ```powershell
   [System.Diagnostics.FileVersionInfo]::GetVersionInfo("PlantillaChanchoV16/bin/Debug/net7.0-windows7.0/PaiPai.exe")
   ```

5. Inclus le `.csproj` modifié dans le commit avec le reste du changement.

## 3. Après un changement d'icône ou de version

`bin/` et `obj/` ne sont pas suivis par git — chaque développeur doit
recompiler **les deux configurations** après avoir récupéré un tel
changement, sinon son ancien exe local (surtout en Release) peut encore
afficher l'ancienne icône/version. Voir `NOTES_MISE_A_JOUR.md` à la racine
du dépôt pour la procédure de nettoyage complète (y compris le cache
d'icônes Windows si besoin).

## 4. Conventions de code

- Tous les commentaires du code sont en **français**. Continue dans cette
  langue.
- Un commentaire explique le **pourquoi** (piège, contrainte cachée), jamais
  le quoi (le code lisible n'a pas besoin de paraphrase).
- Sur un `Guna2Button` icône+texte : toujours `ImageAlign` ET `TextAlign` du
  **même côté** (Left/Left). Left/Right fait chevaucher l'icône et le texte.
- Ne jamais toucher aux identifiants KeyAuth dans `Login.cs` (name, ownerid)
  sauf demande explicite.
- Après chaque modification, compile et vérifie 0 erreur (`error CS`) :
  ```
  dotnet build PlantillaChanchoV16/PlantillaChanchoV16.csproj
  ```
