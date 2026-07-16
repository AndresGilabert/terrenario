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


# ─── Generación de índices ────────────────────────────────────────────────────

ESTADO_EMOJI = {
    "borrador": "📝",
    "en-revision": "👀",
    "aprobado": "✅",
    "en-progreso": "🔄",
    "en-testing": "🧪",
    "completado": "✔️",
    "cancelado": "❌",
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
        print("\n📋 Validando Knowledge Base...\n")
        validar_longitudes_docs()
        validar_template_state()
        validar_desarrollos()
        validar_adrs()

        if warnings:
            print(f"\n⚠️  {len(warnings)} advertencia(s):")
            for w in warnings:
                print(w)

        if errors:
            print(f"\n❌ {len(errors)} error(es) encontrado(s):")
            for e in errors:
                print(e)
            print("\nCorrige los errores antes de continuar.\n")
            sys.exit(1)
        else:
            print(f"\n✅ Validación completada. {len(warnings)} advertencia(s), 0 errores.\n")

    if args.generar_indices:
        print("\n📑 Regenerando índices de épicas...\n")
        generar_todos_los_indices()
        print("\n✅ Índices generados.\n")


if __name__ == "__main__":
    main()
