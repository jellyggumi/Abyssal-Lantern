#!/usr/bin/env python3
"""Capture assistant outputs on Stop, rtk-compress them, and refresh the llm-wiki graph.

Used by ~/.claude/hooks/knowledge-pipeline.sh. Best-effort only: every error
falls through with exit 0 so the host agent is never blocked.
"""
from __future__ import annotations

import hashlib
import importlib.util
import json
import os
import re
import shutil
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

HOME = Path.home()
VAULT_ROOT = Path(os.environ.get("LLM_WIKI_VAULT", str(HOME / "vaults" / "llm-wiki")))
STATE_FILE = VAULT_ROOT / ".state" / "ingest-output.json"
RTK_BIN = os.environ.get("RTK_BIN", "rtk")
MAX_TITLE = 64
IGNORE_TITLES = {"", "ok", "okay", "done"}


def now_utc() -> datetime:
    return datetime.now(timezone.utc)


def read_payload() -> dict[str, Any]:
    raw = ""
    if len(sys.argv) > 1:
        raw = sys.argv[1]
    elif not sys.stdin.isatty():
        raw = sys.stdin.read()
    raw = raw.strip()
    if not raw:
        return {}
    try:
        return json.loads(raw)
    except json.JSONDecodeError:
        return {"raw": raw}


def load_state() -> dict[str, Any]:
    if not STATE_FILE.exists():
        return {}
    try:
        return json.loads(STATE_FILE.read_text(encoding="utf-8"))
    except Exception:
        return {}


def save_state(state: dict[str, Any]) -> None:
    STATE_FILE.parent.mkdir(parents=True, exist_ok=True)
    STATE_FILE.write_text(json.dumps(state, indent=2, ensure_ascii=False), encoding="utf-8")


def ensure_vault() -> None:
    for rel in [
        "raw/sources/outputs",
        "wiki/sources",
        "wiki/reports",
        "graphify-out/prompts",
        ".state",
    ]:
        (VAULT_ROOT / rel).mkdir(parents=True, exist_ok=True)


def flatten_text(value: Any) -> list[str]:
    out: list[str] = []
    if isinstance(value, str):
        out.append(value)
    elif isinstance(value, list):
        for item in value:
            out.extend(flatten_text(item))
    elif isinstance(value, dict):
        if value.get("type") == "text" and isinstance(value.get("text"), str):
            out.append(value["text"])
        else:
            for item in value.values():
                out.extend(flatten_text(item))
    return out


def extract_session_id(payload: dict[str, Any]) -> str:
    for key in ("session_id", "sessionId", "resourceId", "resource_id", "thread_id"):
        value = payload.get(key)
        if isinstance(value, str) and value:
            return value[:32]
    return "session"


def transcript_path(payload: dict[str, Any]) -> Path | None:
    for key in ("transcript_path", "transcriptPath", "conversation_path", "conversationPath"):
        value = payload.get(key)
        if isinstance(value, str) and value:
            path = Path(value).expanduser()
            if path.exists():
                return path
    return None


def extract_last_assistant_output(payload: dict[str, Any]) -> str:
    # Direct payload fallback for non-Claude runtimes or tests.
    for key in ("assistant_output", "output", "response", "text", "raw"):
        value = payload.get(key)
        if isinstance(value, str) and value.strip():
            return value.strip()

    path = transcript_path(payload)
    if not path:
        return ""

    last = ""
    try:
        with path.open(encoding="utf-8") as handle:
            for line in handle:
                line = line.strip()
                if not line:
                    continue
                try:
                    entry = json.loads(line)
                except json.JSONDecodeError:
                    continue
                if entry.get("type") != "assistant":
                    continue
                message = entry.get("message")
                if isinstance(message, dict):
                    parts = flatten_text(message.get("content", []))
                else:
                    parts = flatten_text(entry.get("content", []))
                text = "\n\n".join(part.strip() for part in parts if part and part.strip()).strip()
                if text:
                    last = text
    except Exception:
        return ""
    return last


def compress_text(text: str) -> tuple[str, dict[str, Any]]:
    original = text
    method = "local"
    compacted = text
    rtk_path = shutil.which(RTK_BIN)
    if rtk_path:
        try:
            result = subprocess.run(
                [rtk_path, "pipe"],
                input=text,
                capture_output=True,
                text=True,
                timeout=15,
            )
            if result.returncode == 0 and result.stdout.strip():
                compacted = result.stdout
                method = "rtk"
        except Exception:
            compacted = text
            method = "local"
    lines = [line.rstrip() for line in compacted.replace("\r\n", "\n").split("\n")]
    collapsed: list[str] = []
    blank_run = 0
    for line in lines:
        if line == "":
            blank_run += 1
            if blank_run > 1:
                continue
        else:
            blank_run = 0
        collapsed.append(line)
    compressed = "\n".join(collapsed).strip() or original.strip()
    orig_chars = len(original)
    saved = orig_chars - len(compressed)
    return compressed, {
        "method": method,
        "original_chars": orig_chars,
        "compressed_chars": len(compressed),
        "saved_chars": saved if saved > 0 else 0,
        "saved_pct": round(100 * saved / orig_chars, 1) if orig_chars and saved > 0 else 0.0,
    }


def slugify(text: str, max_len: int = 48) -> str:
    slug = re.sub(r"[^a-z0-9]+", "-", text.lower()).strip("-")
    slug = re.sub(r"-{2,}", "-", slug)
    return (slug or "assistant-output")[:max_len].strip("-") or "assistant-output"


def title_for(text: str) -> str:
    first = next((line.strip("# -*\t ") for line in text.splitlines() if line.strip()), "assistant output")
    if len(first) > MAX_TITLE:
        first = first[: MAX_TITLE - 1].rstrip() + "…"
    return first or "assistant output"


def fence(text: str) -> str:
    return text.replace("```", "'''")


def insert_marker_line(file_path: Path, marker: str, line: str) -> None:
    anchor = f"<!-- {marker}:END -->"
    text = file_path.read_text(encoding="utf-8") if file_path.exists() else ""
    if line in text:
        return
    if anchor not in text:
        text = text.rstrip() + f"\n\n## {marker.title()}\n<!-- {marker}:START -->\n<!-- {marker}:END -->\n"
    text = text.replace(anchor, f"{line}\n{anchor}")
    file_path.write_text(text, encoding="utf-8")


def append_log(title: str, body_lines: list[str]) -> None:
    log_path = VAULT_ROOT / "log.md"
    stamp = now_utc()
    with log_path.open("a", encoding="utf-8") as handle:
        handle.write(f"\n## {stamp.strftime('%Y-%m-%d %H:%M:%S UTC')} — {title}\n")
        for line in body_lines:
            handle.write(line + "\n")


def dedupe_guard(session_id: str, output_text: str) -> bool:
    digest = hashlib.sha256(f"{session_id}\n{output_text}".encode("utf-8")).hexdigest()
    state = load_state()
    if state.get("last_digest") == digest:
        return False
    save_state({"last_digest": digest, "updated_at": now_utc().isoformat(), "session_id": session_id})
    return True


def refresh_prompt_graph() -> None:
    script = VAULT_ROOT / "scripts" / "ingest-prompt.py"
    if not script.exists():
        return
    try:
        spec = importlib.util.spec_from_file_location("ingest_prompt", script)
        if spec and spec.loader:
            module = importlib.util.module_from_spec(spec)
            spec.loader.exec_module(module)  # type: ignore[union-attr]
            module.maybe_refresh_graph()
    except Exception:
        return


def write_output_files(output_text: str, session_id: str) -> None:
    stamp = now_utc()
    date_slug = stamp.strftime("%Y-%m-%d")
    time_slug = stamp.strftime("%H%M%S")
    session_short = (session_id or "session")[:12]
    title = title_for(output_text)
    slug = slugify(title)
    compressed, cmeta = compress_text(output_text)

    raw_dir = VAULT_ROOT / "raw" / "sources" / "outputs" / stamp.strftime("%Y") / stamp.strftime("%m") / stamp.strftime("%d")
    raw_dir.mkdir(parents=True, exist_ok=True)
    raw_path = raw_dir / f"{time_slug}-{session_short}-{slug}.md"

    source_rel = Path("wiki") / "sources" / f"{date_slug}-{time_slug}-{slug}-output.md"
    source_path = VAULT_ROOT / source_rel
    report_rel = Path("wiki") / "reports" / f"{date_slug}-{time_slug}-{slug}-answer.md"
    report_path = VAULT_ROOT / report_rel
    source_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.parent.mkdir(parents=True, exist_ok=True)

    raw_path.write_text(
        "\n".join([
            "---",
            'type: "assistant-output"',
            f'session_id: "{session_id}"',
            f'captured_at: "{stamp.isoformat()}"',
            f'report: "[[{report_rel.with_suffix("").as_posix()}]]"',
            "---",
            "",
            f"# {title}",
            "",
            "## Output",
            "",
            "```text",
            fence(output_text),
            "```",
            "",
        ]),
        encoding="utf-8",
    )

    summary_lines = [
        "---",
        'type: "source-summary"',
        'source_type: "assistant-output"',
        f'captured_at: "{stamp.isoformat()}"',
        f'raw_path: "{raw_path.relative_to(VAULT_ROOT).as_posix()}"',
        f'session_id: "{session_id}"',
        f'rtk_method: "{cmeta["method"]}"',
        f'rtk_original_chars: {cmeta["original_chars"]}',
        f'rtk_compressed_chars: {cmeta["compressed_chars"]}',
        f'rtk_saved_pct: {cmeta["saved_pct"]}',
        "---",
        "",
        f"# {title}",
        "",
        f"- Raw output: [[{raw_path.relative_to(VAULT_ROOT).with_suffix('').as_posix()}]]",
        f"- Filed report: [[{report_rel.with_suffix('').as_posix()}]]",
        f"- rtk compression: {cmeta['method']} ({cmeta['original_chars']}→{cmeta['compressed_chars']} chars, -{cmeta['saved_pct']}%)",
        "",
        "## Compressed Output (rtk)",
        "",
        "```text",
        fence(compressed[:3000]),
        "```",
        "",
    ]
    source_path.write_text("\n".join(summary_lines), encoding="utf-8")

    report_path.write_text(
        "\n".join([
            "---",
            f'title: "{title}"',
            f'created_at: "{stamp.isoformat()}"',
            'section: "reports"',
            'status: "captured"',
            f'session_id: "{session_id}"',
            f'raw_output: "[[{raw_path.relative_to(VAULT_ROOT).with_suffix("").as_posix()}]]"',
            f'source_summary: "[[{source_rel.with_suffix("").as_posix()}]]"',
            "---",
            "",
            f"# {title}",
            "",
            "## Answer Output (rtk-compressed)",
            "",
            compressed,
            "",
            "## Evidence and Citations",
            "",
            f"- [[{source_rel.with_suffix('').as_posix()}]]",
            f"- [[{raw_path.relative_to(VAULT_ROOT).with_suffix('').as_posix()}]]",
            "",
        ]),
        encoding="utf-8",
    )

    insert_marker_line(VAULT_ROOT / "index.md", "SOURCES", f"- [[{source_rel.with_suffix('').as_posix()}]] - {title}")
    insert_marker_line(VAULT_ROOT / "index.md", "REPORTS", f"- [[{report_rel.with_suffix('').as_posix()}]] - {title}")
    append_log(
        title,
        [
            f"- Raw output: [[{raw_path.relative_to(VAULT_ROOT).with_suffix('').as_posix()}]]",
            f"- Source note: [[{source_rel.with_suffix('').as_posix()}]]",
            f"- Report note: [[{report_rel.with_suffix('').as_posix()}]]",
            f"- rtk compression: {cmeta['method']} ({cmeta['original_chars']}→{cmeta['compressed_chars']} chars, -{cmeta['saved_pct']}%)",
        ],
    )


def main() -> int:
    try:
        ensure_vault()
        payload = read_payload()
        session_id = extract_session_id(payload)
        output_text = extract_last_assistant_output(payload)
        if not output_text or title_for(output_text).lower() in IGNORE_TITLES:
            return 0
        if not dedupe_guard(session_id, output_text):
            return 0
        write_output_files(output_text, session_id)
        refresh_prompt_graph()
    except Exception:
        pass
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
