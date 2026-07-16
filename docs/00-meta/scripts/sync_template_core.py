#!/usr/bin/env python3
"""
Sincroniza el núcleo de plantilla declarado en docs/00-meta/template-state.md.

Uso:
    python sync_template_core.py --source ../IA_DOC_Template --plan
    python sync_template_core.py --source ../IA_DOC_Template --apply
    python sync_template_core.py --source ../IA_DOC_Template --apply --delete
"""

from __future__ import annotations

import argparse
import hashlib
import shutil
import sys
from dataclasses import dataclass
from datetime import date
from pathlib import Path

try:
    import yaml
except ImportError:
    print("ERROR: PyYAML no está instalado. Ejecuta: pip install pyyaml")
    sys.exit(1)


SCRIPT_DIR = Path(__file__).resolve().parent
DOCS_ROOT = SCRIPT_DIR.parent.parent
TARGET_REPO_ROOT = DOCS_ROOT.parent
TEMPLATE_STATE_RELATIVE = Path("docs/00-meta/template-state.md")


@dataclass
class Action:
    kind: str
    relative_path: str
    detail: str = ""


def load_frontmatter(path: Path) -> tuple[dict, str]:
    text = path.read_text(encoding="utf-8-sig")
    if not text.startswith("---"):
        raise ValueError(f"{path}: falta frontmatter YAML")

    end = text.find("---", 3)
    if end == -1:
        raise ValueError(f"{path}: frontmatter YAML no cerrado correctamente")

    frontmatter = yaml.safe_load(text[3:end]) or {}
    return frontmatter, text


def save_frontmatter(path: Path, frontmatter: dict, original_text: str):
    end = original_text.find("---", 3)
    body = original_text[end + 3:]
    dumped = yaml.safe_dump(frontmatter, allow_unicode=True, sort_keys=False).strip()
    content = f"---\n{dumped}\n---{body}"
    path.write_text(content, encoding="utf-8-sig")


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(65536), b""):
            digest.update(chunk)
    return digest.hexdigest()


def collect_relative_files(root: Path, relative_entry: str) -> tuple[set[str], bool]:
    entry = root / relative_entry
    normalized = relative_entry.replace("\\", "/")

    if not entry.exists():
        raise FileNotFoundError(f"{entry}: no existe")

    if entry.is_file():
        return {normalized}, False

    files = {
        str(path.relative_to(root)).replace("\\", "/")
        for path in entry.rglob("*")
        if path.is_file()
    }
    return files, True


def build_actions(source_root: Path, target_root: Path, core_paths: list[str], allow_delete: bool) -> list[Action]:
    actions: list[Action] = []

    for core_entry in core_paths:
        source_files, is_directory = collect_relative_files(source_root, core_entry)
        target_files, _ = collect_relative_files(target_root, core_entry) if (target_root / core_entry).exists() else (set(), is_directory)

        for relative_file in sorted(source_files):
            source_path = source_root / relative_file
            target_path = target_root / relative_file

            if not target_path.exists():
                actions.append(Action("create", relative_file))
                continue

            if sha256_file(source_path) != sha256_file(target_path):
                actions.append(Action("update", relative_file))
            else:
                actions.append(Action("unchanged", relative_file))

        if allow_delete:
            for relative_file in sorted(target_files - source_files):
                actions.append(Action("delete", relative_file, detail=f"dentro de {core_entry}"))

    order = {"create": 0, "update": 1, "delete": 2, "unchanged": 3}
    actions.sort(key=lambda item: (order.get(item.kind, 99), item.relative_path))
    return actions


def ensure_parent(path: Path):
    path.parent.mkdir(parents=True, exist_ok=True)


def apply_actions(source_root: Path, target_root: Path, actions: list[Action]):
    for action in actions:
        source_path = source_root / action.relative_path
        target_path = target_root / action.relative_path

        if action.kind in {"create", "update"}:
            ensure_parent(target_path)
            shutil.copy2(source_path, target_path)
        elif action.kind == "delete" and target_path.exists():
            target_path.unlink()


def update_template_state_version(target_root: Path, source_root: Path):
    target_state_path = target_root / TEMPLATE_STATE_RELATIVE
    source_state_path = source_root / TEMPLATE_STATE_RELATIVE

    if not target_state_path.exists() or not source_state_path.exists():
        return

    target_fm, target_text = load_frontmatter(target_state_path)
    source_fm, _ = load_frontmatter(source_state_path)

    source_version = source_fm.get("template_version")
    source_repo = source_fm.get("template_repo")
    changed = False

    if source_version and target_fm.get("template_version") != source_version:
        target_fm["template_version"] = source_version
        changed = True

    if source_repo and not target_fm.get("template_repo"):
        target_fm["template_repo"] = source_repo
        changed = True

    target_fm["template_last_reviewed"] = str(date.today())
    target_fm["actualizado_en"] = str(date.today())
    changed = True

    if changed:
        save_frontmatter(target_state_path, target_fm, target_text)


def print_actions(actions: list[Action]):
    counts: dict[str, int] = {}
    for action in actions:
        counts[action.kind] = counts.get(action.kind, 0) + 1

    print("\nPlan de sincronización del núcleo:\n")
    for action in actions:
        suffix = f" ({action.detail})" if action.detail else ""
        print(f"- {action.kind.upper():9} {action.relative_path}{suffix}")

    print(
        f"\nResumen: create={counts.get('create', 0)}, update={counts.get('update', 0)}, "
        f"delete={counts.get('delete', 0)}, unchanged={counts.get('unchanged', 0)}"
    )


def main():
    parser = argparse.ArgumentParser(description="Sincroniza el núcleo declarado en template-state.md")
    parser.add_argument("--source", required=True, help="Ruta local a la plantilla fuente")
    parser.add_argument("--plan", action="store_true", help="Muestra el plan sin aplicar cambios")
    parser.add_argument("--apply", action="store_true", help="Aplica cambios sobre el núcleo")
    parser.add_argument("--delete", action="store_true", help="Elimina archivos del núcleo ausentes en la fuente")
    args = parser.parse_args()

    if not args.plan and not args.apply:
        parser.error("Debes indicar --plan o --apply")

    source_root = Path(args.source).resolve()
    target_root = TARGET_REPO_ROOT

    if not (source_root / TEMPLATE_STATE_RELATIVE).exists():
        print(f"ERROR: la fuente no contiene {TEMPLATE_STATE_RELATIVE}")
        sys.exit(1)

    target_state_path = target_root / TEMPLATE_STATE_RELATIVE
    if not target_state_path.exists():
        print(f"ERROR: el proyecto consumidor no contiene {TEMPLATE_STATE_RELATIVE}")
        sys.exit(1)

    target_fm, _ = load_frontmatter(target_state_path)
    core_paths = target_fm.get("template_core_paths")
    if not isinstance(core_paths, list) or not core_paths:
        print(f"ERROR: {target_state_path} debe declarar template_core_paths como lista no vacía")
        sys.exit(1)

    actions = build_actions(source_root, target_root, core_paths, allow_delete=args.delete)
    print_actions(actions)

    if args.apply:
        apply_actions(source_root, target_root, [action for action in actions if action.kind != "unchanged"])
        update_template_state_version(target_root, source_root)
        print("\nSincronización aplicada correctamente.")


if __name__ == "__main__":
    main()