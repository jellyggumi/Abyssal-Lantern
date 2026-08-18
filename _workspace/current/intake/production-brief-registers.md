# 인테이크 — 결함 대장 화해와 게이트 차단 해제

- register-role: derived
  인테이크 브리프. 결함을 소유하지 않고 인용한다.

- run-id: 20260809-castle-war-stage1 (cycle 2)
- date: 2026-08-14
- 운영 모드: **Stage 3 — 운영 안정성과 플레이 임팩트** (게이트 측정 차단 해제)
- next_public_beat: WebGL build linked from https://jellyggumi.github.io/ menu
- source_packet: `qa/ux-defect-list.md`, `qa/defect-register.md`,
  `qa/gate-measurements.md`, `skill://game-studio-harness/references/quality-gates.md`
- 발단: 직전 사이클에서 QA 레인이 보고 — *"`ux-defect-list.md:204`가 S1 4건을 등재하는데
  `defect-register.md`(D-001~D-017)에 UX-계열이 한 건도 없고 UX 목록에 status 열이 없다.
  두 문서가 서로소다. 그 4건이 open이면 모든 게이트가 이미 차단이다."*

## main_constraint

계약이 **"Any open S1 defect blocks every gate"** 이다. 그러므로 이 화해가 끝나기 전에
G1~G8 중 무엇도 PASS를 받을 수 없고, **G4/G6/G1 final을 목표로 하는 Stage 3 자체가
진행 불가**다. 이것은 문서 정리 작업이 아니라 **게이트 선행 조건**이다.

## main_question

`ux-defect-list.md`의 S1 4건은 지금 open인가?

**문서로 답할 수 없다** — status 열이 없다. 그래서 코드로 측정했다.

## 측정 결과 — 4건 전부 이미 닫혀 있다

| ID | 증상 | 현재 상태 | 코드 근거 |
|---|---|---|---|
| UX-001 | 바람이 화면에 절대 표시되지 않음 (`WindText`가 씬 루트, Canvas 조상 없음) | **closed** | `GameManager.cs:1176` `HudCanvas.Adopt(windText)` — `SetupUIButtons()` 안, `:361`에서 무조건 호출 |
| UX-002 | 점수가 화면에 절대 표시되지 않음 (`ScoreText` 동일) | **closed** | `GameManager.cs:1177` `HudCanvas.Adopt(scoreText)` |
| UX-003 | 적 턴에 화면이 내리는 지시 2개가 전부 거짓 | **closed** | `SiegeAlarmSystem.cs:228` 이 규칙에 **묻는다** — `BrickPlacementRules.DesignationOpen(...)`. 단언에서 질의로 바뀌어 창이 닫히면 문구도 사라진다 |
| UX-014 | 적 턴 109.7초 동안 활성 버튼 0·유효 입력 0 | **closed (처방 변경)** | `ShotTraceDirector`가 사후 판독을 실어 `SiegeAlarmSystem.cs:234` `LatestLine`으로 표시. 피해 경로에 배선됨(`DestructibleBlock.cs:437`, `CastleCoreGimmick.cs:204`) |

**씬 YAML은 아직 `m_Father: 0`이다**(`WindText` transform 1739190291,
`ScoreText` 835917195). 그것이 문서가 낡아 보이는 이유이고, 동시에 **수정이 씬이 아니라
런타임 입양으로 이뤄졌다**는 사실이다. 대조군 `TurnText`는 씬에서 이미 Canvas 하위
(`m_Father: 339938660` = `Canvas`)인데도 `:1170`에서 함께 입양된다 — 씬 캔버스가
ConstantPixelSize라 크기 규칙이 둘로 갈리기 때문이다.

## 그러나 진짜 결함이 남아 있다 — 고정이 없다

`grep -rln "Adopt\|windText\|orphan" Assets/Tests/` → **0건**.

네 건 모두 코드에서 닫혔고 증거도 있다(`qa/evidence/font/orphan-labels.md`가 입양
전/후 좌표를 표로 실측). **그런데 회귀를 막는 것이 아무것도 없다.**

이것은 이번 세션이 `CLAUDE.md`에 방금 기록한 불변식이 그대로 적용되는 자리다:

> **A test that walks a declared list cannot see what is missing from the list.**

`HudCanvasContractTests`가 있지만 그것은 **소스에서 캔버스 조회를 금지**하는 테스트다.
"모든 HUD 라벨이 실제로 HUD 캔버스에 붙어 있다"는 별개 계약이고 **그 계약은 존재하지
않는다.** `SetupUIButtons`에서 `Adopt` 한 줄이 지워지면 바람과 점수가 다시 조용히
사라지고, 스위트는 녹색이다.

## 이 사이클이 할 일

1. **두 대장을 화해**시킨다 — UX 목록에 status 열을 넣고, S1 4건의 닫힘을 코드 근거와
   함께 등재하고, `defect-register.md`와의 관계를 명시한다(둘 중 무엇이 정본인가).
2. **입양을 테스트로 고정**한다 — 씬이 저작한 HUD 라벨 전부가 런타임에 HUD 캔버스
   조상을 갖는지. 뮤테이션으로 증명한다.
3. **판독 존재를 고정**한다 — 적 턴에 표시할 것이 있는지(UX-014의 처방).
4. 그 뒤에 **차단됐던 게이트를 측정**한다.

## 하지 않을 것

- **씬 YAML을 고치지 않는다.** 입양이 이미 정식 경로이고(`HudCanvas.Adopt`의 docstring이
  "씬은 *어디*의 소유권을 갖고 HUD 캔버스가 *얼마나 크게*를 갖는다"고 계약을 적었다),
  씬을 고치면 두 수정이 같은 것을 두 번 하게 된다. **씬 값은 앵커의 출처로 계속 쓰인다.**
- **UX-014를 다시 열지 않는다.** 조사가 사전 텔레그래프를 반증하고 사후 판독을 처방했고
  그것이 구현됐다. 판정을 뒤집으려면 새 측정이 필요하고 이 사이클의 범위가 아니다.
- **S2 이하를 이 사이클에서 처리하지 않는다.** 차단 규칙은 S1만 걸린다.
