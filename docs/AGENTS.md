# AGENTS.md — Reglas para la carpeta docs/

> Estas reglas aplican a **todos** los archivos `.md` bajo `docs/`.
> Complementan las reglas globales del `AGENTS.md` raíz — léelo primero.

---

## Reglas de codificación y formato para archivos Markdown

Estas reglas aplican a **TODOS** los archivos `.md` sin excepción.

### Encoding — UTF-8 BOM obligatorio

- **TODOS** los archivos `.md` deben crearse y guardarse con codificación **UTF-8 BOM**.
- Nunca crear un `.md` sin BOM. Tras cualquier `create_file`, aplicar inmediatamente:

```powershell
$path = "ruta/al/archivo.md"
$content = [System.IO.File]::ReadAllText($path)
$utf8BOM = [System.Text.UTF8Encoding]::new($true)
[System.IO.File]::WriteAllText($path, $content, $utf8BOM)
```

- Verificar el BOM tras crear o modificar cualquier `.md`:

```powershell
$bytes = [System.IO.File]::ReadAllBytes("ruta/al/archivo.md")
if ($bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
    Write-Host "UTF-8 BOM OK"
} else {
    Write-Error "FALTA UTF-8 BOM — aplícalo ahora"
}
```

- Antes de dar por completada cualquier tarea que involucre `.md`, verificar el BOM en **todos** los archivos afectados.

---

## Frontmatter YAML obligatorio

Cada `.md` debe incluir frontmatter según su tipo de documento:

### Documentos de KB estándar (`docs/` secciones 00–08, 99)

```yaml
---
bloque: "{id-seccion}"        # ej. 01-producto, 02-arquitectura
documento: "{nombre-archivo}" # slug sin extensión
actualizado_en: "YYYY-MM-DD"
---
```

### Feature / Historia spec (`docs/09-desarrollos/epicas/{EPIC}/{TICKET}/spec.md`)

```yaml
---
id: ""
tipo: feature                  # feature | bugfix | mejora | spike | tarea
titulo: ""
estado: borrador               # borrador | en-revision | aprobado | en-progreso | en-testing | completado | cancelado
prioridad: media               # critica | alta | media | baja
sprint: ""
hito: ""
esfuerzo_estimado: ""
tickets: []
epica: ""
depende_de: []
bloquea: []
relacionado_con: []
responsable: ""
revisores: []
ai_context:
  dominios: []
  modulo_path: ""
  componentes: []
  etiquetas: []
  nivel_riesgo: bajo
creado_en: "YYYY-MM-DD"
actualizado_en: "YYYY-MM-DD"
---
```

### Épica (`docs/09-desarrollos/epicas/{EPIC}/spec.md`)

```yaml
---
id: ""
tipo: epica
titulo: ""
estado: borrador
prioridad: media
hito: ""
tickets: []
historias: []
depende_de: []
bloquea: []
relacionado_con: []
responsable: ""
revisores: []
ai_context:
  dominios: []
  modulo_path: ""
  componentes: []
  etiquetas: []
  nivel_riesgo: bajo
creado_en: "YYYY-MM-DD"
actualizado_en: "YYYY-MM-DD"
---
```

### Tech Design (`docs/09-desarrollos/epicas/{EPIC}/{TICKET}/tech-design.md`)

```yaml
---
id: ""
tipo: feature
titulo: "TDD: {Título}"
estado: borrador
tickets: []
epica: ""
responsable: ""
revisores: []
ai_context:
  dominios: []
  modulo_path: ""
  componentes: []
  etiquetas: []
  nivel_riesgo: bajo
creado_en: "YYYY-MM-DD"
actualizado_en: "YYYY-MM-DD"
---
```

### ADR (`docs/02-arquitectura/decisiones/` o `docs/03-modulos/{modulo}/decisiones/`)

```yaml
---
id: "ADR-XXXX"
titulo: ""
estado: propuesta              # propuesta | aceptada | rechazada | obsoleta | supersedida-por:ADR-XXXX
fecha: "YYYY-MM-DD"
decisores: []
etiquetas: []
---
```

### Release (`docs/10-releases/v{MAJOR}.{MINOR}.{PATCH}.md`)

```yaml
---
id: "vX.Y.Z"
tipo: release
titulo: "Release vX.Y.Z"
estado: borrador
fecha: "YYYY-MM-DD"
hito: ""
responsable: ""
revisores: []
tickets: []
creado_en: "YYYY-MM-DD"
actualizado_en: "YYYY-MM-DD"
---
```

---

## Campos de fecha

- Al **crear**: establecer `creado_en` y `actualizado_en` (o `fecha` en ADRs) con la fecha actual (`YYYY-MM-DD`).
- Al **modificar**: actualizar siempre `actualizado_en` (o `fecha`) con la fecha actual.
- Nunca dejar campos de fecha vacíos (`""`) en un archivo que se escribe o revisa.

---

## Lista de verificación antes de completar una tarea

1. UTF-8 BOM verificado en todos los `.md` creados o modificados.
2. Frontmatter correcto aplicado según el tipo de documento.
3. Campo `actualizado_en` (o `fecha`) actualizado a la fecha de hoy.
4. Ningún `.md` creado sin frontmatter.

---

## Reglas obligatorias para altas en 09-desarrollos

1. El nombre de carpeta de épica e historia se genera automáticamente desde el título del desarrollo o, si existe, desde el ticket fuente.
2. No pedir ni introducir slugs manuales como parte del proceso estándar.
3. Las referencias externas en `tickets` son opcionales y pueden apuntar a distintos sistemas.
4. Si existe ticket fuente, debe documentarse su trazabilidad real en el documento; no se inventa contenido ausente.
5. Las carpetas y archivos deben respetar los límites de longitud definidos en `docs/00-meta/convenciones.md`.
6. El cumplimiento de estas reglas se valida por script y CI.
