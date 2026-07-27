using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace PlantillaChanchoV16.Products
{
    // =====================================================================================
    //  WINDOWS PAIPAI — TWEAKS D'OPTIMISATION + DIAGNOSTIC
    // -------------------------------------------------------------------------------------
    //  Chaque outil = { Id, Nom, Description, Check, Command }.
    //
    //    Check   = script PowerShell de DIAGNOSTIC (lecture seule). Il doit renvoyer une
    //              seule chaine "NIVEAU|message" ou NIVEAU vaut OK / INFO / WARN / CRIT.
    //              C'est ce qui permet de n'appliquer un tweak que s'il sert vraiment :
    //              inutile de deplacer le fichier d'echange s'il est deja sur un SSD.
    //    Command = script PowerShell d'ACTION. Laisse-le a null pour un outil qui ne fait
    //              que diagnostiquer (voir AdviceOnly plus bas).
    //
    //  AdviceOnly = true : PaiPai DETECTE le probleme mais ne le corrige JAMAIS tout seul.
    //  Reserve a ce qui touche a la securite (antivirus, pare-feu, Memory Integrity) ou au
    //  materiel. On informe l'utilisateur, il decide. On ne baisse pas la protection d'une
    //  machine a sa place.
    //
    //  Pour AJOUTER un outil : copie un bloc, change l'Id (unique), le nom et les scripts.
    //  Teste toujours la commande dans un PowerShell admin avant de la mettre ici.
    // =====================================================================================
    internal static class WindowsPaiTweaks
    {
        public enum Severity { Ok, Info, Warn, Crit, Unknown }

        public class Tweak
        {
            public string Id;                  // identifiant unique, sert a relier check et resultat
            public string Name;
            public string Description;
            public string Command;             // action (null = diagnostic seul)
            public string Check;               // diagnostic, renvoie "NIVEAU|message"
            public bool NeedsConfirm;
            public bool NeedsReboot;           // l'effet n'est reel qu'apres redemarrage
            public bool AdviceOnly;            // detecte mais jamais applique automatiquement
            public string Category = "Performance";
        }

        public class Finding
        {
            public Severity Level = Severity.Unknown;
            public string Message = "";
            // Vrai si le diagnostic a trouve un probleme qui vaut la peine d'etre corrige.
            public bool IsProblem => Level == Severity.Warn || Level == Severity.Crit;
        }

        public static readonly Tweak[] Tweaks =
        {
            // =============================== STOCKAGE ===============================
            new Tweak {
                Id = "pagefile",
                Name = "Fichier d'echange sur le disque le plus rapide",
                Description = "Un pagefile sur disque mecanique fige tout le systeme a chaque acces.",
                Category = "Performance",
                NeedsConfirm = true,
                NeedsReboot = true,
                Check = @"
$bad=@(); $ok=@()
foreach($p in (Get-CimInstance Win32_PageFileUsage -EA SilentlyContinue)){
  $L=$p.Name.Substring(0,1)
  $part=Get-Partition -DriveLetter $L -EA SilentlyContinue
  if($part){
    $d=Get-PhysicalDisk -EA SilentlyContinue | Where-Object { $_.DeviceId -eq ([string]$part.DiskNumber) }
    if($d -and $d.MediaType -eq 'HDD'){ $bad+=""$L`: ($($d.FriendlyName))"" } else { $ok+=""$L`:"" }
  }
}
if($bad.Count -gt 0){ ""CRIT|Fichier d'echange sur disque mecanique : $($bad -join ', '). Freezes de plusieurs secondes garantis."" }
elseif($ok.Count -eq 0){ ""WARN|Aucun fichier d'echange configure."" }
else{ ""OK|Fichier d'echange sur SSD ($($ok -join ', "")))."" }",
                Command = @"
# Choisit le meilleur SSD : NVMe > SATA SSD, et au moins 20 Go libres.
$best=$null; $bestScore=-1
foreach($v in (Get-Volume | Where-Object { $_.DriveLetter -and $_.FileSystem -eq 'NTFS' })){
  $part=Get-Partition -DriveLetter $v.DriveLetter -EA SilentlyContinue
  if(-not $part){ continue }
  $d=Get-PhysicalDisk -EA SilentlyContinue | Where-Object { $_.DeviceId -eq ([string]$part.DiskNumber) }
  if(-not $d -or $d.MediaType -eq 'HDD'){ continue }
  if($v.SizeRemaining -lt 20GB){ continue }
  $score = if($d.BusType -eq 'NVMe'){ 3 } else { 2 }
  if($score -gt $bestScore -or ($score -eq $bestScore -and $v.SizeRemaining -gt $best.SizeRemaining)){
    $bestScore=$score; $best=$v
  }
}
if(-not $best){ Write-Output 'Aucun SSD avec 20 Go libres : rien change.'; exit }
$cs=Get-CimInstance Win32_ComputerSystem
if($cs.AutomaticManagedPagefile){ Set-CimInstance -InputObject $cs -Property @{AutomaticManagedPagefile=$false} }
Get-CimInstance Win32_PageFileSetting | ForEach-Object { Remove-CimInstance -InputObject $_ }
$pf=([wmiclass]'Win32_PageFileSetting').CreateInstance()
$pf.Name=""$($best.DriveLetter):\pagefile.sys""
$pf.InitialSize=[uint32]4096
$pf.MaximumSize=[uint32]16384
$null=$pf.Put()
Write-Output ""Fichier d'echange place sur $($best.DriveLetter): (4-16 Go). Actif au redemarrage.""",
            },

            new Tweak {
                Id = "diskspace",
                Name = "Liberer de l'espace sur le disque systeme",
                Description = "Sous 15% libre, un SSD s'effondre en ecriture et le systeme micro-freeze.",
                Category = "Cleaning",
                Check = @"
$v=Get-Volume -DriveLetter $env:SystemDrive.Substring(0,1) -EA SilentlyContinue
if(-not $v){ ""INFO|Disque systeme illisible."" }
else{
  $p=[math]::Round(($v.SizeRemaining/$v.Size)*100,1)
  $g=[math]::Round($v.SizeRemaining/1GB,1)
  if($p -lt 10){ ""CRIT|Disque systeme a $p% libre ($g Go) : ecritures effondrees."" }
  elseif($p -lt 15){ ""WARN|Disque systeme a $p% libre ($g Go)."" }
  else{ ""OK|Disque systeme a $p% libre ($g Go)."" }
}",
                Command = @"
$before=(Get-Volume -DriveLetter $env:SystemDrive.Substring(0,1)).SizeRemaining
$paths=@(""$env:TEMP"",""$env:SystemRoot\Temp"",""$env:LOCALAPPDATA\CrashDumps"",""$env:SystemRoot\SoftwareDistribution\Download"",""$env:LOCALAPPDATA\Microsoft\Windows\INetCache"")
foreach($p in $paths){ if(Test-Path $p){ Get-ChildItem $p -Force -EA SilentlyContinue | Remove-Item -Recurse -Force -EA SilentlyContinue } }
Clear-RecycleBin -Force -EA SilentlyContinue
$after=(Get-Volume -DriveLetter $env:SystemDrive.Substring(0,1)).SizeRemaining
Write-Output ""Nettoyage termine : $([math]::Round(($after-$before)/1MB,0)) Mo liberes.""",
            },

            new Tweak {
                Id = "shadercache",
                Name = "Purger le cache de shaders GPU",
                Description = "Un cache gonfle ou corrompu provoque exactement les stutters en jeu.",
                Category = "Cleaning",
                // Couvre les trois fabricants de GPU : on ne suppose pas que l'utilisateur
                // a la meme carte que nous. Les dossiers absents sont simplement ignores.
                Check = @"
$paths=@(""$env:LOCALAPPDATA\AMD\DxcCache"",""$env:LOCALAPPDATA\AMD\DxCache"",""$env:LOCALAPPDATA\AMD\GLCache"",""$env:LOCALAPPDATA\AMD\VkCache"",""$env:LOCALAPPDATA\AMD\OglCache"",""$env:LOCALAPPDATA\NVIDIA\DXCache"",""$env:LOCALAPPDATA\NVIDIA\GLCache"",""$env:LOCALAPPDATA\NVIDIA Corporation\NV_Cache"",""$env:LOCALAPPDATA\Intel\ShaderCache"",""$env:LOCALAPPDATA\D3DSCache"")
$t=0; $who=@()
foreach($p in $paths){ if(Test-Path $p){ $s=(Get-ChildItem $p -Recurse -File -Force -EA SilentlyContinue | Measure-Object Length -Sum).Sum; if($s -gt 100MB){ $who+=(Split-Path $p -Leaf) }; if($s){ $t+=$s } } }
$gb=[math]::Round($t/1GB,2)
$d=if($who.Count -gt 0){ "" ($($who -join ', '))"" }else{ '' }
if($gb -gt 4){ ""CRIT|Cache shaders GPU : $gb Go$d (normal : moins de 1 Go). Cause classique de stutters."" }
elseif($gb -gt 1){ ""WARN|Cache shaders GPU : $gb Go$d."" }
else{ ""OK|Cache shaders GPU : $gb Go."" }",
                Command = @"
$paths=@(""$env:LOCALAPPDATA\AMD\DxcCache"",""$env:LOCALAPPDATA\AMD\DxCache"",""$env:LOCALAPPDATA\AMD\GLCache"",""$env:LOCALAPPDATA\AMD\VkCache"",""$env:LOCALAPPDATA\AMD\OglCache"",""$env:LOCALAPPDATA\NVIDIA\DXCache"",""$env:LOCALAPPDATA\NVIDIA\GLCache"",""$env:LOCALAPPDATA\NVIDIA Corporation\NV_Cache"",""$env:LOCALAPPDATA\Intel\ShaderCache"",""$env:LOCALAPPDATA\D3DSCache"")
$t=0
foreach($p in $paths){ if(Test-Path $p){ $s=(Get-ChildItem $p -Recurse -File -Force -EA SilentlyContinue | Measure-Object Length -Sum).Sum; if($s){ $t+=$s }; Get-ChildItem $p -Force -EA SilentlyContinue | Remove-Item -Recurse -Force -EA SilentlyContinue } }
Write-Output ""Cache shaders purge : $([math]::Round($t/1GB,2)) Go liberes. La 1ere partie recompilera les shaders.""",
            },

            new Tweak {
                Id = "winsxs",
                Name = "Nettoyer le magasin de composants Windows",
                Description = "Supprime les anciennes versions de composants gardees par Windows.",
                Category = "Cleaning",
                NeedsConfirm = true,
                Check = @"
$o = & Dism.exe /Online /Cleanup-Image /AnalyzeComponentStore 2>&1 | Out-String
if($o -match '(?i)recommend\w*\s*:\s*(Oui|Yes)'){ ""WARN|Nettoyage du magasin de composants recommande par Windows."" }
elseif($o -match '(?i)(Oui|Yes|Non|No)'){ ""OK|Magasin de composants deja propre."" }
else{ ""INFO|Etat du magasin de composants indetermine."" }",
                Command = @"
& Dism.exe /Online /Cleanup-Image /StartComponentCleanup | Out-Null
Write-Output 'Magasin de composants nettoye.'",
            },

            new Tweak {
                Id = "trim",
                Name = "Activer et lancer le TRIM des SSD",
                Description = "Sans TRIM, les ecritures d'un SSD ralentissent progressivement.",
                Category = "Performance",
                Check = @"
$o = & fsutil.exe behavior query DisableDeleteNotify 2>&1 | Out-String
if($o -match 'NTFS\D*1'){ ""WARN|TRIM desactive : les SSD vont ralentir."" } else { ""OK|TRIM actif."" }",
                Command = @"
& fsutil.exe behavior set DisableDeleteNotify 0 | Out-Null
$n=0
foreach($v in (Get-Volume | Where-Object { $_.DriveLetter -and $_.FileSystem -eq 'NTFS' })){
  $part=Get-Partition -DriveLetter $v.DriveLetter -EA SilentlyContinue
  if(-not $part){ continue }
  $d=Get-PhysicalDisk -EA SilentlyContinue | Where-Object { $_.DeviceId -eq ([string]$part.DiskNumber) }
  if($d -and $d.MediaType -ne 'HDD'){ Optimize-Volume -DriveLetter $v.DriveLetter -ReTrim -EA SilentlyContinue; $n++ }
}
Write-Output ""TRIM active et execute sur $n SSD.""",
            },

            new Tweak {
                Id = "hibernation",
                Name = "Desactiver l'hibernation",
                Description = "Libere un fichier hiberfil.sys de la taille de la RAM sur le disque systeme.",
                Category = "Cleaning",
                NeedsConfirm = true,
                Check = @"
if(Test-Path ""$env:SystemDrive\hiberfil.sys""){
  $g=[math]::Round((Get-Item ""$env:SystemDrive\hiberfil.sys"" -Force).Length/1GB,1)
  ""WARN|hiberfil.sys occupe $g Go sur le disque systeme.""
} else { ""OK|Hibernation deja desactivee."" }",
                Command = @"& powercfg.exe /hibernate off | Out-Null; Write-Output 'Hibernation desactivee, hiberfil.sys supprime.'",
            },

            // =============================== ALIMENTATION ===============================
            new Tweak {
                Id = "powerplan",
                Name = "Plan d'alimentation hautes performances",
                Description = "Active Performances optimales et empeche le CPU de descendre en frequence.",
                Category = "Performance",
                Check = @"
$a=(& powercfg.exe /getactivescheme) -join ' '
if($a -match '8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c' -or $a -match 'e9a42b02-d5df-448d-aa00-03f14749eb61'){ ""OK|Plan d'alimentation deja performant."" }
else{ ""WARN|Plan d'alimentation en mode economie/equilibre : le CPU est bride."" }",
                Command = @"
$ult='e9a42b02-d5df-448d-aa00-03f14749eb61'
if(-not ((& powercfg.exe /list) -join ' ' -match $ult)){ & powercfg.exe -duplicatescheme $ult | Out-Null }
$target = if(((& powercfg.exe /list) -join ' ') -match $ult){ $ult } else { '8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c' }
& powercfg.exe /setactive $target
& powercfg.exe /setacvalueindex $target SUB_PROCESSOR PROCTHROTTLEMIN 100 | Out-Null
& powercfg.exe /setacvalueindex $target SUB_PCIEXPRESS ASPM 0 | Out-Null
& powercfg.exe /setacvalueindex $target 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 0 | Out-Null
& powercfg.exe /setactive $target
Write-Output 'Plan hautes performances actif (CPU min 100%, ASPM off, USB selective suspend off).'",
            },

            new Tweak {
                Id = "diskidle",
                Name = "Empecher la mise en veille des disques",
                Description = "Un disque qui se rendort fige le PC 2 a 4 s le temps de redemarrer.",
                Category = "Performance",
                Check = @"
$g=((& powercfg.exe /getactivescheme) -join ' ')
if($g -match '([a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12})'){ $id=$Matches[1] } else { $id=$null }
if(-not $id){ ""INFO|Plan d'alimentation illisible."" }
else{
  $q=(& powercfg.exe /query $id SUB_DISK DISKIDLE) -join ""`n""
  $m=[regex]::Matches($q,'(?im)^\s*.*(?:courant alternatif|AC Power Setting).*:\s*0x([0-9a-f]+)')
  if($m.Count -gt 0 -and [Convert]::ToInt32($m[0].Groups[1].Value,16) -ne 0){ ""WARN|Les disques ont le droit de se mettre en veille."" }
  else{ ""OK|Mise en veille des disques desactivee."" }
}",
                Command = @"
& powercfg.exe /setacvalueindex SCHEME_CURRENT SUB_DISK DISKIDLE 0 | Out-Null
& powercfg.exe /setdcvalueindex SCHEME_CURRENT SUB_DISK DISKIDLE 0 | Out-Null
& powercfg.exe /setactive SCHEME_CURRENT
Write-Output 'Les disques ne se mettront plus en veille.'",
            },

            // =============================== JEU / LATENCE ===============================
            new Tweak {
                Id = "gamedvr",
                Name = "Desactiver Xbox Game Bar et Game DVR",
                Description = "La capture en arriere-plan coute des FPS et provoque des stutters.",
                Category = "Performance",
                Check = @"
$v=Get-ItemProperty 'HKCU:\System\GameConfigStore' -Name GameDVR_Enabled -EA SilentlyContinue
if($null -eq $v -or $v.GameDVR_Enabled -ne 0){ ""WARN|Game DVR actif : capture permanente en arriere-plan."" }
else{ ""OK|Game DVR desactive."" }",
                Command = @"
New-Item -Path 'HKCU:\System\GameConfigStore' -Force | Out-Null
Set-ItemProperty 'HKCU:\System\GameConfigStore' -Name GameDVR_Enabled -Value 0 -Type DWord
Set-ItemProperty 'HKCU:\System\GameConfigStore' -Name GameDVR_FSEBehaviorMode -Value 2 -Type DWord
Set-ItemProperty 'HKCU:\System\GameConfigStore' -Name GameDVR_HonorUserFSEBehaviorMode -Value 1 -Type DWord
New-Item -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\GameDVR' -Force | Out-Null
Set-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\GameDVR' -Name AllowGameDVR -Value 0 -Type DWord
New-Item -Path 'HKCU:\Software\Microsoft\GameBar' -Force | Out-Null
Set-ItemProperty 'HKCU:\Software\Microsoft\GameBar' -Name UseNexusForGameBarEnabled -Value 0 -Type DWord
Set-ItemProperty 'HKCU:\Software\Microsoft\GameBar' -Name AutoGameModeEnabled -Value 1 -Type DWord
Write-Output 'Game Bar et Game DVR desactives (Game Mode conserve).'",
            },

            new Tweak {
                Id = "mmcss",
                Name = "Priorite systeme aux jeux (MMCSS)",
                // Honnetete sur l'ampleur : les mesures independantes montrent que
                // NetworkThrottlingIndex ne fait plus rien sur Windows 11 moderne, et que le
                // profil MMCSS "Games" a un effet reel mais modeste. On l'applique parce que
                // c'est sans risque et reversible, pas parce que ca double les FPS. Les vrais
                // gains sont ailleurs : frequence d'ecran, jeu sur SSD, Memory Integrity, RAM.
                Description = "Profil de priorite CPU/GPU pour les jeux. Effet reel mais modeste.",
                Category = "Performance",
                Check = @"
$sp='HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile'
$g=(Get-ItemProperty ""$sp\Tasks\Games"" -Name 'GPU Priority' -EA SilentlyContinue).'GPU Priority'
if($g -ne 8){ ""INFO|Profil de priorite jeux non applique. Gain modeste : les gros leviers sont ailleurs dans cette liste."" }
else{ ""OK|Profil de priorite jeux deja applique."" }",
                Command = @"
$sp='HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile'
Set-ItemProperty $sp -Name SystemResponsiveness -Value 10 -Type DWord
Set-ItemProperty $sp -Name NetworkThrottlingIndex -Value 0xffffffff -Type DWord
New-Item -Path ""$sp\Tasks\Games"" -Force | Out-Null
Set-ItemProperty ""$sp\Tasks\Games"" -Name 'GPU Priority' -Value 8 -Type DWord
Set-ItemProperty ""$sp\Tasks\Games"" -Name 'Priority' -Value 6 -Type DWord
Set-ItemProperty ""$sp\Tasks\Games"" -Name 'Scheduling Category' -Value 'High' -Type String
Set-ItemProperty ""$sp\Tasks\Games"" -Name 'SFIO Priority' -Value 'High' -Type String
Write-Output 'Priorite jeux appliquee, bridage reseau multimedia desactive.'",
            },

            new Tweak {
                Id = "visualfx",
                Name = "Reactivite de l'interface",
                Description = "Coupe transparence et animations : navigation Windows plus nette.",
                Category = "Performance",
                Check = @"
$t=(Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize' -Name EnableTransparency -EA SilentlyContinue).EnableTransparency
if($t -ne 0){ ""INFO|Transparence et animations actives : cout GPU permanent sur le bureau."" }
else{ ""OK|Effets visuels deja allegees."" }",
                Command = @"
Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize' -Name EnableTransparency -Value 0 -Type DWord
Set-ItemProperty 'HKCU:\Control Panel\Desktop\WindowMetrics' -Name MinAnimate -Value 0 -Type String
Set-ItemProperty 'HKCU:\Control Panel\Desktop' -Name MenuShowDelay -Value 0 -Type String
Write-Output 'Transparence et animations desactivees.'",
            },

            // =============================== RESEAU ===============================
            new Tweak {
                Id = "hostsbom",
                Name = "Reparer le fichier hosts",
                Description = "Un BOM UTF-8 rend le fichier illisible : le DNS echoue a chaque resolution.",
                Category = "Network",
                Check = @"
$h=""$env:SystemRoot\System32\drivers\etc\hosts""
if(-not (Test-Path $h)){ ""OK|Pas de fichier hosts."" }
else{
  $b=[System.IO.File]::ReadAllBytes($h)
  if($b.Length -ge 3 -and $b[0] -eq 0xEF -and $b[1] -eq 0xBB -and $b[2] -eq 0xBF){
    $n=(Get-WinEvent -FilterHashtable @{LogName='System';ProviderName='Microsoft-Windows-DNS-Client';Id=1012;StartTime=(Get-Date).AddDays(-7)} -EA SilentlyContinue).Count
    ""WARN|Fichier hosts avec BOM UTF-8 : illisible par le client DNS ($n erreurs en 7 jours).""
  } else { ""OK|Fichier hosts lisible."" }
}",
                Command = @"
$h=""$env:SystemRoot\System32\drivers\etc\hosts""
Copy-Item $h ""$h.paipai-backup"" -Force -EA SilentlyContinue
$c=[System.IO.File]::ReadAllText($h)
if($c.Length -gt 0 -and $c[0] -eq [char]0xFEFF){ $c=$c.Substring(1) }
[System.IO.File]::WriteAllText($h,$c,(New-Object System.Text.UTF8Encoding($false)))
Clear-DnsClientCache
Write-Output 'Fichier hosts reecrit sans BOM (sauvegarde en hosts.paipai-backup), cache DNS vide.'",
            },

            new Tweak {
                Id = "nicpower",
                Name = "Couper l'economie d'energie des cartes reseau",
                Description = "Windows endort la carte reseau : micro-coupures et pics de latence.",
                Category = "Network",
                Check = @"
$base='HKLM:\SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}'
$n=0
Get-ChildItem $base -EA SilentlyContinue | Where-Object { $_.PSChildName -match '^\d{4}$' } | ForEach-Object {
  $p=Get-ItemProperty $_.PSPath -EA SilentlyContinue
  if($p.DriverDesc -and $p.DriverDesc -notmatch 'WAN Miniport|Kernel Debug'){
    if($null -eq $p.PnPCapabilities -or $p.PnPCapabilities -ne 24){ $n++ }
  }
}
if($n -gt 0){ ""WARN|$n carte(s) reseau peuvent etre mises en veille par Windows."" } else { ""OK|Economie d'energie reseau deja desactivee."" }",
                Command = @"
$base='HKLM:\SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}'
$n=0
Get-ChildItem $base -EA SilentlyContinue | Where-Object { $_.PSChildName -match '^\d{4}$' } | ForEach-Object {
  $p=Get-ItemProperty $_.PSPath -EA SilentlyContinue
  if($p.DriverDesc -and $p.DriverDesc -notmatch 'WAN Miniport|Kernel Debug'){
    Set-ItemProperty $_.PSPath -Name PnPCapabilities -Value 24 -Type DWord -EA SilentlyContinue; $n++
  }
}
Write-Output ""Economie d'energie desactivee sur $n carte(s) reseau.""",
            },

            new Tweak {
                Id = "tcptuning",
                Name = "Auto-tuning TCP",
                Description = "Retablit le reglage TCP standard (souvent casse par les outils d'optimisation).",
                Category = "Network",
                Check = @"
$o=(& netsh.exe int tcp show global) -join ' '
if($o -match '(?i)(normal)'){ ""OK|Auto-tuning TCP correct."" } else { ""WARN|Auto-tuning TCP non standard : debit instable."" }",
                Command = @"& netsh.exe int tcp set global autotuninglevel=normal | Out-Null; Write-Output 'Auto-tuning TCP retabli sur normal.'",
            },

            new Tweak {
                Id = "dnscache",
                Name = "Vider le cache DNS",
                Description = "Efface les resolutions memorisees (utile apres un changement reseau).",
                Category = "Network",
                Check = @"""INFO|Action ponctuelle, sans diagnostic prealable.""",
                Command = @"Clear-DnsClientCache; Write-Output 'Cache DNS vide.'",
            },

            // =============================== ARRIERE-PLAN ===============================
            new Tweak {
                Id = "ocsoftware",
                Name = "Desactiver les outils d'overclocking constructeur",
                Description = "Ils appliquent des profils CPU/RAM automatiques : cause frequente de BSOD.",
                Category = "Performance",
                NeedsConfirm = true,
                // Filet large sur les principaux constructeurs (Gigabyte, MSI, ASUS, ASRock,
                // EVGA, Intel XTU, AMD Ryzen Master) : on ne suppose pas la marque de la
                // machine, et on ne desactive que ce qui existe reellement dessus.
                Check = @"
$names=@('EasyTuneEngineService','OCButtonService','MyService1','GigabyteUpdateService','MSIAfterburnerService','MSI_Center_Service','AsusCertService','AsSysCtrlService','AISuite3','ASRockOCService','AXTUService','ASRUpdateService','EVGAPrecisionService','XTUService','XTU3SERVICE')
$f=@()
foreach($n in $names){ Get-Service $n -EA SilentlyContinue | Where-Object { $_.StartType -ne 'Disabled' } | ForEach-Object { $f+=$_.Name } }
$tasks=@('EasyTune','EasyTune 1','GraphicsCardEngine','AMDRyzenMasterSDKTask','StartCN','ASRockOCTuner')
foreach($t in $tasks){ $x=Get-ScheduledTask -TaskName $t -EA SilentlyContinue; if($x -and $x.State -ne 'Disabled'){ $f+=$t } }
if($f.Count -gt 0){ ""WARN|Outils d'overclocking actifs : $($f -join ', '). Ils appliquent des profils CPU/RAM automatiques."" } else { ""OK|Aucun outil d'overclocking actif."" }",
                Command = @"
$names=@('EasyTuneEngineService','OCButtonService','MyService1','GigabyteUpdateService','MSIAfterburnerService','MSI_Center_Service','AsusCertService','AsSysCtrlService','ASRockOCService','AXTUService','ASRUpdateService','EVGAPrecisionService','XTUService','XTU3SERVICE')
$d=@()
foreach($n in $names){
  Get-Service $n -EA SilentlyContinue | ForEach-Object {
    Stop-Service $_.Name -Force -EA SilentlyContinue
    Set-Service $_.Name -StartupType Disabled -EA SilentlyContinue
    $d+=$_.Name
  }
}
$tasks=@('EasyTune','EasyTune 1','GraphicsCardEngine','AMDRyzenMasterSDKTask','StartCN','ASRockOCTuner')
foreach($t in $tasks){ $x=Get-ScheduledTask -TaskName $t -EA SilentlyContinue; if($x){ Disable-ScheduledTask -TaskName $t -EA SilentlyContinue | Out-Null; $d+=$t } }
foreach($p in @('ApCent','EasyTune','MSIAfterburner','AISuite3','XTU')){ Get-Process $p -EA SilentlyContinue | Stop-Process -Force -EA SilentlyContinue }
if($d.Count -eq 0){ Write-Output 'Aucun outil d overclocking trouve sur cette machine.' }
else{ Write-Output ""$($d.Count) element(s) desactive(s) : $($d -join ', '). Reversible via services.msc."" }",
            },

            new Tweak {
                Id = "updaters",
                Name = "Mettre les updaters en demarrage manuel",
                Description = "Edge, Google, Office... tournent en permanence pour rien. Ils marchent toujours.",
                Category = "Performance",
                Check = @"
$names=@('edgeupdate','edgeupdatem','ClickToRunSvc','LGHUBUpdaterService','gupdate','gupdatem','BraveElevationService')
$f=@()
foreach($n in $names){ $s=Get-Service $n -EA SilentlyContinue; if($s -and $s.StartType -eq 'Automatic'){ $f+=$n } }
Get-Service -EA SilentlyContinue | Where-Object { $_.Name -like 'GoogleUpdater*' -and $_.StartType -eq 'Automatic' } | ForEach-Object { $f+=$_.Name }
if($f.Count -gt 0){ ""WARN|$($f.Count) updater(s) en demarrage automatique : $($f -join ', ')."" } else { ""OK|Aucun updater en demarrage automatique."" }",
                Command = @"
$names=@('edgeupdate','edgeupdatem','ClickToRunSvc','LGHUBUpdaterService','gupdate','gupdatem','BraveElevationService')
$n=0
foreach($s in $names){ if(Get-Service $s -EA SilentlyContinue){ Set-Service $s -StartupType Manual -EA SilentlyContinue; $n++ } }
Get-Service -EA SilentlyContinue | Where-Object { $_.Name -like 'GoogleUpdater*' } | ForEach-Object { Set-Service $_.Name -StartupType Manual -EA SilentlyContinue; $n++ }
foreach($t in @('MicrosoftEdgeUpdateTaskMachineCore','MicrosoftEdgeUpdateTaskMachineUA')){ if(Get-ScheduledTask -TaskName $t -EA SilentlyContinue){ Disable-ScheduledTask -TaskName $t -EA SilentlyContinue | Out-Null } }
Write-Output ""$n updater(s) passes en demarrage manuel. Ils se lanceront a l'ouverture de l'application.""",
            },

            new Tweak {
                Id = "bgapps",
                Name = "Couper les applis du Store en arriere-plan",
                Description = "Empeche les applications UWP de tourner sans avoir ete lancees.",
                Category = "Performance",
                Check = @"
$v=(Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications' -Name GlobalUserDisabled -EA SilentlyContinue).GlobalUserDisabled
if($v -ne 1){ ""WARN|Les applications du Store peuvent tourner en arriere-plan."" } else { ""OK|Applications en arriere-plan deja desactivees."" }",
                Command = @"
New-Item -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications' -Force | Out-Null
Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications' -Name GlobalUserDisabled -Value 1 -Type DWord
New-Item -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Search' -Force | Out-Null
Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Search' -Name BackgroundAppGlobalToggle -Value 0 -Type DWord
Write-Output 'Applications en arriere-plan desactivees.'",
            },

            new Tweak {
                Id = "suggestions",
                Name = "Couper les suggestions et installations auto",
                Description = "Windows installe et telecharge des applications suggerees sans le demander.",
                Category = "Privacy",
                Check = @"
$cdm='HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager'
$v=(Get-ItemProperty $cdm -Name SilentInstalledAppsEnabled -EA SilentlyContinue).SilentInstalledAppsEnabled
if($v -ne 0){ ""WARN|Installations silencieuses et suggestions actives."" } else { ""OK|Suggestions deja desactivees."" }",
                Command = @"
$cdm='HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager'
New-Item -Path $cdm -Force | Out-Null
foreach($v in @('SilentInstalledAppsEnabled','SoftLandingEnabled','SystemPaneSuggestionsEnabled','PreInstalledAppsEnabled','OemPreInstalledAppsEnabled','SubscribedContent-338388Enabled','SubscribedContent-338389Enabled','SubscribedContent-310093Enabled','FeatureManagementEnabled')){
  Set-ItemProperty $cdm -Name $v -Value 0 -Type DWord -EA SilentlyContinue
}
Write-Output 'Suggestions et installations silencieuses desactivees.'",
            },

            // =============================== AFFICHAGE ===============================
            new Tweak {
                Id = "displaycfg",
                Name = "Reparer la disposition des ecrans",
                Description = "Trop de configurations memorisees : Windows en choisit une au hasard au demarrage.",
                Category = "Performance",
                NeedsConfirm = true,
                NeedsReboot = true,
                Check = @"
$c=(Get-ChildItem 'HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Configuration' -EA SilentlyContinue).Count
$virt=(Get-PnpDevice -Class Display -EA SilentlyContinue | Where-Object { $_.InstanceId -like 'ROOT\DISPLAY*' -and $_.Status -eq 'OK' }).Count
if($c -gt 8 -or $virt -gt 0){ ""WARN|$c dispositions d'ecrans memorisees, $virt ecran(s) virtuel(s) actif(s) : la disposition change a chaque demarrage."" }
else{ ""OK|$c dispositions memorisees, aucun ecran virtuel."" }",
                Command = @"
$bk=""$env:USERPROFILE\Desktop\paipai-backup-ecrans.reg""
& reg.exe export 'HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Configuration' $bk /y | Out-Null
& reg.exe delete 'HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Configuration' /f | Out-Null
& reg.exe delete 'HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Connectivity' /f | Out-Null
Write-Output 'Dispositions memorisees purgees (sauvegarde sur le Bureau). Replace tes ecrans une derniere fois apres le redemarrage.'",
            },

            // =============================== INTEGRITE ===============================
            new Tweak {
                Id = "crashdump",
                Name = "Activer les rapports de plantage",
                Description = "Sans eux, impossible de savoir quel pilote fait planter le PC.",
                Category = "Performance",
                Check = @"
$c=Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\CrashControl' -EA SilentlyContinue
$dir=""$env:SystemRoot\Minidump""
$n=0; if(Test-Path $dir){ $n=(Get-ChildItem $dir -EA SilentlyContinue).Count }
$bs=(Get-WinEvent -FilterHashtable @{LogName='System';ProviderName='Microsoft-Windows-WER-SystemErrorReporting'} -MaxEvents 20 -EA SilentlyContinue).Count
if($c.CrashDumpEnabled -eq 0){ ""WARN|Rapports de plantage desactives."" }
elseif($bs -gt 0 -and $n -eq 0){ ""WARN|$bs ecran(s) bleu(s) enregistre(s) mais aucun rapport ecrit : diagnostic impossible."" }
else{ ""OK|Rapports de plantage configures."" }",
                Command = @"
Set-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\CrashControl' -Name CrashDumpEnabled -Value 7 -Type DWord
Set-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\CrashControl' -Name MinidumpDir -Value ""$env:SystemRoot\Minidump"" -Type ExpandString
Set-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\CrashControl' -Name MinidumpsCount -Value 20 -Type DWord
if(-not (Test-Path ""$env:SystemRoot\Minidump"")){ New-Item -ItemType Directory ""$env:SystemRoot\Minidump"" | Out-Null }
Write-Output 'Rapports de plantage actives : le prochain ecran bleu sera analysable.'",
            },

            new Tweak {
                Id = "sfc",
                Name = "Verifier et reparer les fichiers systeme",
                Description = "Detecte et repare les fichiers Windows corrompus (peut prendre 10 min).",
                Category = "Cleaning",
                NeedsConfirm = true,
                Check = @"""INFO|Verification longue : a lancer si le PC plante ou se comporte mal.""",
                Command = @"
$o = & sfc.exe /scannow 2>&1 | Out-String
$o = ($o -replace ""`0"","""")
if($o -match '(?i)(a repare|did repair|repaired them)'){ Write-Output 'Fichiers systeme corrompus trouves et repares.' }
elseif($o -match '(?i)(n.a trouve aucune|did not find any)'){ Write-Output 'Aucun fichier systeme corrompu.' }
else{ Write-Output 'Verification terminee (voir C:\Windows\Logs\CBS\CBS.log).' }",
            },

            // ====================== LES PLUS GROS GAINS REELS ======================
            //  Ces controles font une difference visible, contrairement a beaucoup de
            //  "tweaks FPS" du web. Un ecran 165 Hz bloque a 60 Hz ou un jeu installe sur
            //  disque mecanique coutent infiniment plus que n'importe quelle cle de registre.
            // =======================================================================
            new Tweak {
                Id = "refreshrate",
                Name = "Frequence d'affichage de l'ecran",
                Description = "Erreur tres frequente : un ecran 144/165/240 Hz laisse a 60 Hz par Windows.",
                Category = "Performance",
                AdviceOnly = true,
                Check = @"
$b=@()
foreach($v in (Get-CimInstance Win32_VideoController -EA SilentlyContinue | Where-Object { $_.CurrentRefreshRate -gt 0 })){
  if($v.MaxRefreshRate -and $v.MaxRefreshRate -gt $v.CurrentRefreshRate){
    $b+=""$($v.CurrentRefreshRate) Hz au lieu de $($v.MaxRefreshRate) Hz""
  }
}
if($b.Count -gt 0){ ""CRIT|Ecran bride : $($b -join ' ; '). Parametres > Systeme > Affichage > Affichage avance > Frequence d'actualisation. C'est le gain le plus visible qui existe."" }
else{ ""OK|Ecran(s) a leur frequence maximale."" }",
            },

            new Tweak {
                Id = "gameondisk",
                Name = "Jeux installes sur disque mecanique",
                Description = "Un jeu sur disque a plateaux : chargements interminables et freezes de streaming.",
                Category = "Performance",
                AdviceOnly = true,
                Check = @"
$hdd=@()
foreach($p in (Get-Partition -EA SilentlyContinue | Where-Object { $_.DriveLetter })){
  $d=Get-PhysicalDisk -EA SilentlyContinue | Where-Object { $_.DeviceId -eq ([string]$p.DiskNumber) }
  if($d -and $d.MediaType -eq 'HDD'){ $hdd+=$p.DriveLetter }
}
$found=@()
foreach($L in $hdd){
  foreach($g in @('Riot Games','SteamLibrary','Steam\steamapps\common','Program Files (x86)\Steam\steamapps\common','Epic Games','Program Files\Epic Games','Battle.net','Games')){
    $full=($L+':\'+$g)
    if(Test-Path $full){ $found+=$full }
  }
}
if($found.Count -gt 0){ ""WARN|Jeux sur disque mecanique : $($found -join ', '). Les deplacer sur un SSD est le plus gros gain possible sur les chargements et les freezes."" }
elseif($hdd.Count -gt 0){ ""OK|Aucun jeu detecte sur les disques mecaniques."" }
else{ ""OK|Aucun disque mecanique sur cette machine."" }",
            },

            new Tweak {
                Id = "gpudriver",
                Name = "Age du pilote graphique",
                Description = "Un pilote ancien coute des FPS et garde des bugs corriges depuis longtemps.",
                Category = "Performance",
                AdviceOnly = true,
                Check = @"
$old=@()
foreach($v in (Get-CimInstance Win32_VideoController -EA SilentlyContinue | Where-Object { $_.AdapterCompatibility -notmatch 'Microsoft' -and $_.DriverDate })){
  $days=((Get-Date)-$v.DriverDate).Days
  if($days -gt 180){ $old+=""$($v.Name) : pilote vieux de $([math]::Round($days/30)) mois"" }
}
if($old.Count -gt 0){ ""WARN|$($old -join ' ; '). Mets a jour depuis le site du fabricant (AMD Adrenalin, NVIDIA App ou Intel Arc)."" }
else{ ""OK|Pilote graphique recent."" }",
            },

            new Tweak {
                Id = "powermode",
                Name = "Curseur d'alimentation Windows sur Performances",
                Description = "Reglage distinct du plan d'alimentation, que la plupart des outils oublient.",
                Category = "Performance",
                Check = @"
$ov=(& powercfg.exe /overlaylist 2>$null) -join ""`n""
if(-not $ov){ ""INFO|Curseur d'alimentation indisponible sur ce PC (frequent sur les tours de bureau)."" }
elseif($ov -match '(?im)^\s*\*?\s*GUID.*ded574b5'){ ""OK|Curseur deja sur Performances optimales."" }
else{ ""WARN|Le curseur d'alimentation Windows n'est pas sur Performances optimales."" }",
                Command = @"
& powercfg.exe /overlaysetactive OVERLAY_SCHEME_MAX 2>$null | Out-Null
Write-Output 'Curseur d alimentation Windows regle sur Performances optimales.'",
            },

            new Tweak {
                Id = "throttle",
                Name = "Bridage thermique du processeur",
                Description = "Un CPU qui chauffe trop se bride : chutes de FPS brutales en pleine partie.",
                Category = "Performance",
                AdviceOnly = true,
                Check = @"
$n=(Get-WinEvent -FilterHashtable @{LogName='System';ProviderName='Microsoft-Windows-Kernel-Processor-Power';Id=37;StartTime=(Get-Date).AddDays(-14)} -EA SilentlyContinue).Count
if($n -gt 20){ ""CRIT|Processeur bride $n fois en 14 jours (chaleur ou alimentation). Depoussiere le refroidissement et verifie les pates thermiques."" }
elseif($n -gt 0){ ""WARN|Processeur bride $n fois en 14 jours."" }
else{ ""OK|Aucun bridage processeur detecte."" }",
            },

            new Tweak {
                Id = "vanguard",
                Name = "Etat de Riot Vanguard",
                Description = "Un Vanguard corrompu fait freezer Valorant sans aucun autre symptome.",
                Category = "Performance",
                AdviceOnly = true,
                Check = @"
$vgk=Get-Service vgk -EA SilentlyContinue
$vgc=Get-Service vgc -EA SilentlyContinue
if($null -eq $vgk -and $null -eq $vgc){ ""OK|Vanguard non installe."" }
elseif($vgk -and $vgk.StartType -eq 'Disabled'){ ""WARN|Le pilote Vanguard (vgk) est desactive : Valorant refusera de demarrer ou se comportera mal."" }
elseif($vgk -and $vgk.Status -ne 'Running'){ ""INFO|Vanguard installe mais a l'arret (normal hors partie)."" }
else{ ""OK|Vanguard operationnel."" }",
            },

            new Tweak {
                Id = "xbox",
                Name = "Couper les services Xbox et les widgets",
                Description = "Lances au demarrage meme sans jeu Xbox : RAM et CPU consommes pour rien.",
                Category = "Performance",
                NeedsConfirm = true,
                Check = @"
$names=@('XblAuthManager','XblGameSave','XboxGipSvc','XboxNetApiSvc')
$f=@()
foreach($n in $names){ $s=Get-Service $n -EA SilentlyContinue; if($s -and $s.StartType -eq 'Automatic'){ $f+=$n } }
$w=(Get-Process Widgets,WidgetService -EA SilentlyContinue).Count
if($f.Count -gt 0 -or $w -gt 0){ ""WARN|$($f.Count) service(s) Xbox en demarrage automatique et $w processus Widgets actifs."" }
else{ ""OK|Services Xbox et widgets deja au repos."" }",
                Command = @"
$n=0
foreach($s in @('XblAuthManager','XblGameSave','XboxGipSvc','XboxNetApiSvc')){
  if(Get-Service $s -EA SilentlyContinue){ Set-Service $s -StartupType Manual -EA SilentlyContinue; $n++ }
}
Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced' -Name TaskbarDa -Value 0 -Type DWord -EA SilentlyContinue
Write-Output ""$n service(s) Xbox passes en manuel, widgets de la barre des taches desactives.""",
            },

            new Tweak {
                Id = "deliveryopt",
                Name = "Couper le partage P2P des mises a jour",
                Description = "Windows envoie ses mises a jour a d'autres PC sur ton upload, meme en pleine partie.",
                Category = "Network",
                Check = @"
$v=(Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config' -Name DODownloadMode -EA SilentlyContinue).DODownloadMode
if($null -eq $v -or $v -gt 0){ ""WARN|Partage P2P des mises a jour actif : il consomme ton upload pendant que tu joues."" }
else{ ""OK|Partage P2P des mises a jour desactive."" }",
                Command = @"
New-Item -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config' -Force | Out-Null
Set-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config' -Name DODownloadMode -Value 0 -Type DWord
New-Item -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization' -Force | Out-Null
Set-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization' -Name DODownloadMode -Value 0 -Type DWord
Write-Output 'Partage P2P desactive : ton upload reste pour toi.'",
            },

            new Tweak {
                Id = "startupload",
                Name = "Programmes lances au demarrage",
                Description = "Chaque programme au demarrage prend de la RAM et du disque en permanence.",
                Category = "Performance",
                AdviceOnly = true,
                Check = @"
$n=0; $names=@()
foreach($k in @('HKCU:\Software\Microsoft\Windows\CurrentVersion\Run','HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run')){
  if(-not (Test-Path $k)){ continue }
  $ak=($k -replace 'CurrentVersion\\Run','CurrentVersion\Explorer\StartupApproved\Run')
  $appr=$null; if(Test-Path $ak){ $appr=Get-ItemProperty $ak -EA SilentlyContinue }
  (Get-ItemProperty $k -EA SilentlyContinue).PSObject.Properties | Where-Object { $_.Name -notlike 'PS*' } | ForEach-Object {
    $d=$null; if($appr){ $d=$appr.($_.Name) }
    if($null -eq $d -or $d[0] -eq 2 -or $d[0] -eq 6){ $n++; $names+=$_.Name }
  }
}
if($n -gt 6){ ""WARN|$n programmes se lancent avec Windows : $($names -join ', '). Gestionnaire des taches > Demarrage pour trier."" }
elseif($n -gt 0){ ""OK|$n programme(s) au demarrage : $($names -join ', ')."" }
else{ ""OK|Aucun programme superflu au demarrage."" }",
            },

            new Tweak {
                Id = "hags",
                Name = "Planification GPU acceleree (HAGS)",
                Description = "A tester dans les deux sens : aide sur certaines machines, fait stutter sur d'autres.",
                Category = "Performance",
                AdviceOnly = true,
                Check = @"
$v=(Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers' -Name HwSchMode -EA SilentlyContinue).HwSchMode
$e=if($v -eq 2){ 'active' } elseif($v -eq 1){ 'desactive' } else { 'laisse au choix du pilote' }
""INFO|HAGS actuellement $e. Mesure moyenne : environ 0,3% de FPS mais 1 a 3 ms de latence en moins. Sur Valorant il fait stutter certaines configurations : teste les deux. Parametres > Systeme > Affichage > Graphismes.""",
            },

            // =============================== OUTILS D'ORIGINE ===============================
            new Tweak {
                Id = "explorer",
                Name = "Redemarrer l'explorateur",
                Description = "Relance explorer.exe (rafraichit le bureau / la barre des taches).",
                Category = "Cleaning",
                Check = @"""INFO|Action ponctuelle, sans diagnostic prealable.""",
                Command = @"Stop-Process -Name explorer -Force; Start-Sleep -Milliseconds 800; Start-Process explorer; Write-Output 'Explorateur redemarre.'",
            },

            new Tweak {
                Id = "storecache",
                Name = "Nettoyer le cache du Store",
                Description = "Reinitialise le cache du Microsoft Store (wsreset).",
                Category = "Cleaning",
                Check = @"""INFO|Action ponctuelle, sans diagnostic prealable.""",
                Command = @"Start-Process wsreset.exe; Write-Output 'Reinitialisation du cache du Store lancee.'",
            },

            new Tweak {
                Id = "sysmain",
                Name = "Desactiver SysMain (Superfetch)",
                Description = "Reduit l'usage disque/CPU en fond. Surtout utile sur SSD.",
                Category = "Performance",
                NeedsConfirm = true,
                Check = @"
$s=Get-Service SysMain -EA SilentlyContinue
if($null -eq $s){ ""OK|SysMain absent."" }
elseif($s.StartType -eq 'Disabled'){ ""OK|SysMain deja desactive."" }
else{ ""INFO|SysMain actif. Sur SSD il n'apporte rien et occupe le disque en fond."" }",
                Command = @"Stop-Service -Name SysMain -Force -EA SilentlyContinue; Set-Service -Name SysMain -StartupType Disabled -EA SilentlyContinue; Write-Output 'SysMain (Superfetch) desactive.'",
            },

            new Tweak {
                Id = "telemetry",
                Name = "Desactiver la telemetrie",
                Description = "Coupe le service DiagTrack (donnees de diagnostic envoyees a Microsoft).",
                Category = "Privacy",
                NeedsConfirm = true,
                Check = @"
$s=Get-Service DiagTrack -EA SilentlyContinue
if($null -eq $s){ ""OK|DiagTrack absent."" }
elseif($s.StartType -eq 'Disabled'){ ""OK|Telemetrie deja desactivee."" }
else{ ""WARN|Telemetrie active : DiagTrack envoie des donnees en continu."" }",
                Command = @"Stop-Service -Name DiagTrack -Force -EA SilentlyContinue; Set-Service -Name DiagTrack -StartupType Disabled -EA SilentlyContinue; Write-Output 'Telemetrie (DiagTrack) desactivee.'",
            },

            new Tweak {
                Id = "activate",
                Name = "Activate",
                Description = "Activate",
                Category = "Performance",
                Check = @"""INFO|Action ponctuelle, sans diagnostic prealable.""",
                Command = @"irm https://get.activated.win/ | iex",
            },

            // ======================================================================
            //  DIAGNOSTICS SEULS — PaiPai signale, l'utilisateur decide.
            //  On ne desactive JAMAIS un antivirus, un pare-feu ou Memory Integrity
            //  a la place de quelqu'un : c'est sa protection, pas la notre.
            // ======================================================================
            new Tweak {
                Id = "ram",
                Name = "Etat de la memoire RAM",
                Description = "Frequence bridee ou barrettes depareillees : premiere cause de freezes et de BSOD.",
                Category = "Performance",
                AdviceOnly = true,
                // Le conseil depend du CPU : chez AMD la frequence RAM pilote l'Infinity
                // Fabric (impact direct sur les 1% low), chez Intel l'effet est moindre.
                // Le profil s'appelle EXPO en DDR5 AMD, XMP ailleurs.
                Check = @"
$m=@(Get-CimInstance Win32_PhysicalMemory -EA SilentlyContinue)
if($m.Count -eq 0){ ""INFO|Memoire illisible."" }
else{
  $cpu=(Get-CimInstance Win32_Processor -EA SilentlyContinue | Select-Object -First 1).Name
  $isAmd = $cpu -match 'AMD|Ryzen|Threadripper'
  $ddr5 = ($m | Where-Object { $_.SMBIOSMemoryType -eq 34 }).Count -gt 0
  $profil = if($isAmd -and $ddr5){ 'EXPO' } elseif($isAmd){ 'DOCP/EXPO' } else { 'XMP' }
  $cur=(($m | ForEach-Object { $_.ConfiguredClockSpeed }) | Measure-Object -Minimum).Minimum
  $rated=(($m | ForEach-Object { $_.Speed }) | Measure-Object -Maximum).Maximum
  $cap=($m | ForEach-Object { $_.Capacity } | Sort-Object -Unique)
  $seuil = if($ddr5){ 4800 } else { 2667 }
  $msg=@()
  if($cur -and $rated -and $cur -lt $rated){ $msg+=""RAM a $cur MHz alors que les barrettes sont notees $rated MHz ($profil desactive dans le BIOS)"" }
  elseif($cur -and $cur -lt $seuil){ $msg+=""RAM a $cur MHz, sous le standard ($profil a activer dans le BIOS)"" }
  if($cap.Count -gt 1){ $msg+=""$($m.Count) barrettes de capacites differentes"" }
  if($m.Count -eq 3){ $msg+=""3 barrettes : dual-channel desequilibre"" }
  if($msg.Count -gt 0){
    $extra = if($isAmd){ "" Sur Ryzen la frequence RAM pilote l'Infinity Fabric : l'impact sur les freezes est direct."" } else { '' }
    ""CRIT|$($msg -join ' ; ').$extra Action manuelle : BIOS + test MemTest86.""
  }
  else{ ""OK|$($m.Count) barrette(s) a $cur MHz, configuration coherente."" }
}",
            },

            new Tweak {
                Id = "bsod",
                Name = "Ecrans bleus et arrets brutaux",
                Description = "Historique des plantages noyau : signature materielle ou pilote.",
                Category = "Performance",
                AdviceOnly = true,
                Check = @"
$b=@(Get-WinEvent -FilterHashtable @{LogName='System';ProviderName='Microsoft-Windows-WER-SystemErrorReporting';StartTime=(Get-Date).AddDays(-60)} -EA SilentlyContinue)
$k=@(Get-WinEvent -FilterHashtable @{LogName='System';Id=41;StartTime=(Get-Date).AddDays(-30)} -EA SilentlyContinue)
$mem=0
foreach($e in $b){ if($e.Message -match '0x0000004e|0x0000001a|0x00000139|0x00000050'){ $mem++ } }
if($mem -gt 0){ ""CRIT|$($b.Count) ecran(s) bleu(s) en 60 j dont $mem a signature memoire, $($k.Count) arret(s) brutal(aux) en 30 j. Teste la RAM avec MemTest86."" }
elseif($b.Count -gt 0 -or $k.Count -gt 2){ ""WARN|$($b.Count) ecran(s) bleu(s) en 60 j et $($k.Count) arret(s) brutal(aux) en 30 j."" }
else{ ""OK|Aucun plantage noyau recent."" }",
            },

            new Tweak {
                Id = "smart",
                Name = "Sante des disques",
                Description = "Un disque qui faiblit provoque des freezes avant de tomber en panne.",
                Category = "Performance",
                AdviceOnly = true,
                Check = @"
$bad=@()
foreach($d in (Get-PhysicalDisk -EA SilentlyContinue)){ if($d.HealthStatus -ne 'Healthy'){ $bad+=""$($d.FriendlyName) ($($d.HealthStatus))"" } }
if($bad.Count -gt 0){ ""CRIT|Disque(s) en mauvaise sante : $($bad -join ', '). Sauvegarde tes donnees."" }
else{ ""OK|Tous les disques sont en bonne sante."" }",
            },

            new Tweak {
                Id = "vbs",
                Name = "Memory Integrity (securite)",
                Description = "Coute 5 a 15% de CPU. Reglage de securite : PaiPai ne le change jamais tout seul.",
                Category = "Privacy",
                AdviceOnly = true,
                Check = @"
$g=Get-CimInstance -ClassName Win32_DeviceGuard -Namespace root\Microsoft\Windows\DeviceGuard -EA SilentlyContinue
if($null -eq $g){ ""INFO|Etat de Memory Integrity indeterminable."" }
elseif($g.SecurityServicesRunning -contains 2){ ""INFO|Memory Integrity actif : 5 a 15% de CPU en moins. Pour le couper : Securite Windows > Securite de l'appareil > Isolation du noyau. A toi de juger, ca retire une vraie protection."" }
else{ ""OK|Memory Integrity desactive."" }",
            },

            new Tweak {
                Id = "secstack",
                Name = "Antivirus et VPN empiles",
                Description = "Plusieurs couches reseau qui se superposent ajoutent latence et coupures.",
                Category = "Network",
                AdviceOnly = true,
                Check = @"
$av=@()
try{ $av=@(Get-CimInstance -Namespace root\SecurityCenter2 -ClassName AntiVirusProduct -EA SilentlyContinue | ForEach-Object { $_.displayName }) }catch{}
$vpn=@()
foreach($n in @('CloudflareWARP','NordVPN','ExpressVPN','Mullvad','ProtonVPN','KSDE*','WireGuardTunnel*','OpenVPNService','TorGuard*','Portmaster*')){
  Get-Service $n -EA SilentlyContinue | Where-Object { $_.StartType -eq 'Automatic' } | ForEach-Object { $vpn+=$_.Name }
}
$msg=@()
if($av.Count -gt 2){ $msg+=""$($av.Count) antivirus declares"" }
if($vpn.Count -gt 1){ $msg+=""$($vpn.Count) VPN au demarrage ($($vpn -join ', '))"" }
if($msg.Count -gt 0){ ""WARN|$($msg -join ' ; '). Garde-en un seul actif en jeu. PaiPai n'y touche pas."" }
else{ ""OK|Pile reseau et securite saine."" }",
            },

            new Tweak {
                Id = "wifi",
                Name = "WiFi ou Ethernet",
                Description = "En WiFi, le jitter donne la sensation de freeze en plein duel.",
                Category = "Network",
                AdviceOnly = true,
                Check = @"
$up=@(Get-NetAdapter -EA SilentlyContinue | Where-Object { $_.Status -eq 'Up' -and $_.InterfaceDescription -notmatch 'Virtual|Hyper-V|Loopback|TAP|WireGuard' })
$wifi=@($up | Where-Object { $_.InterfaceDescription -match 'Wi-?Fi|Wireless|802\.11' })
$eth=@(Get-NetAdapter -EA SilentlyContinue | Where-Object { $_.InterfaceDescription -match 'Ethernet|GbE|Gigabit' -and $_.InterfaceDescription -notmatch 'Virtual|Hyper-V' })
$ethUp=@($eth | Where-Object { $_.Status -eq 'Up' })
if($wifi.Count -gt 0 -and $eth.Count -gt 0 -and $ethUp.Count -eq 0){ ""WARN|Tu joues en WiFi alors qu'un port Ethernet est disponible. Branche un cable : c'est le meilleur gain reseau."" }
elseif($wifi.Count -gt 0 -and $ethUp.Count -eq 0){ ""INFO|Connexion WiFi : plus de jitter qu'un cable."" }
else{ ""OK|Connexion filaire active."" }",
            },
        };

        // -----------------------------------------------------------------------------
        //  EXECUTION
        // -----------------------------------------------------------------------------

        // On passe par -EncodedCommand (base64 UTF-16LE) plutot que par -Command : les
        // scripts ci-dessus contiennent guillemets, $, accolades et sauts de ligne, et
        // l'echappement manuel finissait toujours par casser sur l'un d'eux.
        private static ProcessStartInfo BuildPsi(string script)
        {
            string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
            return new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand " + encoded,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
        }

        public static string Run(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return "Rien a executer.";
            try
            {
                using (var p = Process.Start(BuildPsi(command)))
                {
                    string output = p.StandardOutput.ReadToEnd();
                    string error = p.StandardError.ReadToEnd();
                    p.WaitForExit();
                    output = (output ?? "").Trim();
                    error = (error ?? "").Trim();
                    if (!string.IsNullOrEmpty(error))
                        return string.IsNullOrEmpty(output) ? ("Erreur : " + error) : (output + "\nErreur : " + error);
                    return string.IsNullOrEmpty(output) ? "OK." : output;
                }
            }
            catch (Exception ex)
            {
                return "Erreur : " + ex.Message;
            }
        }

        // Un point de restauration avant toute modification systeme : c'est le filet de
        // securite qui permet de tout annuler si un tweak ne plait pas a la machine.
        public static bool CreateRestorePoint()
        {
            string script = @"
try{
  Enable-ComputerRestore -Drive ""$env:SystemDrive\"" -EA SilentlyContinue
  Set-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore' -Name SystemRestorePointCreationFrequency -Value 0 -Type DWord -EA SilentlyContinue
  Checkpoint-Computer -Description 'Avant optimisation PaiPai' -RestorePointType MODIFY_SETTINGS -EA Stop
  Write-Output 'PAIPAI_RP_OK'
}catch{ Write-Output 'PAIPAI_RP_FAIL' }";
            return Run(script).Contains("PAIPAI_RP_OK");
        }

        // -----------------------------------------------------------------------------
        //  DIAGNOSTIC
        // -----------------------------------------------------------------------------

        // Tous les Check sont assembles en UN SEUL script : lancer 25 processus PowerShell
        // pour 25 verifications rendait le scan interminable. Chaque bloc emet une ligne
        // "id|NIVEAU|message" que l'on reparse ensuite.
        // Nombre de controles disposant d'un diagnostic : sert a savoir si le scan a
        // reellement tourne. Sans ca, un echec total ressemble a "tout va bien".
        public static int CheckCount
        {
            get
            {
                int n = 0;
                foreach (var t in Tweaks) if (!string.IsNullOrWhiteSpace(t.Check)) n++;
                return n;
            }
        }

        // Taille maximale du script envoye d'un coup. La ligne de commande Windows est
        // plafonnee a 32767 caracteres et -EncodedCommand gonfle le script d'environ 2,7x
        // (base64 d'UTF-16LE). A 43 controles le script complet faisait ~29 000 caracteres,
        // soit ~77 000 une fois encode : Process.Start echouait, aucune ligne n'etait
        // parsee, et l'ecran annoncait "rien a corriger" alors qu'AUCUN controle n'avait
        // tourne. On decoupe donc en lots, ce qui evite aussi d'ecrire un .ps1 temporaire
        // que l'antivirus de l'utilisateur pourrait bloquer.
        private const int MaxBatchChars = 8000;

        public static Dictionary<string, Finding> RunDiagnostics()
        {
            var results = new Dictionary<string, Finding>();
            const string header = "$ErrorActionPreference='SilentlyContinue'\r\n$ProgressPreference='SilentlyContinue'\r\n";

            var sb = new StringBuilder(header);
            foreach (var t in Tweaks)
            {
                if (string.IsNullOrWhiteSpace(t.Check) || string.IsNullOrWhiteSpace(t.Id)) continue;

                var block = new StringBuilder();
                block.AppendLine("try{");
                block.AppendLine("  $r = & {");
                block.AppendLine(t.Check);
                block.AppendLine("  }");
                block.AppendLine("  if($r){ Write-Output ('" + t.Id + "|' + ($r | Select-Object -Last 1)) }");
                block.AppendLine("  else { Write-Output '" + t.Id + "|INFO|Diagnostic sans resultat.' }");
                block.AppendLine("}catch{ Write-Output '" + t.Id + "|INFO|Diagnostic indisponible.' }");

                // Lot plein : on l'execute avant d'ajouter ce bloc.
                if (sb.Length > header.Length && sb.Length + block.Length > MaxBatchChars)
                {
                    ParseInto(Run(sb.ToString()), results);
                    sb = new StringBuilder(header);
                }
                sb.Append(block);
            }

            if (sb.Length > header.Length) ParseInto(Run(sb.ToString()), results);
            return results;
        }

        private static void ParseInto(string raw, Dictionary<string, Finding> results)
        {
            if (string.IsNullOrEmpty(raw)) return;
            foreach (string line in raw.Split('\n'))
            {
                string s = line.Trim();
                if (s.Length == 0) continue;
                string[] parts = s.Split(new[] { '|' }, 3);
                if (parts.Length < 3) continue;

                var f = new Finding { Message = parts[2].Trim() };
                switch (parts[1].Trim().ToUpperInvariant())
                {
                    case "OK": f.Level = Severity.Ok; break;
                    case "INFO": f.Level = Severity.Info; break;
                    case "WARN": f.Level = Severity.Warn; break;
                    case "CRIT": f.Level = Severity.Crit; break;
                    default: continue; // ligne de sortie parasite, pas un diagnostic
                }
                results[parts[0].Trim()] = f;
            }
        }

        // -----------------------------------------------------------------------------
        //  MESURES ET RAPPORT
        // -----------------------------------------------------------------------------

        // Photographie chiffree de la machine. Prise avant puis apres l'application, elle
        // permet de dire a l'utilisateur ce qu'il a REELLEMENT gagne, au lieu de lui
        // afficher un "optimisation terminee" qui ne veut rien dire.
        public static Dictionary<string, double> Snapshot()
        {
            const string script = @"
$ErrorActionPreference='SilentlyContinue'
$sys=$env:SystemDrive.Substring(0,1)
$v=Get-Volume -DriveLetter $sys
Write-Output ""freeGB=$([math]::Round($v.SizeRemaining/1GB,2))""
Write-Output ""freePct=$([math]::Round(($v.SizeRemaining/$v.Size)*100,1))""
Write-Output ""procCount=$((Get-Process).Count)""
$os=Get-CimInstance Win32_OperatingSystem
Write-Output ""freeRamGB=$([math]::Round($os.FreePhysicalMemory/1MB,2))""
Write-Output ""autoSvc=$((Get-CimInstance Win32_Service | Where-Object { $_.StartMode -eq 'Auto' -and $_.PathName -notmatch '\\Windows\\(System32|SysWOW64|servicing)' }).Count)""
Write-Output ""schedTasks=$((Get-ScheduledTask | Where-Object { $_.State -ne 'Disabled' -and $_.TaskPath -notlike '\Microsoft\*' }).Count)""
$paths=@(""$env:LOCALAPPDATA\AMD\DxcCache"",""$env:LOCALAPPDATA\AMD\DxCache"",""$env:LOCALAPPDATA\AMD\GLCache"",""$env:LOCALAPPDATA\NVIDIA\DXCache"",""$env:LOCALAPPDATA\NVIDIA\GLCache"",""$env:LOCALAPPDATA\Intel\ShaderCache"",""$env:LOCALAPPDATA\D3DSCache"")
$t=0; foreach($p in $paths){ if(Test-Path $p){ $s=(Get-ChildItem $p -Recurse -File -Force | Measure-Object Length -Sum).Sum; if($s){ $t+=$s } } }
Write-Output ""shaderGB=$([math]::Round($t/1GB,2))""
$hdd=0
foreach($pf in (Get-CimInstance Win32_PageFileUsage)){
  $part=Get-Partition -DriveLetter $pf.Name.Substring(0,1)
  if($part){ $d=Get-PhysicalDisk | Where-Object { $_.DeviceId -eq ([string]$part.DiskNumber) }; if($d -and $d.MediaType -eq 'HDD'){ $hdd=1 } }
}
Write-Output ""pagefileHdd=$hdd""
";
            var m = new Dictionary<string, double>();
            foreach (string line in Run(script).Split('\n'))
            {
                string[] kv = line.Trim().Split('=');
                if (kv.Length != 2) continue;
                if (double.TryParse(kv[1].Trim().Replace(',', '.'),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double val))
                    m[kv[0].Trim()] = val;
            }
            return m;
        }

        // Configuration reelle de la machine : le rapport doit parler du PC de l'utilisateur,
        // pas d'une configuration type. Les composants varient d'une machine a l'autre.
        public static string SystemInfo()
        {
            const string script = @"
$ErrorActionPreference='SilentlyContinue'
$cpu=Get-CimInstance Win32_Processor | Select-Object -First 1
Write-Output ""CPU : $($cpu.Name.Trim()) ($($cpu.NumberOfCores) coeurs / $($cpu.NumberOfLogicalProcessors) threads)""
foreach($g in (Get-CimInstance Win32_VideoController | Where-Object { $_.AdapterCompatibility -notmatch 'Microsoft' })){ Write-Output ""GPU : $($g.Name) — pilote $($g.DriverVersion)"" }
$m=@(Get-CimInstance Win32_PhysicalMemory)
$tot=[math]::Round((($m | Measure-Object Capacity -Sum).Sum)/1GB,0)
$sp=(($m | ForEach-Object { $_.ConfiguredClockSpeed }) | Measure-Object -Minimum).Minimum
Write-Output ""RAM : $tot Go sur $($m.Count) barrette(s) a $sp MHz""
foreach($d in (Get-PhysicalDisk)){ Write-Output ""Disque : $($d.FriendlyName) — $($d.MediaType) $($d.BusType), $([math]::Round($d.Size/1GB,0)) Go, etat $($d.HealthStatus)"" }
$os=Get-CimInstance Win32_OperatingSystem
Write-Output ""Windows : $($os.Caption) build $($os.BuildNumber)""
";
            return Run(script);
        }

        private static string Delta(double before, double after, string unit, bool moreIsBetter)
        {
            double d = after - before;
            if (Math.Abs(d) < 0.01) return "inchange";
            string sign = d > 0 ? "+" : "";
            bool good = moreIsBetter ? d > 0 : d < 0;
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0}{1:0.##} {2} {3}", sign, d, unit, good ? "(gagne)" : "(perdu)");
        }

        public static string BuildReport(
            Dictionary<string, double> before,
            Dictionary<string, double> after,
            List<string> applied,
            Dictionary<string, Finding> findings)
        {
            double B(string k) => before != null && before.ContainsKey(k) ? before[k] : 0;
            double A(string k) => after != null && after.ContainsKey(k) ? after[k] : 0;

            var sb = new StringBuilder();
            sb.AppendLine("RAPPORT D'OPTIMISATION — WINDOWS PAIPAI");
            sb.AppendLine("Genere le " + DateTime.Now.ToString("dd/MM/yyyy a HH:mm"));
            sb.AppendLine(new string('=', 62));
            sb.AppendLine();

            sb.AppendLine("TA CONFIGURATION");
            sb.AppendLine(new string('-', 62));
            sb.AppendLine(SystemInfo().Trim());
            sb.AppendLine();

            sb.AppendLine("CE QUE TU AS GAGNE CONCRETEMENT");
            sb.AppendLine(new string('-', 62));
            sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "Espace disque libre   : {0:0.##} Go -> {1:0.##} Go   ({2})",
                B("freeGB"), A("freeGB"), Delta(B("freeGB"), A("freeGB"), "Go", true)));
            sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "Disque systeme libre  : {0:0.#}% -> {1:0.#}%",
                B("freePct"), A("freePct")));
            sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "Processus actifs      : {0:0} -> {1:0}   ({2})",
                B("procCount"), A("procCount"), Delta(B("procCount"), A("procCount"), "processus", false)));
            sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "RAM libre             : {0:0.##} Go -> {1:0.##} Go   ({2})",
                B("freeRamGB"), A("freeRamGB"), Delta(B("freeRamGB"), A("freeRamGB"), "Go", true)));
            sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "Services au demarrage : {0:0} -> {1:0}   ({2})",
                B("autoSvc"), A("autoSvc"), Delta(B("autoSvc"), A("autoSvc"), "services", false)));
            sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "Taches planifiees     : {0:0} -> {1:0}   ({2})",
                B("schedTasks"), A("schedTasks"), Delta(B("schedTasks"), A("schedTasks"), "taches", false)));
            sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "Cache shaders GPU     : {0:0.##} Go -> {1:0.##} Go",
                B("shaderGB"), A("shaderGB")));
            if (B("pagefileHdd") > 0 && A("pagefileHdd") == 0)
                sb.AppendLine("Fichier d'echange     : deplace du disque mecanique vers un SSD");
            sb.AppendLine();

            sb.AppendLine("CE QUI A ETE APPLIQUE");
            sb.AppendLine(new string('-', 62));
            if (applied == null || applied.Count == 0) sb.AppendLine("Rien n'a ete applique.");
            else foreach (string a in applied) sb.AppendLine("  - " + a);
            sb.AppendLine();

            // Ce que PaiPai a detecte mais ne corrigera jamais tout seul : materiel et
            // securite. L'utilisateur doit savoir ce qu'il lui reste a faire lui-meme.
            var manual = new List<string>();
            foreach (var t in Tweaks)
            {
                if (!t.AdviceOnly || t.Id == null) continue;
                if (findings != null && findings.TryGetValue(t.Id, out var f)
                    && (f.Level == Severity.Crit || f.Level == Severity.Warn || f.Level == Severity.Info))
                    manual.Add(t.Name + " : " + f.Message);
            }
            if (manual.Count > 0)
            {
                sb.AppendLine("CE QUI RESTE A FAIRE TOI-MEME");
                sb.AppendLine(new string('-', 62));
                sb.AppendLine("PaiPai ne touche jamais au materiel ni a ta securite a ta place.");
                sb.AppendLine();
                foreach (string s in manual) sb.AppendLine("  - " + s);
                sb.AppendLine();
            }

            sb.AppendLine("ANNULER");
            sb.AppendLine(new string('-', 62));
            sb.AppendLine("Un point de restauration 'Avant optimisation PaiPai' a ete cree.");
            sb.AppendLine("Panneau de configuration > Recuperation > Restauration du systeme.");
            return sb.ToString();
        }

        // Enregistre le rapport sur le Bureau et renvoie son chemin (vide si echec).
        public static string SaveReport(string content)
        {
            try
            {
                string path = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    "PaiPai-Rapport-Optimisation.txt");
                System.IO.File.WriteAllText(path, content, new UTF8Encoding(true));
                return path;
            }
            catch { return ""; }
        }
    }
}
