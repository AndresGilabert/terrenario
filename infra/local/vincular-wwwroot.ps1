# Enlaza `wwwroot` del backend con `dist/` del frontend mediante una junction de directorio
# (NTFS), no una copia: cada `npm run build` deja el backend sirviendo exactamente lo mismo que
# generó, sin ningún paso manual de por medio. No requiere permisos de administrador (las
# junctions, a diferencia de los symlinks, no los piden en Windows).
#
# Uso (una sola vez por clon del repositorio):
#   ./infra/local/vincular-wwwroot.ps1

$ErrorActionPreference = 'Stop'

$raiz = Resolve-Path (Join-Path $PSScriptRoot '../..')
$wwwroot = Join-Path $raiz 'src/backend/Terrenario.Api/wwwroot'
$dist = Join-Path $raiz 'src/frontend/terrenario-web/dist'

if (Test-Path $wwwroot) {
    $item = Get-Item $wwwroot
    if ($item.LinkType -eq 'Junction') {
        Write-Host "Ya enlazado: $wwwroot -> $($item.Target)"
        exit 0
    }
    throw "$wwwroot ya existe y no es una junction. Bórralo a mano si quieres reemplazarlo (podría contener un wwwroot real de otra prueba)."
}

if (-not (Test-Path $dist)) {
    # La junction necesita que el destino exista; `npm run build` lo rellenará después.
    New-Item -ItemType Directory -Path $dist | Out-Null
    Write-Host "Creado $dist vacío (todavía no has ejecutado 'npm run build')."
}

New-Item -ItemType Junction -Path $wwwroot -Target $dist | Out-Null
Write-Host "Enlazado: $wwwroot -> $dist"
Write-Host ""
Write-Host "Siguiente:"
Write-Host "  cd src/frontend/terrenario-web && npm run build"
Write-Host "  cd ../../backend/Terrenario.Api && dotnet run"
