---
type: "source-summary"
source_type: "assistant-output"
captured_at: "2026-06-16T03:10:30.899102+00:00"
raw_path: "raw/sources/outputs/2026/06/16/031030-b133d257-c74-typesense-828fd48-origin-main.md"
session_id: "b133d257-c741-478d-b124-4905d8bb"
rtk_method: "rtk"
rtk_original_chars: 1406
rtk_compressed_chars: 1406
rtk_saved_pct: 0.0
---

# 완료했습니다. `typesense` 검색엔진 스킬을 추가하고 커밋 `828fd48`로 origin/main에 푸시…

- Raw output: [[raw/sources/outputs/2026/06/16/031030-b133d257-c74-typesense-828fd48-origin-main]]
- Filed report: [[wiki/reports/2026-06-16-031030-typesense-828fd48-origin-main-answer]]
- rtk compression: rtk (1406→1406 chars, -0.0%)

## Compressed Output (rtk)

```text
완료했습니다. `typesense` 검색엔진 스킬을 추가하고 커밋 `828fd48`로 origin/main에 푸시했습니다 (main ↔ origin/main 동기화).

## 작업 요약

**소스 분석** — scrapling으로 [typesense/typesense](https://github.com/typesense/typesense) README를 가져와 파악: 오타 허용 오픈소스 검색엔진, **Algolia/ElasticSearch 대안**, 단일 C++ 바이너리(런타임 의존성 0, <50ms), Docker/binary/Cloud 3가지 설치, 공식 클라이언트 4종(Python/JS/PHP/Ruby), GPL-3.0(서버)·Apache-2.0(클라이언트).

**신규 스킬 `.agent-skills/typesense/`** (opik/cli-anything 전례 + 최신 래칫 기준 — toon·evals 포함):
- `SKILL.md` — 서버 모드 선택 → 클라이언트 설치 → 스키마 설계 → 색인 → 검색(typo/facet/geo/sort/synonym/scoped-key/multi-search/vector) → InstantSearch UI + Raft HA까지 라우팅. desc 1012자(1024 한도 내), frontmatter YAML 검증 통과
- `references/commands.md` — 서버 실행/클라이언트 매트릭스/검색 파라미터/운영 노트
- `scripts/install.sh` — Docker 서버 기동 + Python 클라이언트, venv/PEP 668 인식, `FORCE`/`NO_SERVER`/버전·포트·키 knobs (`bash -n` 통과, 실행권한 부여)
- `SKILL.toon`, `evals/evals.json`(3케이스, assertion 기반)
- Route-out: `opik`/`langsmith`(LLM 옵저버빌리티), `semble`(에이전트 코드 검색), `monitoring-observability`(비-검색 텔레메트리)

**문서 3종 갱신** — 총계 139 → **140**:
- `README.md` / `README.ko.md` — 배지·총계, What's New(v2026-06-16), Infrastructure 카탈로그 행(영문 17→18, 한글 15→16)
- `setup-all-skills-prompt.md` — 총계, Skill Inventory의 Infrastructure 행, Core Skill Keyword Reference 항목, Step 4 core-skill 검증 목록에 `typesense` 추가

설치: `npx skills add https://github.com/akillness/oh-my-skills --skill typesense` — 검증/설치 실행까지 원하시면 말씀해 주세요.
```
