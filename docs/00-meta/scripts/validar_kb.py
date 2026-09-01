#!/usr/bin/env python3
"""
Knowledge Base Validation Script
Valida la estructura, naming y frontmatter de la KB.
También puede regenerar los _indice.md de las épicas.

Uso:
    python validar_kb.py --validar
    python validar_kb.py --generar-indices
    python validar_kb.py --validar --generar-indices
"""

import os
import re
import sys
import argparse
import subprocess
from pathlib import Path
from datetime import date

try:
    import yaml
except ImportError:
    print("ERROR: PyYAML no está instalado. Ejecuta: pip install pyyaml")
    sys.exit(1)

# ─── Configuración ────────────────────────────────────────────────────────────

DOCS_ROOT = Path(__file__).parent.parent.parent  # docs/
REPO_ROOT = DOCS_ROOT.parent
DESARROLLOS_PATH = DOCS_ROOT / "09-desarrollos" / "epicas"

TIPOS_VALIDOS = {"feature", "bugfix", "mejora", "spike", "tarea", "epica"}
ESTADOS_VALIDOS = {
    "propuesta", "borrador", "en-revision", "aprobado", "en-progreso",
    "en-testing", "completado", "cancelado"
}
PRIORIDADES_VALIDAS = {"critica", "alta", "media", "baja"}
RIESGOS_VALIDOS = {"bajo", "medio", "alto", "critico"}
ESTADOS_ADR_VALIDOS = {"propuesta", "aceptada", "rechazada", "obsoleta"}
ESTADOS_SIN_PLACEHOLDERS = {"en-revision", "aprobado", "en-progreso", "en-testing", "completado"}
PLACEHOLDERS_BLOQUEANTES = ["TODO", "por definir", "pendiente de refinamiento"]

CAMPOS_OBLIGATORIOS_HISTORIA = ["id", "tipo", "titulo", "estado", "epica", "responsable", "ai_context"]
CAMPOS_OBLIGATORIOS_EPICA = ["id", "tipo", "titulo", "estado", "responsable", "ai_context"]
CAMPOS_AI_CONTEXT = ["dominios", "modulo_path", "componentes", "etiquetas", "nivel_riesgo"]

PATRON_TICKET_SLUG = re.compile(r"^[A-Z][A-Z0-9]*(-[A-Z][A-Z0-9]*)*-\d+--[a-z0-9][a-z0-9-]*$")
PATRON_ADR = re.compile(r"^ADR-\d{4}--[a-z0-9][a-z0-9-]*\.md$")
MAX_NOMBRE_CARPETA_DESARROLLO = 64
MAX_RUTA_RELATIVA_DOCS = 180
MAX_RUTA_ABSOLUTA_DOCS = 220
TEMPLATE_STATE_PATH = DOCS_ROOT / "00-meta" / "template-state.md"
TEMPLATE_CORE_POLICIES = {"synced", "manual", "frozen"}

errors = []
warnings = []
SOLO_CAMBIOS = False
CHANGED_DOC_PATHS = set()


# ─── Utilidades ───────────────────────────────────────────────────────────────

def error(msg: str):
    if SOLO_CAMBIOS:
        path = extraer_path_desde_mensaje(msg)
        if path and not es_ruta_cambiada(path):
            warnings.append(f"  WARN:  [legacy] {msg}")
            return
    errors.append(f"  ERROR: {msg}")


def warn(msg: str):
    warnings.append(f"  WARN:  {msg}")


def normalizar_ruta(path: Path) -> str:
    return str(path).replace("\\", "/").lower()


def extraer_path_desde_mensaje(msg: str) -> Path | None:
    # Formato esperado: "<path>.md: detalle..."
    lower = msg.lower()
    idx = lower.find(".md:")
    if idx == -1:
        return None
    raw_path = msg[: idx + 3].strip()
    try:
        p = Path(raw_path)
        return p
    except Exception:
        return None


def obtener_docs_cambiados(base_ref: str) -> set[str]:
    cmd = [
        "git", "diff", "--name-only", "--diff-filter=ACMR", f"{base_ref}...HEAD", "--", "docs/"
    ]
    try:
        result = subprocess.run(
            cmd,
            cwd=REPO_ROOT,
            check=False,
            capture_output=True,
            text=True,
        )
    except Exception as e:
        warn(f"No se pudieron obtener archivos cambiados con git diff ({e})")
        return set()

    if result.returncode not in (0, 1):
        warn(
            f"git diff contra base-ref '{base_ref}' devolvió código {result.returncode}; "
            "se usa fallback a cambios locales"
        )
        return obtener_docs_cambiados_locales()

    changed = set()
    for line in result.stdout.splitlines():
        line = line.strip()
        if not line:
            continue
        changed.add(normalizar_ruta(Path(line)))
    return changed


def obtener_docs_cambiados_locales() -> set[str]:
    cmds = [
        ["git", "diff", "--name-only", "--diff-filter=ACMR", "--", "docs/"],
        ["git", "diff", "--name-only", "--diff-filter=ACMR", "--cached", "--", "docs/"],
        ["git", "ls-files", "--others", "--exclude-standard", "docs/"],
    ]

    changed = set()
    for cmd in cmds:
        try:
            result = subprocess.run(
                cmd,
                cwd=REPO_ROOT,
                check=False,
                capture_output=True,
                text=True,
            )
        except Exception as e:
            warn(f"No se pudieron obtener cambios locales con {' '.join(cmd)} ({e})")
            continue

        if result.returncode not in (0, 1):
            continue

        for line in result.stdout.splitlines():
            line = line.strip()
            if not line:
                continue
            changed.add(normalizar_ruta(Path(line)))

    return changed


def es_ruta_cambiada(path: Path) -> bool:
    if not CHANGED_DOC_PATHS:
        return True

    if path.is_absolute():
        try:
            rel = path.relative_to(REPO_ROOT)
        except ValueError:
            rel = path
    else:
        rel = path

    norm = normalizar_ruta(rel)
    return norm in CHANGED_DOC_PATHS


def parse_frontmatter(filepath: Path) -> dict | None:
    """Extrae y parsea el frontmatter YAML de un archivo Markdown."""
    try:
        content = filepath.read_text(encoding="utf-8-sig")
    except Exception as e:
        error(f"{filepath}: no se puede leer el archivo ({e})")
        return None

    if not content.startswith("---"):
        return None

    end = content.find("---", 3)
    if end == -1:
        error(f"{filepath}: frontmatter YAML no cerrado correctamente")
        return None

    try:
        return yaml.safe_load(content[3:end])
    except yaml.YAMLError as e:
        error(f"{filepath}: YAML inválido — {e}")
        return None


def read_markdown_content(filepath: Path) -> str:
    try:
        return filepath.read_text(encoding="utf-8-sig")
    except Exception:
        return ""


def extraer_ticket_id(nombre_carpeta: str) -> str:
    return nombre_carpeta.split("--", 1)[0]


def validar_placeholders_por_estado(filepath: Path, fm: dict):
    estado = fm.get("estado", "")
    if estado not in ESTADOS_SIN_PLACEHOLDERS:
        return

    content = read_markdown_content(filepath)
    # Excluye frontmatter para revisar solo el cuerpo documental.
    body_start = content.find("---", 3)
    body = content[body_start + 3:] if body_start != -1 else content

    for token in PLACEHOLDERS_BLOQUEANTES:
        # Usa word-boundary Unicode para evitar falsos positivos con palabras
        # españolas que contienen la substring (ej. "todos", "método").
        # Para tokens en mayúscula (ej. "TODO"), la búsqueda es sensible a
        # mayúsculas para no colisionar con la palabra española "todo".
        pattern = r'\b' + re.escape(token) + r'\b'
        flags = 0 if token == token.upper() else re.IGNORECASE
        if re.search(pattern, body, flags):
            error(f"{filepath}: contiene placeholder '{token}' no permitido para estado '{estado}'")


def validar_trazabilidad_referencias(filepath: Path):
    content = read_markdown_content(filepath)
    if "## Trazabilidad Jira" in content:
        requeridos = ["Clave Jira:", "URL Jira:", "Fecha de extracción:"]
        for marcador in requeridos:
            if marcador not in content:
                error(f"{filepath}: sección Trazabilidad Jira incompleta (falta '{marcador}')")

    if "## Trazabilidad externa" in content:
        requeridos = ["Sistema:", "Fecha de sincronización o revisión:"]
        for marcador in requeridos:
            if marcador not in content:
                error(f"{filepath}: sección Trazabilidad externa incompleta (falta '{marcador}')")


def validar_tickets(filepath: Path, tickets):
    if tickets is None:
        return

    if isinstance(tickets, dict):
        return

    if not isinstance(tickets, list):
        error(f"{filepath}: campo 'tickets' debe ser un mapa legacy o una lista de referencias")
        return

    for idx, referencia in enumerate(tickets, start=1):
        if not isinstance(referencia, dict):
            error(f"{filepath}: tickets[{idx}] debe ser un objeto con metadatos de la referencia")
            continue

        if not referencia.get("sistema"):
            error(f"{filepath}: tickets[{idx}].sistema es obligatorio cuando se informa una referencia")
            continue

        if not referencia.get("id") and not referencia.get("url"):
            warn(f"{filepath}: tickets[{idx}] no incluye 'id' ni 'url'; la trazabilidad será limitada")


def validar_longitud_ruta_docs(path: Path):
    try:
        ruta_absoluta = str(path.resolve())
    except Exception:
        ruta_absoluta = str(path)

    try:
        ruta_relativa = str(path.relative_to(REPO_ROOT)).replace("\\", "/")
    except ValueError:
        ruta_relativa = str(path).replace("\\", "/")

    if len(ruta_relativa) > MAX_RUTA_RELATIVA_DOCS:
        error(
            f"{path}: la ruta relativa al repo tiene {len(ruta_relativa)} caracteres y supera el máximo "
            f"de {MAX_RUTA_RELATIVA_DOCS}"
        )

    if len(ruta_absoluta) > MAX_RUTA_ABSOLUTA_DOCS:
        error(
            f"{path}: la ruta absoluta tiene {len(ruta_absoluta)} caracteres y supera el máximo "
            f"de {MAX_RUTA_ABSOLUTA_DOCS}"
        )


def validar_longitudes_docs():
    for path in DOCS_ROOT.rglob("*"):
        validar_longitud_ruta_docs(path)


def validar_template_state():
    if not TEMPLATE_STATE_PATH.exists():
        warn(f"{TEMPLATE_STATE_PATH}: no existe; el proyecto no declara versión de plantilla")
        return

    fm = parse_frontmatter(TEMPLATE_STATE_PATH)
    if fm is None:
        error(f"{TEMPLATE_STATE_PATH}: falta frontmatter YAML")
        return

    campos_requeridos = [
        "bloque",
        "documento",
        "actualizado_en",
        "template_id",
        "template_version",
        "template_core_policy",
        "template_last_reviewed",
        "template_repo",
        "template_core_paths",
    ]

    for campo in campos_requeridos:
        if campo not in fm or fm[campo] in (None, "", []):
            error(f"{TEMPLATE_STATE_PATH}: campo obligatorio '{campo}' ausente o vacío")

    if fm.get("bloque") and fm.get("bloque") != "00-meta":
        error(f"{TEMPLATE_STATE_PATH}: 'bloque' debe ser '00-meta'")

    if fm.get("documento") and fm.get("documento") != "template-state":
        error(f"{TEMPLATE_STATE_PATH}: 'documento' debe ser 'template-state'")

    policy = fm.get("template_core_policy")
    if policy and policy not in TEMPLATE_CORE_POLICIES:
        error(
            f"{TEMPLATE_STATE_PATH}: template_core_policy '{policy}' inválida. "
            f"Valores: {TEMPLATE_CORE_POLICIES}"
        )

    core_paths = fm.get("template_core_paths")
    if core_paths is not None:
        if not isinstance(core_paths, list) or not core_paths:
            error(f"{TEMPLATE_STATE_PATH}: template_core_paths debe ser una lista no vacía")
        else:
            for idx, entry in enumerate(core_paths, start=1):
                if not isinstance(entry, str) or not entry.strip():
                    error(f"{TEMPLATE_STATE_PATH}: template_core_paths[{idx}] debe ser una ruta no vacía")

    version = fm.get("template_version", "")
    if version and not re.match(r"^\d+\.\d+\.\d+$", str(version)):
        error(f"{TEMPLATE_STATE_PATH}: template_version '{version}' debe seguir semver simple X.Y.Z")


# ─── Validaciones ─────────────────────────────────────────────────────────────

def validar_nombre_carpeta_desarrollo(carpeta: Path):
    """Valida que el nombre siga el patrón {ID}--{slug}."""
    nombre = carpeta.name
    if not PATRON_TICKET_SLUG.match(nombre):
        error(f"{carpeta}: el nombre no sigue el patrón {{ID}}--{{slug}} "
              f"(ej: PROJ-123--nombre-feature)")
    if len(nombre) > MAX_NOMBRE_CARPETA_DESARROLLO:
        error(
            f"{carpeta}: el nombre tiene {len(nombre)} caracteres y supera el máximo "
            f"de {MAX_NOMBRE_CARPETA_DESARROLLO}"
        )


def validar_frontmatter_historia(filepath: Path, fm: dict, es_epica: bool = False):
    """Valida los campos obligatorios del frontmatter de una historia o épica."""
    campos_requeridos = CAMPOS_OBLIGATORIOS_EPICA if es_epica else CAMPOS_OBLIGATORIOS_HISTORIA

    for campo in campos_requeridos:
        if campo not in fm or fm[campo] in (None, "", []):
            error(f"{filepath}: campo obligatorio '{campo}' ausente o vacío")

    if "tipo" in fm and fm["tipo"] not in TIPOS_VALIDOS:
        error(f"{filepath}: tipo '{fm['tipo']}' inválido. Valores: {TIPOS_VALIDOS}")

    if "estado" in fm and fm["estado"] not in ESTADOS_VALIDOS:
        error(f"{filepath}: estado '{fm['estado']}' inválido. Valores: {ESTADOS_VALIDOS}")

    if "prioridad" in fm and fm["prioridad"] and fm["prioridad"] not in PRIORIDADES_VALIDAS:
        error(f"{filepath}: prioridad '{fm['prioridad']}' inválida. Valores: {PRIORIDADES_VALIDAS}")

    if "creado_en" not in fm or not fm.get("creado_en"):
        error(f"{filepath}: campo obligatorio 'creado_en' ausente o vacío")
    if "actualizado_en" not in fm or not fm.get("actualizado_en"):
        error(f"{filepath}: campo obligatorio 'actualizado_en' ausente o vacío")

    tickets = fm.get("tickets")
    validar_tickets(filepath, tickets)

    if "ai_context" in fm and isinstance(fm["ai_context"], dict):
        ai = fm["ai_context"]
        for campo in CAMPOS_AI_CONTEXT:
            if campo not in ai:
                warn(f"{filepath}: ai_context.{campo} no definido")
        if "nivel_riesgo" in ai and ai["nivel_riesgo"] not in RIESGOS_VALIDOS:
            error(f"{filepath}: ai_context.nivel_riesgo '{ai['nivel_riesgo']}' inválido")

    if not es_epica and ("epica" not in fm or not fm.get("epica")):
        error(f"{filepath}: campo 'epica' obligatorio en historias")

    validar_placeholders_por_estado(filepath, fm)
    validar_trazabilidad_referencias(filepath)


def validar_adr(filepath: Path):
    """Valida que el archivo ADR siga las convenciones."""
    if not PATRON_ADR.match(filepath.name):
        error(f"{filepath}: el nombre del ADR no sigue el patrón ADR-XXXX--titulo-slug.md")

    fm = parse_frontmatter(filepath)
    if fm is None:
        warn(f"{filepath}: ADR sin frontmatter YAML")
        return

    for campo in ["id", "titulo", "estado", "fecha"]:
        if campo not in fm or fm[campo] in (None, ""):
            error(f"{filepath}: ADR — campo '{campo}' ausente o vacío")

    estado = fm.get("estado", "")
    if estado and not (estado in ESTADOS_ADR_VALIDOS or estado.startswith("supersedida-por:")):
        error(f"{filepath}: estado ADR '{estado}' inválido")


def validar_desarrollos():
    """Recorre 09-desarrollos/epicas/ y valida todas las épicas e historias."""
    if not DESARROLLOS_PATH.exists():
        warn(f"{DESARROLLOS_PATH}: directorio de épicas no encontrado")
        return

    for epica_dir in sorted(DESARROLLOS_PATH.iterdir()):
        if not epica_dir.is_dir() or epica_dir.name.startswith("_"):
            continue

        print(f"  Validando épica: {epica_dir.name}")
        validar_nombre_carpeta_desarrollo(epica_dir)

        spec_epica = epica_dir / "spec.md"
        if not spec_epica.exists():
            error(f"{epica_dir}: falta spec.md en la épica")
        else:
            fm = parse_frontmatter(spec_epica)
            if fm:
                validar_frontmatter_historia(spec_epica, fm, es_epica=True)
                if fm.get("tipo") != "epica":
                    warn(f"{spec_epica}: se esperaba tipo 'epica', encontrado '{fm.get('tipo')}'")
                expected_id = extraer_ticket_id(epica_dir.name)
                if fm.get("id") and fm.get("id") != expected_id:
                    error(f"{spec_epica}: id del frontmatter ({fm.get('id')}) no coincide con carpeta ({expected_id})")

        for historia_dir in sorted(epica_dir.iterdir()):
            if not historia_dir.is_dir() or historia_dir.name.startswith("_"):
                continue

            validar_nombre_carpeta_desarrollo(historia_dir)

            spec_historia = historia_dir / "spec.md"
            if not spec_historia.exists():
                error(f"{historia_dir}: falta spec.md en la historia")
            else:
                fm = parse_frontmatter(spec_historia)
                if fm:
                    validar_frontmatter_historia(spec_historia, fm, es_epica=False)
                    expected_id = extraer_ticket_id(historia_dir.name)
                    if fm.get("id") and fm.get("id") != expected_id:
                        error(f"{spec_historia}: id del frontmatter ({fm.get('id')}) no coincide con carpeta ({expected_id})")
                    epica_declarada = fm.get("epica", "")
                    if epica_declarada and epica_declarada != epica_dir.name:
                        error(f"{spec_historia}: el campo 'epica' ({epica_declarada}) "
                              f"debe coincidir exactamente con la carpeta padre ({epica_dir.name})")


def validar_adrs():
    """Valida todos los ADRs en el repositorio."""
    for adr_file in DOCS_ROOT.rglob("ADR-*.md"):
        validar_adr(adr_file)


# ─── Registro de puntos de MVP-999 ────────────────────────────────────────────

REGISTRO_PUNTOS = DESARROLLOS_PATH / "MVP-999--pendientes-transversales-y-diferidos" / "spec.md"

#  | P-xxx | fecha | origen | categoria | descripcion | riesgo | bloquea | destino | estado | historia |
#
#  Se indexa **desde el final** a proposito: las descripciones son largas y pueden contener `|`
#  escapados, asi que contar desde el principio se rompe al primer punto que lo haga.
COL_ESTADO = -3
COL_DESTINO = -4

# Los cuatro estados que usa el registro hoy son `resuelto`, `backlog-post-mvp`, `pendiente` y
# `descartado`. Solo los dos ultimos —y el historico `aprobado-crear-historia`— dejan algo abierto.
ESTADOS_PUNTO_CERRADOS = {"resuelto", "descartado", "backlog-post-mvp"}

RE_ID_HISTORIA = re.compile(r"\bMVP-\d{3}\b")

# Prefijos de rama que usa el repositorio. Un destino que empiece por uno de ellos describe donde se
# hizo el trabajo, no a donde va: no vale para una fila todavia abierta.
RE_RAMA = re.compile(r"^(chore|feature|fix|hotfix|release)/")


def buscar_spec_por_id(ticket_id: str) -> Path | None:
    """Localiza el `spec.md` de una historia o epica por su identificador."""
    for spec in DESARROLLOS_PATH.rglob("spec.md"):
        if extraer_ticket_id(spec.parent.name) == ticket_id:
            return spec
    return None


def validar_registro_de_puntos():
    """
    MVP-799 (`P-096`) — Un punto cuya historia de destino ya esta `completado` tiene que estar
    `resuelto` en el registro.

    <b>Por que existe esta comprobacion.</b> Al cerrar `MVP-007` habia **quince** filas diciendo
    «pendiente de crear historia» sobre puntos que ya estaban construidos, `P-055` entre ellas — que es
    justo el punto que se perdio por esto: se anota el destino, la historia de destino se cierra sin
    recogerlo y nadie vuelve a mirar. Ninguna era funcionalidad perdida, pero el registro llevaba
    semanas mintiendo, y mientras dependa de que alguien se acuerde volvera a pasar.

    <b>El error se imputa al `spec.md` de la historia, no al registro.</b> No es un detalle: en modo
    `--solo-cambios` un hallazgo sobre un fichero que el PR no toca se degrada a aviso, y el PR que
    cierra una historia toca su spec pero normalmente no el registro. Atribuyendolo al registro, esto
    no bloquearia nunca y seria otra regla que nadie comprueba.
    """
    if not REGISTRO_PUNTOS.exists():
        return

    contenido = read_markdown_content(REGISTRO_PUNTOS)
    estados_por_historia: dict[str, str | None] = {}

    for linea in contenido.splitlines():
        if not linea.startswith("| P-"):
            continue

        celdas = linea.split("|")
        if len(celdas) < 6:
            continue

        punto = celdas[1].strip()
        estado_punto = celdas[COL_ESTADO].strip()
        destino = celdas[COL_DESTINO].strip()

        # Estados **decididos**: la fila no espera nada de nadie. `backlog-post-mvp` entra aqui
        # aunque su columna de destino a veces nombre la historia que lo detecto —«Backlog post-MVP
        # (solo la parte de ER)», por ejemplo—: eso es procedencia, no un encargo pendiente.
        if estado_punto in ESTADOS_PUNTO_CERRADOS:
            continue

        # <b>Punto ciego que costo tres filas.</b> La comprobacion de abajo se apoya en el `estado` del
        # `spec.md` de la historia de destino, asi que solo alcanza a los destinos que nombran una. Los
        # once derivados de `MVP-799` fueron a ramas `chore/`, que no tienen spec, y tres se quedaron
        # en `pendiente` con el trabajo ya en `develop` **sin que nada lo dijera** — el mismo patron de
        # `P-096`, repetido dias despues de construir la guarda contra el.
        #
        # Se cierra por vocabulario en vez de preguntandole a git: una rama es **donde se hace** el
        # trabajo, no un plan. Si esta hecho, la fila va `resuelto` y la rama se anota en la columna de
        # historia, que es su sitio; si no lo esta, el destino es `por decidir`. Un nombre de rama en el
        # destino de una fila abierta describe un estado que no existe.
        if RE_RAMA.search(destino):
            error(
                f"{REGISTRO_PUNTOS}: {punto} esta en '{estado_punto}' y su destino es la rama "
                f"'{destino}'. Una rama no es un destino de una fila abierta: si el trabajo esta "
                f"hecho marca el punto 'resuelto' y deja la rama en la columna de historia; si no, "
                f"pon 'por decidir'."
            )
            continue

        # «por decidir», «Hito H» y demas no nombran una historia: no hay nada que comprobar.
        historias = RE_ID_HISTORIA.findall(destino)
        if not historias:
            continue

        for ticket in historias:
            if ticket not in estados_por_historia:
                spec = buscar_spec_por_id(ticket)
                fm = parse_frontmatter(spec) if spec else None
                estados_por_historia[ticket] = fm.get("estado") if fm else None
                # Se guarda tambien la ruta para poder imputarle el error.
                estados_por_historia[f"{ticket}:path"] = str(spec) if spec else None

            if estados_por_historia[ticket] != "completado":
                continue

            ruta = estados_por_historia.get(f"{ticket}:path")
            destino_error = Path(ruta) if ruta else REGISTRO_PUNTOS
            error(
                f"{destino_error}: {ticket} esta 'completado' pero {punto} sigue en "
                f"'{estado_punto}' en el registro de MVP-999. Cierra la fila con la evidencia "
                f"de lo que se construyo, o cambia su destino si {ticket} no lo recogio."
            )


# ─── Trazabilidad de los requisitos de usuario ────────────────────────────────

REQUISITOS_USUARIO = DOCS_ROOT / "01-producto" / "definicion-requisitos-usuario.md"

# Los 47 requisitos se declaran de dos formas historicas en el documento: los trece primeros como
# seccion (`### RU-01 - ...`) y el resto como vineta (`- **RU-14: ...**`). Se aceptan las dos en vez
# de reescribir el documento, que esta explicitamente fuera del alcance de `MVP-809`.
RE_RU_SECCION = re.compile(r"^###\s+(RU-\d{2})\b")
RE_RU_VINETA = re.compile(r"^-\s+\*\*(RU-\d{2})\s*[:：]")
RE_RU_ESTADO = re.compile(r"^\s*(?:-\s+)?(?:\*\*)?Estado(?:\*\*)?\s*[:：]\s*(.+?)\s*$")
RE_RU_FILA = re.compile(r"^\|\s*(RU-\d{2})\s*\|")

#  | RU-xx | titulo | estado declarado | destino | estado real |
#
#  Se indexa **desde el final**, por el mismo motivo que el registro de puntos: la columna de destino
#  es la larga y la que puede acabar llevando barras verticales escapadas.
COL_RU_ESTADO_REAL = -2
COL_RU_DESTINO = -3
COL_RU_DECLARADO = -4

# <b>Que cuenta como «tener destino».</b> No basta con que la celda tenga texto: tiene que **nombrar
# algo que ya este vigilado por otra parte del sistema**. Una regla de negocio (`RN-xxx`), una historia
# (`MVP-xxx`), un punto del registro (`P-xxx`) o un ADR. Prosa como «pendiente» o «se vera» no vale,
# porque es exactamente lo que dejo `RU-24` sin construir durante siete epicas.
RE_RU_DESTINO_ID = re.compile(r"\b(?:RN-\d{3}|MVP-\d{3}|P-\d{3}|ADR-\d{4})\b")
RE_PUNTO_REGISTRO = re.compile(r"\bP-\d{3}\b")

# Vocabulario cerrado de la columna «estado real». `en <historia>` se valida aparte porque lleva
# argumento.
ESTADOS_RU_REALES = {"entregado", "entregado con hueco", "backlog", "descartado"}
ESTADOS_RU_ENTREGADOS = {"entregado", "entregado con hueco"}


def normalizar_estado_declarado(texto: str) -> str | None:
    """Reduce el «Estado:» escrito a mano a un vocabulario de cuatro valores."""
    t = texto.strip().lower().rstrip(".")
    if t.startswith("mvp"):
        return "mvp"
    if "backlog" in t:
        return "backlog"
    if "fase posterior" in t or "fase futura" in t:
        return "fase-posterior"
    if t.startswith("descartado"):
        return "descartado"
    return None


def leer_requisitos_declarados(contenido: str) -> dict[str, str | None]:
    """Devuelve `{RU-xx: texto del Estado declarado}` leyendo el cuerpo del documento."""
    declarados: dict[str, str | None] = {}
    actual: str | None = None

    for linea in contenido.splitlines():
        m = RE_RU_SECCION.match(linea) or RE_RU_VINETA.match(linea)
        if m:
            actual = m.group(1)
            declarados.setdefault(actual, None)
            continue

        if actual is None:
            continue

        # Un encabezado o un separador cierran el bloque del requisito.
        if linea.startswith("#") or linea.startswith("---"):
            actual = None
            continue

        m = RE_RU_ESTADO.match(linea)
        if m and declarados.get(actual) is None:
            declarados[actual] = m.group(1)

    return declarados


def leer_matriz_requisitos(contenido: str) -> dict[str, list[str]]:
    """Devuelve `{RU-xx: celdas de su fila}` de la matriz de trazabilidad."""
    filas: dict[str, list[str]] = {}
    for linea in contenido.splitlines():
        m = RE_RU_FILA.match(linea)
        if not m:
            continue
        filas[m.group(1)] = [celda.strip() for celda in linea.split("|")]
    return filas


def validar_trazabilidad_requisitos_usuario():
    """
    MVP-809 (`P-114`) — Un requisito de usuario marcado «Estado: MVP» tiene que tener destino
    declarado en la matriz de trazabilidad, y si su destino son historias ya `completado` tiene que
    constar como entregado.

    <b>Por que existe esta comprobacion.</b> De los 47 requisitos, 44 no se citaban en ningun documento
    fuera del que los define: las epicas trazan contra `RN-xxx` y nadie trazaba contra `RU-xxx`. El
    resultado fue que `RU-24` (aviso de duplicados) llego al final del roadmap marcado MVP sin haberse
    construido ni descartado, y que `RU-36` decia una cosa mientras el producto hacia otra. Es el mismo
    patron que `P-096` describio en el registro de puntos: una cadena que solo se sostiene si alguien se
    acuerda.

    <b>Cada error se imputa a quien lo va a tocar.</b> En `--solo-cambios` un hallazgo sobre un fichero
    que el PR no toca se degrada a aviso, asi que la imputacion decide si la regla bloquea o no:

    - Falta de destino, estado incoherente o fila ausente -> al **documento de requisitos**, porque el
      PR que da de alta o cambia un requisito toca ese fichero y ningun otro.
    - Historia de destino ya `completado` sin entregar -> al **`spec.md` de la historia**, exactamente
      como en `validar_registro_de_puntos()`: el PR que cierra una historia toca su spec y casi nunca
      el documento de requisitos.
    """
    if not REQUISITOS_USUARIO.exists():
        return

    contenido = read_markdown_content(REQUISITOS_USUARIO)
    declarados = leer_requisitos_declarados(contenido)
    filas = leer_matriz_requisitos(contenido)

    # Una guarda que se puede desactivar reordenando el documento no es una guarda.
    if not declarados:
        error(
            f"{REQUISITOS_USUARIO}: no se encontro ninguna declaracion de requisito 'RU-xx'. "
            f"La comprobacion de trazabilidad de MVP-809 depende de encontrarlas: revisa el formato "
            f"('### RU-xx - titulo' o '- **RU-xx: titulo**')."
        )
        return

    for ru in sorted(set(filas) - set(declarados)):
        error(
            f"{REQUISITOS_USUARIO}: la matriz de trazabilidad tiene una fila para {ru}, pero el "
            f"documento no define ese requisito."
        )

    estados_por_historia: dict[str, str | None] = {}
    rutas_por_historia: dict[str, str | None] = {}

    for ru in sorted(declarados):
        estado_texto = declarados[ru]
        if estado_texto is None:
            error(
                f"{REQUISITOS_USUARIO}: {ru} no declara 'Estado:'. Todo requisito tiene que decir si "
                f"es MVP, backlog, fase posterior o descartado."
            )
            estado_declarado = None
        else:
            estado_declarado = normalizar_estado_declarado(estado_texto)
            if estado_declarado is None:
                error(
                    f"{REQUISITOS_USUARIO}: {ru} declara 'Estado: {estado_texto}', que no es un valor "
                    f"reconocido (MVP | Backlog | Fase posterior | Descartado)."
                )

        celdas = filas.get(ru)
        if celdas is None:
            error(
                f"{REQUISITOS_USUARIO}: {ru} no tiene fila en la matriz de trazabilidad. Todo "
                f"requisito tiene que declarar donde se recoge y en que estado esta."
            )
            continue

        if len(celdas) < 6:
            error(
                f"{REQUISITOS_USUARIO}: la fila de {ru} en la matriz de trazabilidad no tiene las "
                f"cinco columnas esperadas (requisito, titulo, estado declarado, destino, estado real)."
            )
            continue

        declarado_matriz = normalizar_estado_declarado(celdas[COL_RU_DECLARADO])
        destino = celdas[COL_RU_DESTINO]
        estado_real = celdas[COL_RU_ESTADO_REAL].lower()

        if estado_declarado is not None and declarado_matriz != estado_declarado:
            error(
                f"{REQUISITOS_USUARIO}: {ru} declara 'Estado: {estado_texto}' en su definicion y "
                f"'{celdas[COL_RU_DECLARADO]}' en la matriz de trazabilidad. Las dos tienen que decir "
                f"lo mismo."
            )

        if estado_real.startswith("en "):
            if not RE_ID_HISTORIA.search(celdas[COL_RU_ESTADO_REAL]):
                error(
                    f"{REQUISITOS_USUARIO}: {ru} esta '{celdas[COL_RU_ESTADO_REAL]}' en la matriz, "
                    f"pero no nombra la historia que lo esta construyendo."
                )
        elif estado_real not in ESTADOS_RU_REALES:
            error(
                f"{REQUISITOS_USUARIO}: {ru} tiene estado real '{celdas[COL_RU_ESTADO_REAL]}', que no "
                f"es un valor del vocabulario (entregado | entregado con hueco | en <historia> | "
                f"backlog | descartado)."
            )

        # Un requisito entregado a medias solo vale si el hueco esta anotado en el registro de puntos,
        # que es donde `P-096` ya obliga a que no se pudra.
        if estado_real == "entregado con hueco" and not RE_PUNTO_REGISTRO.search(destino):
            error(
                f"{REQUISITOS_USUARIO}: {ru} esta 'entregado con hueco' pero su destino no cita ningun "
                f"punto 'P-xxx' del registro de MVP-999 que persiga el hueco."
            )

        # El corazon de la comprobacion: un requisito MVP sin destino.
        if estado_declarado == "mvp" and not RE_RU_DESTINO_ID.search(destino):
            error(
                f"{REQUISITOS_USUARIO}: {ru} esta marcado 'Estado: MVP' y no tiene destino declarado. "
                f"Escribe en la matriz de trazabilidad la regla (RN-xxx), la historia (MVP-xxx), el "
                f"punto del registro (P-xxx) o el ADR que lo recoge."
            )

        historias = RE_ID_HISTORIA.findall(destino)
        if not historias:
            continue

        for ticket in historias:
            if ticket not in estados_por_historia:
                spec = buscar_spec_por_id(ticket)
                fm = parse_frontmatter(spec) if spec else None
                estados_por_historia[ticket] = fm.get("estado") if fm else None
                rutas_por_historia[ticket] = str(spec) if spec else None

        # La segunda mitad de la comprobacion es **solo para los requisitos MVP**. Uno declarado
        # backlog o fase posterior puede recibir una rebanada de una historia sin quedar entregado
        # —`RU-31` y el minimo in-app de `MVP-808` son exactamente ese caso— y exigirle «entregado»
        # ensenaria a escribirlo para callar al gate, que es el vicio que esta guarda combate.
        if estado_declarado != "mvp":
            continue

            if rutas_por_historia[ticket] is None:
                error(
                    f"{REQUISITOS_USUARIO}: el destino de {ru} nombra {ticket}, que no existe en "
                    f"09-desarrollos. Corrige la referencia."
                )

        conocidas = [t for t in historias if rutas_por_historia.get(t)]
        if not conocidas:
            continue

        # Basta con que **una** siga abierta para que el requisito pueda estar legitimamente en vuelo:
        # hay requisitos que se reparten entre varias historias y cerrar la primera no los entrega.
        if any(estados_por_historia[t] != "completado" for t in conocidas):
            continue

        if estado_real in ESTADOS_RU_ENTREGADOS:
            continue

        ultima = conocidas[-1]
        destino_error = Path(rutas_por_historia[ultima])
        error(
            f"{destino_error}: {ultima} esta 'completado' y es el destino de {ru}, pero el requisito "
            f"sigue como '{celdas[COL_RU_ESTADO_REAL]}' en la matriz de trazabilidad de "
            f"01-producto/definicion-requisitos-usuario.md. Marcalo 'entregado', o 'entregado con "
            f"hueco' con el punto que persigue lo que falta, o cambia su destino."
        )


# ─── Generación de índices ────────────────────────────────────────────────────

ESTADO_EMOJI = {
    "borrador": "📝",
    "en-revision": "👀",
    "aprobado": "[OK]",
    "en-progreso": "🔄",
    "en-testing": "🧪",
    "completado": "✔️",
    "cancelado": "[X]",
}


def generar_indice_epica(epica_dir: Path):
    """Genera o actualiza el _indice.md de una épica."""
    historias = []

    for historia_dir in sorted(epica_dir.iterdir()):
        if not historia_dir.is_dir() or historia_dir.name.startswith("_"):
            continue

        spec = historia_dir / "spec.md"
        if not spec.exists():
            continue

        fm = parse_frontmatter(spec)
        if not fm:
            continue

        historias.append({
            "id": fm.get("id", historia_dir.name),
            "titulo": fm.get("titulo", "Sin título"),
            "estado": fm.get("estado", "borrador"),
            "responsable": fm.get("responsable", "—"),
            "prioridad": fm.get("prioridad", "—"),
            "hito": fm.get("hito", "—"),
            "path": f"./{historia_dir.name}/spec.md",
        })

    spec_epica = epica_dir / "spec.md"
    fm_epica = parse_frontmatter(spec_epica) if spec_epica.exists() else {}
    titulo_epica = fm_epica.get("titulo", epica_dir.name) if fm_epica else epica_dir.name
    hito_epica = fm_epica.get("hito", "—") if fm_epica else "—"

    completadas = sum(1 for h in historias if h["estado"] == "completado")
    total = len(historias)

    lines = [
        f"# Índice — {epica_dir.name}: {titulo_epica}",
        "",
        f"> **Progreso**: {completadas}/{total} completadas · **Hito**: {hito_epica}",
        f"> _Generado automáticamente por `validar_kb.py`. No editar manualmente._",
        "",
    ]

    if historias:
        lines += [
            "| Historia | Título | Estado | Responsable | Prioridad |",
            "|----------|--------|--------|-------------|-----------|",
        ]
        for h in historias:
            emoji = ESTADO_EMOJI.get(h["estado"], "")
            lines.append(
                f"| [{h['id']}]({h['path']}) "
                f"| {h['titulo']} "
                f"| {emoji} {h['estado']} "
                f"| {h['responsable']} "
                f"| {h['prioridad']} |"
            )
    else:
        lines.append("_Sin historias documentadas aún._")

    indice_path = epica_dir / "_indice.md"
    indice_path.write_text("\n".join(lines) + "\n", encoding="utf-8-sig")
    print(f"  Generado: {indice_path}")


def generar_todos_los_indices():
    """Regenera _indice.md para todas las épicas."""
    if not DESARROLLOS_PATH.exists():
        print(f"  Directorio {DESARROLLOS_PATH} no encontrado. Nada que generar.")
        return

    for epica_dir in sorted(DESARROLLOS_PATH.iterdir()):
        if epica_dir.is_dir() and not epica_dir.name.startswith("_"):
            generar_indice_epica(epica_dir)


# ─── Main ─────────────────────────────────────────────────────────────────────

def main():
    global SOLO_CAMBIOS, CHANGED_DOC_PATHS

    parser = argparse.ArgumentParser(
        description="Validador y generador de índices de la Knowledge Base"
    )
    parser.add_argument("--validar", action="store_true", help="Validar estructura y frontmatter")
    parser.add_argument("--generar-indices", action="store_true", help="Regenerar _indice.md de épicas")
    parser.add_argument(
        "--solo-cambios",
        action="store_true",
        help="Trata hallazgos en docs no tocados como warning (legacy), y bloquea solo cambios actuales",
    )
    parser.add_argument(
        "--base-ref",
        default="main",
        help="Ref git para calcular cambios cuando se usa --solo-cambios (default: main)",
    )
    args = parser.parse_args()

    if not args.validar and not args.generar_indices:
        parser.print_help()
        sys.exit(0)

    if args.solo_cambios:
        SOLO_CAMBIOS = True
        CHANGED_DOC_PATHS = obtener_docs_cambiados(args.base_ref)
        if CHANGED_DOC_PATHS:
            print(f"\nℹ️  Modo solo cambios activo. Archivos docs en alcance: {len(CHANGED_DOC_PATHS)}")
        else:
            print("\nℹ️  Modo solo cambios activo, pero no se detectaron cambios en docs. Se valida en modo estricto.")

    if args.validar:
        print("\nValidando Knowledge Base...\n")
        validar_longitudes_docs()
        validar_template_state()
        validar_desarrollos()
        validar_adrs()
        validar_registro_de_puntos()
        validar_trazabilidad_requisitos_usuario()

        if warnings:
            print(f"\n[WARN] {len(warnings)} advertencia(s):")
            for w in warnings:
                print(w)

        if errors:
            print(f"\n[FAIL] {len(errors)} error(es) encontrado(s):")
            for e in errors:
                print(e)
            print("\nCorrige los errores antes de continuar.\n")
            sys.exit(1)
        else:
            print(f"\n[OK] Validación completada. {len(warnings)} advertencia(s), 0 errores.\n")

    if args.generar_indices:
        print("\nRegenerando índices de épicas...\n")
        generar_todos_los_indices()
        print("\n[OK] Índices generados.\n")


if __name__ == "__main__":
    main()
