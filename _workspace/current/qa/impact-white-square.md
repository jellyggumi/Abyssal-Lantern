# 충돌 시 흰 네모 — 측정 기록과 정정

- run-id: 20260809-castle-war-stage1 (cycle 2)
- owner: QA lane
- date: 2026-08-14
- 발단: 사용자 보고 — *"충돌 시 왜 이미지가 흰 네모로 나오지?"*

## 0. 요약 — 무엇이 확정이고 무엇이 아닌가

| # | 주장 | 상태 |
|---|---|---|
| 1 | 충돌 프레임에 **순백(>230) 픽셀은 0개**다 | **[OBSERVED]** 두 캡처 모두 |
| 2 | 충격점에 **연회색 (209,209,207)** 덩어리가 있다 | **[OBSERVED]** |
| 3 | 그 회색의 원인은 `face_s0`를 파티클 텍스처로 넘긴 것이다 | **[반증됨]** — §3 |
| 4 | `DestructibleBlock`이 자기 스프라이트를 파티클 텍스처로 넘긴 것은 사실이고, 그 자체로 잘못이다 | **[OBSERVED]** 수정됨 |
| 5 | 수정 후 파티클은 `particle_ember`/`particle_smoke`를 쓴다 | **[OBSERVED]** 씬 열거 |
| 6 | **연회색의 원인은 순백 틴트로 그려지는 `fx_spark` 플래시다** | **[OBSERVED]** — A/B로 확정, §5 |
| 7 | 사용자가 본 "흰 네모"가 이 연회색과 같은 것이다 | **[INFERENCE]** — 충격점에서 유일한 무채색 요소, §5 |

---

## 1. 측정 방법

`Assets/Tests/PlayMode/ImpactVfxCaptureProbe.cs` — 실제 씬을 띄우고 적 성벽 블록을
카메라 중앙에 잡은 뒤, **타격 직전 프레임과 타격 프레임을 각각 PNG로 캡처**한다.
두 프레임을 픽셀 단위로 차분하므로 "충돌로 인해 새로 나타난 것"만 남는다.

캡처: `qa/evidence/impact-vfx/{before-impact,impact-frame-1,impact-frame-2,impact-frame-3}.png`

---

## 2. 최초 측정 결과

```
changed pixels (sampled every 2px): 21048
new pixels that are near-WHITE (>230 all): 0
new pixels that are near-BLACK (<60 all) : 1885
brightest new pixels: (690,408) (106,145,109) -> (210,209,207)
```

**순백은 없었다.** 충격점에 나타난 가장 밝은 것은 **연회색 (210,209,207)** 이었다.

당시 `DestructibleBlock:227`이 `spriteRenderer.sprite`를 `SpawnImpactBurst`에 넘기고
있었고, 그 스프라이트는 `CastleSkin/face_s0.png` — **512×512 벽돌 줄눈 오버레이**로
거의 전면이 흰색이다. **연회색 = 축소된 흰 오버레이**라고 판단했다.

---

## 3. 그 판단은 틀렸다

수정 후 동일 지표로 재측정했다. 수정 전 캡처를 보존해 두고 **같은 코드로 양쪽을 셌다**:

```
IDENTICAL metric, both captures, same 120x120 box at the impact:
  BEFORE fix: 7 pale-neutral pixels near the impact   (209,209,207) 포함
  AFTER  fix: 8 pale-neutral pixels near the impact   (209,209,207) 포함
```

**7 → 8.** 변화 없음(표본 오차 범위). `face_s0`를 파티클에서 제거해도 **충격점의
연회색은 그대로다** — 따라서 §2의 인과 판단은 반증됐다.

수정 자체는 유효하다(§4). 다만 **신고된 증상의 원인이 아니었다.**

---

## 4. 수정이 실제로 한 일

씬에 무엇이 그려지는지 **직접 열거**했다(`DumpRenderersNear`, 캡처와 같은 프레임):

```
[at-impact] ParticleSystem 'ImpactBurst' alive=6  tex=particle_ember  startColor=(0.80,0.50,0.20,a1.00)
[at-impact] ParticleSystem 'ImpactBurst' alive=11 tex=particle_smoke  startColor=(0.72,0.62,0.48,a0.85)
```

수정 전에는 이 텍스처가 `face_s0`였다. **파티클이 의도된 아트를 쓰게 된 것은 확정**이며,
흰 오버레이를 파티클로 쓰는 것은 신고와 무관하게 잘못이었다.

---

## 5. 진짜 원인 — A/B로 확정

같은 열거에서 **순백 틴트로 최상단(order 36)에 그려지는 것 2종**이 나왔다:

| 객체 | 스프라이트 | 틴트 | order | scale |
|---|---|---|---|---|
| `Fx_fx_spark` | `fx_spark_000` | **(1.00,1.00,1.00,a1.00)** | 36 | **0.46** |
| `HiggsfieldImpactAccent` | `Impact` | (1.00,1.00,1.00,a0.90) | 36 | 0.05 |

추정으로 끝내지 않고 **격리 실험**을 했다. 같은 높이의 이웃 블록을 동일하게 때리고,
플래시 렌더러 3개를 **그 프레임에 비활성화**한 뒤 같은 지표로 셌다:

```
flash ENABLED : 6 pale-neutral pixels   (209,209,207) 포함
flash DISABLED: 0 pale-neutral pixels
```

**6 → 0. 인과 확정.** 연회색은 순백 틴트 플래시다.

### 왜 흰색이 문제였나

아트는 **무채색으로 그려져 있고 코드가 틴트를 곱한다**. `Color.white`를 곱하면 아트가
그대로 나오므로 **결과가 무채색**이다. 충격점의 다른 모든 요소는 온색이다 —
버스트 (0.80,0.50,0.20), 피해 숫자 (1.00,0.85,0.25), Higgsfield 별섬광 주황.
**밝은 초원·하늘 위에서 유일한 무채색 밝은 덩어리**가 신고된 "흰 네모"다.

궤적 아크가 하늘 대비 1.13:1로 안 보였던 것과 **같은 종류의 결함**이다 — 형태가 아니라
**색이 배경과 구별되지 않는 것**.

### 수정

`DestructibleBlock:229`의 틴트를 `Color.white` → **`(1.00,0.78,0.36)`** 온색 앰버로 교체.
`FrameAnimScaleTests.TheImpactFlashTint_IsNotNeutral`이 **채도 0.35 이상**을 요구하므로
누가 다시 `Color.white`로 "단순화"하면 실패한다.

### 부수 수정 — 크기 튐도 코드로 고쳤다

`FrameAnimEffect.Initialize`가 `frames[0]`으로 스케일을 **한 번** 계산하고 `Update`는
스프라이트만 갈아 끼우고 있었다. 182px→256px 프레임에서 **1.41배 커진다**(테스트가 이 수를
고정한다). `ApplyScaleFor`를 프레임 교체마다 호출하도록 바꿔 **아트 재생성 없이** 해결했다 —
D-2는 더 이상 아트 요청이 아니다.

---

## 6. 확정된 별개 결함 3건 (이 조사에서 파생)

| # | 결함 | 상태 |
|---|---|---|
| D-1 | `fx_muzzle`·`fx_arcane`이 `textureType: 0`(Default)로 임포트돼 `Resources.LoadAll<Sprite>`가 **빈 배열** → **작업 #19의 대포 포구 화염이 계속 투명**이었다 | **수정됨** (임포터 메타 재작성, 로드 확인) |
| D-2 | 프레임 크기 불일치가 **6개 스트립** (`fx_sparkle` 77×77 vs 256×256 = 3.3배 최악) → 재생 중 크기 튐 | 기준선 고정, **아트 재생성 필요** |
| D-3 | `fx_frost` 파일 0장 → Stage3 서리 벤트 무연출 | 공백 고정, **아트 필요** |

D-1은 **D-2를 가리고 있었다** — 로드가 0장이면 크기 불일치도 보이지 않는다.

---

## 7. 아직 하지 않은 측정 — 정직한 공백

**프로브는 `TakeDamage`를 직접 호출한다.** 실제 발사체가 날아와 `OnCollisionEnter2D`를
타는 경로는 캡처하지 않았다. 사용자 표현이 *"충돌 시"* 이므로 **충돌 경로 고유의 연출이
있으면 이 조사는 그것을 보지 못했다.**

다음 측정은 이것이어야 한다 — 합성 피해가 아니라 **실제 발사·명중을 캡처**하고,
`fx_spark`를 일시 비활성화한 대조군과 비교해 연회색의 인과를 격리한다.

## 8. 주장하지 않는 것

- 신고가 해결됐다고 말하지 않는다. 원인 미확정이다.
- `fx_spark`가 원인이라고 단정하지 않는다 — 후보 1위일 뿐이다.
- 사용자가 본 것이 이 연회색이라고 단정하지 않는다. "네모"는 별섬광의 형태가 아니다.
