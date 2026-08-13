# 고아 HUD 라벨 — 입양 시 어디에 놓이는가

화면 640x480

## 입양 전

| 라벨 | 캔버스 | 부모 | anchor | anchoredPos | size |
|---|---|---|---|---|---|
| windText | GameplayHudCanvas | MobileSafeArea | (0,1) | (150,-30) | (250x50) |
| scoreText | GameplayHudCanvas | MobileSafeArea | (1,1) | (-150,-30) | (250x50) |

## 입양 후

| 라벨 | 캔버스 | 부모 | anchor | anchoredPos | size |
|---|---|---|---|---|---|
| windText | GameplayHudCanvas | MobileSafeArea | (0,1) | (150,-30) | (250x50) |
| scoreText | GameplayHudCanvas | MobileSafeArea | (1,1) | (-150,-30) | (250x50) |

## 화면상 사각형 (입양 후)

| 라벨 | x범위 | y범위 | 화면 안 |
|---|---|---|---|
| windText | 80~213 | 437~464 | 예 |
| scoreText | 427~560 | 437~464 | 예 |

## 같은 띠를 쓰는 기존 요소

- `Label` 상단 y=415 — "KEEP CORE  150/150"
- `TimerText` 상단 y=409 — "14"
- `TurnToastText` 상단 y=381 — "내 턴"
- `DeployToggleLabel` 상단 y=399 — "배치 모드 OFF (D)"
- `SupplyText` 상단 y=425 — "보급 9/24  ·  3턴 해금"
- `WindText` 상단 y=464 — "WIND <<< 0.4"
- `TurnText` 상단 y=469 — "YOUR SIEGE TURN"
- `Label` 상단 y=415 — "BREACH CORE  150/150"
- `ScoreText` 상단 y=464 — "SIEGE SCORE  0 - 0"
