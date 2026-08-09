---
type: "source-summary"
source_type: "assistant-output"
captured_at: "2026-06-11T12:36:17.299094+00:00"
raw_path: "raw/sources/outputs/2026/06/11/123617-b133d257-c74-semble-vs-rtk.md"
session_id: "b133d257-c741-478d-b124-4905d8bb"
rtk_method: "rtk"
rtk_original_chars: 1894
rtk_compressed_chars: 1894
rtk_saved_pct: 0.0
---

# semble vs rtk — 같은 "토큰 절감"이지만 다른 층위

- Raw output: [[raw/sources/outputs/2026/06/11/123617-b133d257-c74-semble-vs-rtk]]
- Filed report: [[wiki/reports/2026-06-11-123617-semble-vs-rtk-answer]]
- rtk compression: rtk (1894→1894 chars, -0.0%)

## Compressed Output (rtk)

```text
# semble vs rtk — 같은 "토큰 절감"이지만 다른 층위

둘 다 에이전트의 토큰 사용량을 줄이는 도구지만, **개입하는 지점이 완전히 다릅니다**. semble은 "무엇을 읽을지"를 바꾸고, rtk는 "읽은 것을 어떻게 압축할지"를 바꿉니다.

## 핵심 비교

| 항목 | **semble** | **rtk (Rust Token Killer)** |
|------|-----------|------------------------------|
| 역할 | 시맨틱 **코드 검색 엔진** | 셸 명령 출력 **압축 프록시** |
| 대체 대상 | grep + 파일 전체 읽기 | `git status`, `cat`, `grep`, `test`, `lint` 등의 원본 출력 |
| 동작 방식 | 저장소를 임베딩 인덱싱(~250ms) → 질의와 관련된 청크만 반환 | 명령을 감싸서 실행 → 출력에서 군더더기 제거 후 압축 포맷으로 반환 |
| 절감 원리 | 애초에 **관련 코드만 가져옴** (~98% 절감) | 가져온 출력의 **부피를 줄임** (60–90% 절감) |
| 질의 형태 | 자연어("rate limiting 어디서 처리?") / 심볼 이름 | 질의 개념 없음 — 기존 명령을 그대로 래핑 (`rtk git status`) |
| 적용 범위 | 코드 검색에 한정 | git, 파일 읽기, grep, 테스트, 린트 등 개발 작업 전반 |
| 통합 방식 | MCP 서버 또는 CLI (의식적으로 호출) | **훅 기반 자동 재작성** — `git status` → `rtk git status`로 투명하게 치환, 0 토큰 오버헤드 |
| 부가 기능 | `find-related`(유사 코드 확장), 원격 저장소 클론 없이 인덱싱 | `rtk gain`(절감량 분석), `rtk discover`(놓친 절감 기회 탐지), `rtk proxy`(무필터 디버그) |
| 구현 | Python (uv/pip) | Rust 바이너리 (brew/cargo) |

## 겹치는 지점: grep

유일하게 충돌하는 영역이 grep인데, 성격이 다릅니다.
- **semble search** — "이 동작이 어디 구현됐지?"처럼 *무엇을 찾는지 설명*할 때. 정확한 문자열을 모를 때 강함
- **rtk grep** — 정확한 패턴/정규식을 *이미 알 때*. 매칭 결과의 출력만 압축

즉 탐색(discovery)은 semble, 확인(verification)은 rtk grep이 맞습니다.

## 상호 보완 — 현재 환경에서의 실제 구성

이 환경에는 둘 다 이미 설치되어 함께 동작하고 있습니다:

1. **rtk 훅**이 제 모든 셸 명령을 자동 래핑 — 이 대화에서도 `git status`, `grep` 출력이 rtk의 압축 포맷("3 matches in 1F:", "ok main" 등)으로 돌아오고 있었습니다
2. **semble MCP**(`search`, `find_related`)가 연결되어 있고, 프로젝트 CLAUDE.md가 코드 탐색 시 serena/semble 우선을 지시

전형적인 흐름으로 보면:
'''
코드 위치 탐색  → semble search        (관련 청크만, ~2k 토큰)
유사 패턴 확장  → semble find-related
정확 매칭 확인  → rtk grep             (압축된 매칭 결과)
상태/테스트/린트 → rtk git status, rtk test, rtk lint  (훅이 자동 적용)
절감량 점검     → rtk gain
'''

## 한 줄 요약

**semble은 "검색의 정밀도"로, rtk는 "출력의 밀도"로 토큰을 아낍니다.** 코드를 찾을 땐 semble이 1차 패스, 그 외 모든 셸 작업은 rtk가 백그라운드에서 자동으로 절감 — 경쟁 관계가 아니라 파이프라인의 앞뒤 단계입니다.
```
