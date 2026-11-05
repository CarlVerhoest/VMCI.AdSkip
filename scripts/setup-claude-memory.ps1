<#
.SYNOPSIS
  Koppelt het lokale Claude Code-geheugen van dit project aan .claude/memory in de repo.

.DESCRIPTION
  Claude Code bewaart zijn auto-memory per project in
    %USERPROFILE%\.claude\projects\<slug>\memory
  waarbij <slug> is afgeleid van het repopad (bv. C--DATA-Cave-MyRepos-VMCI-AdSkip).
  Dit script vervangt die map door een directory junction naar <repo>\.claude\memory,
  zodat alle kennis van Claude mee in git zit en op elke pc gedeeld wordt.

  Bestaande lokale geheugenbestanden worden eerst naar de repo gekopieerd (bestaande bestanden
  in de repo worden NIET overschreven). Veilig om meerdere keren uit te voeren.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File scripts/setup-claude-memory.ps1
#>
$ErrorActionPreference = 'Stop'

$repoRoot  = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$repoMem   = Join-Path $repoRoot '.claude\memory'
$slug      = ($repoRoot -replace '[^A-Za-z0-9]', '-')
$localMem  = Join-Path $env:USERPROFILE ".claude\projects\$slug\memory"

Write-Host "Repo-geheugen : $repoMem"
Write-Host "Claude-map    : $localMem"

New-Item -ItemType Directory -Force -Path $repoMem | Out-Null
New-Item -ItemType Directory -Force -Path (Split-Path $localMem) | Out-Null

if (Test-Path $localMem) {
    $item = Get-Item $localMem -Force
    if ($item.LinkType -eq 'Junction' -or $item.LinkType -eq 'SymbolicLink') {
        if ($item.Target -contains $repoMem -or "$($item.Target)" -eq $repoMem) {
            Write-Host 'Junction bestaat al en wijst naar de repo. Niets te doen.' -ForegroundColor Green
            exit 0
        }
        Write-Host "Bestaande link wijst naar '$($item.Target)'; wordt vervangen."
        $item.Delete()
    }
    else {
        # Gewone map: lokale geheugenbestanden veiligstellen in de repo.
        Get-ChildItem $localMem -File -Force | ForEach-Object {
            $dest = Join-Path $repoMem $_.Name
            if (Test-Path $dest) {
                Write-Warning "Overgeslagen (bestaat al in repo): $($_.Name). Lokaal bewaard als backup."
            } else {
                Copy-Item $_.FullName $dest
                Write-Host "Gekopieerd naar repo: $($_.Name)"
            }
        }
        if (-not (Get-ChildItem $localMem -Force)) {
            Remove-Item $localMem
        } else {
            $backup = "$localMem.backup-$(Get-Date -Format yyyyMMddHHmmss)"
            Rename-Item $localMem $backup
            Write-Host "Oude lokale map bewaard als: $backup"
        }
    }
}

New-Item -ItemType Junction -Path $localMem -Target $repoMem | Out-Null
Write-Host 'Junction aangemaakt. Claude-geheugen staat nu in de repo.' -ForegroundColor Green
