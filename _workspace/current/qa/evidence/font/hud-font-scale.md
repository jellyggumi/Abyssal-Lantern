# HUD 폰트 실효 픽셀 크기

화면 640x480

- canvas `GameplayHudCanvas` mode=ConstantPixelSize ref=1920x1080 match=0.5 **scaleFactor=0.5333**
- canvas `Canvas` mode=ConstantPixelSize ref=800x600 match=0 **scaleFactor=1**

| 라벨 | 텍스트 | fontSize | canvas scale | 실효 px | 위험 |
|---|---|---|---|---|---|
| Label | BREACH CORE  150/150 | 26 | 0.5333 | **13.9** |  |
| WindText | WIND <<< 1.0 | 36 | 0.5333 | **19.2** |  |
| TurnToastText | 내 턴 | 28 | 0.5333 | **14.9** |  |
| ScoreText | SIEGE SCORE  0 - 10 | 36 | 0.5333 | **19.2** |  |
| TurnText | YOUR SIEGE TURN | 24 | 0.5333 | **12.8** |  |
| DeployToggleLabel | 대포 — 3턴 후 해금 | 23 | 0.5333 | **12.3** |  |
| Label | KEEP CORE  150/150 | 26 | 0.5333 | **13.9** |  |
| ControlGuideText | <b>기사</b> 준비  ·  아무 곳이나 … | 23 | 0.5333 | **12.3** |  |
| TimerText | 14 | 32 | 0.5333 | **17.1** |  |
| SupplyText | 보급 9/24  ·  3턴 해금 | 26 | 0.5333 | **13.9** |  |

위험(12px 미만) 0 / 검사 10
