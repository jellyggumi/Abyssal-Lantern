# Presentation spec — castle-war (Stage 1)

- run-id: 20260809-castle-war-stage1
- owner: game-designer lane (Stage 3 execution: designer + programmer)
- UX pillar: 직관성 우선 — 모든 연출은 상태 전달 수단이다. 스펙터클이
  가독성을 가리면 그 연출은 G4 결함이다.

## Camera (기존 시스템 위에 규칙 부여)

| Moment | Behavior | Readability rule |
|---|---|---|
| Aim | Fixed board view, launch ring pulse, 3.0 s fixed-step trajectory | 양 진영 성이 항상 동시에 보인다 — 전황 파악이 조준보다 우선; 표시 궤적과 실제 궤적 오차는 결함 |
| Launch | Follow fired unit (existing focus cam) | 복귀는 impact 후 ≤1.2s; 유닛 추적 중에도 코어 HP HUD 고정 |
| Collapse chain | Zoom-out + shake (existing) | 셰이크 진폭은 파괴 규모에 비례하되 입력 대기 상태에서는 0 |
| War-bar swing | 전선 이동 시 바 플래시 | 색은 진영색만 사용 (worldview §1) |

## Art

- 진영 가독성: 유닛/성/투사체 전부 팀 틴트(청/홍) 적용 — 실루엣이 같아도
  소속은 0.2초 안에 읽혀야 한다.
- 신규 리소스는 Codex 생성 → `design/concept/` → 감사 후 `Assets/` 승격
  (CLAUDE.md §3). 스타일 앵커: 기존 픽셀아트 실루엣 + 채도 높은 진영색.
- 플레이 유닛은 원본 콜라이더 월드 면적을 유지한 채 시각 스케일만
  확대한다. Knight/Archer 프리팹은 0.48 visual scale을 사용하고,
  콜라이더는 0.42 reference scale로 역보정되어 명중 판정이 커지지 않는다.
  Cannon/Barrel은 별도 실루엣 소유권을 유지해 본체 스케일 로직을 중복 적용하지 않는다.

## Sound

- BGM [OBSERVED 2026-08-09]: 전장 루프(`Audio/BGM/battle-loop`, 60.0초,
  130 BPM 지시 생성)와 승리/패배 스팅어(각 10.0초)를 번들한다. Higgsfield
  `sonilo_music` 생성 → OGG 변환(Unity는 m4a를 소스 포맷으로 받지 않음) →
  provenance 첨부 후 승격. 루프 볼륨 0.28, 스팅어 0.6으로 SFX 버스 아래에
  둔다 — 음악이 발사·명중 큐를 덮으면 그 자체가 G4 결함이다.
- 재생 시점은 매치 시작 이후다(`BgmManager`). 브라우저는 사용자 제스처
  전에는 AudioContext를 열지 않으므로 로드 중 `Play()`는 세션 전체를
  무음으로 만든다. 실패 시 0.5초 간격으로 재시도한다.
- SFX 우선순위(발사 > 명중 > 콤보 > UI): `Audio/SFX/launch`,
  `impact`, `combo`를 번들한다. 모두 44.1 kHz mono이며 각각
  약 0.25/0.18/0.27초다. 발사·명중은 입력/충돌 프레임에서 재생하고,
  중첩 붕괴 시 합산 피크를 제한하도록 one-shot 볼륨을 0.72 이하로 둔다.
- 타격 숫자는 피해량에 따라 크기와 색이 변하며, 코어 명중/파괴는 일반
  블록 명중보다 강한 링·셰이크·토스트로 구분한다.

## G4 측정 대상 씬 (Stage 3에서 QA 채점)

1. 첫 발사(온보딩) 2. 대규모 붕괴 체인 3. 코어 파괴 승리 4. 역전 패배
— 각 씬 몰입도 중앙값 ≥4.0/5, 가독성 불만 S1/S2 = 0.
