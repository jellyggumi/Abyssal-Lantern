# `AimDefaultReachTests` 뮤테이션 증명

- date: 2026-08-14
- 목적: 이 테스트가 **사용자가 보고한 증상을 지금 잡는지**, 그리고 **이 결함을 만든 상수
  드리프트를 앞으로 잡는지** 실증한다. 통과하는 테스트는 그것만으로는 아무것도 증명하지 않는다.
- 재현: `-testFilter "AimDefaultReachTests"`, Unity 6000.5.6f1 EditMode

## 뮤테이션 1 — 기본값을 출하 상태로 되돌린다

`LaunchManager.aimPower` `0.82f` → **`0.55f`** (사용자가 보고한 그 값)

**결과: 4 중 3 실패.**

| 테스트 | 메시지 |
|---|---|
| `ShippedDefaultAim_LandsOnTheEnemyKeep...` | `lands at x=-4.74 — the PLAYER'S OWN keep (x -7.0..-4.0)` |
| `ShippedDefaultAim_DoesNotDetonateOnThePlayersOwnKeep` | `Firing with the shipped defaults destroys your own wall and does nothing to the enemy` |
| `ReachingBand_LeavesRoomOnBothSides...` | `0.55 sits -5.6 presses above the bottom of the band (0.78..0.86)` |

**녹색으로 남은 것: `TheIntegratorAgreesWithTheClosedForm...`** — 옳다. 그것은 측정 장치를
검증하며 기본값에 의존하지 않는다. 기본값 뮤테이션에 붉어졌다면 그 테스트가 잘못 쓰인 것이다.

## 뮤테이션 2 — 결함을 만든 상수 드리프트를 재현한다

`LaunchPowerCurve.MaxSpeed` `17.5f` → **`25.2f`** (작업 #60 이전 값)

**결과: 4 중 2 실패.**

| 테스트 | 메시지 |
|---|---|
| `ShippedDefaultAim_LandsOnTheEnemyKeep...` | `lands at x=28.58 — past the keep, toward the core or beyond` |
| `ReachingBand_LeavesRoomOnBothSides...` | `0.82 sits -6.5 presses below the top of the band (0.51..0.56)` |

**이것이 이 파일의 존재 이유다.** 작업 #60이 이 상수를 내렸을 때 아무것도 붉어지지 않아
`aimPower = 0.55`가 조용히 낡았다. 이제 두 상수 중 어느 쪽이 움직여도 실패한다.

`DoesNotDetonateOnThePlayersOwnKeep`이 녹색으로 남은 것도 옳다 — 25.2에서는 자기 벽을
넘어간다. 문제가 미달에서 **초과**로 바뀌었고, 그것을 말하는 것은 다른 단언이다.

## 원복 확인

두 파일 모두 뮤테이션 후 **바이트 동일**로 복구했다:

```
LaunchManager.cs      98fb8d43...  (백업과 동일, 2 files 1 hash)
LaunchPowerCurve.cs   036ec7e1...  (백업과 동일, 2 files 1 hash)
```

## 이 증명이 말하지 않는 것

- **45° 축만 검증했다.** 각도 뮤테이션은 하지 않았다 — `aimAngleDegrees`가 움직이면
  대역도 함께 움직이는데(35°에서 0.810~0.900) 그것이 붉어지는지는 미측정이다.
- **PlayMode에서 확인하지 않았다.** 실제 발사체는 박스캐스트이고 이 테스트는 점 모델이다.
  방향은 알고 있다 — 점 모델이 더 비관적이므로 실측 도달은 이보다 넓다.
- **`powerStep`을 뮤테이션하지 않았다.** 대역 폭을 프레스로 나누는 분모이므로 그것이
  커지면 "1 프레스 여유" 요구가 만족 불가능해진다. 그 경우 테스트가 어떻게 실패하는지 미확인.
