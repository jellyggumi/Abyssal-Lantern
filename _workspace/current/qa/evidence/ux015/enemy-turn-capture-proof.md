# UX-015 해소 — 적 턴 프레임과, 그것을 얻으며 찾은 하네스 결함

- date: 2026-08-19
- 실행 방식: **유니티 직접 실행** (`-batchmode`, `-nographics` **없음** — 실제 그래픽 디바이스)
- 산출: `qa/evidence/visual/ux-4-enemy-turn.png` — **이 저장소 최초의 적 턴 스크린샷**

---

## 0. 결과

| 항목 | 상태 |
|---|---|
| `ux-4-enemy-turn.png` | **존재** (1,198,983 bytes) |
| `ux-measurements.txt` | **4블록** (이전 3) |
| 프레임 내용 | `state=AITurn playerTurn=False turn=1`, **buttons=0** |
| 픽셀 육안 확인 | **보드 + HUD 전체** — `적 턴` 배너, `ENEMY BATTERY` 스트립, 봉인된 궤적 아크, 탄착 폭발 |

**UX-014가 측정한 것이 화면으로 확인됩니다** — 적 턴에 활성 버튼 0개.

---

## 1. 그리고 하네스 결함을 찾았다 — 캡처가 프롤로그를 찍고 있었다

첫 성공 실행이 통과했는데 **픽셀이 웹툰 프롤로그였습니다.** 네 프레임 전부.

```
ux-measurements.txt:  state=AITurn playerTurn=False buttons=0     ← 모델은 정확
ux-4-enemy-turn.png:  웹툰 프롤로그 패널                              ← 픽셀은 딴 것
```

**측정은 모델을 읽고 픽셀은 맨 위에 있는 것을 담았습니다.** 이 사이클이 반복해서 기록한
그 실패 계열이고, 이번에는 **라벨과 이미지가 어긋나는** 형태입니다.

### 원인은 제가 만들었다

MCP 플러그인 노이즈가 테스트를 실패시켜서 `keepConnected: false`로 껐습니다. 그것이
**부팅을 빠르게 해** 콜드오픈 영상이 아직 화면을 덮은 채로 캡처가 돌았습니다.

`ReloadArena`가 **고정 1.5초**를 기다립니다(`VisualEvidenceCapture.cs:107`) — 그것은
"그 사이 컷신이 끝난다"는 **베팅**이고, 부팅이 빨라지자 졌습니다.

### 왜 이것이 위험한가 — 그 PNG는 인용되고 있다

`ux-3-player-turn.png`는 `ux-defect-list.md`가 **세 결함의 근거로 지목**합니다:

- UX-002: *"화면 부재: `ux-3-player-turn.png` 우상단 공백"*
- §밀도 표: *"`ux-3-player-turn.png`에서 세어지는 텍스트 덩어리 수와 일치"*
- UX-010: *"육안: `ux-3-player-turn.png` 좌상단 `배치 모드` 회색 바와 `KEEP CORE` 배지"*

**프롤로그 프레임에서는 HUD 라벨의 부재를 증명할 수 없습니다** — HUD의 카메라가 아닙니다.
제가 그 파일을 덮어썼고 **`git checkout`으로 복원**했습니다(추적 중이었습니다).

---

## 2. 고친 것 — 시계가 아니라 조건을 기다린다

### (1) 콜드오픈을 플레이어처럼 넘긴다

```csharp
NarrativeVideoIntro.Active?.Skip();   // :63 Settle(true) — 키 입력과 같은 경로
```

### (2) 전면 차폐물이 없어질 때까지 기다리고, 안 되면 크게 실패한다

`WaitForBoardVisible()` — 화면 면적 80% 이상을 덮는 활성 그래픽이 있으면 폴링하고,
12초 뒤에도 있으면 **캡처하지 않고 실패**합니다:

```
After 12s the board is still covered by 'Frame', so every frame this run captured
would show that instead of the game. ... Capturing anyway would produce images
whose labels disagree with their pixels.
```

**차폐물을 클래스 이름 목록으로 찾지 않습니다** — 사각형 면적으로 잽니다. 목록은 새 컷신이
들어오는 순간 낡고, 이 저장소는 그 형태로 다섯 번 대가를 치렀습니다(`CLAUDE.md` §5).

### (3) 가드를 보드가 기대되는 자리에만 둔다

첫 구현이 `BootArena` 안에 있어서 **타이틀 화면을 차폐물로 잡았습니다**(`Backdrop`,
`IntroScreenController.cs:111`). `ux-1-title`은 타이틀을 찍는 것이 맞으므로 가드를
`BeginSiege()` 뒤로 옮겼습니다 — **라벨이 게임플레이를 주장하는 자리**입니다.

### (4) 리로드 뒤에 로그 억제를 다시 세운다

`LogAssert.ignoreFailingMessages`는 정적이고 **씬 로드가 리셋합니다.** 그래서 4프레임을
정확히 찍고도 실패하는 실행이 나왔습니다. `BootArena` 뒤에 한 번 더 세웁니다.

---

## 3. 증거 — 양쪽 조건에서 검증

| 파일 | 조건 | 결과 |
|---|---|---|
| `playmode-guard-RED-occluder.xml` | 가드 있음 + 스킵 없음 | **실패** — `'Frame'`을 이름으로 지목 |
| `playmode-capture-GREEN-mcp-quiet.xml` | `keepConnected: false` (**회귀 조건**) | **1/1 통과**, 4프레임 |
| `playmode-capture-GREEN-mcp-on.xml` | `keepConnected: true` (출하 설정) | **1/1 통과**, 4프레임 |
| `editmode-551-553.xml` | 전체 회귀 | 551/553 — 실패 1은 **MCP 인증 노이즈**(격리 통과), 비통과 1은 정직한 Skipped |

**빠른 부팅과 느린 부팅 양쪽에서 통과합니다.** 그것이 타이밍 의존이 사라졌다는 뜻입니다.

`UserSettings/AI-Game-Developer-Config.json`은 **바이트 동일 원복**했습니다
(`shasum` 2파일 1해시 `3b1c4a8c…`). gitignore 대상이고 추적되지 않습니다.

---

## 4. 캡처가 새로 보여주는 것 — 미등록

적 턴 프레임과 플레이어 턴 프레임에서 **HUD 라벨 두 개가 화면 가장자리에서 잘립니다**:

- 좌상단 `WIND <<< 0.3` → **`W`가 잘려 `IND`로 읽힘**
- 우상단 `SIEGE SCORE` → 오른쪽 끝이 잘림

**UX-001을 닫아 바람이 보이게 되자 드러난 것**입니다 — 안 보이던 동안에는 잘릴 것도 없었습니다.
UX-019(배럴 바람 25% 거짓)와 같은 계열이고, **등급을 부여하지 않습니다** — 이 문서는 캡처가
보여주는 것만 적고 판정은 QA 레인이 소유합니다.

---

## 5. 이 문서가 주장하지 않는 것

- **적 턴 109.7초 전체를 담지 않습니다.** 한 프레임이고, 볼리 해결 직후 0.6초 지점입니다.
  그 시간 동안 화면이 어떻게 변하는지는 미측정입니다.
- **UX-014를 닫지 않습니다.** 이 캡처는 UX-015(증거 부재)를 해소하고, UX-014(입력 0)의
  **해제 경로를 열어줍니다.** 입력 차단 4곳은 여전히 0곳 수정입니다.
- **잘린 라벨을 결함으로 등재하지 않았습니다.** §4 참조.
- **08-14 원본이 왜 정상이었는지 확정하지 않았습니다.** MCP 재시도가 부팅을 늦춰 컷신이
  끝났다는 것이 유력하나, 그 시점 플러그인 상태를 재현하지 않았으므로 `[INFERENCE]`입니다.
