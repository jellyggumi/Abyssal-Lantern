---
type: "source-summary"
source_type: "assistant-output"
captured_at: "2026-06-15T05:34:07.370765+00:00"
raw_path: "raw/sources/outputs/2026/06/15/053407-64bc66d3-5b2-assistant-output.md"
session_id: "64bc66d3-5b28-4779-bc73-f58d1d88"
rtk_method: "rtk"
rtk_original_chars: 748
rtk_compressed_chars: 748
rtk_saved_pct: 0.0
---

# 설치 완료 및 검증됐습니다.

- Raw output: [[raw/sources/outputs/2026/06/15/053407-64bc66d3-5b2-assistant-output]]
- Filed report: [[wiki/reports/2026-06-15-053407-assistant-output-answer]]
- rtk compression: rtk (748→748 chars, -0.0%)

## Compressed Output (rtk)

```text
설치 완료 및 검증됐습니다.

## 설치 결과

- ✅ **SSOT 위치**: `.agents/skills/ponytail/` — 전체 파일 정상 설치
  - `SKILL.md` (9.9K), `references/commands.md`, `scripts/install.sh`, `evals/evals.json`
- ✅ **42개 에이전트에 연결됨**
  - universal 복사: Amp, Cline, Codex, Cursor, Gemini CLI 외 4개
  - symlink: Claude Code, Antigravity, Augment, OpenClaw, CodeBuddy 외 26개
- ✅ **Claude Code 심링크 확인**: `.claude/skills/ponytail → ../../.agents/skills/ponytail`
- ✅ `name: ponytail` frontmatter 정상

이제 이 프로젝트에서 ponytail 스킬을 바로 쓸 수 있습니다. Claude Code에서는 `/ponytail` 또는 "write less code / YAGNI / over-engineering" 같은 트리거로, 강도는 `lite/full/ultra/off`로 조절하면 됩니다.

참고: 설치본은 `.agents/skills/`·`.claude/skills/` 등 gitignore 대상 디렉터리로 들어간 것이고, 깃에 푸시된 정본은 `.agent-skills/ponytail/`입니다 — 둘은 별개로 정상 동작합니다.
```
