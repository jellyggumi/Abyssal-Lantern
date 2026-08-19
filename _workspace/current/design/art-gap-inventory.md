# 아트 간극 인벤토리 — 무엇이 게임처럼 안 보이는가

- run-id: 20260809-castle-war-stage1 (cycle 3)
- date: 2026-08-18
- owner: game-designer 레인
- 발단: 사용자 요청 — *"그래픽 추가해야 될 것들 있는지 봐주고, 좀 더 게임스럽게 만들기
  위해서 필요한 그래픽들 전부 정리해서 문서로 남겨"*
- 방법: **코드 0줄 수정.** `Resources` 전수 계수, PNG 채도 실측(디코드 후 알파>32 픽셀
  샘플링), `Sprite.Create` 호출부 16곳 전수 분류
- next_public_beat: WebGL build linked from https://jellyggumi.github.io/ menu

---

## 0. 요약 — 간극은 "아트가 없다"가 아니다

**아트는 249장 있다.** 그리고 오디오는 6/6 완비다. 제가 처음 의심한 것 세 가지가 전부
틀렸다:

| 의심 | 실측 | 판정 |
|---|---|---|
| 플레이스홀더 7종이 남아 있다 | `ui_ph_*` **0건** — `9bd3494e`·`dbcfd78f`가 의도적으로 삭제 | **틀림** |
| `GimmickSpriteLibrary` 키 10건에 아트가 없다 | 그 10건은 `HiggsfieldSpriteLibrary` 키이고 아트는 `Higgsfield/UI`·`/VFX` 하위 폴더에 있다 | **제 조사 오류** |
| `CastleSkin`이 무채색이라 결함이다 | 채도 0.000~0.147은 **의도된 틴트 소스**다 — `DestructibleBlock.cs:85`가 `blockData.blockColor`로 칠하고 Wood 0.75 / Iron 0.40 / Stone 0.00(의도적 회색) | **틀림** |

**진짜 간극은 절차 생성이 그리는 평면 도형이다.** `Sprite.Create` 16곳 중 **11곳이 아트
경로 없이** 코드가 픽셀을 찍는다. 그중 화면 최대 면적이 지면이다.

---

## 1. 있는 것 (재작업 불필요)

| 자산군 | 수량 | 해상도 | 채도 실측 | 상태 |
|---|---:|---|---|---|
| `Backgrounds/` | 3 | 1774×887 ~ 1693×929 | 0.643 / 0.667 / 0.694 | 양호 |
| `GeneratedUnitFrames/` | 64 | — | — | **3액터 × 4상태 × 4~6프레임.** Knight/Archer/Bomber × Idle·Attack·Walk·Launch |
| `GeneratedExplosionFrames/` | 6 | 512×512 | 0.313~0.954 | 임포터 수정 완료(작업 #67) |
| `Higgsfield/UI/` | 6 | 512×512 | 0.319~0.713 | Knight·Archer·Cannon·Barrel·Ram·Trap 초상 |
| `Higgsfield/VFX/` | 4 | 512×512 | 0.320~0.843 | Impact·Wind·CoreCrack·CollapseDust |
| `CastleSkin/` | 12 | 512×512 | 0.000~0.147 | **무채색이 정답** — 런타임 틴트 소스 |
| `Gimmicks/` | 88 | 혼재 | — | 선언 키 38개 전부 충족 |
| `Webtoon/` | 11 | — | — | 임포터 수정 완료(작업 #67) |
| `IntroAnim/` | 6 | — | — | 콜드오픈 |
| `Audio/` | 6 | — | — | BGM 3(battle-loop·victory·defeat) + SFX 3(impact·launch·combo). **코드가 요구하는 6경로 전부 존재** |

---

## 2. 우선순위 1 — 지면 (화면 최대 면적이 평면 3색 밴드)

**위치**: `GameManager.cs:1628-1730` `GenerateGroundTexture()`
**소비**: `:1544` → `:1585` 41×5 = **205 타일로 슬라이스**

### 무엇을 그리는가

```csharp
Color32 grassColor = new Color32(76, 154, 42, 255);
Color32 dirtColor  = new Color32(139, 90, 43, 255);
Color32 stoneColor = new Color32(90, 90, 90, 255);
...
byte noise = (byte)Random.Range(-10, 10);   // 픽셀당 ±10
```

**세 개의 평면 색 밴드 + 픽셀당 ±10 노이즈.** 그것이 전부다.

### 그리고 주석이 없는 기능을 약속한다

`:1652-1662`:

```csharp
// The organic sine-wave boundaries only vary with x, ...
float[] grassBoundaryByColumn = new float[width];
float[] stoneBoundaryByColumn = new float[width];
for (int x = 0; x < width; x++)
{
    grassBoundaryByColumn[x] = height * 0.8f;   // 상수
    stoneBoundaryByColumn[x] = height * 0.4f;   // 상수
}
```

주석은 **"organic sine-wave boundaries"** 를 말하는데 두 배열이 **전 컬럼에 같은 상수**를
받는다. `git log -S"grassBoundaryByColumn"`이 답을 준다 — 그 배열은 **최초 임포트 커밋
`b639788c`부터 상수**이고 이 저장소에 sin 계산이 있던 커밋은 없다. 그러므로 **"최적화가
곡선을 지웠다"는 제 첫 추측은 틀렸다.** 주석과 최적화 노트("~10M → ~13K trig calls")가
**임포트 전 프로젝트에서 함께 넘어온 것**이고 곡선은 여기 온 적이 없다 — 회귀가 아니라
**처음부터 미구현**이다.

### 필요한 아트

| # | 자산 | 사양 | 왜 |
|---|---|---|---|
| A-1 | `Ground/ground_tile_grass.png` | 128×128, 타일링 이음 없음(seamless), 채도 ≥0.30 | 205타일 중 상단 1행 |
| A-2 | `Ground/ground_tile_dirt.png` | 128×128, seamless | 중간 대역 |
| A-3 | `Ground/ground_tile_stone.png` | 128×128, seamless | 하단 대역 |
| A-4 | `Ground/ground_edge_grass.png` | 128×128, 위쪽만 풀·아래 흙 전이 | **경계가 직선인 것이 평면감의 주범** |
| A-5 | `Ground/ground_variant_a/b/c.png` | 128×128 각 3장 | 205타일이 전부 같으면 반복이 보인다 — 무작위 3종 순환 |

**틴트 계약**: 타일은 자기 색을 갖는다(`:1587-1589`가 이미 `blockColor`를 흰색으로
리셋한다 — 지면 슬라이스는 자기 색을 쓴다는 뜻). 그러므로 **무채색으로 만들지 말 것.**

---

## 3. 우선순위 2 — 매 턴 보이는 절차 도형 4종

### B-1 발사점 표시기 + 탄착 마커

**위치**: `LaunchManager.cs:166-183` `CreateCircleSprite()`
**소비**: `:231`(발사점, 반경 0.55, 청색 0.35/0.9/1.0) · `:300`(탄착, 반경 0.22, 적색)

32×32 텍스처에 `dist < center ? color : clear` — **안티에일리어싱 없는 하드 엣지 원**이다.
그리고 이 두 개가 **매 턴 조준 내내 화면에 있다.**

| # | 자산 | 사양 |
|---|---|---|
| B-1a | `Gimmicks/ui_launch_origin.png` | 128×128, 발사 지점 링(내부 투명), 청색 계열, 부드러운 외곽 |
| B-1b | `Gimmicks/ui_impact_marker.png` | 128×128, 착탄 조준 표식. **아군 성벽 자가 피해 시 색이 반전**되므로 무채색 + 런타임 틴트가 낫다(`LaunchManager.cs:93` `SelfHitTrajectoryColor` 참조) |

### B-2 궤적선

**위치**: `LaunchManager.cs:553` `new Material(Shader.Find("Sprites/Default"))`
**색**: `:555-556` 흰색 0.9 → 하늘색 0.25 선형 보간

`LineRenderer`에 텍스처가 없다 — **단색 그라디언트 선**이다.

| # | 자산 | 사양 |
|---|---|---|
| B-2 | `Effects/trajectory_dash.png` | 64×16, 가로 반복 대시 패턴, 알파 있음. `LineRenderer`의 `textureMode = Tile`과 함께 쓰면 궤적이 **흐르는 점선**이 된다 |

### B-3 배치 고스트

**위치**: `DeploymentController.cs:551-562`
24×24에 `edge ? Color.white : white*0.25` — **흰 사각 윤곽선**이다.

| # | 자산 | 사양 |
|---|---|---|
| B-3 | `Gimmicks/ui_deploy_ghost.png` | 128×128, 9-slice 가능한 배치 슬롯 프레임(코너 12px). 무채색 + 틴트 |

### B-4 파편

**위치**: `DebrisSystem.cs:117-168` — 원형 알파 감쇠 흰 원, 블록 색으로 틴트

블록이 부서질 때마다 나오므로 **가장 자주 보이는 VFX**다. 원이라서 벽돌 파편으로 안 읽힌다.

| # | 자산 | 사양 |
|---|---|---|
| B-4 | `Effects/debris_chunk_01~04.png` | 각 64×64, **불규칙 다각형** 4종. 무채색(틴트가 재질 색을 실음) |

---

## 4. 우선순위 3 — 있으면 게임이 두꺼워지는 것

### C-1 충격파 링

**위치**: `GameFeelVfx.cs:423-445` — 32px 링, `d <= outer && d >= inner ? white : clear`

아트 폴백 경로가 있으나(`GetParticleMaterial`) **링 자체는 절차 전용**이다.

| # | 자산 | 사양 |
|---|---|---|
| C-1 | `Effects/fx_shockwave_ring.png` | 256×256, 중심 투명, 외곽 부드러운 감쇠, 무채색 |

### C-2 균열 오버레이

**위치**: `GameManager.cs:1733-1843` `CreateCrackedSlice()` — 지면 텍스처와 균열 스프라이트를
CPU에서 블렌드한다. 균열 스프라이트 자체는 `blockData.crackedSprite`(에셋)이므로 **아트가
있다** — 다만 지면 타일이 A-1~A-5로 교체되면 **이 블렌드가 재검증돼야 한다**(`:1743`
`GetPixels`가 읽기 가능 텍스처를 요구한다).

### C-3 인트로 엠버

**위치**: `IntroScreenController.cs:555-572` — 16px 방사 감쇠

| # | 자산 | 사양 |
|---|---|---|
| C-3 | `Effects/particles/particle_spark.png` | 64×64, 불꽃 파편. 무채색 |

### C-4 만화 말풍선

**위치**: `WebtoonPrologueController.cs:681-700` — 16px 테두리 사각형, 9-slice(4,4,4,4)

프롤로그 11장에 얹히는 것이므로 **웹툰 톤과 어긋나면 눈에 띈다.**

| # | 자산 | 사양 |
|---|---|---|
| C-4 | `Webtoon/ui_speech_bubble.png` | 256×256, 9-slice 경계 32px, 손그림 느낌 테두리 |

---

## 5. 우선순위 4 — 화면에 없는 것 (신규 자산)

조사 중 **코드가 요구하지 않지만 게임 느낌에 기여할 것**으로 식별한 것. 이것들은
**아트만으로 되지 않고 코드가 필요하다** — 그래서 별도로 묶는다.

| # | 자산 | 왜 | 선행 코드 |
|---|---|---|---|
| D-1 | 성벽 파괴 단계별 스킨 | 지금은 균열 2단계뿐이다. 3~4단계면 공성 진행이 읽힌다 | `DestructibleBlock` 상태 확장 |
| D-2 | 적 진영 색 변형 | `CastleSkin` 12장이 양 진영 공용이다. 틴트만으로 갈리므로 **실루엣이 같다** | 없음(틴트 값만) |
| D-3 | 승리/패배 전면 일러스트 | 결과 화면이 텍스트다 | `resultText` 주변 |
| D-4 | 스테이지 전환 카드 | `ui_stage1~3_card` 있으나 전환 연출 없음 | `StageInterlude` |

**D-1~D-4는 이 문서가 요청하지 않는다** — 우선순위 1~3이 끝나기 전에 손대면 범위가 늘어난다.

---

## 6. 공통 사양 (모든 요청 자산)

| 항목 | 값 | 근거 |
|---|---|---|
| 임포터 | `textureType: 8` (Sprite), `spriteMode: 1`, `alphaIsTransparency: 1` | 작업 #67이 이것으로 24장을 고쳤다. **`textureType: 0`은 `LoadAll<Sprite>`에 빈 배열을 준다** |
| 배치 | `Assets/Resources/{폴더}/` | `Resources` 밖은 런타임에 닿지 않는다 — `#if UNITY_EDITOR AssetDatabase`는 빌드에서 컴파일 제외 |
| 픽셀/유닛 | 100 (Resources 기본) | `CannonController.cs:344-347` — 플레이스홀더는 32였고 코드가 정규화한다 |
| 채도 | **무채색(≤0.15)은 틴트 소스일 때만** | `CastleSkin`이 그 예다. 틴트가 없는 자산이 무채색이면 흰 폭발과 같은 결함이 된다 |
| 검증 | 새 자산은 `ResourceSpriteImportTests`가 자동 검사 | 폴더를 순회하므로 **추가만 하면 검사 대상이 된다** |

---

## 7. 이 문서가 주장하지 않는 것

- **"지금 못생겼다"고 판정하지 않는다.** 채도와 절차 여부는 측정값이고, 미적 판정은
  사람 세션(G4 몰입 채점)이 소유한다. 그 세션은 **미실시**다(`gate-measurements.md`).
- **우선순위가 측정에서 나오지 않았다.** 화면 면적(지면 205타일)과 노출 빈도(매 턴 조준)를
  근거로 삼았고 그 둘은 실측이지만, **"면적이 크면 먼저 고쳐야 한다"는 설계 판단**이다.
- **A-1~A-5 교체가 성능에 어떤 영향인지 모른다.** 지금은 텍스처 1장을 205번 슬라이스한다.
  타일 5종으로 바꾸면 아틀라스 구성이 달라지고, `SpriteAtlasPacker`가 512~4096으로
  클램프한다(`:93`). **재측정 필요.**
- **오디오는 6/6이지만 충분한지는 판정하지 않았다.** 코드가 요구하는 경로가 다 찼다는
  것만 확인했다. 발사·충격·콤보 3종 SFX가 공성 게임에 충분한지는 별개 질문이다.
- **`GenerateGroundTexture`의 sine 곡선이 왜 상수가 됐는지 확인하지 않았다.** 주석과 코드가
  어긋나는 것은 실측이고, 원인은 `[INFERENCE]`다.
