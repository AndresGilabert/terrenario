Set-Location "d:\PROJECTES\Terrenario"
$root = (Get-Location).Path
$storySpecs = Get-ChildItem -Path 'docs/09-desarrollos/epicas/*/MVP-*/spec.md' -File | Sort-Object FullName
$rows = @()
foreach($f in $storySpecs){
  $txt = [System.IO.File]::ReadAllText($f.FullName)
  $idMatch = [regex]::Match($txt,'id:\s*"(MVP-\d{3})"')
  if(-not $idMatch.Success){ continue }
  $id = $idMatch.Groups[1].Value
  $titleMatch = [regex]::Match($txt,'titulo:\s*"([^"]+)"')
  $title = if($titleMatch.Success){ $titleMatch.Groups[1].Value } else { '' }

  $check = [regex]::Match($txt,'(?s)## Checklist de implementacion \(prototipo \+ KB\)\s*\r?\n\r?\n\|.*?\r?\n\|---\|---\|---\|---\|(?<body>.*?)\r?\n\r?\n## Notas y decisiones')
  $covered = 0
  $partial = 0
  $missing = 0
  if($check.Success){
    $body = $check.Groups['body'].Value
    $lines = $body -split "`r?`n" | Where-Object { $_ -match '^\|' }
    foreach($line in $lines){
      $parts = $line.Trim('|').Split('|').ForEach({ $_.Trim() })
      if($parts.Length -ge 3){
        switch ($parts[2].ToLower()) {
          'cubierto' { $covered++ }
          'parcial' { $partial++ }
          'falta' { $missing++ }
        }
      }
    }
  }

  $global = if($missing -gt 0){'falta'} elseif($partial -gt 0){'parcial'} elseif($covered -gt 0){'cubierto'} else {'sin-datos'}
  $rel = $f.FullName.Substring($root.Length + 1).Replace('\','/')
  $rows += [pscustomobject]@{ Id=$id; Titulo=$title; Global=$global; Cubierto=$covered; Parcial=$partial; Falta=$missing; Ruta=$rel }
}

$cub = ($rows | Where-Object Global -eq 'cubierto').Count
$par = ($rows | Where-Object Global -eq 'parcial').Count
$fal = ($rows | Where-Object Global -eq 'falta').Count

$md = @()
$md += '# Informe consolidado de cobertura prototipo vs KB (MVP)'
$md += ''
$md += '- Fecha: 2026-07-21'
$md += '- Fuente visual: prototype/terrenario-mvp'
$md += '- Regla de precedencia: la KB define requisitos; el prototipo solo referencia visual/flujo.'
$md += ''
$md += '## Resumen'
$md += ''
$md += "- Historias analizadas: $($rows.Count)"
$md += "- Estado global cubierto: $cub"
$md += "- Estado global parcial: $par"
$md += "- Estado global falta: $fal"
$md += ''
$md += '## Detalle por historia'
$md += ''
$md += '| Historia | Titulo | Estado global | Cubierto | Parcial | Falta | Spec |'
$md += '|---|---|---|---:|---:|---:|---|'
foreach($r in $rows | Sort-Object Id){
  $md += "| $($r.Id) | $($r.Titulo) | $($r.Global) | $($r.Cubierto) | $($r.Parcial) | $($r.Falta) | [$($r.Ruta)](../../$($r.Ruta)) |"
}
$md += ''
$md += '## Uso recomendado'
$md += ''
$md += '1. Planificar desarrollo por historias con estado global `falta` primero.'
$md += '2. Mantener los checklists dentro de cada spec como fuente operativa diaria.'
$md += '3. Actualizar este informe al cerrar cada historia o al cambiar prototipo.'

New-Item -ItemType Directory -Path 'prototype/reports' -Force | Out-Null
$enc = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllText('prototype/reports/mvp-prototype-coverage.md', ($md -join "`r`n"), $enc)
Write-Output 'Generado: prototype/reports/mvp-prototype-coverage.md'
