# Context: 공성 물리 · 턴제 포병 장르

## Workflow Context

이 장르는 **두 개의 독립된 계보가 2009년에 갈라졌다가 아직 완전히 합쳐지지 않은** 상태다.

```
1969  Potshot (플로터 디스플레이)          ─┐
1972  War 3 / Artillery-3 (텍스트)          │
1980  Super Artillery (Apple II, 바람 도입) │  포병 계보
1991  Gorillas (QBasic), Scorched Earth     │  = 각도 + 힘 + 바람, 양방향 교대
1995  Worms (Team17)                        │
1999  포트리스2 (CCR)                        │
2002  Gunbound (Softnyx)                    │
2001~ Pocket Tanks / ShellShock Live (2015) ─┘

2008  Castle Clout (Liam Bowmers)          ─┐
2009  Crush the Castle (Armor Games, 4/28)  │  물리 파괴 계보
2009  Angry Birds (Rovio, 12/11)            │  = 새총 + 구조 붕괴, 단방향
2012  Bad Piggies / Siege Hero 등           ─┘
```

**castle-war는 정확히 그 접점에 있다.** 포병 계보에서 각도·힘·바람·교대 턴을 가져오고,
물리 파괴 계보에서 새총 드래그 입력과 BFS 구조 붕괴를 가져왔다.

> 1982년 Magnavox Odyssey2의 *Smithereens!* 가 이 조합의 원형이다 —
> **"두 대의 투석기가 각자 성벽 뒤에서 서로에게 돌을 던진다."**
> 단 턴제가 아니었고 구조 붕괴도 없었다. [direct page retrieval — en.wikipedia.org/wiki/Artillery_game]

## Affected Users

| Role | Responsibility | Skill Level |
|---|---|---|
| 캐주얼 모바일 플레이어 | 짧은 세션, 한 손 조작, 즉시 이해 | 낮음 — 튜토리얼 없이 새총을 이해해야 함 |
| 포병 장르 베테랑 (포트리스/건바운드 출신) | 각도·바람 계산, 정밀 조준 | 높음 — "각샷" 같은 자체 계산법 보유 |
| 물리 퍼즐 팬 (앵그리버드 계열) | 약점 찾기, 별 3개 최적화 | 중간 — 구조를 읽되 탄도는 감각적으로 |
| 웹 아케이드 이용자 | 설치 없이 브라우저에서 즉시 플레이 | 낮음 — 로딩·조작 마찰에 즉시 이탈 |
| 게임 개발자 | 파괴 가능 지형 + 강체 붕괴 두 시스템 결합 | 높음 |

## Current Workarounds

플레이어와 개발자가 장르의 결함을 우회하는 방식:

1. **바람 계산의 암기 우회** — 포트리스 유저는 "각샷"(파워 고정 + 각도만 ±1° 조정)과
   화면 4~8등분 거리 감각을 **외운다**. 게임이 정보를 안 주니 플레이어가 표를 만든다.
   [browser-rendered indexed snippet — 포트리스2 커뮤니티 가이드]
2. **궤적 가이드 후행 도입** — GunboundM(2017 모바일)은 원작에 없던
   **탄도 시각화(shot guide)** 를 추가했다. 20년 뒤에야 "보여주는" 쪽으로 이동.
   [direct page retrieval — en.wikipedia.org/wiki/Gunbound]
3. **단방향 설계로 물리 비용 회피** — Siege Hero / Catapult King / Bad Piggies는
   카메라를 고정하고 한쪽만 쏘게 해서 물리 연산을 관리 가능하게 만든다.
   업계에서 "Destruction Paradox"로 불리는 회피책. [indexed snippet]
4. **가짜 물리** — 사전 정의된 붕괴 청크나 스트레스 임계값 모델로
   "만족스러운 붕괴"를 흉내낸다. 진짜 강체 시뮬레이션을 피하려는 것. [indexed snippet]
5. **메타 진행 부재의 자가 보상** — Archery Bastions 396레벨 플레이어는
   미사용 골드 100만을 쌓아두고 "궁극적 목표가 없다"고 적었다. 경제가 의미를 잃자
   플레이어가 스스로 목표를 만들려다 실패한 사례. [OBSERVED — 2026-08-09 스토어 리뷰 캡처]

## Adjacent Problems

- **보이지 않는 메커니즘 = 부정행위로 읽힘.** Archery Bastions 리뷰에서
  "유닛이 이유 없이 죽는다"는 불만이 업데이트 후 등장. 텔레그래프되지 않은 규칙은
  AI 치팅으로 해석된다. 결정론적 시뮬레이션과 가시적 바람 표시가 이 문제의 정면 답이다.
- **진행도 소실.** 같은 게임에서 315레벨 프리즈로 전 진행 손실, 클라우드 세이브 없음.
- **물리 게임의 성능 절벽.** 파괴 가능 오브젝트가 늘면 연산이 지수적으로 증가.
  castle-war도 같은 벽을 실제로 맞았다 — `CannonController.FindTarget()`이 매 프레임
  200개 블록을 순회해 30게임 심이 1310초 → 2400초 초과. (`task-manifest.md` #20)
- **모바일 터치와 정밀 조준의 충돌.** Angry Birds가 이긴 이유가 바로 이것 —
  트레뷰셋 "클릭-발사, 클릭-정지"를 **드래그-릴리스 새총**으로 바꿔 터치스크린에 맞췄다.
- **장르 이름이 없다.** "artillery game"은 위키피디아 분류상 슈팅과 전략 사이에 걸쳐 있고,
  "physics puzzle"은 파괴를 설명하지만 대전을 설명 못 한다. 마케팅 언어가 부재.

## User Voices

- "**궁극적 목표가 없어 쉽고 반복적이다… 고유한 보스 레벨과 간단한 스토리가 있는
  전체 지도가 필요하다**" — Archery Bastions 396레벨 플레이어
  [OBSERVED — `_workspace/current/design/trend-survey/archery-bastions-castle-war.md` 리뷰 인용]
- "간단한 메커니즘이지만, 만족스럽다는 걸 부정하기 어렵다" — Joystiq, *Crush the Castle* 평
  [direct page retrieval — en.wikipedia.org/wiki/Crush_the_Castle]
- "이렇게 단순한 전제치고 놀랍도록 깊고 재미있다" — IGN, *Crush the Castle* 평
  [direct page retrieval — 동일]
- "쉽게 배우고 플레이할 수 있지만 **매우 반복적인 게임플레이**" — GameZebo, 2/5점 (동일)
- "테스트 플레이어들이 **무엇을 해야 할지 몰랐다**… 개발진은 '알아볼 수 있는 메커니즘'이
  필요하다고 판단했다. 새총을 먼저 실험했으나 너무 뻔하다고 여겨 그네 같은 다른 아이디어를
  시도했다. 그러나 **플레이어들이 즉시 사용법을 이해했기 때문에 새총으로 되돌아왔다**"
  — Angry Birds 개발 기록 [direct page retrieval — en.wikipedia.org/wiki/Angry_Birds_(video_game)]
- "구조물을 새총에서 **더 멀리 배치해 기대감과 흥분을 높였다**" — 동일 개발 기록
- "높은 샷은 낮은 직선 샷보다 바람의 영향을 훨씬 크게 받는다" — Gunbound 플레이어 가이드
  [browser-rendered indexed snippet]
