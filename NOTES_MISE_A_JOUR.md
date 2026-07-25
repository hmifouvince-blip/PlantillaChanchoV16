# Notes de mise à jour

## 2026-07-25 — Nouveau logo PaiPai

Le logo de l'application a été remplacé (icône fleur de sakura). Après avoir
récupéré cette mise à jour (`git pull origin master`), fais ce nettoyage pour
être sûr de voir la bonne icône partout :

### 1. Ferme tout
Ferme complètement Visual Studio et l'application si elle tourne.

### 2. Nettoie les anciens résultats de compilation
`bin/` et `obj/` ne sont **pas** suivis par git — si tu avais déjà compilé le
projet avant de récupérer cette mise à jour, tes fichiers `.exe` locaux
(surtout en config **Release**) peuvent encore contenir l'ancienne icône tant
qu'ils ne sont pas recompilés.

```powershell
cd PlantillaChanchoV16
Remove-Item -Recurse -Force bin, obj -ErrorAction SilentlyContinue
```

### 3. Recompile en Debug ET en Release
```powershell
dotnet build PlantillaChanchoV16.csproj
dotnet build PlantillaChanchoV16.csproj -c Release
```

### 4. Si l'ancienne icône persiste dans la barre des tâches Windows
C'est le cache d'icônes de Windows (pas le projet) — à vider :

```powershell
taskkill /f /im explorer.exe
Remove-Item "$env:LOCALAPPDATA\IconCache.db" -Force -ErrorAction SilentlyContinue
Remove-Item "$env:LOCALAPPDATA\Microsoft\Windows\Explorer\iconcache*" -Force -ErrorAction SilentlyContinue
Start-Process explorer.exe
```

### 5. Relance
Relance le projet depuis Visual Studio — la nouvelle icône (fleur de sakura
rose) doit maintenant s'afficher partout (nav, écran de connexion, barre des
tâches).
