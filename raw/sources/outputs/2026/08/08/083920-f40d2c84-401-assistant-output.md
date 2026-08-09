---
type: "assistant-output"
session_id: "f40d2c84-4012-4f97-bf8c-1ec02651"
captured_at: "2026-08-08T08:39:20.789561+00:00"
report: "[[wiki/reports/2026-08-08-083920-assistant-output-answer]]"
---

# 제출 문서 세트 완료

## Output

```text
# 제출 문서 세트 완료

`docs/submission-nhn/`에 5개 문서와 PDF 3종을 만들고 커밋했습니다 (`d1159da`).

| 제출물 | 파일 | 상태 |
|---|---|---|
| 1 빌드+소스 | https://akillness.github.io/hongT/ · 저장소 PUBLIC, 커밋 246개 | ✅ 이미 충족 |
| 2 플레이 영상 | `04-play-video-plan.md` (촬영 대본) | ⚠️ **미촬영 — 직접 하셔야 함** |
| 3 게임 소개 | `pdf/01-game-overview.pdf` (7쪽) | ✅ |
| 4 AI 활용 기술 | `pdf/02-ai-tech.pdf` (13쪽) | ✅ |
| 5 팀원 롤 | `pdf/03-team-roles.pdf` (6쪽) | ✅ |

## 사실 검증한 것

지어내지 않고 코드·배포에 대조했습니다:
- **조작표** — `InputAdapter.cs` 던전 분기와 1:1 대조 (Q 균열화살 / E 묘지파동 / R 노바 / F 방패 / Shift 질주 / G·H·V 동료)
- **딥링크** — `?mode=arena|prologue|training`, `?intro=off`가 `GameDirector`에 실재하고 라이브에서 전부 HTTP 200
- **라이선스** — provenance 22건을 근거로 표 작성. 말씀하신 **Rodin(자체 컨셉 이미지 → image-to-3D)** 과 **Mixamo 모션**을 타 IP 문제 없음의 근거로 명시
- **PDF 한글 렌더** — 텍스트 추출로 한글 정상 확인 (코드 블록은 D2Coding)

## 직접 하셔야 하는 것

**영상 촬영·업로드.** 규정이 "실제 플레이 화면 그대로, AI 합성 금지"라 제가 만들 수 없습니다. 대신 요청하신 3대 포인트를 60초에 담는 초 단위 샷 대본을 `04-play-video-plan.md`에 준비했습니다 — 동료 3명+스킬 / 보스전 / 텍스트 커맨드 순서, 각 샷의 실제 입력키, 촬영 전 세이브 세팅(localStorage 주입·딥링크)까지 포함했습니다.

업로드 후 YouTube 링크를 알려주시면 `00-submission-index.md`와 `01-game-overview.md`의 TODO 두 곳에 넣고 PDF를 다시 빌드하겠습니다. 그 전까지는 링크 칸이 비어 있는 게 정확한 상태입니다.

**확인 부탁**: 팀원 롤 문서는 기존 NAN2026 문서의 3인 구성(정장영·이석민·정우영)을 git 실측으로 갱신한 것입니다. 이번 공모전 팀 구성이 다르면 알려주세요.
```
