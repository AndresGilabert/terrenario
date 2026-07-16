#!/usr/bin/env python3
"""
Pipeline unico de validacion para KB.

Uso recomendado:
    python docs/00-meta/scripts/validar_pipeline_kb.py

Opciones utiles:
    --solo-cambios --base-ref origin/develop
    --skip-markdownlint
    --check-indices-clean
"""

from __future__ import annotations

import argparse
import shutil
import subprocess
import sys
from pathlib import Path


SCRIPT_PATH = Path(__file__).resolve()
SCRIPTS_DIR = SCRIPT_PATH.parent
DOCS_ROOT = SCRIPTS_DIR.parent.parent  # docs/
REPO_ROOT = DOCS_ROOT.parent
VALIDADOR = SCRIPTS_DIR / "validar_kb.py"
MARKDOWN_CONFIG = DOCS_ROOT / "00-meta" / ".markdownlint.json"


def run_command(cmd: list[str], description: str) -> int:
    print(f"\n[RUN] {description}")
    print("      " + " ".join(cmd))
    result = subprocess.run(cmd, cwd=REPO_ROOT)
    if result.returncode != 0:
        print(f"[FAIL] {description} (exit={result.returncode})")
    else:
        print(f"[OK]   {description}")
    return result.returncode


def check_indices_clean() -> int:
    print("\n[RUN] Comprobar _indice.md sin cambios pendientes")
    result = subprocess.run(
        ["git", "status", "--porcelain"],
        cwd=REPO_ROOT,
        capture_output=True,
        text=True,
        check=False,
    )
    if result.returncode != 0:
        print("[FAIL] No se pudo ejecutar 'git status --porcelain'")
        return result.returncode

    changed_indices: list[str] = []
    for line in result.stdout.splitlines():
        line = line.strip()
        if not line:
            continue
        path = line[3:].strip()
        if path.replace("\\", "/").endswith("_indice.md"):
            changed_indices.append(path)

    if changed_indices:
        print("[FAIL] Hay _indice.md desactualizados. Ejecuta generar indices y anade los cambios:")
        for path in changed_indices:
            print(f"       - {path}")
        return 1

    print("[OK]   Todos los _indice.md estan actualizados")
    return 0


def resolve_markdownlint() -> str | None:
    return shutil.which("markdownlint")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Pipeline unico de validacion de la KB")
    parser.add_argument(
        "--solo-cambios",
        action="store_true",
        help="Pasa --solo-cambios a validar_kb.py",
    )
    parser.add_argument(
        "--base-ref",
        default="main",
        help="Base ref para --solo-cambios (default: main)",
    )
    parser.add_argument(
        "--skip-markdownlint",
        action="store_true",
        help="Omite markdownlint",
    )
    parser.add_argument(
        "--check-indices-clean",
        action="store_true",
        help="Falla si hay _indice.md con cambios pendientes tras generar indices",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()

    if not VALIDADOR.exists():
        print(f"[FAIL] No existe el validador: {VALIDADOR}")
        return 1

    base_cmd = [sys.executable, str(VALIDADOR)]

    cmd_validar = base_cmd + ["--validar"]
    if args.solo_cambios:
        cmd_validar += ["--solo-cambios", "--base-ref", args.base_ref]

    status = run_command(cmd_validar, "Validar estructura y frontmatter")
    if status != 0:
        return status

    status = run_command(base_cmd + ["--generar-indices"], "Regenerar indices de epicas")
    if status != 0:
        return status

    if args.check_indices_clean:
        status = check_indices_clean()
        if status != 0:
            return status

    if not args.skip_markdownlint:
        markdownlint_bin = resolve_markdownlint()
        if not markdownlint_bin:
            print("\n[FAIL] markdownlint no esta instalado o no esta en PATH.")
            print("       Instala markdownlint-cli (npm i -g markdownlint-cli)")
            return 1

        status = run_command(
            [markdownlint_bin, "**/*.md", "-c", str(MARKDOWN_CONFIG)],
            "Linting markdown",
        )
        if status != 0:
            return status

    print("\n[OK] Pipeline de validacion KB completado")
    return 0


if __name__ == "__main__":
    sys.exit(main())
