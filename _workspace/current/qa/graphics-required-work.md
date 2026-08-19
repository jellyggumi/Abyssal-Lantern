# 그래픽 디바이스가 필요한 작업 — 인계 목록

- date: 2026-08-18
- 사유: 이 세션은 `-nographics`로만 돌렸다. 아래 항목은 **실제 GPU/디스플레이가 있는
  실행**을 요구하므로 코드만 남기고 실행을 다음 작업으로 넘긴다.
- 계약 근거: `CLAUDE.md` §5 — *"`-nographics` also avoids [the reload hang] in most cases,
  but `cam.Render()` segfaults without a graphics device, so a probe that writes PNGs must
  run WITH graphics and take the hang risk."*

---

## 1. UX-015 — 적 턴 캡처 (**임계 경로**)

### 코드는 어디에 있는가

| 항목 | 위치 |
|---|---|
| 캡처 테스트 | `Assets/Tests/PlayMode/VisualEvidenceCapture.cs` — `InGameUx_StatesCaptured()` |
| 이번에 추가한 부분 | 같은 파일, `ux-3-player-turn` 직후 ~ `Flush()` 전 (약 40줄) |
| 렌더 헬퍼 | 같은 파일 `Shoot(string label)` (`:135`) — `cam.Render()`를 쓴다 |
| 산출 경로 | `_workspace/current/qa/evidence/visual/ux-4-enemy-turn.png` (아직 없음) |
| 측정 텍스트 | `_workspace/current/qa/evidence/visual/ux-measurements.txt` (현재 3블록) |

### 무엇을 추가했는가

`:299`의 docstring이 테스트 작성 시점부터 **"and the AI turn the player cannot act during"**
을 약속했는데 본문은 캡처 3건에서 멈췄다. 그래서 적 턴에 대한 모든 논증이 **존재하지 않는
스크린샷을 인용**하고 있었다.

추가한 흐름:
1. `lm.SimulateLaunch(lm.GetSeparatedAimVelocity())` — **시계를 기다리지 않고 발사로** 턴을
   넘긴다. 게임이 쓰는 것과 같은 경계이고, 조준은 컴포넌트에서 읽은 튜닝 기본값이다.
2. `while (gm.IsPlayerTurn && waited < 12f)` — **턴이 실제로 넘어갈 때까지 대기.** 고정
   `sleep`은 착지한 프레임이 무엇이든 "적 턴"이라 이름 붙이는데, 그것이 캡처 하네스가
   라벨과 픽셀이 어긋나는 방식이다.
3. `while (gm.IsResolvingTurn && ...)` — 볼리 해결을 지나 **결함이 실제로 말하는 정적**으로.
4. `Shoot("ux-4-enemy-turn")` + `RecordUx(...)`.

### 왜 그래픽이 필요한가

`Shoot()`이 `cam.Render()`를 호출하고, 그것은 그래픽 디바이스 없이 **segfault**한다.
`-nographics`로 돌리면 테스트가 실패하는 것이 아니라 **에디터가 죽는다.**

### 실행 방법

```bash
cd ~/Desktop/castle-war
pkill -f "MacOS/Unity"; rm -f Temp/UnityLockfile
# -nographics 없음 - 이것이 요점이다
timeout 700 "/Applications/Unity/Hub/Editor/6000.5.6f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -projectPath . -runTests -testPlatform PlayMode \
  -testResults ./ux.xml -logFile ./ux.log \
  -testFilter "InGameUx_StatesCaptured"
```

**§5의 2회 실행 규칙이 그대로 적용된다** — 1회차가 도메인 리로드에 행할 수 있고,
**스크립트를 바꾸지 않고** 2회차를 돌리면 완주한다. 이 세션에서 그 행을 네 번 겪었다.

### 성공 판정

- `qa/evidence/visual/ux-4-enemy-turn.png` 존재
- `ux-measurements.txt`가 **4블록** (현재 3)
- 프레임이 실제로 적 턴이다 — 턴 라벨이 플레이어가 아님을 보여야 한다.
  **PNG를 열어 눈으로 확인할 것.** 파일 존재는 라벨이 맞다는 증거가 아니다.

### 왜 이것이 임계 경로인가

UX-015는 **S2인데 S1인 UX-014의 해제 경로를 양방향으로 막는다**:
- 적 턴을 고쳤다는 것을 **검증할 방법이 없다** (전/후 비교 대상이 없다)
- 심각도를 낮출 **근거를 만들 수 없다** (재등급은 논증이 아니라 새 측정으로만 — 디렉터 판정)

그리고 UX-014가 open인 동안 **8개 게이트 전부가 차단**이다.

---

## 2. `DefaultParticleTexture_CarriesNoColourAtAllWhichIsWhyAMissingSpriteLooksLikeAWhiteCircle`

| 항목 | 위치 |
|---|---|
| 테스트 | `Assets/Tests/EditMode/ResourceSpriteImportTests.cs` |
| 현재 상태 | **Skipped** (매 실행마다) |

### 왜 건너뛰는가

테스트 자신이 이유를 적는다:

> LIMITATION — the fallback particle texture could not be sampled. It is created with
> `Apply(false, true)` (CPU copy discarded, `isReadable` false) and has no source file to
> decode off disk, so a GPU readback is the only route and no graphics device was available
> (`graphicsDeviceType=Null`). The colourlessness of the fallback is therefore UNVERIFIED
> in this run.

**이것은 좋은 Skip이다** — 검증했다고 주장하지 않고 무엇을 못 했는지 말한다. 552/553의
그 1건이 이것이고, **실패로 세지 말 것.**

### 그래픽과 함께 돌리면

`GetDefaultParticleTexture()`가 정말 무채색인지 GPU 리드백으로 확인된다. 그것이 확인되면
"자산 누락 → 흰 원"의 인과 사슬 끝점이 측정으로 닫힌다. 지금은 코드 읽기로만 닫혀 있다.

---

## 3. 이번 사이클에 그래픽 없이 얻은 것 (참고 — 다시 돌릴 필요 없음)

| 증거 | 경로 | 상태 |
|---|---|---|
| F-2 게이트 뮤테이션 쌍 | `qa/evidence/registers/cond1-status-column-removed-RED.xml`<br>`qa/evidence/registers/cond1-baseline-GREEN.xml` | 완료 |
| F-2 기준선 | `qa/evidence/registers/editmode-baseline-green.xml` | 552/553 |
| F-1 HUD 핀 방어 | `qa/evidence/hud-pin/ux001-002-defence-GREEN.xml` | PlayMode 2/2 |
| F-1 뮤테이션 | `qa/evidence/hud-pin/ux001-adopt-removed-RED.xml` | `Undrawn: WindText` |

**PlayMode 2/2가 그래픽 없이 나온 이유**: `HudCanvasContractTests`의 두 신규 테스트는
부모 연쇄와 캔버스 이름을 읽고 **픽셀을 렌더하지 않는다.** 그래서 `(씬)` 관측층이고
**`(배포)`가 아니다** — 동료 레인의 정정을 그대로 인용한다.

---

## 4. 그래픽 실행의 알려진 위험

| 위험 | 실측 | 대응 |
|---|---|---|
| 도메인 리로드 행 | 이 세션에서 4회 (902s·702s·746s 타임아웃 + 1회차 행) | 스크립트 무변경으로 연속 2회 |
| MCP 인증 로그 오염 | 임의의 테스트 1건이 매 실행 실패로 뒤집힌다 | 격리 재실행으로 노이즈 확인 |
| ScreenSpaceOverlay 미포착 | `Shoot()` `:129-133`이 기록 — 첫 구현이 **UI 없는 프레임**을 냈다 | 이미 해결됨(캔버스를 ScreenSpaceCamera로 전환·복원) |

세 번째가 특히 중요하다 — 캡처가 **그럴듯하지만 UI가 빠진 프레임**을 낼 수 있고, 그것은
검은 이미지보다 나쁘다. **찍은 PNG를 반드시 열어볼 것.**

---

## 5. 다음 작업 순서 (PM 레인 레버비 분석)

0. **UX-015 캡처** ← 이 문서의 1번. 그래픽 필요.
1. **UX-014 코드 처방** — 레버비 8:1(한 건이 8게이트 지배)
2. **G5** — 가장 싸다. 유료 지점 0건이라 임계값 4개 중 2개가 공허참, cap/cooldown은 이미
   5핀으로 고정. 사람 세션 불요, **그래픽 불요**
3. **G2 런타임** — 기계 4시간 무인, **그래픽 불요**
4. **G6** — perf 4항이 프로파일러 1세션으로 동시 산출. **그래픽 필요**
5. **G3+G4+G7+G8** — 사람 세션 1블록 공유. **0번 뒤여야 한다** — S1 open이면 같은 세션에서
   G4를 못 얻어 가장 비싼 자원을 4개가 아니라 3개에만 쓴다
