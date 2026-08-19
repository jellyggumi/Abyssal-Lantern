# 결함 수정 설계 — D-A 궤적 / D-B 폭발 흰색

- run-id: 20260809-castle-war-stage1 (cycle 2)
- owner: engineering lane (game-programmer)
- date: 2026-08-14
- 운영 모드: Stage 3 — 운영 안정성과 플레이 임팩트
- 범위: **판정과 처방만.** 코드 수정 0건, 테스트 실행 0건.
- 인용 기준 시점: **회의 중 Main이 코드를 계속 고치고 있다.** 아래 행번호는 `LaunchManager.cs`
  **1224행 판**에서 재확인한 것이다. 회의 중 세 번 이동했으므로(1134 → 1206 → 1224), 행번호가
  안 맞으면 심볼 이름으로 찾을 것 — 심볼과 인용 문구는 이동해도 유효하다.

> **읽는 방법.** 모든 주장에 `[OBSERVED]`(파일을 직접 읽고 확인) / `[INFERENCE]`(관측에서
> 도출, 실행으로 확정 안 함) / `[확인 불가]`(확인 방법을 함께 적음) 라벨을 붙였다.
> 회의 중 인테이크 진단이 **두 번 반박됐고 둘 다 맞았으므로**, 이 문서는 인테이크의
> 수치를 그대로 쓰지 않고 재확인한 것만 쓴다. 반박한 곳은 §6에 모았다.

---

## 0. 판정 요약

| # | 질문 | 판정 |
|---|---|---|
| 1 | D-B 자산 로드 세 선택지 | **(b) 직렬화 참조가 이미 정답이고 이미 적용돼 있다.** 셋 다 D-B의 원인이 아니었다 — §1 |
| 2 | 순백 기본 텍스처가 결함인가 | **텍스처 색은 설계다. 결함은 재질의 블렌드 상태다.** — §2 |
| 3 | D-A 기본 조준값 수정 범위 | **상수 1개가 아니다. 다이얼이 두 개고 단위가 다르다.** — §3 |
| 4 | `#if UNITY_EDITOR`를 테스트로 잡을 수 있는가 | **소스 순회로 가능하고 이 저장소에 전례 4건이 있다.** — §4 |

**가장 중요한 한 줄**: D-B의 확정 원인(임포터)은 이미 고쳐졌지만, **그 수정으로도 안 고쳐지는
결함이 하나 남아 있다** — `GameFeelVfx.GetParticleMaterial()`이 만드는 재질이 **불투명**이다(§2).
이것이 신고 문구가 "흰색"이 아니라 **"흰 네모"**였던 이유이고, 아직 아무도 안 고쳤다.

---

## 1. D-B 자산 로드 경로 — 세 선택지 판정

### 1-1. 먼저: 세 선택지 중 어느 것도 D-B의 원인이 아니었다

인테이크는 `Assets/Prefabs/`가 `Resources` 밖이라 **빌드에서 프리팹이 항상 null**이라고 적었다.
**틀렸다.** `[OBSERVED]`

```
Assets/Prefabs/ExplosiveBarrel.prefab:176
  explosionEffectPrefab: {fileID: 3580900068180056100, guid: 5fcf2c0caffc2482d912912b3eb7c094,

Assets/Prefabs/ExplosionEffect.prefab.meta:2
  guid: 5fcf2c0caffc2482d912912b3eb7c094

Assets/Prefabs/ExplosionEffect.prefab:3
  --- !u!1 &3580900068180056100      ← fileID 일치
```

guid와 fileID가 **양쪽 다 일치한다.** 직렬화된 참조는 `Resources` 밖이어도 빌드 의존성이므로
**프리팹은 빌드에 포함되고 로드된다.** 따라서 `#if UNITY_EDITOR` 블록은 이 경로에서 **한 번도
발동하지 않았다** — 필드가 이미 채워져 있어서 `if (explosionEffectPrefab == null)`이 거짓이다.

즉 **선택지 (b)는 "해야 할 일"이 아니라 "이미 되어 있는 일"이다.** 세 선택지를 비교하는 질문
자체가 잘못된 전제 위에 있었다. (QA가 이것을 찾았고 Main이 확인했다. 나는 guid·fileID를
독립적으로 재확인했다.)

### 1-2. 그러면 진짜 원인 — 임포터

`[OBSERVED]` — meta 파일 직접 grep, 6장 전수:

```
수정 전: Assets/Resources/GeneratedExplosionFrames/explosion_00{0..5}.png.meta
  textureType: 0   spriteMode: 0   alphaIsTransparency: 0
작동 대조군: Assets/Resources/Effects/fx_shatter/fx_shatter_000.png.meta
  textureType: 8   spriteMode: 1   alphaIsTransparency: 1
```

`textureType: 0`은 Default이므로 `Resources.LoadAll<Sprite>`가 **빈 배열**을 준다.
`ExplosionEffectConfigurator`가 `sprites.Length > 0` 분기를 못 타고 `GetParticleMaterial(null)`로
떨어진다. **아트는 색이 있는데(채도 0.31~0.95) Sprite가 아니어서 안 나온 것이다.**

**현재 상태 `[OBSERVED]`** — 재확인했고 6장 전부 고쳐져 있다:

```
explosion_000..005.png.meta:  spriteMode: 1  alphaIsTransparency: 1  textureType: 8
```

### 1-3. 이 저장소의 자산 로드 관례 — 전수 조사

`Resources.Load` 계열 호출부를 전수 조사했다. `[OBSERVED]`

| 범위 | 파일 수 |
|---|---:|
| `Assets/Scripts/` (런타임) | **18개 파일** |
| `Assets/Tests/` + `Assets/Editor/` | 13개 파일 |

**런타임 로드 유형별 지배적 관례:**

| 유형 | 대표 호출부 | 패턴 |
|---|---|---|
| 스프라이트 프레임 세트 | `UnitSpriteAnimator:165`, `DynamicBattlefield:172`, `FrameAnimEffect:54` | `Resources.LoadAll<Sprite>($"...")` + `Array.Sort` 이름순 |
| 단일 스프라이트 | `EffectSpriteLibrary.LoadParticleSprite:68`, `GimmickSpriteLibrary:52` | `Resources.Load<Sprite>` + 정적 캐시 |
| ScriptableObject | `GameManager:254-256`, `BrickPlacementController:192-197` | `Resources.Load<BlockData>("StoneBlockData")` |
| 오디오 | `BgmManager:111`, `GameFeelVfx:41-53` | `Resources.Load<AudioClip>` + 지연 캐시 |
| **프리팹** | **4곳 전부 `"DestructibleBlock"`** | `Resources.Load<GameObject>` |

**프리팹 로드는 4곳뿐이고 전부 같은 자산이다** `[OBSERVED]`:
`BrickPlacementController:208`, `DynamicBattlefield:634`, `GameManager:925`, `GameManager:1507`.
그리고 `Assets/Resources/` 루트의 프리팹은 **`DestructibleBlock.prefab` 단 하나다.**

**판정: 이 저장소에 "프리팹을 `Resources`에 둔다"는 관례는 없다.** 예외 1건이 있을 뿐이고,
그것은 *수천 개가 스폰되는 단일 블록 프리팹*이라는 특수 사례다. 반면 **직렬화 참조는 프리팹
연결의 지배적 관례다** — 배럴이 폭발 이펙트를 참조하는 방식이 그것이고, 그것이 작동한다.

따라서 **선택지 (a) 프리팹을 `Resources`로 이동은 관례 역행이며 불필요하다.** `[INFERENCE]`
근거: 이동이 해결하는 문제(빌드 도달성)가 §1-1에서 존재하지 않음이 증명됐고, 관례상 프리팹
연결은 직렬화가 정답이며, `Resources`는 참조 여부와 무관하게 **전량 빌드에 포함**되므로
불필요한 이동은 빌드 용량만 늘린다.

### 1-4. "슬롯 참조는 사람이 유니티를 열어야 한다"는 제약인가

**아니다. 이 저장소에서 반증된다.** `[OBSERVED]`

- 필요한 두 값이 **디스크의 텍스트에 그대로 있다** — 대상의 `guid`는 `.meta:2`에, 루트
  GameObject의 `fileID`는 프리팹 YAML 3행 `--- !u!1 &<fileID>`에. §1-1의 세 grep이 그
  경로를 그대로 보여준다.
- **이 세션에서 이미 CLI로 자산 메타 6장을 편집해 검증했다** — 임포터 수정이 그것이다.
  유니티 UI 없이 `.meta` YAML을 직접 고쳤고 결과를 grep으로 확인했다.
- EditMode 테스트가 프리팹을 **경로로 직접 로드해 단언한다** — `CurrentRosterBalanceGateTests:47`,
  `PreviewParityRegressionTests:143`, `KegPlacementSafetyTests:52`. 즉 참조가 옳게 직렬화됐는지
  **CLI로 검증 가능하다.**

**판정: 제약이 아니다.** 직렬화 참조는 에이전트가 텍스트로 쓰고 테스트로 검증할 수 있다.
다만 **주의 하나** `[INFERENCE]`: 유니티가 그 씬/프리팹을 다시 저장하면 필드 순서가 재정렬될
수 있으므로, 손으로 넣은 참조는 **테스트로 고정해야** 조용히 사라지지 않는다(§4-3).

### 1-5. 선택지 (c) 절차 폴백을 아트 없이도 옳게 — 판정

**부분 채택. 이미 적용됐고, 방식이 옳다.** `[OBSERVED]`

Main이 `ExplosionFrames.Load()`를 만들어 `ExplosiveGimmick:321`이 폴백에서 **같은 실제 아트를**
쓰게 했다. 이것이 옳은 이유는 "아트 없이 옳게"가 아니라 **"폴백도 실제 아트에 닿게"**이기 때문이다.
`ExplosiveGimmick:327-328`이 프레임 1(채도 0.95)을 쓰고 틴트를 걸지 않는 것도 맞다 — 실제
아트에 순백 틴트를 곱하면 §2-4의 함정에 그대로 빠진다.

**다만 아직 남은 null 분기가 있다 — 이것이 내 판정을 요구한 지점이다.**

`GetParticleMaterial()` 무인자 오버로드에 도달하는 곳을 전수 조사했다. `[OBSERVED]`

| 호출부 | 조건 | 위험 |
|---|---|---|
| `ExplosionEffectConfigurator:69` | sprites 비었을 때 | 임포터 수정으로 **닫힘** |
| `ExplosiveGimmick:364` | ember 로드 실패 | ember는 8/1/1이라 **닫힘** |
| `EruptionVentGimmick:321` | particleSprite null | 열림 |
| **`WindVfxManager:63`** | **조건 없음 — 항상** | **항상 열림** |

**`WindVfxManager:63`은 분기가 아니라 무조건 호출이다.** 그리고 `GameManager:294`가
`WindVfxManager`를 **매 경기 자동으로 추가한다** `[OBSERVED]`:

```
GameManager.cs:292-295
  if (GetComponent<WindVfxManager>() == null)
      gameObject.AddComponent<WindVfxManager>();
```

즉 **기본 파티클 텍스처는 예외 경로가 아니라 상시 렌더되는 경로다.** 이것이 §2의 판정을
"이론적 방어선"에서 "실제로 화면에 있는 것"으로 바꾼다.

**처방 (c-1)**: `ExplosionEffectConfigurator`의 `:69` 무인자 분기는 **삭제한다.** 임포터가
고쳐진 뒤 그 분기에 도달하는 것은 임포터가 다시 깨졌다는 뜻이고, 그때 순백을 그리는 것보다
**아무것도 그리지 않는 것이 정직하다** — `StageWeather:45-49`가 이미 그 관례다(스프라이트가
null이면 `Stop()` 후 return, 조용히 없음). 순백을 그리면 결함이 "아트처럼" 보여 또 한 번
살아남는다. 그것이 이 결함이 세 번 반복된 방식이다.

---

## 2. 순백 기본 텍스처 — 결함인가 설계인가

### 2-1. 질문의 핵심을 확정했다: URP 셰이더는 `startColor`를 전달한다

이것이 판정의 갈림길이었다. **URP 17.5.0 패키지 소스를 직접 읽어 확정했다.** `[OBSERVED]`

`Library/PackageCache/com.unity.render-pipelines.universal@73b4c4ff130e/`

```
Shaders/Particles/ParticlesUnlitInput.hlsl:58-68
  half4 SampleAlbedo(TEXTURE2D_PARAM(...), ParticleParams params)
  {
      half4 albedo = BlendTexture(...) * params.baseColor;
      ...
      albedo = MixParticleColor(albedo, half4(params.vertexColor), colorAddSubDiff);

ShaderLibrary/Particles.hlsl:63-83
  half4 MixParticleColor(half4 baseColor, half4 particleColor, half4 colorAddSubDiff)
  {
  #if defined(_COLOROVERLAY_ON)   ...
  #else // Default to Multiply blend
      return baseColor * particleColor;
  #endif
```

**판정: `startColor`는 정점 색으로 셰이더에 전달되고, 키워드가 없는 기본값은 `_ColorMode = 0.0`
(Multiply)이므로 `텍스처 × 파티클색`으로 곱해진다.** `[OBSERVED — 셰이더 소스]`

`ParticlesUnlit.shader:35`이 `_ColorMode("_ColorMode", Float) = 0.0`을 선언하고,
`:36-37`의 `_COLOROVERLAY_ON` / `_COLORCOLOR_ON` / `_COLORADDSUBDIFF_ON`은 전부
`shader_feature_local_fragment`이므로 코드로 만든 재질에서는 **꺼져 있다** → `#else` 분기.

**따라서 순백 텍스처 자체는 결함이 아니다. 틴트를 곱하기 위한 중성 피승수라는 설계가 맞다.**
`GameFeelVfx.cs:470`의 `Color(1f, 1f, 1f, alpha)`는 의도대로 작동한다 — 호출부가 색을 주면
그 색이 나온다. `ExplosiveGimmick`의 앰버 그라디언트도, `WindVfxManager:45`의 창백한 청색
`(0.65, 0.9, 1, 0.35)`도 셰이더에 도달한다.

이것은 `impact-white-square.md:99-104`가 이미 적어둔 교훈과 **같은 구조다** — *"아트는 무채색으로
그려져 있고 코드가 틴트를 곱한다. `Color.white`를 곱하면 아트가 그대로 나온다."* 기본 텍스처는
그 규약의 텍스처 쪽 절반이다.

### 2-2. 그러나 진짜 결함을 찾았다 — 재질이 불투명이다

**순백 텍스처는 설계지만, 그 텍스처를 담는 재질은 결함이다.** `[OBSERVED]`

```
GameFeelVfx.cs:524-536
  public static Material GetParticleMaterial(Texture2D customTexture = null)
  {
      Texture2D texture = customTexture != null ? customTexture : GetDefaultParticleTexture();
      if (cachedParticleMaterials.TryGetValue(texture, out Material material)) return material;

      Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
      if (shader == null) shader = Shader.Find("Sprites/Default");
      if (shader == null) return null;

      material = new Material(shader);
      material.mainTexture = texture;
      cachedParticleMaterials.Add(texture, material);
      return material;
```

`mainTexture` 외에 **아무것도 설정하지 않는다.** 이 파일에 `SetFloat` / `EnableKeyword` /
`renderQueue`는 **0건이다** `[OBSERVED — grep 무결과]`.

URP 셰이더의 선언 기본값 `[OBSERVED — ParticlesUnlit.shader:23-32]`:

```
_Surface("__surface", Float) = 0.0            ← Opaque
_SrcBlend("__src", Float) = 1.0               ← One
_DstBlend("__dst", Float) = 0.0               ← Zero
_ZWrite("__zw", Float) = 1.0                  ← 깊이 기록
Tags { "RenderType" = "Opaque" }              ← :67
```

그리고 패스가 그 값을 **그대로 읽는다** `[OBSERVED — ParticlesUnlit.shader:80-84]`:

```
BlendOp[_BlendOp]
Blend[_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]
ZWrite[_ZWrite]
```

마지막 한 조각 — 알파를 되살릴 수 있는 유일한 지점도 키워드로 잠겨 있다
`[OBSERVED — ShaderLibrary/Particles.hlsl:131-139]`:

```
half3 AlphaModulateAndPremultiply(half3 albedo, half alpha)
{
#if defined(_ALPHAMODULATE_ON)
    return AlphaModulate(albedo, alpha);
#elif defined(_ALPHAPREMULTIPLY_ON)
    return AlphaPremultiply(albedo, alpha);
#endif
    return albedo;        ← 키워드 없음 → alpha를 rgb에 반영하지 않는다
}
```

**연쇄 결론 `[INFERENCE — 셰이더 소스에서 도출, 실행으로 확정 안 함]`:**

`new Material(URP Particles/Unlit)`은 `Blend One Zero` + `ZWrite On` + Opaque 큐로 그려진다.
`One Zero`는 **알파를 완전히 무시하고 소스 rgb로 프레임버퍼를 덮는다.**

그런데 `GetDefaultParticleTexture()`가 만드는 텍스처는 **rgb가 전 픽셀 (1,1,1)로 상수이고,
원형은 알파에만 담겨 있다** `[OBSERVED — GameFeelVfx.cs:462-471]`:

```
float alpha = Mathf.Clamp01(1f - (dist / center));
alpha = Mathf.Pow(alpha, 1.5f);
texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));   ← rgb 상수, 모양은 alpha에만
```

**알파가 버려지면 원형 정보가 통째로 사라지고 32×32 쿼드 전체가 순백으로 채워진다.**

### 2-3. 이것이 신고 문구를 설명한다

신고와 이력의 표현이 **"흰색"이 아니라 "흰 네모"였다** (`impact-white-square.md:6`,
사용자 원문 *"충돌 시 왜 이미지가 흰 네모로 나오지?"*). 순백 원형 텍스처는 그 자체로는
**네모가 아니다** — `impact-white-square.md:150`이 정직하게 남긴 미해결도 그것이다:
*"「네모」는 별섬광의 형태가 아니다."*

**불투명 블렌드는 원을 네모로 만든다.** 형태 불일치가 이 기제로 해소된다. `[INFERENCE]`

### 2-4. 부수 판정 — 남은 순백 틴트 3곳은 별개 결함이다

`[OBSERVED]` — 실제 아트에 `Color.white`를 곱하는 곳:

| 호출부 | 아트 | 아트 채도 | 판정 |
|---|---|---|---|
| `GameFeelVfx.cs:410` | `CollapseDust` | 0.050 (중립 81~83%) | **결함** — 흰 연기 |
| `DynamicBattlefield.cs:381`, `:545` | `fx_shatter` | 0.073 (중립 90.9%) | **결함** — 흰 파편 |
| `GameFeelVfx.cs:244` | `Impact` | 0.574 | 결함 아님 — 유채색 아트 |
| `GameFeelVfx.cs:824` | `CoreCrack` | 미측정 | `[확인 불가]` — 채도 측정 필요 |

(채도 수치는 ArtRequestSpec 레인 측정을 인용. 호출부와 틴트 값은 내가 직접 확인.)

**판정: 이 3곳은 D-B와 별개이고, §2-2를 고쳐도 남는다.** 무채색 아트에 순백을 곱하면 무채색이
나오는 것은 셰이더가 정상 작동한 결과다. `impact-white-square.md:111-113`이 이미
`DestructibleBlock:229`에서 이 수정을 했고(`Color.white` → `(1.00, 0.78, 0.36)`), 채도 0.35
이상을 요구하는 테스트로 고정했다 — **같은 처방을 같은 테스트로 확장하면 된다**(§4-3).

### 2-5. 확인 불가 — 빌드에서 어느 셰이더가 잡히는가

**`[확인 불가]`** — 그리고 이것이 §2-2 판정의 유일한 불확실성이다.

`Shader.Find`는 **빌드에 포함된 셰이더만** 찾는다. 확인한 것 `[OBSERVED]`:

- `ProjectSettings/GraphicsSettings.asset`의 `m_AlwaysIncludedShaders`에 URP 파티클 셰이더가
  **없다** (7개 항목 전부 `guid: 0000000000000000f000000000000000` 내장 셰이더)
- 프로젝트의 `.mat` 자산은 **2개뿐이고 둘 다 TextMesh Pro**다
- `ExplosionEffect.prefab`의 `ParticleSystemRenderer`는 `m_Materials: - {fileID: 0}` —
  **재질 참조가 없다**

즉 **URP 파티클 셰이더를 참조하는 자산이 프로젝트에 하나도 없다.** `[OBSERVED]`

**따라서 `[INFERENCE]`**: 플레이어 빌드에서 `Shader.Find("Universal Render Pipeline/Particles/Unlit")`이
**null을 반환할 가능성이 높고**, 그러면 `Sprites/Default`로 폴백한다. `Sprites/Default`는
알파 블렌딩이 셰이더에 내장돼 있어 **정상적으로 부드러운 원을 그린다.**

**이 추론이 맞으면 §2-2의 결함은 에디터에서만 나타난다.** 그것은 Main의 원래 비대칭
주장("에디터 정상·빌드 흰색")과 **정확히 반대 방향의 비대칭**이다.

**확인 방법 (내가 못 하는 것, 재우면 되는 것):**

1. **에디터에서 즉시 확인 (권장, CLI 가능)** — `GetParticleMaterial()`의 실제 상태를 찍는다.
   `ImpactVfxCaptureProbe:108-112`가 이미 셰이더 이름을 로그하는 전례다. 여기에 블렌드
   상태를 추가하면 §2-2가 실행으로 확정된다:
   ```csharp
   var m = GameFeelVfx.GetParticleMaterial();
   Debug.Log($"shader={m.shader.name} src={m.GetFloat("_SrcBlend")} "
           + $"dst={m.GetFloat("_DstBlend")} zwrite={m.GetFloat("_ZWrite")} "
           + $"queue={m.renderQueue} surface={m.GetFloat("_Surface")}");
   ```
   `dst=0` 이면 §2-2 확정. `src=5, dst=10`(SrcAlpha/OneMinusSrcAlpha)이면 내 추론이 틀렸다.

2. **빌드 분기 확인** — WebGL 빌드 후 브라우저 콘솔에서 같은 로그를 읽는다. 셰이더 이름이
   `Sprites/Default`면 §2-5의 추론이 확정되고, 결함은 에디터 한정이다.

### 2-6. 처방

**(2-A) 재질을 투명으로 설정한다 — `GameFeelVfx.cs:533` 직후.** 우선순위 최상.

```csharp
material = new Material(shader);
material.mainTexture = texture;
// URP 셰이더는 선언 기본값이 Opaque(One/Zero, ZWrite On)이고, ShaderGUI를 거치지 않는
// 코드 생성 재질은 그 기본값을 그대로 쓴다. 설정하지 않으면 알파가 버려져 파티클이
// 불투명 사각형으로 그려진다 — 기본 텍스처는 rgb가 상수 백색이고 모양이 알파에만
// 있으므로, 그 경우 순백 네모가 된다.
if (shader.name.StartsWith("Universal Render Pipeline"))
{
    material.SetFloat("_Surface", 1f);                       // Transparent
    material.SetFloat("_Blend", 0f);                         // Alpha
    material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
    material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
    material.SetFloat("_ZWrite", 0f);
    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
    material.renderQueue = (int)RenderQueue.Transparent;
}
```
`Sprites/Default` 폴백 경로는 셰이더가 이미 블렌딩을 내장하므로 건드리지 않는다. 그래서
분기가 필요하다.

**(2-B) 셰이더를 빌드에 고정한다.** `[INFERENCE]` §2-5가 확정되면 필수. `Shader.Find`가
빌드에서 조용히 다른 셰이더를 잡는 것은 **에디터와 빌드가 다른 그림을 그린다**는 뜻이고,
그것은 이 결함군의 뿌리와 같은 종류다. `GraphicsSettings`의 `m_AlwaysIncludedShaders`에
URP 파티클 언릿을 추가하거나, `Resources`에 그 셰이더를 쓰는 재질 자산 1개를 둔다.
**후자를 권한다** — 자산이 있으면 EditMode 테스트가 그것을 로드해 단언할 수 있다.

**(2-C) `ExplosionEffectConfigurator:69` 무인자 분기 삭제** — §1-5.

---

## 3. D-A 수정 범위

프리뷰는 사실을 말하므로 렌더러 버그가 아니다. 그 전제에 동의한다 `[OBSERVED]` —
`LaunchManager:1072`가 같은 팀 **유닛만** 무시하고(`hitUnit.isPlayerUnit == previewIsPlayer`),
`:1093`이 첫 비트리거 히트에서 멈춘다. 자기 성벽은 무시 대상이 아니다.

> **회의 중 진행 상황.** 이 절을 쓰는 동안 Main이 §3-B를 적용했다 — `aimPower`가
> **0.55 → 0.84**로 올라갔고(`LaunchManager.cs:80`), 근거 문서
> `qa/evidence/aim-space/trajectory-blockers.md`가 생겼다. **§3-A(다이얼 통일)는 적용되지
> 않았고 여전히 유효하다** — `OneShotSiegeRules.Velocity:57-61`이 그대로 선형 Lerp다.
> Main의 새 주석이 그 사실을 **코드 안에 명시한다**(`:67-68`: *"This path does not use the
> draw curve"*). 즉 내 §3-(i) 판정은 반박된 것이 아니라 **인정된 상태로 남아 있다.**

### 3-(i) 기본 조준값 — 상수 몇 개이고 무엇이 읽는가

**답: 상수는 2개지만, 그 값을 바꾸는 것만으로는 안 된다. 다이얼이 두 개고 단위가 다르다.**

**상수 `[OBSERVED — LaunchManager.cs:61, :80]`:**
```csharp
[Range(10f, 80f)] public float aimAngleDegrees = 45f;   // :61
[Range(0f, 1f)]   public float aimPower = 0.84f;        // :80  (회의 중 0.55에서 상향됨)
```

**중요 — 이 값들은 씬에 직렬화돼 있지 않다.** `[OBSERVED]`
`SampleScene.unity`의 `LaunchManager` 블록(`&83740687`)은 `timeStep: 0.02` 다음에
`impactMarkerPrefab`으로 건너뛴다. `aimPower`/`aimAngleDegrees` 항목이 **없다.**
`Assets/Scenes/` + `Assets/Prefabs/` 전체 grep도 **0건**이다.

→ **따라서 코드 기본값이 곧 출하값이다.** 씬 저장 이후 필드가 추가됐고 씬이 재저장되지
않았기 때문이다 `[INFERENCE]`. 좋은 소식이다: 상수 수정이 곧 출하값 수정이다. 나쁜 소식도
있다: 누가 유니티에서 씬을 저장하면 그 순간 현재 값이 씬에 박히고, 이후 상수 수정이 조용히
무효가 된다 → **§4-3의 G4가 필요한 이유.**

**무엇이 읽는가 `[OBSERVED]`:**

```
LaunchManager:136-137  AdjustAimAngle / AdjustAimPower  (화살표 키)
LaunchManager:139-145  GetSeparatedAimVelocity() → OneShotSiegeRules.Velocity(aimAngleDegrees, aimPower, ...)
LaunchManager:784      launchVelocity = GetSeparatedAimVelocity()   ← 매 프레임 무조건
LaunchManager:791      if (Input.GetKeyDown(KeyCode.Space)) LaunchUnit()
```

**삼자 정합 판정:**

| 경로 | 속도 산출 | 정합 |
|---|---|---|
| 키보드 프리뷰 (`:787` `DrawTrajectory`) | `:784`의 `launchVelocity` | ✅ 같은 필드 |
| Space 커밋 (`:791` `LaunchUnit`) | `:784`의 `launchVelocity` | ✅ 같은 필드 |
| `SimulateLaunch` (`:1217`) | **인자로 받은 속도를 그대로 대입** | ❌ **우회** |

**프리뷰와 커밋은 정합한다** — `:784`가 무조건 대입하고 둘 다 그 한 필드를 읽는다.
**그러나 `SimulateLaunch:1217-1220`은 `GetSeparatedAimVelocity()`를 부르지 않는다.**
그리고 모든 프로브가 그 문으로 들어온다 `[OBSERVED — ShotReachabilityProbe:114-119, :252-254,
:416-418]`: `LaunchPowerCurve.SpeedForDraw(draw, cap)`로 속도를 직접 만들어 넘긴다.

→ **키보드 조준 경로(`OneShotSiegeRules.Velocity`)는 어떤 PlayMode 프로브도 실행하지 않는다.**
`[OBSERVED]` 기본 조준값을 바꿔도 **실측이 그것을 보지 못한다.** 그래서 Main의 0.84는
오프라인 적분(해석해와 0.1% 일치 검증됨)으로 정해졌다 — 현재 가능한 최선이고,
**§4-3의 G4가 그 값을 고정해야** 다음 커브 변경 때 또 stale이 되지 않는다.

**그리고 결정적 발견 — 두 다이얼의 단위가 다르다.** `[OBSERVED]`

```
드래그 경로   LaunchManager:888-889
  normalizedDraw = |pull| / maxDragDistance
  speed = LaunchPowerCurve.SpeedForDraw(normalizedDraw, maxLaunchVelocity)
        = maxSpeed * pow(draw, 0.5)                    ← 제곱근 커브 (LaunchPowerCurve:79-84)

키보드 경로   OneShotSiegeRules.cs:57-61
  speed = Mathf.Lerp(minSpeed, maxSpeed, ClampPower(normalizedPower))
        = 3 + 14.5 * aimPower                          ← 선형 lerp, 커브를 안 쓴다
```

**`aimPower`는 `draw`가 아니다.** 같은 숫자가 두 경로에서 다른 속도를 낸다
`[OBSERVED — 위 두 식으로 산출]`:

| 값 | 드래그 경로 속도 | 키보드 경로 속도 | 차이 |
|---|---:|---:|---:|
| 0.35 | 10.353 | 8.075 | −2.278 |
| 0.55 | 12.978 | 10.975 | −2.003 |
| 0.84 | 16.037 | 15.180 | −0.857 |

바닥값도 어긋난다: `aimPower = 0` → 속도 **3.0**(하한), `draw = 0` → 속도 **0**.
그리고 `LaunchPowerCurve.MinDrawFraction = 0.119`의 약한-당김 거부 게이트는 드래그 경로에만
있다 — 키보드 경로에는 **최소 당김 게이트가 없다** `[OBSERVED]`.

**이 불일치의 세 번째 귀결 — 화면에 표시되는 숫자가 틀린다.** `[OBSERVED]`
HUD와 리드백은 **커브의 역함수**로 파워를 표시한다:
```
LaunchManager:430   forcePercent = LaunchPowerCurve.DrawForSpeed(velocity.magnitude, maxLaunchVelocity) * 100
LaunchManager:1168  powerPercent = LaunchPowerCurve.DrawForSpeed(reportedVelocity.magnitude, ...) * 100
```
그런데 키보드 경로의 속도는 선형 Lerp에서 나온다. 따라서 `aimPower = 0.84`로 쏘면
**화면에는 (15.18/17.5)² = 75.2%로 표시된다.** 플레이어가 보는 숫자와 모델의 숫자가 다르다.
`:1168`의 주석은 *"the number they are learning to repeat is the pull"*이라고 적는데,
키보드 경로에서는 **그 숫자가 pull이 아니다.** 표시 지점 두 곳 모두 영향을 받는다.

`LaunchManager.cs:15-16`의 주석은 *"the curve is the authority and this field exists so the
keyboard aim path and the power readout share one number"*라고 적는데, **키보드 경로는 커브를
쓰지 않으므로 그 한 숫자를 공유하지 않는다.** 주석이 실제와 다르다 `[OBSERVED]`.

**처방 (3-A) — 다이얼을 통일한다.** 우선순위 상. **아직 적용되지 않았다.**

`OneShotSiegeRules.Velocity`의 속도 산출을 `LaunchPowerCurve.SpeedForDraw`로 교체한다.
그러면 세 가지가 동시에 닫힌다:
1. `aimPower`가 진짜 draw가 되어 **두 입력 경로가 같은 커브를 공유한다.**
2. **HUD·리드백 표시값이 모델값과 일치한다**(현재 0.84 → 75.2%로 표시되는 문제).
3. `MinDrawFraction` 게이트가 두 경로에 공통 적용된다(3-C가 자동 해소).

**주의 — 통일하면 Main의 0.84를 다시 재야 한다.** `[INFERENCE]` 통일 후 `aimPower = 0.84`는
속도 16.037이 되어(현재 15.180) 더 멀리 간다. `trajectory-blockers.md`의 착지표는 **선형 Lerp
기준**이므로 커브 기준으로 다시 산출해야 한다. **그래서 3-A와 3-B 재산출은 한 커밋이어야
한다** — 따로 하면 그 사이에 기본값이 도달 대역을 벗어난다.

**처방 (3-B) — 기본값. Main이 이미 적용했고, 근거를 확인했다.** `[OBSERVED]`

`trajectory-blockers.md`의 45° 착지표(오프라인 적분, 해석해와 0.1% 일치 검증됨):

| aimPower | 속도 | 착지 x | 판정 |
|---:|---:|---:|---|
| 0.55 (구 기본값) | 10.975 | −4.7 | **자기 성채** (x=−7..−4) |
| 0.80 | 14.60 | 4.68 | 적 성채 (최소 도달) |
| **0.84 (현 기본값)** | **15.18** | **6.40** | **적 성채 중앙** |
| 0.88 | 15.76 | 8.19 | 적 코어대 |

**구 기본값 0.55는 자기 성채에 떨어졌다.** 도달 최소값 0.80까지 0.25이고 `powerStep = 0.04`이므로
**키 입력 6.2회**다 — 즉 조준을 배우지 않은 플레이어의 도달률은 6.3%가 아니라 **0%였다.**
원인은 작업 #60이 `MaxSpeed`를 25.2 → 17.5로 내릴 때 `aimPower`를 함께 조정하지 않은 것이다.
**이것이 사용자 신고 D-A 절반의 확정 원인이다** — *"적쪽으로 갈 수 없는 위치"* 가 맞았다.

**내 이전 권고(0.86)는 철회한다.** 그것은 QA의 draw 86%를 aimPower로 환산한 값(0.912)과
원문 숫자를 혼동한 것이었다. Main처럼 **aimPower 공간에서 직접 재는 것이 옳다** — 환산 단계가
없으므로 §3-(i)의 단위 혼동에 빠지지 않는다.

**처방 (3-C) — 키보드 경로 최소 당김 게이트.** 3-A가 흡수한다. 별도 작업 불필요.

**처방 (3-E) — `AimDefaultReachTests`가 존재하지 않는다.** 우선순위 상. `[OBSERVED]`
`LaunchManager.cs:76-77`이 *"which is what `AimDefaultReachTests` now does"*라고 적지만
**`Assets/Tests/`에 그 파일이 없다**(경로 확인 실패, 저장소 전체 grep이 이 주석 1건만 반환).
주석이 존재하지 않는 테스트를 참조한다 — **§4 결함군과 정확히 같은 형태**(문서상 안전해
보이지만 실제 보증이 없음)다. G4가 이 테스트여야 한다.

### 3-(ii) 프리뷰가 "자기 벽에 맞는다"를 구분하는 것의 버튼

**판정: 가능하고, 값이 크고, 이 저장소에 정확히 같은 조회 전례가 있다.**

**`nearestHit`에서 진영을 읽을 수 있는가 — 답: 그렇다.** `[OBSERVED]`

`CastleController:10`이 `public bool isPlayerCastle = true;` — 공개 필드다.

성벽 블록이 성채 아래에 붙는다 `[OBSERVED — GameManager.cs:929-932]`:
```csharp
var root = new GameObject(isPlayerSide ? "PlayerWall" : "EnemyWall");
root.transform.position = basePosition;
var parentCastle = isPlayerSide ? playerCastle : enemyCastle;
if (parentCastle != null) root.transform.SetParent(parentCastle.transform);
```

그리고 씬이 두 성채를 **실제로 할당한다** `[OBSERVED — SampleScene.unity]`:
```
1488:  playerCastle: {fileID: 359753119}
1489:  enemyCastle: {fileID: 1914710606}
1420:  isPlayerCastle: 1        (PlayerCastle, :1402)
4112:  isPlayerCastle: 0        (EnemyCastle,  :4094)
```

**전례 — `DestructibleBlock`이 이미 정확히 이 조회를 한다** `[OBSERVED — DestructibleBlock.cs:436-440]`:
```csharp
var owner = GetComponentInParent<CastleController>();
ShotTraceDirector.NoteBlockDestroyed(
    owner != null ? ShotTraceDirector.TargetKind.Wall
                  : ShotTraceDirector.TargetKind.FieldObstacle,
    owner != null ? owner.isPlayerCastle : (bool?)null);
```
`:448-456`이 한 번 더 같은 조회를 한다. **같은 저장소, 같은 객체 종류(성벽 블록), 같은
호출, null 처리(`(bool?)null`)까지 완성돼 있다.**

그리고 프리뷰 루프는 이미 같은 형태의 조회를 두 번 한다 `[OBSERVED — LaunchManager:1071, :1083]`:
`GetComponentInParent<UnitController>()`, `GetComponentInParent<EventGateGimmick>()`.

**따라서 처방 (3-D)** — `:1093` 히트 확정 지점에서:
```csharp
var ownerCastle = nearestHit.collider.GetComponentInParent<CastleController>();
bool blockedByOwnWall = ownerCastle != null && ownerCastle.isPlayerCastle == previewIsPlayer;
```
`blockedByOwnWall`이면 impact marker와 궤적 선을 **경고 색으로** 그린다(예: 적 타격은
현행 색, 자기 벽은 채도 있는 적색). `UpdateImpactMarker(hitDetected, hitPoint)`가 이미
단일 갱신 지점이므로 인자 하나만 늘리면 된다.

**버튼(비용/이득) 판정 `[INFERENCE]`:**
- 비용: 조회 1회 + 색 분기. 전례가 있으므로 설계 위험 없음. 프리뷰 루프는 이미
  `GetComponentInParent`를 2회 하므로 성능 성격이 바뀌지 않는다.
- 이득: 사용자 신고는 *"앞에 장애물로 포커싱되어서 포물선이 이상하게 표현돼"* 였다.
  **프리뷰가 옳다는 것을 사용자가 못 읽은 것이 신고의 내용이다.** 자기 성벽이 **40.7%**
  (정정판 전수 계산)인데 플레이어는 그 경우를 **한 번도 이름으로 본 적이 없다.** 색 하나가
  "버그"를 "규칙"으로 바꾼다.
- **B1이 자기 턴의 71%(10/14)에서 자기 성벽 블록 파괴를 실측했다**
  `[OBSERVED — trajectory-blockers.md §2 인용]`. 프리뷰만의 문제가 아니라 **실제로 자기 벽을
  부수고 있다.**
- **`readback-attribution.md`가 이미 같은 종류의 결함을 등록해 뒀다** —
  `aim-space-reachability.md:64-68`, 리드백이 맞은 대상을 틀리게 말하는 문제. **같은 조회가
  두 결함을 함께 닫는다.**

**범위 정밀화 `[OBSERVED — trajectory-blockers.md §5]`**: 자기 벽 40.7%는 **낮은 각도 구간이
대부분이고 45°에서는 문제가 아니다.** 그러므로 3-D의 값은 "기본값으로 쏘는 플레이어"보다
**"각도를 내려 보는 플레이어"** 에게 크다. 3-B가 기본값 경로를 닫았으므로 3-D는 **탐색 중인
플레이어**를 위한 것 — 둘은 서로 다른 사용자를 돕고 겹치지 않는다.

**판정: (3-D)는 (3-A)와 독립이며 되돌리기 쉽다.** 3-B가 이미 들어갔으므로 3-D는 남은
40.7%(낮은 각도)를 읽히게 만드는 작업이다.

### 3-(iii) 레벨 기하는 이 회의에서 건드리지 않는다

`LaunchPowerCurve.cs:30-34`가 **명시적으로** 그렇게 적어 뒀다 `[OBSERVED]`:
*"it does not shorten the apron. That distance is level design ... Changing both at once would
make neither measurable."* 이 판단에 동의한다. 커브와 기하를 같이 옮기면 어느 쪽이 효과를
냈는지 측정이 불가능해진다.

---

## 4. 재발 방지 — `#if UNITY_EDITOR` 자산 로드를 테스트가 잡을 수 있는가

### 4-1. 왜 어떤 에디터 테스트도 이것을 못 잡는가

**질문의 전제가 맞다.** `#if UNITY_EDITOR` 블록은 에디터에서 **컴파일되고 성공한다.**
EditMode든 PlayMode든 **에디터 안에서 돌기 때문에** 그 블록이 살아 있고, 자산이 로드되고,
테스트가 초록이 된다. 빌드에서만 그 코드가 사라진다.

**이 저장소가 그 벽을 이미 문서로 인정했다** `[OBSERVED — SiegeArtResourceTests.cs:409-413]`:

> *"Resources.LoadAll is the whole of the animated path: unlike the single-sprite library,
> GimmickAnimLibrary has no editor-side repair to fall back on, and any such repair is compiled
> out of a player build anyway. Calling the runtime API directly is therefore the only way to
> prove the art is import-correct at rest rather than being rescued by the Editor at load time."*

그리고 그 테스트가 실제로 그 규율을 집행한다 `[OBSERVED — :399-425]`:
`AnimFrameSets_ResolveThroughTheRuntimeLoaderAloneWithNoEditorOnlySelfHeal`.

**판정: 두 가지 서로 다른 테스트 전략이 필요하다.**
1. **런타임 API만 호출하는 테스트** — 에디터 복구를 우회한다. 전례 있음(위).
   `Resources.Load`가 진짜 경로일 때 유효하다.
2. **소스를 읽는 테스트** — 코드가 *존재하면 안 되는 형태*를 금지한다. `#if UNITY_EDITOR`
   자산 로드는 **런타임 API 호출이 아예 없으므로 (1)로 못 잡는다.** (2)가 유일한 수단이다.

### 4-2. 소스 순회는 가능하고, 전례가 4건 있다

**`[OBSERVED]` — `Assets/Tests/`에서 소스/씬을 텍스트로 읽는 테스트:**

| 테스트 | 무엇을 읽는가 | 방법 |
|---|---|---|
| **`AccessibleBlinkTests.cs:143-164`** | `Assets/Scripts/UnitController.cs` | `File.ReadAllText` + `Regex` 금지 패턴 |
| `GroundAtlasBudgetTests.cs:103-106` | `Assets/Scripts/GameManager.cs` | `File.ReadAllText` + `StringAssert.Contains` |
| `LaunchPowerCurveTests.cs:175-182` | `Scenes/SampleScene.unity` | `File.ReadAllText` + `Regex.Matches` |
| `WorldLabelLegibilityTests.cs:173, 230-233` | 소스 파일 doc/본문 | `File.ReadAllLines` / `ReadAllText` |

**가장 가까운 구조적 일치는 `AccessibleBlinkTests`다** — 목적이 동일하다: *"코드가 이 형태로
다시 작성되는 것을 금지한다."* `[OBSERVED — :135-141]`:

> *"No blink rate may be authored as a literal again. The two violations were possible only
> because the rates lived inline. This scans the sources that own blinks for raw multipliers ...
> so the next inline rate fails here instead of shipping."*

그리고 임포터 설정을 직접 읽는 전례도 있다 `[OBSERVED — CastleSkinResourceTests.cs:162-166]`:
```csharp
string assetPath = UnityEditor.AssetDatabase.GetAssetPath(texture);
var importer = UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.TextureImporter;
if (importer == null || importer.alphaSource != ... || !importer.alphaIsTransparency)
```

**판정: 소스 순회로 잡는 것이 가능하고, 이 저장소의 확립된 관례다.** 새 기법을 도입하는
것이 아니라 **있는 관례를 이 결함군에 적용하는 것이다.**

### 4-3. 세워야 할 게이트 — 무엇을 단언해야 세 번째가 마지막이 되는가

이 결함군은 **세 번 살아남았다**: `fx_muzzle`, `fx_arcane`, `GeneratedExplosionFrames`
(`impact-white-square.md:128`, `ExplosiveGimmick.cs:298-301`). 살아남은 기제는 두 개다 —
(가) 기존 테스트가 **선언된 키만** 순회했다, (나) `#if UNITY_EDITOR`가 에디터에서 성공했다.
따라서 게이트도 두 종류여야 한다.

**게이트 G1 — 런타임 자산 로드가 `#if UNITY_EDITOR` 안에 있으면 실패.** 소스 순회.
`AccessibleBlinkTests` 형태.

대상 = 런타임 어셈블리(`Assets/Scripts/`)의 `.cs` 전체를 순회하고, `#if UNITY_EDITOR` …
`#endif` 블록 안의 `AssetDatabase.LoadAssetAtPath`를 찾는다. **`Resources.Load`가 선행하는
자기복구 블록은 허용 목록으로 뺀다**(§4-4의 구분).

**현재 위반 전수 `[OBSERVED — grep 전수, 2026-08-14 기준]`:**

| 위치 | 자산 | 런타임 대체 경로 | 판정 |
|---|---|---|---|
| `GameManager.cs:956` | `Assets/Sprites/block_normal.png` | 없음 | **위반** |
| `UnitController.cs:1093` | `Assets/Prefabs/Arrow.prefab` | `:1095` null 폴백 있음 | **위반**(폴백이 절차 생성) |
| `UnitController.cs:1316` | `Assets/Prefabs/ExplosionEffect.prefab` | 없음 | **위반** |
| `SpriteAtlasPacker.cs:187` | `Assets/Sprites/{name}.png` | 없음 | **위반** |
| `GimmickSpriteLibrary.cs:57,67,72` | `Assets/Resources/Gimmicks/*` | `:52` `Resources.Load` 선행 | 자기복구 — §4-4 |

`ExplosiveGimmick`는 목록에서 **빠졌다** — 이번 수정으로 제거됐다 `[OBSERVED — 현재 소스에
해당 블록 없음, `:305`가 곧바로 null 검사]`.

**주의**: `LaunchManager:731`·`848`·`875`와 `MobileStorefront:132`는 `#if UNITY_EDITOR`이지만
**자산 로드가 아니다**(테스트 시뮬레이션 시임, 플랫폼 분기). 게이트가 이것을 잡으면 오탐이다 —
정규식은 `AssetDatabase.Load*`를 표적으로 해야 하고 `#if UNITY_EDITOR` 자체를 금지하면 안 된다.

**게이트 G2 — 자산 폴더를 순회한다(선언 키가 아니라).** 이것이 (가)를 닫는다.
`Assets/Resources/` 아래 스프라이트로 소비되는 모든 폴더에 대해 `textureType == 8`을 단언한다.
`CastleSkinResourceTests:162-166`의 임포터 조회를 재사용하면 된다.

**단언해야 할 것 — 정확히 이 형태여야 세 번째가 마지막이 된다** `[INFERENCE]`:
> **폴더를 순회하되, 오탐 3건을 이름이 아니라 이유로 면제한다.**

면제해야 하는 것 `[OBSERVED — ArtRequestSpec 레인 전수 스캔, 내가 대조 확인]`:
`Backgrounds/Background_Stage1..3`은 `textureType: 0`이 **옳다** — `GameManager:581`이
`Resources.Load<Texture2D>` 후 `Sprite.Create`를 한다. 면제 근거를 **소비 코드로** 적어야
한다. 이름 목록으로 면제하면 그 목록이 다시 §4-3의 (가)가 된다.

**그리고 G2는 폭발 6장만으로 끝나지 않는다 `[OBSERVED — 내가 직접 meta 확인]`:**

```
Assets/Resources/Effects/particles/
  particle_ash    textureType: 0  spriteMode: 0  alphaIsTransparency: 0   ← 깨짐
  particle_rain   textureType: 0  spriteMode: 0  alphaIsTransparency: 0   ← 깨짐
  particle_snow   textureType: 0  spriteMode: 0  alphaIsTransparency: 0   ← 깨짐
  particle_ember  textureType: 8  spriteMode: 1  alphaIsTransparency: 1
  particle_petal  textureType: 8  spriteMode: 1  alphaIsTransparency: 1
  particle_smoke  textureType: 8  spriteMode: 1  alphaIsTransparency: 1
```

이 셋을 로드하는 `EffectSpriteLibrary.LoadParticleSprite`에는 **에디터 복구가 없다**
`[OBSERVED — FrameAnimEffect.cs:64-71, `#if UNITY_EDITOR` 없음]`. 따라서 **에디터와 빌드
양쪽에서 죽는다(대칭 파손)** — `GimmickSpriteLibrary`와 다르다. **G2를 지금 세우면 이 3장이
즉시 빨간불이 된다.** 그것이 G2가 제대로 만들어졌다는 증거다.

**회의 중 G2가 실제로 만들어졌다 — 그리고 즉시 15장을 더 찾았다.** `[OBSERVED — 내가 대조 확인]`
`ResourceImportTests` 레인이 `Assets/Tests/EditMode/ResourceSpriteImportTests.cs`로 폴더 순회를
구현했다. 그 결과가 G2의 설계 근거를 그대로 확인해 준다:

| 발견 | 내 확인 결과 | 판정 |
|---|---|---|
| `Gimmicks/` 4장 (`gimmick_muzzle_flash`·`shell`·`wall_brick`·`wall_brick_cracked`) | **키는 선언돼 있다**(`GimmickSpriteLibrary.cs:22-25`), 그런데 **호출부는 0건**(`GimmickSpriteLibrary.<키>` grep 무결과) | **면제하지 말 것.** 소비자가 생기는 순간 빌드에서만 깨진다 |
| `Webtoon/panel-01..11` | **11장 전부 이미 `textureType: 8`/`spriteMode: 1`** | **오탐.** 그쪽 스캔이 stale이거나 `.provenance.json.meta`를 함께 센 것 |
| `Backgrounds/Background_Stage1..3` | `textureType: 0`이 **옳다** | **오탐 — 이유로 면제** |

**`.provenance.json.meta` 함정 `[OBSERVED]`**: `Assets/Resources/Webtoon/`에는 패널마다
`.jpg.meta`와 `.provenance.json.meta`가 쌍으로 있고 **후자에는 `textureType` 필드가 아예 없다.**
필드 부재를 0으로 읽는 순회는 그 파일들을 영구 실패로 보고한다. **G2는 이미지 확장자만 대상으로
해야 한다.**

**이것이 §4-3의 "이름이 아니라 이유로 면제하라"를 검증한다** — 이름 목록으로 오탐 3건을 빼면
그 목록이 다시 (가)가 되고, 필드 부재를 0으로 읽는 버그는 목록으로는 절대 안 잡힌다.

**게이트 G3 — 재질 블렌드 상태.** §2-2를 고정한다. 이것은 소스 순회가 아니라 **런타임 상태
단언**이고, EditMode에서 CLI로 돌 수 있다. 확장 지점이 이미 있다
`[OBSERVED — RuntimeReliabilityRegressionTests.cs:181-187]`
(`GetParticleMaterial_DefaultTexture_ReusesSharedMaterial`):
```csharp
var m = GameFeelVfx.GetParticleMaterial();
Assert.AreNotEqual(0f, m.GetFloat("_DstBlend"),
    "코드로 만든 URP 파티클 재질은 선언 기본값이 Blend One Zero(불투명)다. "
    + "그러면 알파가 버려지고, 기본 텍스처는 rgb가 상수 백색이므로 순백 사각형이 된다.");
```
**이 게이트가 §2-2를 "추론"에서 "확정"으로 바꾼다** — 그리고 통과/실패 어느 쪽이든 §2-5의
불확실성이 해소된다.

**게이트 G4 — 조준 기본값과 다이얼 정합.** §3을 고정한다.
- `aimPower` 기본값이 실측 도달 대역 안이라는 단언(`LaunchPowerCurveTests:175-182`가 씬 YAML을
  읽는 전례를 그대로 쓸 수 있다 — 씬이 값을 **재도입하지 않았는지**도 같이 검사할 것).
- `GetSeparatedAimVelocity()`와 `CalculateLaunchVelocity()`가 **같은 draw에서 같은 속도**를
  내는지. §3-(i)의 표(0.35에서 −2.278 차이)가 지금 실패하는 값이다.

### 4-4. 자기복구 블록은 별개로 다뤄야 한다

`GimmickSpriteLibrary.cs:52-70`은 구조가 다르다 `[OBSERVED]`: `Resources.Load<Sprite>`가
**먼저** 오고, 실패했을 때만 `#if UNITY_EDITOR` 안에서 임포터를 고쳐 재임포트한다.

이것은 G1의 위반이 아니다(런타임 경로가 존재한다). **그러나 더 나쁜 종류의 위험이다** —
에디터에서 자산을 **자동으로 고쳐 주므로 결함이 에디터에서 영구히 안 보인다.**
**이 블록이 가리고 있던 자산 4장** `[OBSERVED — meta 확인]`: `gimmick_muzzle_flash`,
`gimmick_shell`, `gimmick_wall_brick`, `gimmick_wall_brick_cracked`.

**두 레인이 이 4장을 놓고 다른 말을 했고 둘 다 맞았다** `[OBSERVED — 내가 양쪽 확인]`:
ArtRequestSpec은 *"소비자 0건"*, ResourceImportTests는 *"선언된 키"*라고 했다. 사실은
**선언은 있고**(`GimmickSpriteLibrary.cs:22-25`) **호출부는 0건**이다(grep 무결과). 둘이 서로
다른 층을 본 것이다.

**판정: 자기복구 블록을 지운다.** 우선순위 중. 이유: 그것이 하는 일은 **결함을 숨기는 것**이고,
G2가 있으면 복구가 하던 일(임포터 교정)을 **실패로 보고**하는 편이 낫다. 다만 호출부가 0건이므로
**급하지 않다** — 그리고 급하지 않다는 것이 면제 사유는 아니다. 누가 그 키를 쓰는 첫 커밋이
빌드에서만 깨지는 커밋이 된다.

---

## 5. 수정 순서

각 항목은 독립 검증 가능하다. 순서 근거는 **"측정이 가능해지는 순서"**다.

| # | 처방 | 근거 | 왜 이 순서 | 상태 |
|---|---|---|---|---|
| 1 | **G3 (재질 블렌드 단언)** | §4-3 | 코드 수정이 아니라 **측정**이다. §2-2가 실재하는지 즉시 확정하고 §2-5의 불확실성을 해소한다. **나머지 판정이 이 결과에 달려 있다.** | 미적용 |
| 2 | **2-A (재질 투명 설정)** | §2-6 | G3이 빨간불이면 이것이 D-B의 남은 절반이다. 임포터 수정만으로는 색 있는 **사각형**이 된다. | 미적용 |
| 3 | **3-E (`AimDefaultReachTests` 실재화)** | §3-(i) | 주석이 **존재하지 않는 테스트**를 참조한다. 0.84가 지금 아무것도 고정하지 않으므로 다음 커브 변경에 또 stale이 된다. | 미적용 |
| 4 | **3-D (자기 벽 구분 프리뷰)** | §3-(ii) | 전례가 있어 위험이 낮고, 남은 40.7%(낮은 각도)를 읽히게 하며, `readback-attribution` 결함도 같이 닫는다. | 미적용 |
| 5 | **G2 (폴더 순회 임포터 게이트)** | §4-3 | 회의 중 만들어졌고 15장을 더 찾았다. **오탐 처리(이유 기반 면제 + 이미지 확장자 한정)가 남았다.** | 부분 적용 |
| 6 | **임포터 3장 (날씨 파티클 ash·rain·snow)** | §4-3 | G2가 지적한 것 중 **값이 가장 큰 것** — 복구 경로가 없어 대칭 파손이고, 세 스테이지 날씨가 무연출이다. | 미적용 |
| 7 | **G1 (`#if UNITY_EDITOR` 소스 게이트)** | §4-3 | 위반 4건이 즉시 빨간불. 그 4건 수정과 짝지어 진행. | 미적용 |
| 8 | **3-A (다이얼 통일) + 3-B 재산출** | §3-(i) | **한 커밋이어야 한다.** 통일하면 0.84가 속도 15.18 → 16.04로 바뀌어 착지표를 다시 내야 한다. | 미적용 |
| 9 | **2-C, §2-4 (순백 틴트 3곳), §4-4 (자기복구 제거)** | §1-5, §2-4, §4-4 | 정리. 별개 결함이고 각각 독립. | 미적용 |
| 10 | **2-B (셰이더 빌드 고정)** | §2-6 | G3 결과가 §2-5 추론을 확정하면 필수, 반증하면 불필요. | 조건부 |
| — | ~~3-B (기본값 0.55 → 0.84)~~ | §3-(i) | 구 기본값이 **자기 성채에 떨어지고 있었다.** D-A 신고 절반의 확정 원인. | **회의 중 적용됨** |
| — | ~~임포터 6장 (폭발 프레임)~~ | §1-2 | D-B 확정 원인. | **회의 중 적용됨** |
| — | ~~`ExplosionFrames` 런타임 경로~~ | §1-5 | 폴백이 실제 아트에 닿게 됐다. | **회의 중 적용됨** |

**인테이크의 main_question에 대한 답** — *"폴백이 순백으로 보이는 것을 고치는 것과, 폴백이
발동하지 않게 하는 것 중 무엇을 먼저 하는가"*:

**질문의 전제가 틀렸다.** 폴백은 발동하지 않았다(§1-1: 프리팹은 직렬화 참조로 로드된다).
순백은 폴백이 아니라 **정상 경로 안에서** 나왔다 — 임포터가 로드를 비웠고, configurator가
자기 null 분기로 떨어졌다. 그리고 **그 순백이 네모인 이유는 재질이 불투명이기 때문이고,
그것은 아직 안 고쳐졌다.** 따라서 우선순위는 "폴백 어느 쪽"이 아니라 **재질 블렌드 상태**다.

---

## 6. 인테이크·상위 진단에 대한 반박 (근거 포함)

이 저장소가 이 사이클에 21번 배운 것 — *가정 위에서 계산한 결론은 뒤집힌다.* 아래는 내가
재확인하며 뒤집은 것이다.

**6-1. "빌드에서 프리팹이 항상 null이다" — 반증.** `[OBSERVED]`
`ExplosiveBarrel.prefab:176`의 guid `5fcf2c0c…`가 `ExplosionEffect.prefab.meta:2`와 일치하고,
fileID `3580900068180056100`이 프리팹 3행의 루트와 일치한다. 직렬화 참조는 빌드 의존성이다.
→ **§1-1. 세 선택지 비교 자체가 불필요했다.**

**6-2. "튜닝 기본값 45°·당김 86%가 x=38.5에 떨어져 31유닛 지나친다" — 반증.** `[OBSERVED]`
**방향이 정반대였다 — 지나치는 것이 아니라 자기 성채에 떨어지고 있었다.**
(a) 당시 출하 기본값은 `aimPower = 0.55`였고 씬에 직렬화돼 있지 않았다(전수 grep 0건).
**86%는 기본값이 아니다.**
(b) `draw`와 `aimPower`는 **단위가 다르다**(§3-(i)) — 두 숫자를 같은 다이얼로 취급한 것이
혼동의 뿌리다. 0.55는 드래그 39.3% 상당이다.
(c) x=38.5는 `aim-space-reachability.md:29-35`가 **판정에 쓰지 말라고 명시한** 오프라인
모델값이고, 그 모델은 이후 **자기 자신도 정정됐다** — 정정판(`trajectory-blockers.md`)이
초판이 `MaxSpeed`를 25.2로 쓴 것과 착지 판정이 발사점을 반환한 것을 고쳤다.
(d) **정정 후 실제 값**: `aimPower = 0.55` → 속도 10.975 → 착지 **x = −4.7**, 자기 성채
구간(x = −7..−4)이다. 적 성채(x ≥ 4)까지 **8.74u 부족**했다.
(e) 인테이크의 조합 분포표도 초판 값이다. 정정판: 적 성채 **6.3%**(18조합), 자기 성벽
**40.7%**(116), 지면 40.0%(114), Patrol 9.5%(27), MiniTower 3.5%(10).

→ **인테이크는 결함의 존재를 맞혔고 부호를 틀렸다.** 그리고 그 부호가 처방을 반대로
만들었다 — "파워를 낮춰야 한다"가 아니라 **올려야** 했다.

**6-3. "폴백의 파티클 재질에 텍스처가 없어 순백이 된다" — 폭발 경로에 대해 반증.** `[OBSERVED]`
`ExplosiveGimmick`는 `particle_ember`를 로드해 넘긴다(`:359-360`), 그리고 `particle_ember`는
`textureType: 8`로 정상 임포트된다. 무인자 오버로드에 실제로 상시 도달하는 것은
**`WindVfxManager:63`**이다(§1-5) — 폭발이 아니다.

**6-4. "에디터 100% 정상 / 빌드 100% 깨짐" — 반증(내 자신의 중간 주장도 포함).** `[OBSERVED]`
임포터 파손은 **대칭**이다(양쪽 다 빈 배열). 그래서 사용자의 *"아직도 흰색"*이 에디터에서도
재현된다. 그리고 §2-5의 `Shader.Find` 분기는 **반대 방향** 비대칭일 가능성이 있다 —
빌드에서 `Sprites/Default`로 폴백하면 빌드가 오히려 옳게 보인다. `[INFERENCE]`
**나는 이 회의에서 반대 방향 비대칭을 ArtRequestSpec에게 한 번 잘못 주장했고, QA의 직렬화
증거로 철회했다.** 기록으로 남긴다.

**6-5. `DynamicBattlefield.cs:310` 주석 오류 — 확인.** `[OBSERVED]` 인테이크가 옳다.
다만 이것은 D-A의 원인이 아니다(필드 장애물 9.8%는 부수적이라는 인테이크 판단에 동의).

---

## 7. 이 문서가 주장하지 않는 것

- **§2-2를 실행으로 확정하지 않았다.** 셰이더 소스에서 도출한 `[INFERENCE]`다. G3이 그것을
  확정하거나 반증한다 — 그것이 G3을 1순위로 둔 이유다.
- **빌드에서 어느 셰이더가 잡히는지 모른다** (§2-5, `[확인 불가]`). 확인 방법 2가지를 적었다.
- **사용자가 본 흰색이 폭발인지 붕괴 먼지(`CollapseDust`, §2-4)인지 확정하지 않았다.**
  QA 재현이 두 경로를 갈라 캡처해야 한다.
- **어떤 수치도 새로 측정하지 않았다.** 채도값은 ArtRequestSpec 레인 인용이고, 도달성 수치는
  `aim-space-reachability.md` 인용이다. 내가 직접 확인한 것은 **코드·meta·YAML·셰이더 소스**이고,
  §3-(i)의 속도 환산표는 인용한 두 식으로 산출했다.
- **테스트를 돌리지 않았고 코드를 고치지 않았다.** 지시대로 판정과 처방만이다.
