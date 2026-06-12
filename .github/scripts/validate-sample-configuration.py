#!/usr/bin/env python3
"""Validate slnx-mermaid.yml against schema and sample coverage rules."""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any

import yaml
from jsonschema import Draft7Validator

SCHEMA_PATH = Path("slnx-mermaid.schema.json")
SAMPLE_PATH = Path("slnx-mermaid.yml")


def fail(message: str) -> None:
    print(message)
    raise SystemExit(1)


def load_inputs() -> tuple[dict[str, Any], Any]:
    if not SCHEMA_PATH.is_file():
        fail(f"Missing schema file: {SCHEMA_PATH}")
    if not SAMPLE_PATH.is_file():
        fail(f"Missing sample configuration file: {SAMPLE_PATH}")

    schema = json.loads(SCHEMA_PATH.read_text(encoding="utf-8-sig"))
    documents = list(yaml.safe_load_all(SAMPLE_PATH.read_text(encoding="utf-8-sig")))
    if len(documents) != 1 or documents[0] is None:
        fail(f"{SAMPLE_PATH} must contain exactly one non-empty YAML document.")

    return schema, documents[0]


def validate_schema_conformance(schema: dict[str, Any], sample: Any) -> None:
    errors = sorted(Draft7Validator(schema).iter_errors(sample), key=lambda error: list(error.path))
    if not errors:
        print(f"Schema conformance: PASS ({SAMPLE_PATH} conforms to {SCHEMA_PATH})")
        return

    print(f"{SAMPLE_PATH} does not conform to {SCHEMA_PATH}:")
    for error in errors:
        location = ".".join(str(part) for part in error.path) or "<root>"
        print(f"- {location}: {error.message}")
    raise SystemExit(1)


def nested_value(document: Any, path: tuple[str, ...]) -> Any:
    current = document
    for part in path:
        if not isinstance(current, dict) or part not in current:
            return None
        current = current[part]
    return current


def fixed_schema_paths(schema: dict[str, Any]) -> list[tuple[str, ...]]:
    definitions = schema.get("definitions", {})

    def resolve(node: Any) -> Any:
        if not isinstance(node, dict) or "$ref" not in node:
            return node

        prefix = "#/definitions/"
        ref = node["$ref"]
        if not isinstance(ref, str) or not ref.startswith(prefix):
            fail(f"Unsupported schema reference: {ref}")
        return definitions[ref[len(prefix):]]

    def collect(node: Any, prefix: tuple[str, ...]) -> list[tuple[str, ...]]:
        node = resolve(node)
        if not isinstance(node, dict):
            return []

        paths: list[tuple[str, ...]] = []
        for name, child_schema in node.get("properties", {}).items():
            path = prefix + (name,)
            paths.append(path)
            paths.extend(collect(child_schema, path))
        return paths

    return collect(schema, ())


def dynamic_mapping_gaps(schema: dict[str, Any], sample: Any) -> list[str]:
    mappings = nested_value(sample, ("ui", "mappings"))
    values = list(mappings.values()) if isinstance(mappings, dict) else []
    object_values = [value for value in values if isinstance(value, dict)]

    covered = {
        "ui.mappings.<project>: named palette color string": any(
            isinstance(value, str) and not value.startswith("#") for value in values
        ),
        "ui.mappings.<project>: #RRGGBB color string": any(
            isinstance(value, str) and value.startswith("#") for value in values
        ),
        "ui.mappings.<project>: object style override": bool(object_values),
    }

    style_schema = schema.get("definitions", {}).get("UiStyleConfig", {})
    for name in sorted(style_schema.get("properties", {})):
        covered[f"ui.mappings.<project>.{name}"] = any(name in value for value in object_values)

    return [label for label, is_covered in covered.items() if not is_covered]


def validate_sample_coverage(schema: dict[str, Any], sample: Any) -> None:
    missing = [
        ".".join(path)
        for path in fixed_schema_paths(schema)
        if nested_value(sample, path) is None
    ]
    missing.extend(dynamic_mapping_gaps(schema, sample))
    missing = sorted(set(missing))

    if not missing:
        print(f"Schema option coverage: PASS ({SAMPLE_PATH} covers fixed options from {SCHEMA_PATH})")
        return

    print("Sample configuration coverage validation failed.")
    print(f"Schema: {SCHEMA_PATH}")
    print(f"Sample: {SAMPLE_PATH}")
    print("Missing schema coverage:")
    for item in missing:
        print(f"- {item}")
    raise SystemExit(1)


def main() -> int:
    schema, sample = load_inputs()
    validate_schema_conformance(schema, sample)
    validate_sample_coverage(schema, sample)
    print("Sample configuration validation: PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
