# 협상 기록 — 매출 지점과 밸런스의 결합

- run-id: 20260809-castle-war-stage1 (cycle 3)
- owner: game-pm 레인 ↔ game-designer 레인
- date: 2026-08-19
- 계약: `skill://game-studio-harness/references/quality-gates.md` G5 —
  *"every revenue point has a signed negotiation-record entry"*
- 자매 문서: `pm/reward-bands.md` (수치), `production/decision-log.md` (디렉터 결정)

---

## 0. 이 문서가 왜 지금 생겼는가

계약이 이 파일을 이름으로 요구하는데 **없었다.** PM 레인이 2026-08-18에 `reward-bands.md`와
함께 MISSING으로 보고했고, 그것이 G5의 네 번째 임계값을 자동 실패시키고 있었다.

**없던 이유는 협상이 없었기 때문이 아니다** — 협상 결과가 여러 문서에 흩어져 있었고
계약이 요구하는 형식(매출 지점 ↔ 밸런스 결합의 서명된 항목)으로 모인 적이 없다.

---

## 1. 매출 지점 전수 — 1건

| # | 지점 | 종류 | 게임플레이 영향 |
|---|---|---|---|
| R-1 | **Chronicle Pack** | 일회성 비소비성 IAP (`MobileStorefront`) | **없음** |

`Assets/Scripts/MobileStorefront.cs:57` Unity IAP 5 어댑터, `:143`
`UnityIAPServices.StoreController()`. 상품은 하나다.

**다른 후보는 매출 지점이 아니다**:
- `SiegePrototypeEconomy` — 마크는 **구매할 수 없다.** 시리즈 승리로만 얻고(`:12`),
  클래스 주석이 *"no price, IAP catalog, receipt, advertisement, random reward"*(`:7-8`)라
  적는다. 무료 경로 전용 원장이다.
- 광고 — 코드에 없다.
- 랜덤 보상 — 위 주석이 명시적으로 배제한다.

---

## 2. R-1 협상 항목

### 결합 주장

**PM**: Chronicle Pack은 프롤로그 리플레이를 연다. 그것은 서사 자산의 재접근이고
**밸런스와 결합하지 않는다.**

### 디자이너 검증

**소비처 전수로 검증했다** — `HasChroniclePack`을 읽는 곳:

| 위치 | 효과 | 밸런스 결합 |
|---|---|---|
| `IntroScreenController.cs:234` | 리플레이 버튼 1개 생성 | **없음** |
| `MobileStorefront.cs:75` | 재구매 차단 | 없음 |
| `:164`, `:290`, `:514` | 스토어 문구 | 없음 |

**게임플레이 소비처 0건.** 유닛·피해·성벽·바람·경제·난이도 어디에도 닿지 않는다.

### 합의

**R-1은 밸런스와 결합하지 않는다.** 따라서 G5의 *"유료/무료 승률 격차 ≤5%p"* 는
**0%p로 구조상 충족**된다 — 격차를 만들 경로가 없다.

### 서명

| 역할 | 판정 | 근거 |
|---|---|---|
| game-pm | 결합 없음 | 상품 정의, 소비처 전수 |
| game-designer | 결합 없음 확인 | 같은 전수를 독립 재현 |
| game-production-director | **채택** | 위 두 판정이 같은 방법으로 같은 결과 |

---

## 3. 결합하지 않지만 기록해야 하는 것 — 컴백

컴백(LAST STAND)은 **매출 지점이 아니다.** 무료다. 그런데 G5가 그것을 임계값에 명명하므로
(*"comeback instant-reversal probability ≤30% per activation with recorded cap/cooldown"*)
여기 기록한다.

| 항목 | 값 | 위치 |
|---|---|---|
| 캡 | 140 (코어 최대 150) | `DynamicBattlefield.cs:726` |
| 쿨다운 | **없음 — 1회성**, `Phase.Consumed` 종단 | `ComebackAsymmetryTests:115-121` |
| 발동 | 자기 코어 ≤35% | `:712` |

**≤30% 확률은 미측정이다.** `SiegeDuelSimulation`이 LastStand를 모델하지 않고(grep 0건)
`Telemetry.EventKind`에 컴백 이벤트가 없다. 계측 추가가 선행 조건이며 `reward-bands.md` §2가
상세를 담는다.

**협상 관점의 기록**: 컴백은 무료 장치이므로 유료 우위를 만들지 않는다. 그러나 **캡이
배수 비대칭을 출하값에서 지운다**는 것이 측정됐고(`AtTheShippedShot_...`), 그것은 문서가
적은 "플레이어가 더 강한 컴백을 갖는다"가 실제로는 발현되지 않는다는 뜻이다. **이 불일치는
매출이 아니라 밸런스 문서의 문제**이고 디렉터 판단 대기다(회고 인용:
*"그 캡이 다른 요구를 만족시키므로 디렉터 판단"*).

---

## 4. 미해결 — 무료 경로 패리티

계약이 *"free-path parity within stated 10–20 session band"* 를 요구하는데:

```
SiegePrototypeEconomy: 시리즈 승리 1회 = 마크 12, 유일한 구매 항목 = 12
→ 시리즈 승리 1회로 열린다
```

**밴드 밖이다**(1 ≪ 10). 그런데 그 항목은 무료 경로 전용이고 유료 경로가 존재하지 않으므로
**비교 대상이 없는 패리티**다.

**PM 제안**: 해당 없음으로 면제. **디렉터 소유**이고 계약이 면제에 이유와 만료일을 요구한다.
`decision-log.md`에 항목이 필요하다 — 이 문서가 그것을 대신하지 않는다.

---

## 5. 이 문서가 하지 않는 것

- **게이트를 통과시키지 않는다.** §4가 미해결이고 §3의 확률이 미측정이다.
- **R-1의 결제 흐름을 검증하지 않았다.** 에디터에서 `Unavailable`로 조기 반환하므로
  (`MobileStorefront.cs:133`) 실기기 검증이 필요하다.
- **가격을 논의하지 않았다.** 현지 통화 문자열을 스토어에서 받아오고(`:82`) 이 문서는
  그것이 밸런스와 무관하다는 것만 다룬다.
- **미래 매출 지점을 예단하지 않았다.** R-1이 유일한 현재 지점이고, 추가되면 **이 문서에
  항목이 추가돼야** G5가 다시 충족된다 — 그것이 이 파일이 존재하는 이유다.
