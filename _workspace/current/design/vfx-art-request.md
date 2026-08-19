# VFX 아트 발주서 — 필요한 이미지 명세

- run-id: 20260809-castle-war-stage1 (cycle 2 계속)
- date: 2026-08-14
- owner: design lane (ArtRequestSpec)
- 운영 모드: Stage 3 — 운영 안정성과 플레이 임팩트
- 근거 인테이크: `_workspace/current/intake/production-brief-defects.md`
- 사용자 지시: *"vfx 가 없으면 필요로하는 이미지로 남겨줘"*

---

## 0. 결론 먼저 — 발주할 것은 1건이다

사용자 지시는 "없으면 발주하라"였다. 전수 조사 결과 **이 저장소의 결함은 "없는 것"이 아니다.**

`Assets/Resources` 아래 PNG **238장**을 전수 조사했다 `[OBSERVED]`. 사용자가 신고한 폭발을
포함해 거의 모든 이펙트 아트가 **이미 디스크에 있고, 채도도 살아 있다.** 닿지 않을 뿐이다.

| 판정 | 건수 | 내용 |
|---|---:|---|
| **신규 아트 발주 필요** | **1** | `fx_frost` — 진짜로 파일이 0장인 유일한 항목 |
| 재생성 필요(기존 자산 수정) | 2 | `fx_spark_000` 치수, `CollapseDust` 채도 |
| **발주 아님 — 임포터 3필드** | 9 | 폭발 6장(수정 완료), 날씨 3장(미수정) |
| **발주 아님 — 코드** | 2 | `fx_shatter`·`CollapseDust` 순백 틴트 call site |
| 오탐(정상) | 3 | `Backgrounds/` — Texture2D로 로드되므로 Default가 맞음 |

> **폭발은 발주 대상이 아니다.** 아트가 이미 있고 채도 0.309~0.953으로 살아 있다.
> 발주하면 중복 지불이다. §2에 실측을 남긴다.

---

## 1. 전수 조사 — 디스크에 무엇이 있는가

### 1-1. `Assets/Resources/Effects/` 전수 (실측) `[OBSERVED]`

채도·명도는 **알파 8 초과 픽셀만** 대상으로 HSV 변환해 평균했다. 중립%는 `S < 0.12` 비율.

| 키 | 프레임 | 치수 | 채도(평균) | 명도 | 중립% | 임포터 |
|---|---:|---|---:|---:|---:|---|
| `fx_arcane` | 4 | 512×457 ~ 413×512 **(불일치)** | 0.413~0.601 | 0.77~0.96 | 0~31% | 8 ✓ |
| `fx_dust` | 4 | 190×190 + 256×256 ×3 **(불일치)** | 0.279~0.331 | 0.42~0.52 | 2~29% | 8 ✓ |
| `fx_eruption` | 5 | 545×639 ~ 180×640 **(불일치)** | 0.151~0.955 | 0.38~0.85 | 0~74% | 8 ✓ |
| `fx_muzzle` | 6 | 256×256 (전부 동일) ✓ | 0.604~0.614 | 0.75 | 4~6% | 8 ✓ |
| `fx_petals` | 4 | 368×640 ~ 235×640 **(불일치)** | 0.177~0.430 | 0.76~0.83 | 1~40% | 8 ✓ |
| `fx_shatter` | 6 | 256×256 (전부 동일) ✓ | **0.072~0.226** | 0.40~0.44 | **34~91%** | 8 ✓ |
| `fx_spark` | 4 | **182×182** + 256×256 ×3 **(불일치)** | 0.512~0.681 | 0.97~0.99 | 9~33% | 8 ✓ |
| `fx_sparkle` | 4 | **77×77** + 256×256 ×3 **(불일치)** | 0.429~0.544 | 0.89~0.94 | 16~28% | 8 ✓ |
| `fx_spawn` | 4 | 256×256 (전부 동일) ✓ | 0.246~0.458 | 0.54~0.66 | 1~46% | 8 ✓ |
| `fx_frost` | **0** | — | — | — | — | **파일 없음** |

`particles/` 단일 스프라이트 6장:

| 파일 | 치수 | 채도 | 명도 | 임포터 | 상태 |
|---|---|---:|---:|---|---|
| `particle_ember` | 62×128 | 0.851 | 0.80 | 8 ✓ | 정상 |
| `particle_petal` | 89×128 | 0.319 | 0.81 | 8 ✓ | 정상 |
| `particle_smoke` | 120×127 | 0.181 | 0.64 | 8 ✓ | 정상 |
| `particle_ash` | 57×64 | 0.568 | 0.74 | **0 ✗** | **로드 불가** |
| `particle_rain` | 39×64 | 0.209 | 0.99 | **0 ✗** | **로드 불가** |
| `particle_snow` | 58×64 | 0.123 | 0.99 | **0 ✗** | **로드 불가** |

### 1-2. `Assets/Resources/Higgsfield/VFX/` 전수 `[OBSERVED]`

| 파일 | 치수 | **채도** | 명도 | 중립% | 받는 틴트 | 결과 |
|---|---|---:|---:|---:|---|---|
| `Impact.png` | 512×512 | **0.573** | 0.92 | 12.7% | `(1,1,1,0.90)` `GameFeelVfx.cs:244` | 색 **생존** |
| `CoreCrack.png` | 512×512 | **0.439** | 0.96 | 21.7% | `(1,1,1,0.94)` `GameFeelVfx.cs:824` | 색 **생존** |
| `Wind.png` | 512×512 | **0.587** | 0.83 | 0.0% | `(0.9,1,1,0.82)` `WindVfxManager.cs:99` | 색 **생존** |
| **`CollapseDust.png`** | 512×512 | **0.050** | 0.67 | **99.4%** | `(1,1,1,0.88)` `GameFeelVfx.cs:409` | **흰 연기** |

> **인테이크 수치 보정** `[OBSERVED]`. 인테이크는 `CollapseDust` 중립 픽셀을 **83%**로 적었고
> 나는 **99.4%**를 얻었다. 임계값 차이다 — 나는 `S < 0.12`를 중립으로 셌다. 채도 평균
> **0.050**은 인테이크와 정확히 일치하므로 **결론은 동일하고 오히려 더 강해진다.**
> `Impact.png`도 인테이크 0.574 대 내 측정 0.573으로 일치한다.

### 1-3. `Assets/Sprites/` 폭발 관련 `[OBSERVED]`

| 파일 | 치수 | 채도 | 상태 |
|---|---|---:|---|
| `explosion.png` | **1254×1254** | 0.567 | **완전 고아.** `Resources` 밖이고 코드 참조 **0건** — 아래 참조 |

> **`Assets/Sprites/explosion.png`는 이제 완전한 고아다** `[OBSERVED]`. 인테이크 시점에는
> `ExplosiveGimmick`이 `#if UNITY_EDITOR`+`AssetDatabase`로 이 파일을 읽었으나, 수정 과정에서
> 그 경로가 `ExplosionFrames.Load()`로 교체되면서 **코드 참조가 0건이 됐다.** 파일은 디스크에
> 남아 있다. 1254×1254는 이 저장소 최대 이펙트 자산이고 채도 0.567로 품질도 나쁘지 않으나,
> `GeneratedExplosionFrames` 6프레임이 그 역할을 대체했다.
>
> **발주 판단: 삭제 후보이지 재생성 대상이 아니다.** 다만 삭제는 이 문서의 권한 밖이므로
> 등록만 한다 — 남겨두면 다음 조사자가 "폭발 아트가 두 벌"로 오독할 위험이 있다.

---

## 2. 폭발 — 발주하지 않는다, 근거

**나는 처음에 `Effects`·`Higgsfield`·`Sprites` 세 곳만 봤고 그것이 내 조사의 결함이었다.**
`Assets/Resources` 전체를 다시 돌려 `GeneratedExplosionFrames/`를 찾았다 `[OBSERVED]`.

| 프레임 | 치수 | **채도** | 명도 | 근백색% |
|---|---|---:|---:|---:|
| `explosion_000` | 512×512 | 0.309 | 0.42 | 0.61% |
| `explosion_001` | 512×512 | **0.953** | 0.93 | 0.00% |
| `explosion_002` | 512×512 | 0.780 | 0.66 | 0.00% |
| `explosion_003` | 512×512 | 0.514 | 0.40 | 0.00% |
| `explosion_004` | 512×512 | 0.443 | 0.28 | 0.00% |
| `explosion_005` | 512×512 | 0.324 | 0.33 | 0.00% |

**6프레임 512×512, 채도 0.309~0.953, 근백색 픽셀 0.61% 이하.** 그레이스케일이 아니고
흰색도 아니다. **폭발 아트는 이미 완성품이다.**

`ExplosionEffectConfigurator`가 `Resources.LoadAll<Sprite>("GeneratedExplosionFrames")`로
정확히 로드하고 텍스처 시트에 넣는다. **런타임 경로도 이미 옳았다.**

깨진 것은 **임포터 3필드**였다 `[OBSERVED — .meta 직접 확인]`:

```
GeneratedExplosionFrames/explosion_000..005.png.meta (내 최초 측정 시점)
  textureType: 0        (Default)   ← 정상 자산은 8 (Sprite)
  spriteMode: 0                     ← 정상 자산은 1
  alphaIsTransparency: 0            ← 정상 자산은 1
대조군 Effects/fx_shatter/fx_shatter_000.png.meta → 8 / 1 / 1
```

그 결과 `LoadAll<Sprite>`가 **빈 배열** → 텍스처 시트 미설정 → `GetParticleMaterial(null)`
→ `GameFeelVfx.cs:451-477`의 **`Color(1f,1f,1f,alpha)` 순백 원**.

> **현재 상태 — 수정 완료 `[OBSERVED — 수정 후 재측정]`.** 내가 이 문서를 쓰는 동안
> Main이 두 가지를 고쳤고 재측정으로 확인했다:
> 1. 메타 6장 전량 `textureType: 8`. `Resources` 전체 재스캔에서
>    `GeneratedExplosionFrames`는 더 이상 목록에 없다 (비-Sprite 16장 → **10장**).
> 2. `ExplosionFrames.cs` 신설 — `Load()`가 이름 정렬로 단일 런타임 경로를 제공하고
>    `ExplosionEffectConfigurator:25`가 그것을 쓴다.
>
> **그리고 폴백 색도 고쳐졌다.** `ExplosionEffectConfigurator:64-66`의 프레임 부재 분기가
> 이제 순백이 아니라 앰버 그라데이션
> `(1, 0.72, 0.20, 0.95) → (1, 0.28, 0.05, 0.55)`이다. 성공 분기의
> `main.startColor = Color.white`(`:50`)는 **남아 있으나 옳다** — 아트가 색을 들고 있고
> 흰색은 곱셈 항등원이므로 색을 지우지 않는다(§4-3의 순백 틴트 규칙은 **저채도 아트**에
> 걸리는 것이고, 채도 0.309~0.953 아트에는 해당하지 않는다).

**따라서 폭발은 발주 항목이 아니다.** 아트 0장으로 종결됐다.

> **인테이크 진단 1건 반증** `[OBSERVED]`. 인테이크는 프리팹이
> `#if UNITY_EDITOR AssetDatabase`로만 읽히므로 *"빌드에서 항상 null"*이라 적었다.
> QA가 `ExplosiveBarrel.prefab:176`이 `explosionEffectPrefab`을 guid로 **직렬화**하는 것을
> 찾았고, 직렬화 참조는 `Resources` 밖이어도 빌드 의존성이다. **프리팹은 빌드에서도
> 로드된다.** 그러므로 흰색은 에디터에서도 재현됐고, 그것이 사용자의 *"아직도 흰색"*과
> 정합한다. 원인은 배선이 아니라 임포터였다.

---

## 3. 선언된 키 ↔ 디스크 자산 대조표

### 3-1. 고아 키 (선언됐으나 디스크에 없음)

| 키 | 선언 위치 | 디스크 | 소비자 | 판정 |
|---|---|---|---|---|
| `fx_frost` | `FrameAnimEffect.cs:19` | **0장** | `[확인 불가]` — 상수 참조 0건 | **유일한 진짜 아트 공백** → §5 A1 |

`fx_frost`는 `ImpactParticleArtTests.cs:167`에 `knownGaps`로 **핀 고정**되어 있다. 아트가
도착하면 그 테스트가 실패하고, 그 실패가 공백이 닫혔다는 기록이 된다 `[OBSERVED]`.

### 3-2. 고아 자산 (디스크에 있으나 선언 없음)

| 자산 | 프레임 | 상수 선언 | 실제 사용 | 판정 |
|---|---:|---|---|---|
| `fx_shatter` | 6 | **없음** | **있음** — `DynamicBattlefield.cs:381`, `:545`, `GamePlayTests.cs:2403` | **이전 판정 반증** |

> **이전 사이클 판정을 반박한다** `[OBSERVED]`.
> `design/impact-vfx-and-projectile-art-request.md:20`은 `fx_shatter`를
> *"코드에서 참조되지 않음 — **확정 결함**"*, `:45`는 *"아트만 있고 아무도 안 쓴다"*,
> `:207`은 *"1순위: fx_shatter 연결 확인"*으로 적었다. **틀렸다.**
> `DynamicBattlefield.cs:381`과 `:545`가 실제로 스폰한다. 다만 **상수가 없어 매직 스트링**이라
> `EffectSpriteLibrary` 기준으로 훑으면 안 보인다. 그것이 "안 쓴다"로 오독된 경위다.
>
> **그리고 이 반증이 새 결함을 연다.** 두 call site 모두 틴트가 `Color.white`이고
> `fx_shatter`의 채도는 0.072~0.226·중립 최대 90.9%다. **인테이크가 잡지 못한 세 번째
> 순백 지점이다** — `CollapseDust`와 같은 종류다.

### 3-3. 디스크에 있고 선언도 있으나 **로드되지 않는 것** — 이 저장소의 실제 결함 종류

`Assets/Resources` PNG 238장 중 `textureType != 8`은 **최초 측정 16장 → 현재 10장** `[OBSERVED]`.

| 그룹 | 장수 | 로드 호출 | 판정 |
|---|---:|---|---|
| `Backgrounds/Background_Stage1..3` | 3 | `Resources.Load<Texture2D>` `GameManager.cs:581` | **정상.** Texture2D로 읽고 `Sprite.Create`하므로 Default가 맞다 — **오탐으로 폐기** |
| `Effects/particles/` ash·rain·snow | 3 | `Resources.Load<Sprite>` `FrameAnimEffect.cs:68` | **결함.** 아래 참조 |
| `Gimmicks/` 4장 | 4 | `Resources.Load<Sprite>` `GimmickSpriteLibrary.cs:52` | **잠복 결함.** 소비자 0건 |
| `GeneratedExplosionFrames/` | ~~6~~ | `Resources.LoadAll<Sprite>` | **수정 완료** |

**날씨 파티클 3장이 가장 조용한 결함이다** `[OBSERVED]`:

- `particle_ash`·`particle_rain`·`particle_snow`가 `textureType: 0` → `Load<Sprite>`가 null
- 이 셋이 `StageWeather.cs:40-42`가 쓰는 **전부**다 (Stage1 비 / Stage2 눈 / Stage3 재)
- `EffectSpriteLibrary.LoadParticleSprite`(`FrameAnimEffect.cs:64-71`)에는
  `GimmickSpriteLibrary.cs:55-70`과 달리 **에디터 복구 블록이 없다**
- → **에디터·빌드 양쪽에서 죽는다. 세 스테이지 전부 대기 날씨가 한 번도 렌더된 적이 없다.**
- 다만 **흰색은 아니다**: `StageWeather.cs:45-49`가 null이면 `system.Stop()` 후 return한다.
  조용히 없다. `[OBSERVED]`
- **아트는 멀쩡하다** (채도 ash 0.568 / rain 0.209 / snow 0.123, 파일 존재) → **발주 아님, 임포터**

`Gimmicks` 4장(`gimmick_muzzle_flash`, `gimmick_shell`, `gimmick_wall_brick`,
`gimmick_wall_brick_cracked`)은 `GimmickSpriteLibrary.Load`의 `#if UNITY_EDITOR` 블록이
임포터를 고쳐 재임포트하므로 **에디터에서만 복구된다.** 다만 `Shell`/`MuzzleFlash`/`WallBrick`
상수 참조가 **0건**이라 현재 소비자가 없다 → 우선순위 낮음 `[OBSERVED]`.

### 3-4. 재발 이력 — "있는데 닿지 않는 것"은 이번이 **네 번째 사례**다

| # | 자산 | 증상 | 출처 |
|---|---|---|---|
| 1 | `fx_muzzle` | `textureType: Default` → 빈 배열 → 대포 포구 무연출 | `ImpactParticleArtTests.cs:142-145` |
| 2 | `fx_arcane` | 동일 | `ImpactParticleArtTests.cs:142-145`, `:98-100` |
| 3 | **`GeneratedExplosionFrames`** | 동일 → 순백 폴백 | 이 문서 §2 |
| 4 | **`particle_ash`/`rain`/`snow`** | 동일 → 날씨 전면 무연출 | 이 문서 §3-3 |

작업 #17이 그 앞 사례로 기록돼 있다 `[OBSERVED — ImpactParticleArtTests.cs:145]`.

**세 번·네 번 살아남은 이유** `[OBSERVED]`: 회귀 테스트
`ImpactParticleArtTests.OnlyTheKnownArtGapIsMissingItsFrames`(`:151-168`)가
**`EffectSpriteLibrary`에 선언된 9개 키만** 순회한다. `GeneratedExplosionFrames`도
`particles/`도 그 목록에 없다. **선언 기준으로 도는 테스트는 선언되지 않은 자산을 못 본다.**

---

## 4. 채도 정책 — "그레이스케일로 그리고 틴트로 색을 넘긴다"는 이 저장소에서 실패했다

### 4-1. 실패 이력은 문서화된 지시에서 나왔다

이전 발주서 `design/impact-vfx-and-projectile-art-request.md:101`이 생산자에게 이렇게 지시했다:

> | **색** | **무채색/흰색 실루엣.** 코드가 `SpriteRenderer.color`에 팀색·피해색을 **곱한다** —
> 색이 든 아트를 넣으면 곱셈으로 탁해진다 |

**그 지시가 오늘의 결함을 만들었다** `[INFERENCE — 인과는 추론, 지시문과 결과 자산은 모두 OBSERVED]`.
무채색으로 그린 자산이 **순백 틴트를 받는 call site**에 걸리면 곱셈 결과가 흰색이다.
`1.0 × 0.95 = 0.95`. 색을 넘길 틴트가 없으면 넘어갈 색도 없다.

### 4-2. 측정이 경계를 정한다

**순백 계열 틴트를 받는 자산만** 놓고 보면 경계가 선명하다 `[OBSERVED]`:

| 자산 | 채도 | 중립% | 틴트 | 결과 |
|---|---:|---:|---|---|
| `Wind.png` | 0.587 | 0.0% | `(0.9,1,1,0.82)` | **통과** |
| `Impact.png` | 0.573 | 12.7% | `(1,1,1,0.90)` | **통과** |
| `CoreCrack.png` | **0.439** | **21.7%** | `(1,1,1,0.94)` | **통과(최저 통과선)** |
| — 경계 — | | | | |
| `fx_shatter_004` | **0.226** | 34.7% | `Color.white` | **실패(최고 실패선)** |
| `fx_shatter_000` | 0.073 | 90.9% | `Color.white` | 실패 |
| `CollapseDust.png` | 0.050 | 99.4% | `(1,1,1,0.88)` | 실패 |

최고 실패 **0.226** / 최저 통과 **0.439**. 중간값 0.33.

### 4-3. 규칙 — call site 종류에 따라 요구가 **반대**다

이것이 이전 발주서가 놓친 구분이다. 틴트에는 두 종류가 있고 요구가 정반대다 `[OBSERVED]`:

| call site 종류 | 예 | 아트 요구 | 이유 |
|---|---|---|---|
| **순백 계열 틴트** (`1,1,1,a`) | `Impact` `:244`, `CollapseDust` `:410`, `CoreCrack` `:824`, `fx_shatter` `:381`/`:545` | **채도 ≥ 0.35**, 중립 픽셀 ≤ 25% | 틴트가 색을 **안 준다.** 아트가 색을 들고 있어야 한다 |
| **유채색 틴트** (팀색·피해색) | `fx_spawn` `(1,0.96,0.88)`, `fx_dust`, 룬 `(0.6,0.9,1)` | 채도 0.15~0.45, 명도 중간대(0.4~0.7) | 틴트가 색을 **준다.** 아트가 색을 들면 곱셈으로 탁해진다 |

이전 발주서 `:101`은 **두 번째 규칙을 첫 번째에도 적용하라고 지시했다.** 그것이 오류다.
`fx_dust`(채도 0.279, 유채색 틴트)는 정상이고 `CollapseDust`(0.050, 순백 틴트)는 흰색인
이유가 정확히 이 구분이다.

**발주 시 반드시 명시할 것: 이 자산이 어느 종류의 틴트를 받는가.**

---

## 5. 밝기·배경 대비 — 흰색은 "안 보여서" 문제가 아니다

### 5-1. 필드 색 실측

`GameManager.GenerateGroundTexture`(`:1642-1644`) 인용 `[OBSERVED]`:

```csharp
Color32 grassColor = new Color32(76, 154, 42, 255);   // relLum 0.2481
Color32 dirtColor  = new Color32(139, 90, 43, 255);   // relLum 0.1298
Color32 stoneColor = new Color32(90, 90, 90, 255);    // relLum 0.1022
```

배경 3장 평균색 실측 `[OBSERVED]`:

| 배경 | 평균 RGB | relLum |
|---|---|---:|
| Stage1 | (94,144,148) | 0.2446 |
| **Stage2** | **(127,149,134)** | **0.2773** ← 가장 밝다 = 최악 조건 |
| Stage3 | (92,35,55) | 0.0375 |

### 5-2. 대비비 (WCAG 상대휘도 공식)

| 후보 색 | grass | dirt | stone | Stage1 | **Stage2** | Stage3 |
|---|---:|---:|---:|---:|---:|---:|
| **순백 (현 폴백)** | 3.52 | 5.84 | 6.90 | 3.56 | **3.21** | 12.00 |
| CollapseDust 틴트 후 | 2.82 | 4.67 | 5.52 | 2.85 | **2.57** | 9.60 |
| 앰버 (1.0,0.78,0.36) | 2.28 | 3.78 | 4.46 | 2.30 | **2.07** | 7.75 |
| 서리 (0.55,0.85,1.0) | 2.26 | 3.75 | 4.43 | 2.29 | **2.06** | 7.70 |
| 엠버 (1.0,0.50,0.10) | 1.40 | 2.32 | 2.74 | 1.42 | **1.28** | 4.77 |

### 5-3. 이 표가 뒤집는 것

> **순백이 모든 배경에서 대비가 가장 높다 (3.21~12.00).** `[OBSERVED]`
> 그러므로 **"흰색이라 안 보인다"는 주장은 데이터가 반박한다.** 흰색은 가장 잘 보인다.

사용자도 "안 보인다"고 하지 않았다 — *"흰색으로 표시되는데 이부분 문제가 어떤건지"*라고 했다.
**보이는데 의미가 없는 것**이 신고 내용이다. 순백 원은 "폭발"로 읽히지 않고
**"텍스처 누락 플레이스홀더"로 읽힌다** `[INFERENCE — 사용자 표현에서 추론]`.

따라서 발주서의 밝기 요구는 *"배경보다 밝게"*가 아니다. 그것은 이미 만족돼 있다. 요구는:

**(a) 순백과 구별될 것** — 채도 하한 §4-2로 강제.
**(b) 최악 배경(Stage2, relLum 0.2773)에서 대비 ≥ 2.0을 유지할 것.**
표에서 엠버 오렌지(1.28)는 이 선을 **통과 못 한다** — 폭발 폴백의 주역 색이 엠버라는 점이
`ExplosiveGimmick.cs:339`에 있으므로, **단색 엠버만으로 채운 이펙트는 잔디 위에서 약하다.**
밝은 코어(고명도)와 채도 있는 외곽을 함께 가져야 한다.

---

## 6. 발주 항목

### 공통 계약 조항 (모든 항목에 적용)

**Main 질의에 대한 답: 임포터 설정은 발주 계약에 들어가야 한다. 근거는 재발 4회다.**

이 저장소에서 **PNG만 납품된 자산은 납품되지 않은 것과 같다.** §3-4가 보여주듯 같은 실패가
4번 반복됐고, 그때마다 아트는 디스크에 멀쩡히 있었다. 자산의 정의는 **PNG + `.meta`**다.

```yaml
# 모든 납품 자산의 .meta 가 만족해야 하는 값 (Effects/fx_shatter 기준 — 작동 확인된 대조군)
textureType: 8           # Sprite. 0(Default)이면 Resources.Load<Sprite>가 null
spriteMode: 1            # Single
alphaIsTransparency: 1
spritePixelsToUnits: 100 # 프로젝트 전역 PPU
spritePivot: {x: 0.5, y: 0.5}
filterMode: 1            # Bilinear
```

- **인수 조건**: `Resources.LoadAll<Sprite>("Effects/{키}")`가 **프레임 수와 같은 길이**의
  배열을 돌려줄 것. 0이면 미납이다.
- **프레임 치수는 전 프레임 동일**할 것. §1-1에서 9개 스트립 중 **6개가 불일치**다
  (`fx_sparkle` 77×77 대 256×256 = 3.3배). `FrameAnimEffect.cs:138-142`가 보정하지만
  보정은 결함의 은폐지 해결이 아니다.
- **provenance JSON 동반**. 스키마는 `Effects/fx_muzzle/fx_muzzle.provenance.json` 참조
  (`asset`/`derived_from`/`method`/`sha256`/`generated`/`audited`).
- **배경은 완전 알파 투명.** 불투명 배경이 곧 "흰 네모"로 보이는 경로다.

---

### A1 · `fx_frost` — 유일한 진짜 신규 발주 `[최우선 신규 아트]`

| 항목 | 값 |
|---|---|
| **키** | `fx_frost` — `EffectSpriteLibrary.Frost`, `FrameAnimEffect.cs:19`에 **이미 선언됨** |
| **경로** | `Assets/Resources/Effects/fx_frost/` |
| **파일명** | `fx_frost_000.png` … `fx_frost_003.png` (4장) |
| **치수** | **256×256, 4프레임 전부 동일** (`fx_muzzle`·`fx_shatter`가 지키는 유일한 규격) |
| **피벗** | `{0.5, 0.5}` / PPU 100 → 월드 2.56u 기준, 호출부가 `worldSize`로 재조정 |
| **틴트 종류** | **유채색 틴트** — `DynamicBattlefield`가 벤트에 청색 계열을 곱한다 |
| **채도 요구** | **0.15 ~ 0.45** (유채색 틴트 규칙 §4-3). `fx_dust` 0.279가 이 대역의 실증 사례 |
| **명도 요구** | 평균 **0.45~0.70**. 순백 금지 — 0.90 초과 픽셀 5% 이하 |
| **대비 요구** | Stage3 배경 relLum 0.0375 위에 얹히므로 대비는 여유롭다. 다만 Stage2(0.2773)에서도 ≥2.0 |
| **소비자** | Stage3 서리 벤트. 현재 `LoadFrames`가 null → `FrameAnimEffect.Spawn`이 `:101`에서 soft-fail → **무연출** |
| **생산 경로** | **Codex CLI(god-tibo-imagen) 우선.** `fx_spawn`·`fx_shatter`가 이 경로 산출물이고 채도·알파가 규격에 맞다. Higgsfield는 512² 단일 스프라이트에 강하나 프레임 정합이 약하다(§1-1에서 Higgsfield 계열은 전부 단일 장) |

**프롬프트 초안**:
```
2D game VFX sprite sheet frame, single effect on a fully transparent background.
A burst of pale ice crystals and frost shards expanding radially from the centre,
with a cold cyan-teal core and lighter rime at the edges. Crisp shard silhouettes,
no motion blur, no ground, no character, no text, no border, no drop shadow.
Centred composition, uniform 256x256 canvas, the effect occupying ~70% of the frame.
Muted saturation so a colour tint can multiply over it — avoid pure white pixels.
Frame {N} of 4: {000 = tight dense core / 001 = mid expansion, brightest /
002 = wide and thinning / 003 = sparse dissipating shards}.
```

---

### A2 · `CollapseDust` 재생성 — 순백 결함의 직접 원인 `[최우선 결함 해소]`

| 항목 | 값 |
|---|---|
| **키** | `HiggsfieldSpriteLibrary.CollapseDust` = `"CollapseDust"` (`GimmickSpriteLibrary.cs` 내 선언) |
| **경로** | `Assets/Resources/Higgsfield/VFX/CollapseDust.png` **(덮어쓰기)** |
| **치수** | **512×512** — 같은 폴더 3장이 전부 512×512이므로 유지 |
| **피벗** | `{0.5, 0.5}` / PPU 100 |
| **틴트 종류** | **순백 틴트** `(1,1,1,0.88)` `GameFeelVfx.cs:409` |
| **채도 요구** | **평균 ≥ 0.35**, 목표 0.45. 현재 **0.050** |
| **중립 픽셀** | **≤ 25%** (`S<0.12` 기준). 현재 **99.4%**. 통과 실증: `CoreCrack` 21.7% |
| **명도 요구** | 평균 0.50~0.75 (현재 0.673은 유지 가능). 근백색(S<0.10 & V>0.90) **≤ 5%** |
| **대비 요구** | Stage2(relLum 0.2773) 기준 **≥ 2.0** |
| **생산 경로** | **Higgsfield.** 같은 폴더 3장이 전부 Higgsfield 산출이고 512² 단일 스프라이트가 그 파이프라인의 강점이다(`design/ai/higgsfield/higgsfield-combat-vfx-sheet.png` 존재) |

**프롬프트 초안**:
```
2D game VFX sprite, single effect on a fully transparent background.
A billowing cloud of pulverised stone and masonry dust from a collapsing wall.
Warm earthy palette — ochre, tan, and dusty brown with darker umber in the folds
and a lighter sunlit rim on the upper left. Visible colour separation between
the lit and shadowed masses; this must NOT read as a grey or white cloud.
No fire, no sparks, no character, no ground line, no text, no border.
Centred, 512x512, effect occupying ~75% of the frame, soft alpha falloff at the edges.
```

> **대안(아트 0장)**: `GameFeelVfx.cs:409`의 틴트를 순백에서 흙빛
> (예: `(0.78, 0.66, 0.52, 0.88)`)으로 바꾸면 현 자산으로도 흰색은 사라진다.
> **채도 0.050 자산은 어떤 틴트를 줘도 그 틴트의 단색이 된다** — 형태만 남고 색 정보는
> 아트가 아니라 코드가 준다. 그것을 수용할지가 판정 사항이다. `[INFERENCE]`
> **비용 비교: 코드 1줄 대 512² 재생성 1장.** 순서상 코드 수정을 먼저 시도할 것을 권한다.

---

### A3 · `fx_shatter` — 발주 아님, 그러나 §4-3 위반 `[코드 판정 필요]`

| 항목 | 값 |
|---|---|
| **현 상태** | 6프레임 256×256 **전부 동일 치수** — 규격상 모범 자산 |
| **채도** | 0.072~0.226, 중립 34.7~90.9% |
| **틴트** | `Color.white` — `DynamicBattlefield.cs:381`, `:545` |
| **판정** | **순백 틴트 + 채도 0.073 = §4-3 위반.** `CollapseDust`와 동일 구조 |

**아트 재생성은 권하지 않는다.** 이 자산은 "부서진 돌 파편"이고 회색이 **의미상 옳다.**
문제는 자산이 아니라 **거기에 순백을 곱하는 call site**다. A2와 같은 판단:
`DynamicBattlefield.cs:381`/`:545`의 `Color.white`를 석재 톤
(예: `(0.75, 0.72, 0.66, 1f)` — `:546`이 이미 그 색을 `SpawnImpactBurst`에 쓰고 있다)으로
바꾸는 것이 최소 수정이다 `[OBSERVED — :546에 해당 색 존재]`.

**부수 발주 1건**: `fx_shatter`에 상수 선언이 없어 매직 스트링으로 쓰인다(§3-2).
`EffectSpriteLibrary`에 `public const string Shatter = "fx_shatter";`를 추가하면
§3-4의 회귀 테스트가 이 키를 순회 대상에 포함하게 된다 — **재발 방지의 최소 조치**다.

---

### A4 · `fx_spark_000` 재생성 — 치수 불일치 `[낮음]`

| 항목 | 값 |
|---|---|
| **키** | `EffectSpriteLibrary.Spark` = `fx_spark` |
| **파일** | `Assets/Resources/Effects/fx_spark/fx_spark_000.png` **1장만** |
| **현재** | **182×182**, 나머지 3장은 256×256 |
| **요구** | **256×256**으로 재생성. 나머지 3장과 채도 정합(0.512~0.681 대역), 현재 0.590 유지 |
| **근거** | `FrameAnimScaleTests.cs:28`이 이 값을 실측으로 고정하고 있다 `[OBSERVED]` |
| **생산 경로** | 기존 182² 원본을 **업스케일하지 말 것.** 동일 프롬프트로 256² 재생성 |

동일 결함이 `fx_sparkle`(77×77 → 3.3배 점프)·`fx_arcane`·`fx_dust`·`fx_eruption`·`fx_petals`에
있다. `ImpactParticleArtTests.cs:97-101`이 `knownOffenders`로 고정하고 있으므로 **일괄
재생성은 그 테스트 갱신과 함께** 진행해야 한다. 이번 결함과 직접 관련은 없다.

---

## 7. 우선순위 — "폴백이 발동해도 순백이 아니게 만드는 최소 자산"

### 7-1. 답: **최소 자산은 0장이다. 이미 디스크에 있다.**

지시받은 질문에 정직하게 답한다. 순백의 발생지는 `GameFeelVfx.GetDefaultParticleTexture()`
(`:451-477`)이고, 이 함수는 **어떤 자산도 로드하지 않는다** — 32×32를 `Color(1f,1f,1f,alpha)`로
**절차적으로 굽는다** `[OBSERVED]`. 따라서 자산을 추가해도 이 함수는 그것을 쓰지 않는다.

**최소 조치는 아트가 아니라 이 함수에 닿는 경로를 막는 것이다.** 방법이 둘이고
**Main이 이미 그중 하나를 폭발 경로에 적용했다.**

**방법 A — 틴트로 색을 준다 (채택됨).** 기본 텍스처는 무채색 방사 falloff이므로
**틴트만이 색을 실을 수 있다.** `ExplosionEffectConfigurator:64-66`이 프레임 부재 시
앰버 그라데이션을 주도록 고쳐졌다 `[OBSERVED]`. 아트가 닿지 않아도 "불"로 읽힌다.
**이것이 이 질문의 정답이고 비용은 0장이다.**

**방법 B — 기본 텍스처 자체를 기존 자산으로 교체.** 후보는 이미 있고 `textureType: 8`이다:

| 후보 | 채도 | 명도 | 적합성 |
|---|---:|---:|---|
| `particle_smoke` (120×127) | 0.181 | 0.635 | **권장.** 중간 명도·저채도라 어떤 틴트도 받아낸다 |
| `particle_ember` (62×128) | 0.851 | 0.797 | 비권장 — 채도가 높아 범용 기본값이 되면 전부 주황이 된다 |

**남은 순백 노출 지점** — 무인자 `GetParticleMaterial()` 호출부 전수 `[OBSERVED]`:

| 호출부 | 틴트가 색을 주는가 | 판정 |
|---|---|---|
| `ExplosionEffectConfigurator.cs:69` | **예** — `:64-66` 앰버 | **해소됨** |
| `ExplosiveGimmick.cs:364` | 예 — `main.startColor`가 황→적 그라데이션 | 낮음 |
| `EruptionVentGimmick.cs:321` | `[확인 불가]` — 벤트 스타일별 틴트 확인 필요 | **점검 필요** |
| `WindVfxManager.cs:63` | `[확인 불가]` — 바람 연출 틴트 확인 필요 | **점검 필요** |

즉 방법 A를 남은 두 곳에도 적용하면 아트 0장으로 순백이 사라진다.

### 7-2. 순위표

| 순위 | 항목 | 종류 | 새 아트 | 상태 | 근거 |
|---:|---|---|---|---|---|
| ~~1~~ | 폭발 임포터 6장 + `ExplosionFrames` | 메타·코드 | **0장** | **완료** | 사용자 신고 항목 |
| ~~2~~ | 폭발 폴백 앰버 틴트 | 코드 | **0장** | **완료** | 순백 종점 차단(방법 A) |
| **3** | `CollapseDust` 순백 틴트 (`GameFeelVfx.cs:409`) | 코드 우선 / A2 차선 | 0장 또는 1장 | 미착수 | 인테이크가 지목한 **별개** 흰색. 코드 1줄이 먼저 |
| **4** | `fx_shatter` 순백 틴트 (`DynamicBattlefield.cs:381`,`:545`) | 코드 | **0장** | 미착수 | **미보고 세 번째 순백 지점** (§3-2) |
| **5** | `EruptionVentGimmick:321`·`WindVfxManager:63` 틴트 점검 | 코드 | **0장** | 미착수 | 남은 순백 노출 2곳 |
| **6** | 날씨 파티클 3장 임포터 | 메타 | **0장** | 미착수 | 세 스테이지 대기 연출 **전면 무연출** |
| **7** | **`fx_frost` 4프레임** | **신규 아트** | **4장** | 미착수 | **유일한 진짜 아트 공백** |
| 8 | `EffectSpriteLibrary.Shatter` 상수 추가 | 코드 | 0장 | 미착수 | 재발 방지 |
| 9 | `fx_spark_000` 256² 재생성 | 아트 수정 | 1장 | 미착수 | 이번 결함과 무관 |
| 10 | `Gimmicks` 4장 임포터 | 메타 | 0장 | 미착수 | 소비자 0건, 잠복 |

> **1~6번이 전부 아트 0장이다.** 사용자 지시는 "vfx가 없으면 이미지를 남겨라"였으나,
> **조사 결과 vfx는 있다.** 발주로 해결되는 것은 7번(`fx_frost`) 하나뿐이고
> 나머지는 임포터와 틴트다.

---

## 8. 확정하지 못한 것

1. **사용자가 본 흰색이 폭발인지 붕괴인지 `[확인 불가]`.** 후보가 최소 3개다 —
   폭발 폴백(§2), `CollapseDust`(§1-2), `fx_shatter`(§3-2). 셋 다 순백 틴트를 받고
   셋 다 같은 프레임 근처에서 발생할 수 있다. QA 재현 시 **폭발 프레임과 붕괴 프레임을
   갈라 캡처**해야 구분된다.
2. **`fx_frost`의 실제 호출부 `[확인 불가]`.** `EffectSpriteLibrary.Frost` 참조는 **3건이고
   전부 테스트다** — `ImpactParticleArtTests.cs:161`(선언 목록), `:167`(`knownGaps` 핀),
   `ImpactVfxCaptureProbe.cs:116`(캡처 프로브). **프로덕션 코드 참조는 0건이다**
   `[OBSERVED]`. 아트를 만들어도 **부를 사람이 없으면 여전히 무연출**이다 — 발주와 함께
   `DynamicBattlefield`의 서리 벤트 경로에 호출을 넣는 작업이 동반돼야 한다.
3. **A2 코드 대안의 시각적 결과 `[확인 불가`]**. 채도 0.050 자산에 흙빛 틴트를 곱했을 때
   "먼지"로 읽히는지는 렌더해 봐야 안다. 정적 분석으로는 색이 흰색이 아니게 된다는 것까지만
   말할 수 있다.
4. **`Gimmicks` 4장의 과거 소비자 `[확인 불가]`.** 지금 참조가 0건인 것은 확인했으나,
   과거에 쓰였다가 떨어져 나간 것인지 처음부터 미사용인지는 이 조사 범위 밖이다.

---

## 9. 이 문서가 이전 판정을 수정한 곳

| 대상 | 이전 | 이 문서 | 근거 |
|---|---|---|---|
| `fx_shatter` 사용 여부 | *"아무도 안 쓴다 — 확정 결함"* (`impact-vfx-…:20`,`:45`,`:207`) | **사용 중.** 매직 스트링이라 안 보였을 뿐 | `DynamicBattlefield.cs:381`,`:545` |
| `CollapseDust` 중립% | 83% (인테이크) | **99.4%** (임계 `S<0.12`) — 결론 동일, 더 강함 | 직접 측정 |
| 발주서 색 지침 | *"무채색/흰색 실루엣"* (`impact-vfx-…:101`) | **틴트 종류에 따라 정반대.** 순백 틴트엔 채도 ≥0.35 | §4-3 |
| 폭발 아트 유무 | "vfx가 없으면" (사용자 전제) | **있다.** 채도 0.309~0.953, 6프레임 512² | §2 |
| 내 자신의 1차 조사 | `Effects`·`Higgsfield`·`Sprites` 3곳만 | **238장 전수.** `GeneratedExplosionFrames` 누락했었음 | §2 서두 |
| 내 자신의 `Backgrounds` 판정 | 비-Sprite 16장을 전부 결함으로 셈 | **3장은 정상** — `Load<Texture2D>`로 읽히므로 Default가 맞다. 오탐 폐기 | `GameManager.cs:581` |
| 인테이크 "빌드에서 항상 null" | 프리팹이 에디터에서만 로드된다 | **빌드에서도 로드된다** — `ExplosiveBarrel.prefab:176`이 guid 직렬화 | QA 발견, §2 |
| 내 자신의 `fx_frost` 참조 수 | "참조 0건" | **3건(전부 테스트), 프로덕션 0건** | §8-2 |
