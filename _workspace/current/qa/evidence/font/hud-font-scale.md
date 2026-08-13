# HUD 폰트 실효 픽셀 크기

화면 640x480

- canvas `GameplayHudCanvas` mode=ConstantPixelSize ref=1920x1080 match=0.5 **scaleFactor=0.5333**
- canvas `Canvas` mode=ConstantPixelSize ref=800x600 match=0 **scaleFactor=1**

| 라벨 | 텍스트 | fontSize | canvas scale | 실효 px | 위험 |
|---|---|---|---|---|---|
| Label | KEEP CORE  150/150 | 26 | 0.5333 | **13.9** |  |
| SupplyText | 보급 9/24  ·  3턴 해금 | 26 | 0.5333 | **13.9** |  |
| TimerText | 14 | 32 | 0.5333 | **17.1** |  |
| TurnToastText | 내 턴 | 28 | 0.5333 | **14.9** |  |
| TurnText | YOUR SIEGE TURN | 24 | 0.5333 | **12.8** |  |
| DeployToggleLabel | 배치 모드 OFF (D) | 23 | 0.5333 | **12.3** |  |
| ScoreText | SIEGE SCORE  0 - 0 | 36 | 0.5333 | **19.2** |  |
| WindText | WIND >>> 0.9 | 36 | 0.5333 | **19.2** |  |
| ControlGuideText | <b>KNIGHT</b> 준비  ·  푸른 … | 23 | 0.5333 | **12.3** |  |
| LaunchStatsText | <b>발사!</b>  ·  파워 60% · … | 26 | 0.5333 | **13.9** |  |
| Label | BREACH CORE  150/150 | 26 | 0.5333 | **13.9** |  |

위험(12px 미만) 0 / 검사 11
