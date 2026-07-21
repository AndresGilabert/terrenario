Set-Location "d:\PROJECTES\Terrenario"
$baseLink = '../../../../../prototype/terrenario-mvp'

$map = @{
  'MVP-101' = @{ links=@('src/components/LoginPage.tsx','src/components/LandingPage.tsx','src/App.tsx'); rows=@(@{s='LoginPage'; r='RN-018, RN-036, RN-020'; st='parcial'; e='Prueba manual: login UI y navegacion a diario; pendiente OIDC real'}, @{s='LandingPage'; r='RN-018'; st='cubierto'; e='Prueba visual: CTA a login/onboarding'}) }
  'MVP-102' = @{ links=@('src/components/OnboardingStep1.tsx','src/components/OnboardingStep2.tsx','src/App.tsx'); rows=@(@{s='OnboardingStep1'; r='RN-021'; st='parcial'; e='Prueba manual: alta guiada visual de Workspace'}, @{s='OnboardingStep2'; r='RN-021, RN-022, RN-023'; st='parcial'; e='Prueba manual: temporada en UI; pendiente reglas backend'}) }
  'MVP-103' = @{ links=@('src/components/AjustesView.tsx','src/components/Sidebar.tsx'); rows=@(@{s='AjustesView'; r='RN-035'; st='falta'; e='No existe flujo de invitacion por email/enlace en el prototipo'}, @{s='Sidebar'; r='RN-034'; st='parcial'; e='Solo referencia de contexto visual de usuario/workspace'}) }
  'MVP-104' = @{ links=@('src/components/Sidebar.tsx','src/components/TopNavbar.tsx','src/App.tsx'); rows=@(@{s='Sidebar'; r='RN-034'; st='parcial'; e='Selector visual de workspace sin alternancia multi-workspace real'}, @{s='TopNavbar'; r='RN-034'; st='parcial'; e='Muestra contexto activo de workspace/temporada'}) }
  'MVP-105' = @{ links=@('src/App.tsx','src/components/LoginPage.tsx','src/components/Sidebar.tsx'); rows=@(@{s='App shell y rutas'; r='RN-034'; st='parcial'; e='Navegacion interna disponible; sin autorizacion real por scope'}, @{s='LoginPage'; r='RN-020, RN-017'; st='falta'; e='No hay trazabilidad de exito/abandono ni eventos de seguridad'}) }

  'MVP-201' = @{ links=@('src/components/OnboardingStep1.tsx','src/components/OnboardingStep2.tsx'); rows=@(@{s='OnboardingStep1'; r='RN-021'; st='parcial'; e='Wizard inicial disponible'}, @{s='OnboardingStep2'; r='RN-021, RN-022'; st='parcial'; e='Configuracion de temporada visible'}) }
  'MVP-202' = @{ links=@('src/components/TerrenosView.tsx','src/components/TerrenoModal.tsx','src/components/TerrenoDetailModal.tsx'); rows=@(@{s='TerrenoModal'; r='RN-028'; st='parcial'; e='Formulario de alta de terreno disponible'}, @{s='TerrenosView'; r='RN-001, RN-028'; st='parcial'; e='Listado y detalle visual; faltan restricciones completas MVP'}) }
  'MVP-203' = @{ links=@('src/components/TemporadasView.tsx','src/components/OnboardingStep2.tsx'); rows=@(@{s='TemporadasView'; r='RN-022, RN-024'; st='parcial'; e='Activacion de temporada en UI'}, @{s='OnboardingStep2'; r='RN-023'; st='falta'; e='No hay aviso explicito de fecha fuera de rango'}) }
  'MVP-204' = @{ links=@('src/components/TrabajadoresView.tsx','src/components/ActivityModal.tsx'); rows=@(@{s='TrabajadoresView'; r='RN-027'; st='parcial'; e='Maestro de trabajadores operativo en UI'}, @{s='ActivityModal'; r='RN-002, RN-027'; st='parcial'; e='Seleccion de responsable disponible'}) }
  'MVP-205' = @{ links=@('src/components/ActivityModal.tsx','src/components/DiarioView.tsx'); rows=@(@{s='ActivityModal'; r='RN-025'; st='parcial'; e='Entrada de actividad y titulo disponibles'}, @{s='Catalogo tareas workspace'; r='RN-026'; st='falta'; e='No existe pantalla/catalogo de tareas reutilizable'}) }

  'MVP-301' = @{ links=@('src/components/DiarioView.tsx','src/components/ActivityModal.tsx'); rows=@(@{s='ActivityModal'; r='RN-002, RN-003, RN-025'; st='parcial'; e='Formulario de actividad disponible'}, @{s='DiarioView'; r='RN-033'; st='cubierto'; e='Visualizacion cronologica del diario'}) }
  'MVP-302' = @{ links=@('src/components/ActivityModal.tsx'); rows=@(@{s='ActivityModal'; r='RN-026'; st='falta'; e='No existe opcion de guardar tarea libre al catalogo'}, @{s='ActivityModal'; r='RN-025'; st='parcial'; e='Existe captura de texto libre de actividad'}) }
  'MVP-303' = @{ links=@('src/components/ComprasView.tsx','src/components/DiarioView.tsx'); rows=@(@{s='ComprasView'; r='RN-031, RN-003'; st='parcial'; e='Alta y listado de compras disponibles'}, @{s='DiarioView'; r='RN-033'; st='parcial'; e='Compras se reflejan en diario'}) }
  'MVP-304' = @{ links=@('src/components/ComprasView.tsx','src/components/DiarioView.tsx'); rows=@(@{s='ComprasView'; r='RN-032'; st='falta'; e='No existe imputacion de compra por terreno'}, @{s='DiarioView'; r='RN-032'; st='falta'; e='No hay flujo de consumo sin compra previa con aviso'}) }
  'MVP-305' = @{ links=@('src/components/DiarioView.tsx','src/components/ActivityModal.tsx'); rows=@(@{s='DiarioView'; r='RN-033'; st='cubierto'; e='Timeline cronologico unificado implementado'}, @{s='DiarioView'; r='RN-037'; st='falta'; e='Borrado existe pero sin confirmacion explicita'}) }

  'MVP-401' = @{ links=@('src/components/CosechasView.tsx','src/components/CosechaModal.tsx'); rows=@(@{s='CosechaModal'; r='RN-029, RN-004'; st='parcial'; e='Formulario de cosecha disponible'}, @{s='CosechasView'; r='RN-029'; st='cubierto'; e='Listado de cosechas y borrado visual'}) }
  'MVP-402' = @{ links=@('src/components/CosechaModal.tsx','src/components/CosechasView.tsx'); rows=@(@{s='CosechaModal'; r='RN-030, RN-012'; st='parcial'; e='Destino visible, pero catalogo cerrado MVP no completo'}, @{s='CosechaModal'; r='RN-004, RN-013, RN-014'; st='falta'; e='No aplica regla XOR rendimiento/litros en UI'}) }
  'MVP-403' = @{ links=@('src/components/DashboardView.tsx'); rows=@(@{s='DashboardView - resumen'; r='RN-005, RN-009'; st='parcial'; e='Widget de resumen visible'}, @{s='DashboardView - destino'; r='RN-012'; st='parcial'; e='Incluye Sin destino; faltan taxonomias canonicas completas'}) }
  'MVP-404' = @{ links=@('src/components/DashboardView.tsx'); rows=@(@{s='DashboardView - kg por terreno'; r='RN-011'; st='parcial'; e='Desglose por terreno disponible'}, @{s='DashboardView - evolucion'; r='RN-013, RN-015'; st='parcial'; e='Serie temporal visible; faltan reglas de historico completas'}) }
  'MVP-405' = @{ links=@('src/components/DashboardView.tsx','src/components/TerrenosView.tsx'); rows=@(@{s='DashboardView - filtros'; r='RN-007, RN-008'; st='parcial'; e='Filtros visuales disponibles; no se valida persistencia real tras recarga'}, @{s='TerrenosView + DashboardView'; r='RN-010'; st='parcial'; e='Hay estado incompleto en terrenos, falta regla completa en KPI global'}) }

  'MVP-501' = @{ links=@('README.md','src/App.tsx'); rows=@(@{s='App shell'; r='docs/04-ingenieria/estrategia-testing.md'; st='parcial'; e='Smoke manual posible sobre rutas MVP'}, @{s='Build Vite'; r='docs/04-ingenieria/estrategia-testing.md'; st='cubierto'; e='Evidencia: npm run build ejecutado correctamente'}) }
  'MVP-502' = @{ links=@('src/components/LoginPage.tsx','src/components/AjustesView.tsx','src/App.tsx'); rows=@(@{s='LoginPage'; r='RN-017, RN-018, RN-036'; st='parcial'; e='UX de acceso definida; sin hardening real'}, @{s='Ajustes/App'; r='docs/07-seguridad/modelo-seguridad.md'; st='falta'; e='No se implementan controles de seguridad avanzados'}) }
  'MVP-503' = @{ links=@('src/components/AjustesView.tsx','src/components/LoginPage.tsx'); rows=@(@{s='AjustesView'; r='RN-017'; st='parcial'; e='Campos PII visibles para inventario de cumplimiento'}, @{s='Flujo autenticacion'; r='docs/07-seguridad/privacidad-datos.md'; st='falta'; e='Pendiente checklist legal y evidencia RGPD'}) }
  'MVP-504' = @{ links=@('README.md','src/App.tsx'); rows=@(@{s='README run local'; r='docs/08-procesos/proceso-release.md'; st='parcial'; e='Arranque local documentado'}, @{s='Navegacion MVP'; r='docs/08-procesos/definition-of-done.md'; st='parcial'; e='Base para gate visual, faltan gates formales'}) }

  'MVP-601' = @{ links=@('src/components/LoginPage.tsx','src/App.tsx'); rows=@(@{s='LoginPage'; r='RN-020'; st='falta'; e='No hay eventos de abandono/exito instrumentados'}, @{s='App rutas auth'; r='RN-020'; st='parcial'; e='Puntos de enganche definidos por navegacion'}) }
  'MVP-602' = @{ links=@('src/components/DashboardView.tsx','src/components/TopNavbar.tsx'); rows=@(@{s='DashboardView'; r='RN-006, RN-007'; st='parcial'; e='Interacciones de filtros y recarga identificables'}, @{s='TopNavbar'; r='RN-006'; st='parcial'; e='Botones y contexto visibles para instrumentacion'}) }
  'MVP-603' = @{ links=@('src/App.tsx','src/components/DashboardView.tsx','src/components/DiarioView.tsx'); rows=@(@{s='App routing'; r='docs/05-infraestructura/observabilidad.md'; st='parcial'; e='Rutas clave para definir alertas de degradacion'}, @{s='Dashboard/Diario'; r='docs/05-infraestructura/observabilidad.md'; st='falta'; e='No hay alertas implementadas en prototipo'}) }
}

$storySpecs = Get-ChildItem -Path 'docs/09-desarrollos/epicas/*/MVP-*/spec.md' -File

foreach ($file in $storySpecs) {
  $content = [System.IO.File]::ReadAllText($file.FullName)
  $idMatch = [regex]::Match($content, 'id:\s*"(MVP-\d{3})"')
  if (-not $idMatch.Success) { continue }
  $id = $idMatch.Groups[1].Value
  if (-not $map.ContainsKey($id)) { continue }

  $entry = $map[$id]
  $content = [regex]::Replace($content, 'actualizado_en:\s*"[0-9]{4}-[0-9]{2}-[0-9]{2}"', 'actualizado_en: "2026-07-21"', 1)

  $lines = New-Object System.Collections.Generic.List[string]
  $lines.Add('## Maquetas y referencias visuales')
  $lines.Add('')
  $lines.Add("- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md]($baseLink/README.md)")
  foreach ($l in $entry.links) {
    $lines.Add("- Referencia UI: [prototype/terrenario-mvp/$l]($baseLink/$l)")
  }
  $lines.Add('')
  $lines.Add('> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.')
  $lines.Add('')
  $lines.Add('## Checklist de implementación (prototipo + KB)')
  $lines.Add('')
  $lines.Add('| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |')
  $lines.Add('|---|---|---|---|')
  foreach ($r in $entry.rows) {
    $lines.Add("| $($r.s) | $($r.r) | $($r.st) | $($r.e) |")
  }

  $replacement = ($lines -join "`r`n") + "`r`n`r`n## Notas y decisiones"
  $pattern = '(?s)## Maquetas y referencias visuales\r?\n.*?\r?\n## Notas y decisiones'
  if ([regex]::IsMatch($content, $pattern)) {
    $content = [regex]::Replace($content, $pattern, $replacement, 1)
  }

  $enc = [System.Text.UTF8Encoding]::new($true)
  [System.IO.File]::WriteAllText($file.FullName, $content, $enc)
  Write-Output "Actualizado: $($file.FullName)"
}
