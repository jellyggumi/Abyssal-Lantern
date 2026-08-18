# 차단 술어를 집행 가능하게 만든 증거 (F-2)

- date: 2026-08-18
- 산출: `Assets/Tests/EditMode/DefectRegisterGateTests.cs` (4 테스트)
- 근거: `production/decision-log.md` D-2026-08-18 (디렉터가 F-2를 이 사이클의 진짜 산출로 지정)

## 무엇이 문제였는가

계약이 *"Any open S1 defect blocks every gate"* 인데, **무엇이 open인지 알 수 없었다** —
`qa/ux-defect-list.md`에 S1 4건이 있고 **status 열이 없었다.** 판정 불가한 차단은
급한 사람에게 "차단 없음"으로 읽히고, 그래서 16건이 쌓이는 동안 게이트 리뷰가 진행됐다.

계약은 바로 옆 줄에서 이미 부재의 기본값을 골랐다 — *"Missing evidence path = FAIL
regardless of claimed value"*. 증거 부재는 처리하고 status 부재는 처리하지 않은 **비대칭**이
구멍이었고, 이 게이트가 같은 방향으로 확장한다.

## 4개 단언

| 테스트 | 무엇을 막는가 |
|---|---|
| `EverySeverityTable_CanExpressStatus` | 심각도를 매기면서 해소할 수 없는 대장 |
| `NoSeverityRow_LeavesItsStatusBlank` | 빈 status(= 조용한 통과) |
| `WhileAnyS1IsOpen_NoGateReviewClaimsPass` | **계약 문장 자체** |
| `EveryRegisterFile_DeclaresWhetherItIsAuthoritative` | 두 대장이 어긋날 때 무엇을 믿을지 없는 상태 |

**디스크를 걷는다.** 선언 목록을 걸으면 `CLAUDE.md` §5가 이미 네 계층으로 기록한 그 실패의
다섯 번째가 된다 — 나중에 추가된 대장이 영구히 검사 밖에 남는 것이, `ux-defect-list.md`가
녹색 스위트 옆에서 16건 깊이로 자란 방식 그대로다.

## 이빨 증명

가짜 게이트 리뷰 `zz-mutation-probe.md`에 `verdict: PASS` 한 줄:

```
mutation: 0 / 1 failed 1
2 S1 defect(s) read as open ... but these gate reviews claim PASS.
  Open S1: qa/register-reconciliation.md:818 [UX-014], qa/ux-defect-list.md:126 [UX-014]
  Claiming PASS: production/gate-reviews/zz-mutation-probe.md:4 — - verdict: PASS
```

**UX-014를 두 대장에서 찾아 이름과 줄번호로 부르고**, PASS 주장을 잡았다. 프로브 제거 확인
(`gate-reviews/` 1파일).

## 첫 실행이 잡은 진짜 결함

게이트를 처음 돌렸을 때 **4 중 2 실패**:
- 대장 5개가 역할 미선언 → 5개 전부에 `register-role:` 추가(정본 3 / 파생 2)
- 감사 문서의 판정표가 status 없다고 걸림 → **제 게이트의 오탐이었다.** `결함 | 등급 | 한 줄
  판정` 표는 결함을 추적하지 않고 소견을 말하므로 status가 없는 것이 옳다. `IsRegisterFile`로
  범위를 좁혔다 — 동료 레인이 제 HUD 핀에서 방금 잡은 것과 **같은 오탐 형태**이고,
  깨끗한 저장소에서 빨강인 테스트는 더러운 저장소에서 아무것도 증명하지 못한다.

## 화해 결과

`ux-defect-list.md`에 `상태` 열을 넣고 이 사이클 회의가 확정한 값을 등재:
**UX-001/002/003 closed, UX-014 open.**

역할 분리(합치지 않음): D-계열 = 실행 실패(정본 근거는 테스트 XML), UX-계열 = 감사 소견
(정본 근거는 코드 경로). 서로소인 것은 결함이 아니라 역할 분리이며, **같은 결함이 두 대장에
있으면 그것이 결함이다.**

## 이 게이트가 하지 못하는 것

- **파생 대장의 ID가 정본에 있는지 검사하지 않는다.** 역할은 선언되지만 그 함의는
  아직 집행되지 않는다.
- **배포 도달을 보지 않는다.** PM 레인이 제안한 (c) 조건 — closed 근거 커밋이 라이브 배포
  팁의 조상인지 — 은 배포 스크립트 쪽이고 여기 없다. 그리고 프로그래머 레인이 그 조건의
  초안을 **자기 반증으로 철회**했다: 라이브 후보 4개가 전부 `origin/main` 비조상이라
  게이트로 쓰면 이 사이클에 닫은 것 전부가 실패한다.
- **`regression-guard` 열을 요구하지 않는다.** QA가 설계한 9열 중 이 게이트는 `status`와
  역할만 본다. 핀 존재를 요구하려면 테스트 이름이 실재하고 최신 XML에서 통과했는지를
  봐야 하고, 그것은 다음 사이클이다.
- **끝점 표기를 검사하지 않는다.** QA가 `main`과 `origin/main`이 같은 커밋에 반대 답을
  준다는 것을 실측했다(83커밋 격차). 원격 추적 ref 강제는 미구현이다.
## 동료 레인이 제 게이트에서 찾은 결함 3건 (전부 수정)

게이트를 쓴 뒤 `ProgrammerPinPlan`이 검수했고, **셋 다 이 사이클이 이미 기록한 실패 형태의
재발**이었다. 자기 게이트를 자기가 검수하면 이것을 못 찾는다.

### A. 무단언 통과 — 이 사이클이 방금 막은 그 구멍

```csharp
if (!Directory.Exists(reviewRoot)) { Assert.That(openS1, Is.Not.Null); return; }
```

`openS1`은 `.ToList()` 결과이므로 **null이 될 수 없다.** `gate-reviews/`가 이름만 바뀌면
계약의 중심 문장이 **아무것도 단언하지 않고 통과**한다. HUD 핀에서 `Assert.Greater(checkedCount,
0)`이 막으려고 넣은 바로 그 구멍이고, **같은 사이클에 한 테스트에서 막고 다른 테스트에서
재현했다.** 부재를 실패로 바꿨다.

### B. 산문 블랙리스트 — 선언 목록의 다섯 번째 계층

`\bPASS\b` 검색 + 예외 목록(`PASS / FIX`, `가능`, `후보`, `불가`)이었다. 게이트 리뷰를 실제로
쓰는 순간 `PASS 조건`·`PASS 기준`·제목 `## G4 PASS`가 전부 오탐이 된다. **구조로 교체** —
`verdict:` 키 줄만 읽는다. 산문에서 단어를 찾는 대신 형식을 읽으면 블랙리스트가 필요 없다.

### C. 검출이 셀 서식에 의존 — 우연히 통과하던 표

롤업 표(`| S1 (치명) | 4 | … |`)가 앵커 정규식 `^\**\s*(S[123])\s*\**$`에 걸리지 않아
**설계가 아니라 우연으로** 검출을 피했다. 누가 셀을 `| S1 |`로 정리하면 롤업에 status를
요구하며 빨강이 된다. `\b`로 느슨하게 하고 롤업을 **행 모양**(심각도 뒤 순수 숫자)으로
제외했다 — 서식을 정리해도 판정이 움직이지 않는다.

그 셋을 고치니 파생 문서의 산문 표가 새로 걸렸고, **정본만 status를 소유한다**로 좁혔다
(`IsCanonicalRegister`). 역할 선언이 그 구분을 기계가 읽게 만든 것이 여기서 값을 냈다.

## 뮤테이션 재증명 (구조 3곳을 고친 뒤)

| 뮤테이션 | 빨강이 된 테스트 |
|---|---|
| `gate-reviews/zz-probe.md`에 `- verdict: PASS` | `WhileAnyS1IsOpen_NoGateReviewClaimsPass` |
| `ux-defect-list.md`의 상태 열 제거 | `EverySeverityTable_CanExpressStatus` |

**원복 확인**: `shasum` 2파일 1해시 `2b0a1fa8…`, `git diff` 비었음.
기준선: `editmode-baseline-green.xml` — **EditMode 552/553**, 유일한 비통과는 그래픽 디바이스
없음을 정직하게 보고하는 Skipped.

## 원복 규율의 실패 하나 — 기록해 둔다

뮤테이션 실행이 617초였고 그 창 동안 `ProgrammerPinPlan`이 디스크를 읽어 **"원복 누락"으로
보고했다.** 파일은 그 순간 정말 상태 열이 없었다 — 두 관측이 둘 다 참이고 시각이 다르다.
같은 대역에서 QA도 줄번호를 1줄 틀렸다.

**교훈은 관측자 쪽이 아니라 뮤테이션을 돌린 쪽에 있다.** 원복을 한 셸 호출 안에 넣어
원자적으로 만들었지만, **공유 작업 트리에서 그 창 동안 다른 레인이 읽는다**는 것을 고려하지
않았다. 공유 트리에서 뮤테이션은 배타적 작업이고 시작·종료를 브로드캐스트해야 한다.

이것은 `CLAUDE.md` §5의 "고친 것이 그것을 인용한 결정을 무효화한다"의 **시간 상수가 초 단위인
판**이며, 무효화 주체가 관측 대상 자신이다.

## 보존된 뮤테이션 쌍 (조건별)

`ProgrammerPinPlan`이 정당하게 지적했다 — 제가 정리 단계에서 XML을 지워 **조건 1의 이빨
증거가 사라졌고**, 그 표는 증거가 아니라 주장이 됐다. 계약의 *"Missing evidence path =
FAIL"* 을 제 문서에 적용하면 그렇다. 다시 만들어 조건 이름으로 보존했다:

| 파일 | 상태 | 결과 |
|---|---|---|
| `cond1-status-column-removed-RED.xml` | 상태 열 제거 | 3/4 — `EverySeverityTable_CanExpressStatus` 빨강 |
| `cond1-baseline-GREEN.xml` | 원복 | **4/4** |
| `editmode-baseline-green.xml` | 원복, 전체 스위트 | **552/553** |

**쌍이어야 뮤테이션 증명이다.** 실패본만 남기면 "빨강일 수 있다"만 증명되고 *"올바른 상태에서
녹색이다"* 가 빠진다 — 동료 레인이 제 HUD 핀에서 지적한 것과 같은 구멍이다.
**파일명은 조건을 담는다**: `g4`/`g5`는 이 세션 밖에서 의미가 없고, 이름은 이동을 견디고
경로와 줄번호는 못 견딘다.

## bash `grep -c`가 그럴듯한 틀린 수를 낸다 — 창을 보이지 않게 만든다

QA가 보고하고 제가 재현했다. 같은 파일의 두 버전:

| 입력 | 줄 수 | `심각도 \| 상태` 진상 | bash `grep -c` |
|---|---:|---:|---:|
| `HEAD` 블롭 (상태 열 없음) | 228 | **0** | 16 |
| index 블롭 (상태 열 있음) | 232 | **3** | 16 |

**증명적으로 다른 두 입력이 같은 수를 냈다.** 앞서 확인한 "bash grep이 조용히 빈 결과"의
더 위험한 판이다 — 빈 결과는 의심을 부르지만 **그럴듯한 수는 결론이 된다.** 동료 레인이
이 수로 QA의 반증을 기각할 뻔했다("세 상태가 같은 수니 QA가 틀렸다"가 자연스러운 답이다).

**유일한 신호는 값이 아니라 관계였다.** 그러므로 창 전/후를 bash `grep -c`로 대조하면
창이 보이지 않는다. 수를 세는 것은 `grep` 도구나 스크립트로 하고, bash `grep -c`는 이
저장소에서 **측정 도구가 아니다.**
