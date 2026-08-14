# 아트 제작 요청 — 충돌 이펙트 · 발사체 이미지

- run-id: 20260809-castle-war-stage1 (cycle 2)
- owner: design lane
- date: 2026-08-14
- 발단: 사용자 요청 2건
  1. *"오브젝트랑 포탄이 부딪혔을 때 흰색으로 나오는데 이 부분 이미지 VFX 없으면 만들어야
     된다고 요청해 줘"*
  2. *"포탄을 좀 더 알아보기 좋게 이미지 다른 걸로 대체해 줘"*

> **이 문서는 요청서다. 아무 아트도 생성하지 않았다.** CLAUDE.md §3에 따라 생성물은
> `design/concept/`에 먼저 떨어지고 감사 후에만 `Assets/`로 승격된다.

---

## 0. 요약 — 먼저 확인할 것이 있다

| # | 항목 | 판정 |
|---|---|---|
| 1 | `fx_shatter` 6프레임이 **코드에서 참조되지 않음** | **확정 결함.** 새로 만들기 전에 이것부터 확인 |
| 2 | `fx_frost` 선언됐으나 **디스크에 0프레임** | **확정 결함.** Stage3 서리 벤트가 조용히 무연출 |
| 3 | `fx_spark_000`이 182×182, 나머지 3프레임은 256×256 | **확정 결함.** 재생 중 첫 프레임만 크기가 튄다 |
| 4 | 흰색으로 보이는 것의 정체 | **미확정.** 후보 4개, §2 |
| 5 | 발사체 전용 아트 | **필요하나 순서가 있다.** §3 |

**이번 세션의 반복된 패턴이 "필요한 것이 이미 있는데 연결만 안 됐다"였으므로, 1번을 먼저
열어보길 권한다.** 이름과 프레임 수(6장)로 보아 파괴 순간용으로 만들어진 아트일 가능성이 높다.

---

## 1. 현재 이펙트 아트 전수 (실측)

`Assets/Resources/Effects/` 디스크 실측 `[OBSERVED]`:

| 키 | 프레임 | 코드 선언 | 상태 |
|---|---|---|---|
| `fx_spark` | 4 | O | 사용 중 (블록 피해) — 단 §0-3 크기 불일치 |
| `fx_dust` | 4 | O | 사용 중 (붕괴) |
| `fx_sparkle` | 4 | O | 사용 중 (룬·게이트) |
| `fx_spawn` | 4 | O | 사용 중 |
| `fx_eruption` | 5 | O | 사용 중 |
| `fx_petals` | 4 | O | 사용 중 |
| `fx_arcane` | 4 | O | 사용 중 |
| `fx_muzzle` | 6 | O | 사용 중 (대포 포구) |
| **`fx_shatter`** | **6** | **X** | **아트만 있고 아무도 안 쓴다** |
| **`fx_frost`** | **0** | O | **선언만 있고 파일이 없다** |
| `particles/` | 6 | O | ember·smoke·petal·rain·snow·ash 각 1장 |

`fx_spark_000.png`(182×182)와 `fx_spark_001~003`(256×256) — `FrameAnimEffect`는 프레임을
순서대로 같은 `SpriteRenderer`에 갈아 끼우므로, 첫 프레임만 작게 찍히고 두 번째에서 튄다.

`fx_frost`는 `EffectSpriteLibrary.Frost`로 선언돼 있으나 파일이 없어
`FrameAnimEffect.Spawn`이 `null`을 돌려주고 **조용히 아무것도 안 그린다**(`:94` soft-fail).
Stage3 서리 벤트가 연출 없이 작동한다는 뜻이다.

---

## 2. "흰색"의 정체 — 후보 4개, 확정 못 함

정적 분석만으로는 사용자가 본 흰색이 어느 경로인지 **확정할 수 없다.** 후보를 근거와 함께
남긴다. 확정하려면 충돌 순간을 캡처해 픽셀을 재야 한다(§5).

| # | 후보 | 근거 | 등급 |
|---|---|---|---|
| A | **절차적 기본 파티클** — 흰색 방사 그라데이션 | `GameFeelVfx.GetDefaultParticleTexture()`가 `Color(1,1,1,alpha)`로 32×32를 굽고, `GetParticleMaterial(null)`이 이걸 쓴다. 즉 **스프라이트를 못 찾으면 흰 점들이 뿌려진다** | `[OBSERVED — 코드]` 가장 유력 |
| B | **`fx_spark`에 흰색 틴트** | `DestructibleBlock.cs:229` — `FrameAnimEffect.Spawn(Spark, …, Color.white, 20f)`. 아트는 노란 별이지만 작은 크기에서 중심부가 흰색으로 뭉칠 수 있다 | `[INFERENCE]` |
| C | **블록 자체 스프라이트를 파티클 텍스처로 사용** | `DestructibleBlock.cs:212`가 `spriteRenderer.sprite`를 넘기고 `SpawnImpactBurstCore`가 `GetParticleMaterial(sprite.texture)`로 **텍스처 전체**를 머티리얼에 건다. 지면 타일은 `GenerateGroundTexture`로 구운 큰 텍스처라 UV 불일치 시 사각형이 보일 수 있다 | `[INFERENCE — 미확증]` |
| D | 충격파 링 | `GetRingSprite()`가 `Color.white`로 48×48 링을 굽는다. 단 링 형태라 "네모"로 읽히진 않는다 | `[OBSERVED — 코드]` 가능성 낮음 |

> **A가 유력한 이유**: A는 "아트가 없을 때"의 폴백이고, 이 프로젝트는 방금 `fx_frost`가
> 0프레임인 것을 발견했다. 같은 종류의 침묵이 다른 키에서도 일어나고 있을 수 있다.

---

## 3. 요청 1 — 충돌(피격) 이펙트

### 3-A. 먼저: `fx_shatter`를 연결해 보라 (아트 제작 불필요)

```
Assets/Resources/Effects/fx_shatter/fx_shatter_000..005.png   (6프레임, 이미 존재)
```

`EffectSpriteLibrary`에 키가 없어 아무도 부르지 않는다. 추가할 코드는 한 줄이다:

```csharp
public const string Shatter = "fx_shatter";
```

그리고 `DestructibleBlock.DestroyBlock`의 `Dust` 옆이나 `TakeDamage`의 `Spark` 자리에서
호출해 보면 된다. **이미 만들어진 아트일 가능성이 높으므로 새 제작 전에 확인이 먼저다.**

### 3-B. 그래도 새로 만들어야 한다면 — 명세

| 항목 | 값 |
|---|---|
| 키 | `fx_impact` |
| 경로 | `Assets/Resources/Effects/fx_impact/` |
| 파일명 | `fx_impact_000.png` … `fx_impact_005.png` (4~6장) |
| **크기** | **모든 프레임 동일하게 256×256** — `fx_spark`의 불일치(§0-3)를 반복하지 말 것 |
| 배경 | **알파 투명 필수.** 불투명 배경이 곧 "흰 네모"로 보이는 경로다 |
| **색** | **무채색/흰색 실루엣.** 코드가 `SpriteRenderer.color`에 팀색·피해색을 **곱한다** — 색이 든 아트를 넣으면 곱셈으로 탁해진다 |
| 형태 | 돌·석재 파편이 방사형으로 터지는 순간. 회전 불변(코드가 랜덤 회전을 주지 않지만 위치를 흔든다) |
| 프레임 진행 | 1프레임 최대 밀도 → 확산 → 소산. 히트스톱이 **가장 밝은 프레임에서 멈추므로** 첫 프레임이 가장 강해야 한다(`FrameAnimEffect` 주석) |
| `.meta` | **필수.** 없으면 `textureType: Default`로 임포트되어 `Resources.LoadAll<Sprite>`가 빈 배열을 돌려주고 조용히 안 그려진다(작업 #17에서 실제로 겪은 결함) |
| provenance | `fx_impact_000.png.provenance.json` — 스키마는 `Gimmicks/castle_keep_s0.png.provenance.json`과 동일 |

#### 프롬프트 초안 (CLAUDE.md §3 우선순위: Codex CLI → Higgsfield)

```
2D game VFX sprite sheet frame, single effect on a fully transparent background.
A stone impact burst: angular granite shards and dust flung radially outward from
a bright hot centre. Pure white and light grey only, no colour, no gradient
background, no text. Crisp pixel-art edges readable at 64 pixels.
Frame 1 of 6: maximum density, shards still close to the centre.
Dark-fantasy medieval siege game.
```

프레임 2~6은 같은 구도에서 파편을 밖으로 밀고 알파를 낮춘 것 — **개별 생성하지 말 것.**
작업 #19가 `fx_muzzle`에서 같은 이유로 프레임 간 정합 드리프트를 피해 파생 방식을 택했다.

### 3-C. 하지 말 것 (근거가 반대)

| 금지 | 근거 |
|---|---|
| **전체화면 흰색 플래시** | 이 조사에서 **유일하게 실제 인체 피해가 기록된 레이어.** 1997 포켓몬 포리곤 사건 *"Over 600 people, mostly children, were taken to hospitals"*, 방아쇠 장면이 *"an attack, resulting in an explosion… rapid flashing lights that fill the screen"* — 우리 도메인과 동일 |
| 1초에 3회 초과 명멸 | WCAG 2.2 SC 2.3.1, GAG "화면 25% 이상을 덮는 초당 3회 초과 플래시" |
| 다중 동심원 링 | GAG의 **radial 반복 패턴** 조항(동적 5줄 이상) |
| 흔들림 추가 | Nijman 회고 — *"had to put like an option in nuclear [Throne] to disable the screen Shake because some people were getting really nauseous"* |

---

## 4. 요청 2 — 발사체 이미지 (알아보기 좋게)

### 4-A. 현재 상태 (실측)

발사체는 **유닛 스프라이트를 그대로 재사용**한다 — `Knight/Launch/`, `Archer/Launch/` 각
5프레임, 화약통은 `ExplosiveGimmick`이 자기 스프라이트를 소유. 별도 "포탄" 아트는 없다
`[OBSERVED]`.

| 항목 | 값 | 함의 |
|---|---|---|
| 렌더 크기 | **0.93u** | 카메라 폭 45u / 1365px → **약 28px** |
| 콜라이더 | **5.28u** (하프폭 2.64) | 렌더의 **5.7배** — 작업 #47 실측 |
| 배경 | 밝은 초원·하늘·구름 | 28px 캐릭터가 섞여 사라진다 |

28px는 이번 세션에서 궤적 아크가 하늘 대비 **1.13:1**로 사실상 안 보였던 것과 같은 종류의
가독성 문제다. 그때 해법은 알파가 아니라 **다크 카싱**이었다.

### 4-B. 순서 주의 — 콜라이더 결함이 먼저다

> **아트를 지금 만들면 다시 만들 위험이 있다.** 렌더 0.93u vs 콜라이더 5.28u는 **5.7배
> 불일치**이고, 이 불일치 자체가 결함 후보다(`qa/aim-space-reachability.md` §5-2). 그 값이
> 바뀌면 발사체의 물리적 크기가 바뀌고, 아트가 맞춰야 할 크기도 함께 바뀐다.
>
> **권고: 콜라이더/렌더 정합을 먼저 확정하고, 그 다음 아트 크기를 정한다.**

### 4-C. 명세 (콜라이더 확정 후)

| 항목 | 값 |
|---|---|
| 키 | `projectile_knight` / `projectile_archer` / `projectile_barrel` 신설, 또는 기존 `Launch/` 프레임 교체 |
| 크기 | 128×128 이상, 정사각·중심 정렬 |
| 배경 | 알파 투명 |
| **대비** | **어두운 1~2px 외곽선 필수.** 밝은 초원·하늘 위에서 실루엣이 서야 한다 — 궤적 아크와 같은 문제, 같은 해법 |
| 색 | 팀색 틴트가 곱해지므로 무채색 기조 + 어두운 외곽선 |
| 형태 | 비행 중 **120°/s로 회전**하므로(`UnitSpriteAnimator.launchSpin`) 화살표처럼 방향성이 강한 형태는 회전 시 혼란. **회전해도 읽히는 형태** |
| 정체성 | 3종이 실루엣만으로 구분돼야 한다 — 기사(뭉친 덩어리), 궁수(길쭉), 화약통(원통+심지) |

#### 프롬프트 초안

```
2D game sprite, single object on a fully transparent background.
A medieval knight curled into a compact ball mid-flight as a siege projectile:
plate armour tucked, shield hugged to the chest, one dark bold outline around the
whole silhouette so it reads against a bright grass-and-sky background.
Greyscale only, no team colour, no background. Rotationally readable — the pose
must still read when spun. Crisp pixel-art edges legible at 48 pixels.
Dark-fantasy medieval siege game.
```

### 4-D. 아트 없이 지금 가능한 개선 (코드만)

아트 제작과 **독립적으로** 가독성을 올릴 수 있는 것들. 콜라이더 결함 수정 후 검토:

1. **비행 중 렌더 스케일 상향** — 콜라이더와 분리돼 있으므로 판정 불변
   (작업 #23이 `StageActorVisualScale`로 같은 분리를 이미 했다)
2. **실루엣 카싱 한 겹** — 궤적 아크에 쓴 것과 같은 기법
3. **라이브 트레일 굵기 상향** — 현재 0.09~0.14u

---

## 5. 확정하지 못한 것

1. **흰색의 정체 미확정** (§2). 확정 절차: 충돌 순간 `RenderTexture` 캡처 →
   해당 픽셀 색·형태 측정 → `GetDefaultParticleTexture` 폴백이 실제로 타는지 로그 계측.
   이번 세션은 요청이 "문서로 남겨 달라"였으므로 캡처는 하지 않았다.
2. **`fx_shatter`가 충돌용인지 미확정.** 파일명과 프레임 수로 추정한 것이고, 실제로 열어
   무엇을 그린 아트인지 확인하지 않았다.
3. **발사체 콜라이더 5.28u는 작업 #47의 인용값**이고 이 세션이 재측정하지 않았다.

---

## 6. 요청 목록 (우선순위)

| 순위 | 항목 | 새 아트 필요? |
|---|---|---|
| 1 | `fx_shatter` 연결 확인 (§3-A) | **아니다 — 이미 있다** |
| 2 | `fx_frost` 4프레임 제작 (§0-2) | 필요 |
| 3 | `fx_spark_000` 256×256으로 재생성 (§0-3) | 필요(1장) |
| 4 | `fx_impact` 4~6프레임 (§3-B) | **조건부** — 1번 확인 후 |
| 5 | 발사체 3종 (§4-C) | **필요하나 콜라이더 확정 후** |
