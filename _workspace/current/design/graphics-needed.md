# 필요한 그래픽 목록 — 한 장으로

- run-id: 20260809-castle-war-stage1 (cycle 2)
- owner: design lane
- date: 2026-08-14
- 용도: *"필요한 그래픽은 없으면 따로 필요한 그래픽이라고 문서 남겨주고, 어떤건지 알려주면 돼"*

상세 명세는 `design/impact-vfx-and-projectile-art-request.md`. **이 문서는 목록과 판정만
담는다** — 무엇이 정말 없고, 무엇이 이미 있는데 안 쓰이고 있는지.

---

## A. 지금 당장 필요한 것 — 2건

| # | 파일 | 크기 | 무엇인가 | 왜 필요한가 |
|---|---|---|---|---|
| **A1** | `Assets/Resources/Effects/fx_frost/fx_frost_000~003.png` (4장) | **256×256 동일** | 서리 분출 이펙트 — 얼음 결정·냉기가 위로 뿜는 4프레임 | **선언은 있는데 파일이 0장이다.** `EffectSpriteLibrary.Frost`가 선언돼 있어 `FrameAnimEffect.Spawn`이 호출되지만 프레임이 없어 `null`을 돌려주고 **조용히 아무것도 그리지 않는다.** → **Stage3 서리 벤트가 연출 없이 작동 중** |
| **A2** | `Assets/Resources/Effects/fx_spark/fx_spark_000.png` (1장 재생성) | **256×256** (현재 **182×182**) | 기존 충격 별섬광 첫 프레임 | 형제 3장이 256×256인데 이것만 182×182다. `FrameAnimEffect`가 같은 렌더러에 프레임을 갈아 끼우므로 **첫 프레임만 작게 찍히고 2프레임에서 튄다** |

**공통 규칙 (어기면 화면에서 안 맞는다)**
- **배경은 알파 투명.** 불투명 배경이 곧 "흰 네모"로 보이는 경로다.
- **무채색/흰색 실루엣으로 그린다.** 코드가 `SpriteRenderer.color`에 팀색·피해색을 **곱한다** — 색이 든 아트는 곱셈으로 탁해진다.
- **`.meta` 필수.** 없으면 `textureType: Default`로 임포트돼 `Resources.LoadAll<Sprite>`가 빈 배열을 돌려주고 조용히 안 그려진다(작업 #17에서 실제로 겪음).
- **`.provenance.json` 동반.** 스키마는 `Gimmicks/castle_keep_s0.png.provenance.json`과 동일.
- **프레임은 개별 생성하지 말 것.** 1프레임을 그리고 스케일·알파로 파생한다 — 작업 #19가 `fx_muzzle`에서 프레임 간 정합 드리프트를 피한 방식.

---

## B. 만들기 전에 확인할 것 — 1건 (아마 아트 불필요)

| # | 파일 | 상태 |
|---|---|---|
| **B1** | `Assets/Resources/Effects/fx_shatter/fx_shatter_000~005.png` (**6장 이미 있음**) | **`EffectSpriteLibrary`에 키 선언조차 없어 아무도 안 쓴다** |

이름과 프레임 수로 보아 **파괴/파편 순간용으로 만들어진 아트**일 가능성이 높다. 필요한 코드는
한 줄이다:

```csharp
public const string Shatter = "fx_shatter";
```

> **이번 세션의 반복된 패턴이 "필요한 것이 이미 있는데 연결만 안 됐다"였다.**
> 충돌 이펙트를 새로 그리기 전에 **이것부터 열어보길 권한다.**

---

## C. 조건부 — 확인 후 판단

| # | 항목 | 조건 |
|---|---|---|
| **C1** | `fx_impact` 4~6프레임 (돌 파편 방사) | **B1이 충돌용이 아닐 때만.** 명세는 아트 요청서 §3-B |
| **C2** | 발사체 3종 (`기사`/`궁수`/`화약통`) 128×128↑ | **콜라이더 정합 확정 후.** 렌더 0.93u vs 콜라이더 **5.28u(5.7배)** 불일치가 미해결이라, 그 값이 바뀌면 아트 크기 기준도 바뀐다 — **지금 만들면 다시 만들 위험** |

C2의 핵심 요구는 **어두운 외곽선**이다. 발사체는 화면상 28px이고 배경이 밝은 초원·하늘이라
지금은 묻힌다 — 궤적 아크가 하늘 대비 1.13:1로 안 보였던 것과 **같은 문제, 같은 해법**이다.
그리고 비행 중 120°/s로 회전하므로 **화살표처럼 방향성 강한 형태는 금지**(회전하면 혼란).

---

## D. 만들지 말 것 — 근거가 반대한다

| 금지 | 근거 |
|---|---|
| **전체화면 흰색 플래시** | 이 조사에서 **유일하게 실제 인체 피해가 기록된 레이어.** 1997 포켓몬 포리곤 사건 — *"Over 600 people, mostly children, were taken to hospitals"*, 방아쇠 장면이 *"an explosion… rapid flashing lights that fill the screen"* (우리 도메인과 동일) |
| 1초에 3회 초과 명멸 | WCAG 2.2 SC 2.3.1 / GAG "화면 25%+ 를 덮는 초당 3회 초과" |
| 다중 동심원 링 | GAG의 **radial 반복 패턴** 조항(동적 5줄 이상). 현재 링 1개는 안전 |
| 화면 흔들림 추가 | Nijman 자백 — *"had to put like an option in nuclear [Throne] to disable the screen Shake because some people were getting really nauseous"* |
| 취소된 UI 플레이스홀더 7종 부활 | `VisibilitySpecAssetTests.CancelledPlaceholders_StayDeleted`가 막는다. 목록·이유는 `design/visibility-art-handoff.md` §2 |

---

## E. 아트가 필요 없는 것 (혼동 방지)

| 요소 | 구현 |
|---|---|
| 궤적 아크(카싱+코어) | `LineRenderer` 2겹 — 코드 |
| 탄착 표시 | **없음.** 형식을 폐기했다(아이콘은 표본 13종 중 1종, 그것도 월드가 숨겨진 게임) — 궤적 끝점이 지시 |
| 적 발사기 | 기존 `slingshot_anim` 6프레임 **좌우반전 재사용** |
| 발사기 반동·와인드업 | 트랜스폼 산술 |
| 발사 버스트 | 기존 `particle_ember` |
| 행동자 강조 | `SpriteRenderer.color.a` |

---

## 요약

> **정말 없는 것은 2건이다** — `fx_frost` 4장, `fx_spark_000` 1장 재생성.
> **1건은 이미 있는데 연결이 안 됐다** — `fx_shatter` 6장.
> **2건은 선행 확인 후 판단이다** — `fx_impact`, 발사체 3종.
