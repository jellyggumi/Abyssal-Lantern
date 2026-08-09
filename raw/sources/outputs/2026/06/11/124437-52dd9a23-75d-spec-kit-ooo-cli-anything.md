---
type: "assistant-output"
session_id: "52dd9a23-75d2-4469-ba27-aeb83df8"
captured_at: "2026-06-11T12:44:37.920113+00:00"
report: "[[wiki/reports/2026-06-11-124437-spec-kit-ooo-cli-anything-answer]]"
---

# spec-kit × ooo × cli-anything 조합 가이드

## Output

```text
# spec-kit × ooo × cli-anything 조합 가이드

세 스킬은 경쟁 관계가 아니라 **서로 다른 레이어**를 담당합니다. 그리고 이 세션에는 셋 다 이미 MCP로 연결되어 있어(`mcp__spec-kit__speckit_*`, `mcp__ooo__ouroboros_*`, `mcp__cli-anything__hub_*`) CLI 설치 없이 바로 조합할 수 있는 상태입니다.

## 각자의 레이어

| 레이어 | 스킬 | 역할 |
|--------|------|------|
| **명세 (무엇을)** | spec-kit | constitution → specify → plan → tasks 파이프라인으로 사람이 읽는 SDD 아티팩트 생성. 멀티 에이전트(Claude/Codex/Gemini/Cursor) 공유 가능 |
| **실행 루프 (어떻게 끝까지)** | ooo | Socratic 인터뷰로 모호성 ≤ 0.2까지 낮추고, 불변 seed로 동결 → 실행 → drift 측정 → **검증될 때까지** 루프 (ralph) |
| **도구 (무엇으로)** | cli-anything | 에이전트가 실제 소프트웨어(GIMP, Blender, LibreOffice, FFmpeg 계열…)를 `--json` 출력의 결정론적 CLI로 제어 |

## 권장 조합 패턴

**패턴 A — 풀 파이프라인 (외부 소프트웨어를 다루는 중규모+ 작업)**

1. **spec-kit으로 명세**: `/speckit.constitution` → `/speckit.specify` → 모호하면 `/speckit.clarify`. 산출물 `spec.md`/`plan.md`가 팀/멀티 에이전트가 공유하는 문서 SSOT.
2. **ooo로 계약 동결**: spec-kit의 spec을 입력으로 `ouroboros_interview`(brownfield 가중치)로 남은 모호성을 털고 `generate_seed`로 **불변 seed** 생성. 여기서 핵심은 수용 기준(Success 30%)을 **기계 검증 가능한 형태**로 쓰는 것.
3. **cli-anything으로 도구 확보**: 구현이 실제 소프트웨어를 건드리면 코드 짜기 전에 `hub_search`부터. 레지스트리에 있으면 `hub_install`, 없으면 그때만 `/cli-anything <repo>`로 하니스 생성.
4. **실행+검증 루프**: `ouroboros_execute_seed`(또는 `/speckit.implement`)로 구현하고, `ouroboros_evaluate`의 verify 단계에서 cli-anything 하니스의 `--json` 출력으로 **산출물 자체를 검사**(exit code가 아니라 매직 바이트, 픽셀, 문서 구조 — cli-anything 방법론과 동일). 실패하면 ooo가 통과할 때까지 루프.

**패턴 B — 경량 (단일 세션 완주)**: spec-kit 생략, `ooo` 인터뷰 → seed → ralph 루프만. 문서 아티팩트가 필요 없고 "끝까지 검증하며 완주"가 목적일 때.

**패턴 C — 문서 중심 (멀티 에이전트/팀 공유)**: ooo 생략, spec-kit 파이프라인만. GitHub 표준 `.specify/` 아티팩트를 여러 런타임이 공유해야 할 때.

## 시너지가 실제로 생기는 지점

- **ooo의 seed ⊃ spec-kit의 spec**: spec-kit은 명세를 *작성*하지만 구현이 명세에서 벗어나는 걸 막는 장치가 약합니다. ooo의 불변 seed + `measure_drift`가 그 빈자리를 정확히 메웁니다. 순서는 항상 spec-kit(작성) → ooo(동결)이고, 역방향은 어색합니다.
- **cli-anything은 검증을 기계화**: spec의 수용 기준이 "PNG가 1024px로 리사이즈되어야 한다"라면, `cli-anything-gimp --json`의 구조화 출력이 ooo evaluate 단계의 증거가 됩니다. "테스트 통과했다고 주장"이 아니라 산출물 검사로 verify하는 ooo 철학과 cli-anything의 "verify outputs, not exit codes" 원칙이 같은 방향입니다.
- **seed의 Constraints에 도구를 명시**: "이미지 처리는 cli-anything-gimp 하니스 사용, GUI 자동화 금지" 같은 제약을 seed에 넣으면 루프 중 에이전트가 임의 우회하는 drift를 막습니다.

## 겹침 주의 (안티패턴)

- **spec-kit과 ooo를 동시에 풀로 돌리지 마세요** — 둘 다 spec-first라 `spec.md`와 `seed.yaml`이라는 SSOT가 두 개 생깁니다. 역할을 "spec-kit = 작성/문서, ooo = 동결/루프"로 자르거나 하나만 쓰세요.
- **하니스 생성부터 하지 마세요** — `/cli-anything` 7단계 빌드는 한 세션을 소모합니다. 항상 `hub_search` 먼저 (40+ 하니스 기존재).
- **spec 없이 ralph 루프만 돌리지 마세요** — 검증 기준이 없는 지속 루프는 "끝났다고 믿을 때까지"가 아니라 "지칠 때까지"가 됩니다. 루프 전에 seed가 먼저입니다.

## Routing Brief

'''markdown
# spec-kit Routing Brief

## Scope
- Target agent: claude (셋 다 이번 세션 MCP 연결 완료)
- Project state: 조합 설계 단계 (new)
- Pipeline stage: constitution → specify가 출발점

## Recommended next move
- 패턴 A: specify init → /speckit.specify → ouroboros_interview/seed → hub_search → implement+evaluate 루프

## Why
- 세 스킬이 명세/루프/도구로 레이어가 정확히 분리됨
- ooo의 불변 seed가 spec-kit의 약점(구현 drift)을 보완
- cli-anything --json 출력이 ooo verify 단계의 기계 검증 증거가 됨

## Route-outs
- `bmad` — 어떤 기획 아티팩트를 다음에 쓸지부터 정해야 할 때
- `plannotator` — implement 전 사람의 플랜 승인 게이트가 필요할 때
- `survey` — 구현 전 랜드스케이프 조사가 본 작업일 때
'''

한 줄 요약: **spec-kit으로 쓰고, ooo로 동결·완주하고, cli-anything으로 손발을 단다** — 구체적인 프로젝트(예: 특정 소프트웨어 자동화)가 있으면 패턴 A를 실제로 세팅해 드릴 수 있습니다.
```
