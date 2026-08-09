---
title: joc gjc-패리티 deep-dive 트레이스/스펙 결과
type: report
date: 2026-06-10
tags: [jeo-code, joc, gjc, deep-dive, trace, spec]
---

# joc gjc-패리티 deep-dive 결과 (2026-06-10)

3-레인 병렬 트레이스 + 인터뷰로 jeo-code 개선 스펙 확정. 모호도 1.0 → 0.12.

## 핵심 발견
- **1차 원인 = 이중 표면 아키텍처**: 워크플로우 엔진(run*Command)·스킬·compaction이 shell 명령 표면에만 1급, `joc launch` 세션은 4줄 GUIDANCE + LLM 즉흥으로 강등 (launch.ts에 호출자 0건).
- **provider 2차 결함군**: `streamMaxRetries` silent no-op(미구현 버그 확정), setup 자유입력 오라우팅(gpt-oss→openai), 멀티프로세스 OAuth refresh 경쟁(plain writeFile), OpenAI OAuth+비-Codex 불투명 400.
- **"TUI 메모리 비대"는 반증**: TUI ring-cap 500/renderer 1프레임/compaction 유계. 실체는 컨텍스트 한계 초과 인지(char 기반 compaction의 CJK 토큰 어긋남 + system prompt 예산 제외). 무계 벡터는 JSONL resume/list 적재 + team 워커 compaction 부재.

## 산출물
- Trace: `jeo-code/.omc/specs/deep-dive-trace-joc-gjc-parity-improvement.md`
- Spec(6 워크스트림): `jeo-code/.omc/specs/deep-dive-spec-joc-gjc-parity-improvement.md`
- 비교 지식: [[wiki/concepts/gjc-vs-joc-architecture]], [[wiki/sources/2026-06-10-gajae-code-repo]]
