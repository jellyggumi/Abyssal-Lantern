# Presentation spec — castle-war (Stage 1)

- run-id: 20260809-castle-war-stage1
- owner: game-designer lane (Stage 3 execution: designer + programmer)
- UX pillar: 직관성 우선 — 모든 연출은 상태 전달 수단이다. 스펙터클이
  가독성을 가리면 그 연출은 G4 결함이다.

## Camera (기존 시스템 위에 규칙 부여)

| Moment | Behavior | Readability rule |
|---|---|---|
| Aim | Fixed board view, launch ring pulse | 양 진영 성이 항상 동시에 보인다 — 전황 파악이 조준보다 우선 |
| Launch | Follow fired unit (existing focus cam) | 복귀는 impact 후 ≤1.2s; 유닛 추적 중에도 코어 HP HUD 고정 |
| Collapse chain | Zoom-out + shake (existing) | 셰이크 진폭은 파괴 규모에 비례하되 입력 대기 상태에서는 0 |
| War-bar swing | 전선 이동 시 바 플래시 | 색은 진영색만 사용 (worldview §1) |

## Art

- 진영 가독성: 유닛/성/투사체 전부 팀 틴트(청/홍) 적용 — 실루엣이 같아도
  소속은 0.2초 안에 읽혀야 한다.
- 신규 리소스는 Codex 생성 → `design/concept/` → 감사 후 `Assets/` 승격
  (CLAUDE.md §3). 스타일 앵커: 기존 픽셀아트 실루엣 + 채도 높은 진영색.

## Sound

- BGM: Gemini(playwriter) 생성 — 트랙 2개: 전장 루프(120–140 BPM, 긴장
  유지), 승리/패배 스팅어. `Assets/Resources/Audio/BGM/`.
- SFX 우선순위(발사 > 명중 > 붕괴 > UI): 발사·명중은 100ms 이내 반응
  (G4 latency 기준), 붕괴 체인은 블록 수에 따라 피치 상승.

## G4 측정 대상 씬 (Stage 3에서 QA 채점)

1. 첫 발사(온보딩) 2. 대규모 붕괴 체인 3. 코어 파괴 승리 4. 역전 패배
— 각 씬 몰입도 중앙값 ≥4.0/5, 가독성 불만 S1/S2 = 0.
