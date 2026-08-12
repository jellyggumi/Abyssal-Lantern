# PlayMode triage — Unity 6000.5.6f1

- run-id: 20260809-castle-war-stage1 (cycle 2)
- owner: game-qa lane
- date: 2026-08-12
- 증거: `evidence/playmode-6000.xml`, `evidence/playmode-6000-isolated.xml`
- 결과: **54개 중 49 통과 / 5 실패**

---

## 왜 이 문서가 따로 있나

"5건 실패"는 그 자체로 아무것도 말하지 않는다. 업그레이드가 만든 것과 원래 있던 것,
게임 결함과 환경 노이즈를 가르지 않으면 **업그레이드를 되돌려야 하는지 판단할 수 없다.**
아래는 5건 각각을 어느 쪽인지 확정한 기록이다.

---

## 분류

| # | 테스트 | 성격 | 6000이 만들었나 |
|---|---|---|---|
| 1 | `AutoPlayTest.PlaySequenceAndCapture` | **환경 노이즈** | 아니오 |
| 2 | `CastleBustersAnalysisTests.Cycle2_MechanicsValidation` | **선행 결함 + 노이즈** | 아니오 |
| 3 | `Cycle3_PlaytestDataCollection_30Games` | **환경 노이즈** | 아니오 |
| 4 | `RuntimeReliability…StagePicker_UsesDistinctDedicatedCards…` | **낡은 테스트 기대값** | 아니오 |
| 5 | `RuntimeReliability…UnitController_FriendlyBodyContact…` (D-016) | **거동 변화** | **판단 보류 — 아래** |

**실제 게임 에러(`error CS` / 셰이더 / `BuildFailedException`)는 0건이다.**

---

## 1·3 — Unity MCP 플러그인 / TMP 로그 노이즈

NUnit은 **처리되지 않은 `[Error]` 로그를 테스트 실패로 집계한다.** 실행 중이던 테스트가 뒤집어쓴다.

| 로그 | 횟수 | 정체 |
|---|---|---|
| `McpManagerClientHub … Authorization failed` | 2 | Unity MCP 플러그인이 로컬 허브에 붙지 못함. 매니페스트의 MCP 패키지 11종이 출처 |
| `TextMesh Pro Essential Resources are missing` | 1 | **오탐.** `Assets/TextMesh Pro/Resources/`는 온전하다(`TMP Settings.asset`·Fonts & Materials·Sprite Assets·Style Sheets 전부 존재). 6000의 TMP 버전 체크가 다른 경로/버전을 기대하는 것으로 보임 |

> [!note] 내 TMP 삭제 탓이 아니다
> 이번 세션에서 지운 것은 `Examples & Extras` 193파일이고, **Essential Resources는
> 그 삭제 목록에 0건**이다(`git show --name-only ef33af32 -- "Assets/TextMesh Pro"`로 확인).
> EditMode 로그에는 이 경고가 **0회** 나타난다.

MCP 설정은 EditorPrefs(머신 로컬)에 있어 프로젝트에서 끌 수 없다.
근본 해결은 테스트 쪽 `LogAssert` 처리이나, **내가 소유하지 않은 테스트를 환경 노이즈
가리려고 고치지 않는다** — 그건 계측을 고치는 게 아니라 눈을 가리는 것이다.

## 2 — D-010, 이미 기록된 계약 모순

전체 실행에서는 `Expected: AITurn / But was: PlayerTurn`으로 실패하고,
단독 실행에서는 MCP 로그로 실패한다.

`defect-register.md` D-010이 이미 기록한 상태다 — 두 PlayMode 테스트가 **서로 모순되는
계약**을 주장한다. `RuntimeReliabilityRegressionTests`는 타이틀에서 `Intro` 유지가 정상이라
단언하고, `Cycle2`는 로드 직후 발사가 곧바로 `AITurn`이 되기를 기대한다(자동 시작 전제).
출하 동작과 일치하는 쪽은 Intro 유지이며, Cycle2는 레거시 분석 스위트 소유다.

## 4 — 낡은 테스트 기대값 (2026-08-09부터)

```
Expected: RGBA(0.320, 0.320, 0.350, 0.550)   ← 테스트
But was:  RGBA(0.440, 0.440, 0.480, 0.880)   ← 프로덕션
```

**프로덕션이 옳고 테스트가 낡았다.** `IntroScreenController.cs:455`의 주석이 왜 올렸는지 적고 있다:

> 0.55 알파에서 어두운 프레임이 밝은 키아트에 묻혀, 라벨이 하늘에 뜬 낱글자로 읽혔다 —
> 잠긴 스테이지가 아니라 레이아웃 버그처럼 보인다.

git으로 확정:

| 대상 | 커밋 | 날짜 |
|---|---|---|
| 프로덕션 값 변경 | `8b45efae` fix(intro): make locked stage cards read as cards | **2026-08-09** |
| 테스트 기대값 | `b639788c` (최초 임포트) | 갱신된 적 없음 |

즉 **8/9 이후 사흘간 계속 실패하고 있었다.** 6000과 무관하다.
이번 세션이 이 파일에 넣은 변경은 `drag → linearDamping`, `velocity → linearVelocity`뿐이며
색상 단언 라인은 건드리지 않았다(`git diff | grep -c "0.32f"` = 0).

> [!warning] 이건 진짜 결함이다 — 다만 테스트 쪽
> 새 결함으로 등록해야 한다. 고치는 방향은 **테스트 기대값을 프로덕션에 맞추는 것**이며,
> 반대로 하면 8/9에 고친 가독성 문제가 되살아난다.

## 5 — D-016, 거동이 바뀌었다

```
Expected: Launched / But was: Grounded
```

| 시점 | 스위트 실행 | 단독 실행 |
|---|---|---|
| 2022.3 (`defect-register.md` D-016) | 간헐 실패 | **통과** |
| **6000 (이번)** | 실패 | **실패** |

**단독 실행에서 통과하던 것이 실패로 바뀌었다.** 간헐이 상시가 된 것이다.

> [!danger] 6000이 원인이라고 단정하지 않는다 — 확인할 수 없다
> 2022.3이 이 머신에 설치되어 있지 않아 **A/B 대조가 불가능하다.**
> 말할 수 있는 것은 정확히 이것뿐이다: 대장에 "단독 통과"로 기록된 테스트가
> 지금은 단독에서도 실패한다. 원인이 6000의 물리 변경인지, 대장의 기록이
> 이미 낡았던 것인지는 **미확정**이다.
>
> 참고로 D-016의 이전 조사에서 **가설 하나가 이미 반증**됐다 — 픽스처를 전장 밖
> (2000,500)으로 옮기자 간헐이 상시 실패로 **악화**됐다. 위치 의존성이 있다는 뜻이며,
> 6000의 물리 기본값(`Physics2DSettings.asset`이 이번 업그레이드에서 재직렬화됨)이
> 같은 축을 건드렸을 가능성이 다음 조사 지점이다.

---

## 판정

| 항목 | 결론 |
|---|---|
| 업그레이드가 게임을 깨뜨렸나 | **아니오** — 실제 게임 에러 0건, 5건 중 4건이 선행 결함 또는 환경 노이즈 |
| 되돌려야 하나 | **아니오** — 되돌려도 4건은 그대로 남고, D-016은 되돌려도 원인 미확정 |
| 새로 등록할 결함 | **1건** — StagePicker 낡은 기대값 (테스트 측 결함) |
| 재조사할 결함 | **1건** — D-016, 단독 실행 거동 변화 |

---

## 다음 조사 (D-016)

1. `Physics2DSettings.asset` 업그레이드 diff 확인 — 기본 접촉 오프셋·솔버 반복이 바뀌었는지
2. 바뀌었다면 2022.3 값으로 되돌려 재실행 → 원인 확정
3. 확정되면 대장 D-016을 "실행 환경 의존"에서 정확한 원인으로 갱신
