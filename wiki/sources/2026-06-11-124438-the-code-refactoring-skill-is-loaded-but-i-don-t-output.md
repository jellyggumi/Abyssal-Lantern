---
type: "source-summary"
source_type: "assistant-output"
captured_at: "2026-06-11T12:44:38.346704+00:00"
raw_path: "raw/sources/outputs/2026/06/11/124438-a10a9da6-0a3-the-code-refactoring-skill-is-loaded-but-i-don-t.md"
session_id: "a10a9da6-0a3e-4b1c-84ba-9ecc0e97"
rtk_method: "rtk"
rtk_original_chars: 919
rtk_compressed_chars: 919
rtk_saved_pct: 0.0
---

# The `/code-refactoring` skill is loaded, but I don't have a tar…

- Raw output: [[raw/sources/outputs/2026/06/11/124438-a10a9da6-0a3-the-code-refactoring-skill-is-loaded-but-i-don-t]]
- Filed report: [[wiki/reports/2026-06-11-124438-the-code-refactoring-skill-is-loaded-but-i-don-t-answer]]
- rtk compression: rtk (919→919 chars, -0.0%)

## Compressed Output (rtk)

```text
The `/code-refactoring` skill is loaded, but I don't have a target yet — no file, module, or diff was passed in. To pick the right refactor mode and guardrails, I need to know what you want cleaned up.

Tell me one of these, and I'll take it from there:

- **A messy file/module** — e.g. `refactor setup-all-skills-prompt.md` or a specific script in this repo (`local-safe-refactor`)
- **A fragile legacy area** you don't fully trust — I'd freeze behavior with characterization checks first (`behavior-freeze-first`)
- **A repeated pattern across many files** — e.g. restructuring all the skill `SKILL.md` files the same way (`repetitive-migration-codemod`)
- **An oversized diff/branch** that needs splitting into reviewable slices (`diff-shaping-cleanup`)

The working tree is clean on `main` in `oh-my-skills`, so there's no pending diff to reshape — point me at the file(s) or describe the cleanup you have in mind.
```
