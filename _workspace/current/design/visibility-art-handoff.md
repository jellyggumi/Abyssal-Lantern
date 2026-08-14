# 가시성 v2 — 아트 후속 제작 안내서

- run-id: 20260809-castle-war-stage1 (cycle 2)
- owner: design lane
- date: 2026-08-13
- 대상 기획서: `design/visibility-spec-v2.md`
- 이 문서의 용도: **지금 흰 네모로 나가 있는 것 하나를 나중에 실제 아트로 교체하는 방법**,
  그리고 **다시 만들면 안 되는 것 여섯 개와 그 이유**

---

> ## ⚠ §1은 폐기됐다 (2026-08-14). §2는 유효하다.
>
> 이 문서는 착탄 마커 **1장을 실제 아트로 교체하는 절차**로 쓰였다. 이후
> `.survey/siege-impact-vfx-and-attack-motion/` 조사가 **그 마커의 형식 자체를 반증**했다 —
> 표본 13종 중 탄착점에 아이콘을 남기는 게임은 Battleship 하나뿐이고 그건 **월드가 보이지
> 않는 게임**이며, 월드가 전부 보이는 우리 판에서 아이콘은 이미 일어난 지형 파괴를 가리는
> 중복이었다. 마커는 교체되지 않고 **삭제**됐다.
>
> | 절 | 상태 |
> |---|---|
> | §1 교체 절차 (`ui_ph_impact_marker`) | **폐기** — 대상 파일이 없다. 후속본 `design/attack-motion-and-impact-vfx.md` §1·§8 |
> | §2 취소된 것들 | **유효**, 6종 → **7종**(착탄 마커 추가) |
> | §3 이미지가 아닌 것들 | 유효 |
> | §4 도구·쿼터 | 유효 |
> | §5 승격 규칙 | 유효 |
>
> **현재 플레이스홀더는 0개이고, 지금 필요한 생성물도 없다.**
> 지우지 않고 남기는 이유는 §1이 왜 틀렸는지가 다음 사람에게 필요한 정보이기 때문이다 —
> "흰 네모를 예쁜 네모로 바꾸면 된다"는 판단을 반복하지 않기 위해서다.

## 0. 지금 상태 한 줄

> 이번 작업은 이미지 **1장만** 쓴다. 그마저 흰 네모(플레이스홀더)이고, 나머지 시각
> 요소는 전부 이미지가 아니라 선(LineRenderer)과 글자다.

이건 부족이 아니라 결정이다. 조사(`.survey/siege-visibility-and-telegraph/`)가
"놓칠 때마다 표시를 추가하는" 경로를 검증된 실패로 지목했고, 그 근거로 초안의 7장 중
6장을 취소했다. 자세한 이유는 §2.

---

## 1. 교체 대상 — 딱 1장

### `ui_ph_impact_marker.png`

| 항목 | 값 |
|---|---|
| 경로 | `Assets/Resources/Gimmicks/ui_ph_impact_marker.png` |
| 현재 | **흰 네모 + 얇은 검은 테두리**, 128×128 RGBA |
| 코드 참조 | `ShotTraceDirector.ImpactMarkerSprite = "ui_ph_impact_marker"` |
| 로드 경로 | `GimmickSpriteLibrary.Load(key)` → `Resources.Load<Sprite>("Gimmicks/" + key)` |
| 화면에서 무엇인가 | **직전 샷이 착탄한 지점**. 궤적 아크의 마지막 점에 놓인다 |
| 색 | 코드가 **팀색으로 틴트한다** — 아군 `(0.45,0.85,1)`, 적 `(1,0.35,0.25)`, 알파 0.5 |
| 정렬 | `sortingOrder = 3` (자기 궤적선 2 바로 위) |

#### 제작 조건 (이걸 어기면 화면에서 안 맞는다)

1. **크기 128×128 유지.** 늘려도 되지만 정사각·중심 정렬이어야 한다. 마커는 스프라이트
   피벗을 중심으로 착탄점에 놓인다.
2. **흰색/무채색 실루엣으로 그린다.** 코드가 `SpriteRenderer.color`에 팀색을 곱한다 —
   이미 색이 들어간 아트를 넣으면 곱셈으로 탁해지고 아군/적 구분이 사라진다.
3. **알파 배경(투명) 필수.** 불투명 배경은 궤적선과 전장을 가린다.
4. **방향성 없는 형태로.** 마커는 회전하지 않는다. 조준점·십자·동심원·충격 균열처럼
   회전 불변인 형태가 맞고, 화살표는 틀리다.
5. **작은 크기에서 읽히게.** 실제 화면 점유는 1~1.5 월드 유닛 수준이다. 얇은 선 위주
   디테일은 사라진다 — HUD 폰트가 6.5px에서 획을 잃었던 것과 같은 이유
   (`qa/hud-font-defect.md`).

#### 프롬프트 초안 (CLAUDE.md §3 도구 우선순위: Codex CLI → 그다음 Higgsfield)

```
2D game UI asset, single icon on a fully transparent background.
An impact / point-of-strike marker: concentric broken ring with four short
tick marks at the cardinal points and a small solid dot at the exact centre.
Pure white silhouette only, no colour, no gradient, no text, no arrow.
Rotationally symmetric. Crisp edges readable at 32 pixels.
Dark-fantasy medieval siege game HUD style.
```

색은 넣지 말 것 — 위 조건 2.

#### 교체 절차

```
1) 새 PNG를 같은 이름으로 덮어쓴다:
   Assets/Resources/Gimmicks/ui_ph_impact_marker.png
   ★ 파일명을 바꾸지 않는다. 이름은 코드 상수이고, 바꾸면 참조가 끊긴다.

2) .meta 를 지우지 않는다. 기존 .meta 가 textureType: Sprite 를 보장한다.
   메타 없이 PNG만 넣으면 Unity 가 textureType: Default 로 임포트하고
   Resources.Load<Sprite> 가 null 을 돌려준다 → 마커가 조용히 안 그려진다.
   (작업 #17에서 실제로 겪은 결함이다. 예외가 아니라 침묵이다.)

3) provenance 를 나란히 둔다:
   Assets/Resources/Gimmicks/ui_ph_impact_marker.png.provenance.json
   스키마는 같은 폴더의 castle_keep_s0.png.provenance.json 와 동일:
   { file, prompt, tool, model, generatedAt, checksumSha256,
     runtimeEligible, notes, width, height }

4) 테스트 1건이 반드시 빨개진다 — 정상이다:
   VisibilitySpecAssetTests.Placeholders_AreStillWhiteBoxes
   이 테스트는 "가운데 픽셀이 흰색인가"를 본다. 실제 아트가 들어오면 실패한다.
   → 그때 Placeholders 배열에서 이 항목을 지우고 Existing 배열로 옮긴다.
     (그 이동이 "임시가 정식이 됐다"는 기록이다. 테스트를 지우지 말 것.)

5) 검증:
   VisibilitySpecAssetTests.EveryPlaceholder_LoadsAsASprite (또는 이동 후 ReusedArt_…)
   ShotReadbackLiveSceneTests.PlayerShot_LeavesAnArcAnImpactMarkerAndAReadbackLine
   → 후자가 마커가 아크 마지막 점에 실제로 놓이는지 실씬에서 본다.
```

#### 아트가 없어도 게임은 깨지지 않는다

`ShotTraceDirector.Draw()`가 `sprite == null`이면 `Marker.enabled = false`로 둔다.
마젠타 사각형이 뜨는 대신 **마커만 사라지고 궤적 아크는 계속 읽힌다.** 그래서 아트
제작이 배포를 막지 않는다.

---

## 2. 다시 만들면 안 되는 것 — 6장

초안(`design/visibility-spec.md` §1-B)이 만든 흰 네모 7장 중 6장을 **삭제했다.**
`VisibilitySpecAssetTests.CancelledPlaceholders_StayDeleted`가 부활을 막는다.

| 파일 (삭제됨) | 왜 만들지 않는가 |
|---|---|
| `ui_ph_enemy_telegraph.png` | **사전 예고를 안 만든다.** 예고는 대응 수단과 긴 정보 창을 요구하는데 우리는 적 턴 입력 0·0.9초다. Into the Breach 원문이 예고의 목적을 "내 턴에 적의 계획을 교란하는 것"이라 밝히는데, 교란 수단이 없으면 목적을 잃는다 |
| `ui_ph_turn_banner.png` | 턴 라벨 + 플로우 스트립이 이미 누구 차례인지 말한다. 세 번째 표시는 중복이다 |
| `ui_ph_power_meter.png` | 당김 중 실시간 파워/각도 수치가 이미 있다 |
| `ui_ph_angle_dial.png` | **초안 스스로** "궤적이 이미 있어 중복도 있음"이라 적었다(§6 6순위) |
| `ui_ph_projectile_next.png` | 이번 라운드 발사체 초상이 이미 나온다(작업 #48) |
| `ui_ph_step_coach.png` | `controlGuideText`가 이미 그 자리·그 역할이다 |

> 이 목록은 "나중에 하자"가 아니라 **"하지 않기로 했다"**다. 되살리려면 근거가 바뀌어야
> 하고, 근거가 바뀌면 `CancelledPlaceholders_StayDeleted`에서 해당 줄을 **의도적으로**
> 지우는 것이 그 기록이 된다.

근거 원문: `.survey/siege-visibility-and-telegraph/solutions.md`,
`actual-lane-c.md` §D-4, `design/visibility-spec-v2.md` §1-B·§5-B.

---

## 3. 이미지가 아닌 것들 (아트 요청 대상이 아님)

혹시 "이것도 이미지로 만들어야 하나" 싶은 항목을 미리 정리한다.

| 요소 | 구현 | 아트 필요? |
|---|---|---|
| 샷 궤적 아크 (R-1) | `LineRenderer`, 팀색 그라디언트, 폭 0.075 | **아니다.** 선은 코드가 그린다 |
| 턴 판독 한 줄 (R-3) | 기존 `FlowStateStrip` TMP 라벨 | **아니다.** 글자다 |
| 적 턴 상태 문구 | 같은 스트립 | 아니다 |

### 선택 사항 (지금 필요하지 않음)

궤적선에 텍스처를 입히고 싶다면 `ShotTraceDirector.Draw()`의
`new Material(Shader.Find("Sprites/Default"))`에 타일링 스프라이트를 주는 방식이 있다.
다만 **지금 하지 말 것을 권한다** — 조사가 지목한 실패 경로가 정확히 "읽히지 않을 때마다
장식을 더하는 것"이고, 판독 효과가 측정되기 전의 장식은 측정을 흐린다
(`design/visibility-spec-v2.md` §7의 미측정 항목 참조).

---

## 4. 도구·쿼터 상황 (2026-08-13 시점)

CLAUDE.md §3이 정한 소유자를 그대로 따른다.

| 자산 종류 | 도구 | 이번 세션 상태 |
|---|---|---|
| 2D 스프라이트 / UI 아트 | **Codex CLI** (`codex exec`) | 과거 세션에서 쿼터 소진 이력 있음(작업 #17: `Aug 16th` 리셋) — 현재 여부 미확인 `[INFERENCE]` |
| 같은 백엔드 | `gti` (god-tibo-imagen) | 작업 #17·#46에서 HTTP 429 |
| 대체 경로 | Higgsfield CLI (`flux_2` / `gpt_image_2`) | 작업 #17·#21·#46에서 실사용 성공 |

이번 작업에서는 **아트를 생성하지 않았다.** 흰 네모 1장으로 레이아웃이 판정되고,
아트 부재가 기능을 막지 않기 때문이다 — 판독이 실제로 불만을 해소하는지 측정되기 전에
최종 아트를 굽는 것은 순서가 뒤바뀐 일이다(§3 선택 사항과 같은 이유).

---

## 5. 승격 규칙 (CLAUDE.md §3 재확인)

1. 생성물은 **먼저 `_workspace/current/design/concept/`** 에 떨어진다.
2. 감사 후에만 `Assets/`로 승격한다. 감사되지 않은 AI 산출물이 출시 빌드에 있는 것은
   라이선스·품질 부채다.
3. 승격되는 모든 파일에 `.provenance.json`을 붙인다(프롬프트·도구·모델·체크섬).
4. 전체 세트를 굽기 전에 **1장으로 스타일을 증명한다.**
