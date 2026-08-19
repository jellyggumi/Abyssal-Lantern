# 보상 밴드 — G5 임계값 실측

- run-id: 20260809-castle-war-stage1 (cycle 3)
- owner: game-pm 레인
- date: 2026-08-19
- 계약: `skill://game-studio-harness/references/quality-gates.md` G5
- 방법: **코드 0줄 수정.** 상수 읽기, 소비처 grep, 해석적 계산

---

## 0. G5 네 임계값 — 한눈에

| # | 임계값 | 실측 | 판정 |
|---|---|---|---|
| 1 | 유료/무료 승률 격차 ≤5%p (동일 실력) | **0%p** — 구조상 | **충족** |
| 2 | 컴백 즉시 역전 확률 ≤30%, 캡·쿨다운 기록 | 캡·쿨다운 **기록됨**, 확률 **미측정** | **부분** |
| 3 | 무료 경로 패리티 10~20 세션 밴드 | **1 시리즈 승리** | **밴드 밖 — 해석 필요** |
| 4 | 모든 매출 지점에 서명된 협상 기록 | `negotiation-record.md` 신규 작성 | **충족** (이 사이클) |

**게이트 판정은 디렉터 소유이고 이 문서는 수치만 낸다.** 2번이 미측정이므로 PASS를 주장하지
않는다.

---

## 1. 유료/무료 승률 격차 — 0%p, 그리고 PM 레인의 이전 주장을 정정한다

### 정정

2026-08-18 PM 레인이 *"grep 결과 유료 지점 0건"* 이라 보고했다. **틀렸다.**
`MobileStorefront`가 존재하고 **Unity IAP 5** 어댑터이며 실제 상품이 있다:

- `Assets/Scripts/MobileStorefront.cs:57` — *"Unity IAP 5 adapter for the native App Store
  and Google Play billing flows"*
- `:60` `public sealed class MobileStorefront : MonoBehaviour`
- `:143` `controller = UnityIAPServices.StoreController()`

정확한 서술은 **"게임플레이에 영향하는 유료 지점 0건"** 이다.

### 왜 격차가 0%p인가 — 소비처 전수

`HasChroniclePack`(= 구매 권리)의 소비처를 전수했다:

| 위치 | 무엇을 하는가 |
|---|---|
| `IntroScreenController.cs:234` | `BuildChronicleReplayButton` — **프롤로그 리플레이 버튼을 하나 만든다** |
| `MobileStorefront.cs:75` | `CanPurchase` — 이미 소장 시 재구매 차단 |
| `:164`, `:290`, `:514` | 스토어 UI 상태 문구 |

**게임플레이 소비처 0건.** 밸런스·유닛·피해·성벽·경제에 닿는 경로가 없다. 코드 주석이
`:58`에 *"It owns no gameplay state: the single product only unlocks a replayable prologue"*
라 적고, **소비처 전수가 그것을 확인한다.**

그러므로 동일 실력에서 유료/무료 승률 격차는 **측정할 것이 없다 — 0%p가 구조**다.

---

## 2. 컴백 즉시 역전 — 캡은 기록됐고 확률은 미측정

### 기록된 것 (계약이 요구하는 "recorded cap/cooldown")

| 항목 | 값 | 위치 |
|---|---|---|
| 발동 조건 | 자기 코어 ≤ **35%** (150 중 **52.5**) | `DynamicBattlefield.cs:712` `DangerHpFraction` |
| 단일 히트 캡 | **140** | `:726` `SingleHitDamageCap` |
| 코어 최대 | **150** | `CastleCoreGimmick` |
| 플레이어 배수 | 2.2 (피해) / 1.5 (반경) / 1.3 (속도) | `:714-716` |
| AI 배수 | 1.6 / 1.25 / 1.15 | `:718-720` |
| 쿨다운 | **없음 — 1회성.** `Phase.Consumed`가 종단 | `ComebackAsymmetryTests:115-121`이 고정 |

**쿨다운이 "없음"인 것이 기록이다** — 재발동이 불가능하므로 쿨다운이 필요 없고, 그것을
`CampingTheDangerBandCannotReArmTheComeback`이 고정한다(위험 대역에 앉아 있어도 되돌려받지
못한다 — Worms의 Pile 조항이 패치해야 했던 익스플로잇).

### 캡이 실제로 보장하는 것 — 계산

```
캡 140 < 코어 최대 150   →  만피 코어는 한 방에 지워지지 않는다
캡 도달 하한: 플레이어 기본 63.6 / AI 기본 87.5
두 배수가 의미를 갖는 창: 기본 63.6 ~ 87.5  (그 밖에서는 둘 다 캡 또는 둘 다 미달)
```

`AtTheShippedShot_TheCapErasesTheMultiplierAsymmetry`가 출하 기본값에서 **양측이 같은
캡 히트**를 낸다는 것을 고정한다 — 즉 문서가 적은 배수 비대칭(2.2 대 1.6)은 출하값에서
발현되지 않는다.

### 그런데 ≤30%는 이것으로 판정할 수 없다

**캡은 만피 코어만 보호한다.** 10 이상 손상된 코어(≤140)는 버프 히트 한 방에 사라질 수 있다.
역전은 *"내 코어 ≤52.5 이고 동시에 상대 코어 ≤140"* 인 상태를 요구하고, 후자는 거의 항상
참이다.

그러므로 **확률을 내려면 발동 시점의 상대 코어 HP 분포가 필요**하고 그것은 실제 경기에서만
나온다.

**두 경로 다 막혀 있다**:
- `SiegeDuelSimulation`이 **LastStand를 모델하지 않는다** — `grep -c "LastStand"` = **0**
- `Telemetry.EventKind`가 `MatchStart / Volley / Collapse / MatchEnd / Session` 뿐이고
  **컴백 발동 이벤트가 없다**

**계측 추가가 선행 조건이다.** 이 문서는 그것을 요구로 남기고 확률을 주장하지 않는다.

---

## 3. 무료 경로 패리티 — 밴드 밖이고, 밴드가 적용되는지가 먼저다

`SiegePrototypeEconomy`:

```
SeriesVictoryMarks   = 12   (시리즈 승리 1회당 마크 12)
BattleBannerSealPrice = 12  (유일한 구매 항목 가격 12)
```

→ **시리즈 승리 1회로 유일한 구매 항목이 열린다.** 계약이 요구하는 10~20 세션 밴드의
**밖**이다(1 ≪ 10).

**그러나 밴드가 적용되는지가 먼저다.** 그 항목은 코드 주석이 *"one-time,
gameplay-neutral battle-banner seal"*(`:30`)이라 적고, 클래스 주석이 *"no price, IAP catalog,
receipt, advertisement, random reward, or gameplay-stat effect"*(`:7-8`)라 적는다.

10~20 세션 밴드는 **유료 경로와 무료 경로의 도달 시간 차이**를 재는 것이다. 여기에는
유료 경로가 없다(§1) — 마크는 구매할 수 없고 시리즈 승리로만 얻는다. **비교 대상이 없는
패리티는 정의되지 않는다.**

**디렉터 판단 요청**: 이 임계값을 (가) 해당 없음으로 면제할 것인가, (나) 밴드를 재정의할
것인가, (다) 위반으로 기록할 것인가. **PM 레인은 (가)를 제안하나 면제는 디렉터 소유**이고
계약은 면제에 이유와 만료일을 요구한다.

---

## 4. 이 문서가 하지 않는 것

- **게이트 판정을 하지 않는다.** 수치만 낸다.
- **≤30%를 주장하지 않는다.** 계측이 없다.
- **컴백 값을 조정하자고 제안하지 않는다.** 회고가 *"LAST STAND를 건드리지 않았다 — 캡이
  비대칭을 지우는 것은 발견이나 그 캡이 다른 요구를 만족시키므로 디렉터 판단"*이라 적었고
  그 판단은 아직 없다.
- **`MobileStorefront`의 실제 결제 흐름을 검증하지 않았다.** 에디터에서
  `MobileStorefrontState.Unavailable`로 조기 반환하므로(`:133`) 실기기 검증이 필요하고
  이 사이클 범위 밖이다.
