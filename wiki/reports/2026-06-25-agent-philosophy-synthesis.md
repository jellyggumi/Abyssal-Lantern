# AI 코딩 에이전트 철학 합성: gajae-code · lazycodex · ouroboros · loop-engineering · jeo

> 출처 토픽: "gajae-code의 철학과 lazycodex, ouroboros, loop engineering, 그리고 jeo의 철학을 비교하고 특장점·단점을 보완하는 형식의 철학을 도출 — 속도·정확성·메모리 최적화 기반, 10회 회고, clawteam(codex·claude) 합의 도출, graphify→llm-wiki 정리"
> 작성: 2026-06-25 · 방법: deep-research(개요→심층조사→보고) + clawteam 멀티에이전트(architect=codex 관점, critic=claude 관점) 합의
> 근거: `gajae-code-analysis.md`, jeo `README.md`/`AGENTS.md`, jeo `src/agent/{loop-guards,memory,goal-verifier}.ts`, 서브에이전트 검토 2건

## 1. 한 줄 요약

다섯 철학은 각기 **단일 축**을 최적화한다 — gajae-code=신뢰성(상태 규율), lazycodex=속도(지연 평가), ouroboros=자기개선(진화 루프), loop-engineering=제어(루프 자체가 제품), jeo=정직성(실질 게이트). 이들을 합성한 단일 원리는:

> **"기본은 게으르게, 경계에서는 게이트로, 실행 간에는 자기개선으로, 핫패스는 네이티브로, 보고는 정직하게"**
> (Lazy by default · Gated at the boundaries · Self-improving across runs · Native at the hot paths · Honest at the report)

단, 멀티에이전트 검토 결과 이 원리는 **두 개의 HIGH 긴장**을 내장한다 — `lazy-context ↔ honest-gates`(게이트가 미적재 상태에서 통과)와 `self-improvement ↔ reproducibility`(진화가 정확성의 근거인 정책 자체를 변형). 이 둘을 스펙으로 해소해야 "엔지니어링상 건전"하다고 부를 수 있다.

## 2. 다섯 철학 비교

### 2.1 핵심 명제

| 철학 | 한 줄 명제 | 최적화 축 | 대표 기제 |
|------|-----------|-----------|-----------|
| **gajae-code (gjc)** | 상태 규율로 신뢰성을 산다 | 정확성·속도 | 세션 스코프 상태, race-safe guarded writer, mutation guard, Rust 네이티브 크레이트(pi-ast/pi-shell), computer-use |
| **lazycodex** | 필요할 때까지 미룬다 | 속도·메모리 | 지연 컨텍스트, 온디맨드 적재, 최소 토큰, "덜 함으로써 빠름" |
| **ouroboros** | 자기 출력을 먹고 자란다 | 정확성(누적) | 진화 패스, self-improve, evolution-bridge, 출력이 다음 입력 |
| **loop-engineering** | 루프가 곧 제품이다 | 제어·예측가능성 | 동적 step budget, loop-guards, compaction, verification hook, process-reaper, crash-durability |
| **jeo** | 의도를 인코딩하고 소프트웨어를 디코딩한다 | 정직성·이식성 | spec-first 실질 차단 게이트, edit-integrity 앵커, 자기교정 검증, OKF 개념번들 메모리, zero native dep |

### 2.2 특장점 / 단점

**gajae-code (gjc)** — *근거: gajae-code-analysis.md*
- 장점: 세션 격리(`.gjc/_session-{id}/`)로 동시·재개 세션 충돌 제거, lock-owned revision을 `GuardedWriteResult`에 실어 stale-skip 오탐 제거, Rust 핫패스로 네이티브 속도, computer-use·Docker·robot 변종까지 넓은 표면, ~1,100+ 신규 테스트 라인의 방어적 커버리지.
- 단점: TS+Rust 모노레포의 빌드·유지 비용, 네이티브 의존으로 이식성·설치속도 저하, 표면이 넓어 복잡도 상승.

**lazycodex**
- 장점: 적은 선행 작업 → 낮은 지연·낮은 메모리·낮은 토큰 비용, 단순 작업에서 압도적 속도.
- 단점: 컨텍스트 과소수집 → 정확성 위험, 지연된 작업이 나중에 폭발(surprise), 완전성 검증이 어려움.

**ouroboros**
- 장점: 반복으로 복리 개선, 환경 적응, 회차가 늘수록 정확성 향상.
- 단점: 수렴 위험(드리프트·회귀), 회차마다 속도·메모리 비용, 프록시 지표 최적화로 인한 퇴화 — 강한 ratchet(keep/revert) 가드 없이는 자기파괴.

**loop-engineering**
- 장점: 예측가능성·비용 상한·crash-durability, 속도/메모리를 루프 파라미터로 직접 제어.
- 단점: 경직성으로 탐색 제한, 루프 과설계가 지연 추가, 가드 임계값 튜닝 난이도.

**jeo** — *근거: README.md, AGENTS.md, src/agent*
- 장점: no-theater 정직 보고(스위트 실행은 전역 신호이지 조작된 항목별 통과가 아님), zero native dep 이식성, edit-integrity 앵커로 라인 시프트 재매핑/거부, 자기교정 검증 루프(red hook이 done 차단), `.jeo/` 원자적 쓰기·크로스프로세스 락·실패태스크 마커로 durable.
- 단점: `jeo team`이 엄격 직렬(병렬 fan-out 없음, 한 태스크 실패 시 전체 정지), TS-only(네이티브 가속 부재), 단순 작업에도 게이트 오버헤드, `selectWithinBudget`의 O(n²) 재렌더 등 대규모 번들에서의 미세 비용.

## 3. 세 최적화 축으로 본 보완 설계

각 축에서 "한 철학의 단점을 다른 철학의 장점으로 메운다".

### 3.1 속도 (Speed)
- lazycodex의 **지연 컨텍스트** + gjc의 **네이티브 핫패스** + loop-engineering의 **동적 step budget**.
- 단순 작업은 무거운 게이트를 우회하는 **fast-path 분류기**로 처리(jeo의 게이트 오버헤드 단점 보완).
- 보완: jeo는 TS-only지만 핫패스(파싱/diff/검색)는 *선택적 네이티브 가속*으로 끌어올리되 순수 폴백을 유지(이식성 보존).

### 3.2 정확성 (Accuracy)
- jeo의 **실질 차단 게이트**(ralplan consensus hash, critic fail-closed `[OKAY]`) + ouroboros의 **회고적 자기교정** + gjc의 **race-safe 상태**.
- 보완: lazycodex의 과소수집 위험은 "**게이트는 자신의 의존 폐포(dependency closure)를 eager하게 적재**"라는 불변식으로 차단.

### 3.3 메모리 (Memory)
- jeo의 **OKF 개념번들 예산**(`MEMORY_INJECT_MAX_CHARS=5000`, query-aware `selectWithinBudget`, 개념 통째 보존) + lazycodex 지연 적재 + gjc 세션 스코프 + `parsedConceptCache`(mtime:size LRU 512).
- detached `jeo memory-distill`(`finally`에서 항상 `exit(0)`)로 종료 즉시성 + RSS 누수 방지.
- 보완: capped 주입이 **load-bearing 불변식**을 축출하지 않도록 **pin + 예약 예산** 경로 필요(현재 미구현 — 4절 합의 참조).

## 4. 합성 철학: "검증된 게으름의 자기개선 루프"

**5중 원리** — 각 절은 정확히 하나의 단점을 메운다:

1. **Lazy by default** — 모든 적재·계산·확장은 온디맨드. (lazycodex; jeo 게이트 오버헤드 보완)
2. **Gated at the boundaries** — 변형(mutation)·done·승인 경계에서만 eager·차단 검증. 게이트는 의존 폐포를 강제 적재. (jeo + gjc; lazy 과소수집 보완)
3. **Self-improving across runs** — 회차 간 회고로 정책 개선하되, 세대는 append-only·pinned·held-out eval 뒤에서만 승격. (ouroboros + ratchet keep/revert; 드리프트 보완)
4. **Native at the hot paths** — 핫패스만 선택적 네이티브 가속 + 순수 폴백 + parity 테스트. (gjc + jeo 이식성; TS-only 속도 보완)
5. **Honest at the report** — 보고는 증거 결속(evidence-bound). 변형 턴의 done은 *재실행된* 검사의 관측된 성공(exit status)으로만 해제. (jeo no-theater; gate theater 보완)

## 5. 10회 회고 (속도·정확성·메모리 기준 반복 정련)

각 회차는 직전 회차의 미해결 긴장을 하나씩 닫는다.

1. **R1 — 단순 통합 가설.** 다섯 장점을 더하면 최고가 된다? → 거짓. 장점은 *상호 배타적 축*을 최적화하므로 이음새(seam)에서 정확성이 샌다.
2. **R2 — lazy↔gate 충돌 인지.** 지연 적재 상태에서 게이트가 "무지로부터 통과"(confident false-negative)할 수 있음 발견 → 게이트는 lazy를 **경계 내부에서 정지**시켜야 한다.
3. **R3 — 게이트 의존 폐포 불변식.** 게이트는 검증에 필요한 파일·의존을 *eager*하게 materialize. 이는 속도를 일부 희생하지만 단순 작업은 fast-path가 흡수.
4. **R4 — fast-path를 신뢰 경계로.** 분류기를 무비용 통과로 두면 우회 악용 → fast-path에도 **저렴한 sanity 게이트 + 분류 감사 로그**.
5. **R5 — ouroboros 재현성 충돌.** 진화가 step budget·프롬프트·가드를 바꾸면 정확성 근거가 흔들림 → **frozen audited baseline**은 루프가 in-band로 덮어쓸 수 없게 동결.
6. **R6 — eval 표면 분리.** held-out eval set과 eval harness 자체를 mutation surface 밖에 둔다(자기 채점 방지). 메모리: eval 아티팩트는 세션 스코프로 격리.
7. **R7 — done 게이트 증거 결속.** jeo 실코드 점검 결과 `isVerificationSignal`은 명령·출력 앞 2000자 정규식 매칭이며 `classifyDoneGate`는 **1회만 차단**(두 번째 done은 통과) → 키워드가 아닌 **재실행 exit status**에 결속하도록 수정 필요.
7b. **R7 보강 — `verifyGoal`도 트랜스크립트를 신뢰**하므로 최소 한 개의 결정적 검사를 *재실행*하도록 변경.
8. **R8 — 메모리 pin 부재.** query-aware 캡이 load-bearing 불변식을 축출할 수 있음 → `memory.ts`에 **pin/예약 예산** 경로 구현, recency fill 이전에 pinned 불변식 생존 보장.
9. **R9 — 네이티브↔이식성 모순 해소.** "native at hot paths"와 jeo의 zero-native-dep는 표면상 충돌 → 네이티브를 **선택적 가속기 + 필수 순수 폴백 + fail-closed parity CI**로 재정의하면 양립.
10. **R10 — durability barrier.** done 방출 전 detached distill·상태·ledger 쓰기의 fsync 완료를 요구하고 edit-integrity 캐시 무효화와 정합. 속도(즉시 종료)와 정확성(유실 없음)을 둘 다 만족하는 비동기-그러나-결속 종료.

**회고 수렴점:** 합성 원리는 *방향*이 옳다. 그러나 "정직"·"자기개선"·"게으름"은 **스펙 수준의 불변식**(의존 폐포 eager, append-only 세대, 증거 결속 done, pin 예산) 없이는 수사(rhetoric)에 머문다. 10회를 통해 5중 원리는 *5중 원리 + 5개 불변식*으로 강화됨.

## 6. clawteam 멀티에이전트 합의 (codex 관점 · claude 관점)

`$clawteam-multi-agent-coordination`를 jeo의 실제 능력으로 구현 — 읽기전용 서브에이전트 2종을 fan-out:
**architect(=codex 관점, 엔지니어링 건전성)**, **critic(=claude 관점, 정직성·실패모드)**.

### 6.1 codex 관점 (architect) — 판정: WATCH / REQUEST CHANGES
- **HIGH** `lazy-context ↔ honest-gates`: 게이트는 적재한 것에 대해서만 정직할 수 있다. 지연 컨텍스트는 false-negative를 *confident* false-negative로 바꾼다.
- **HIGH** `self-improvement ↔ reproducibility`: 자기수정 루프가 정확성 보장의 근거(정책)를 변형. 동결 baseline 없이는 무성(silent) 회귀.
- **MEDIUM** OKF 캡이 게이트가 필요로 하는 사실을 축출 / 네이티브 핫패스가 jeo의 zero-native-dep과 정면 충돌.
- 권고: 게이트가 의존 폐포를 eager 적재(불변식화), fast-path를 신뢰 경계화, ouroboros 세대 append-only 동결+held-out 승격, 네이티브=선택 가속기+순수 폴백+parity, done 전 durability barrier, OKF verification-aware pin.

### 6.2 claude 관점 (critic) — 판정: [OKAY] (조건부)
실코드 점검(`loop-guards.ts`·`goal-verifier.ts`·`memory.ts`) 후:
- **Gate theater**: `isVerificationSignal`은 정규식 키워드 매칭이고 `classifyDoneGate`는 1회만 차단(두 번째 done 통과) → **불충분**. 수정 전까지 done은 키워드로 만족 가능.
- **`verifyGoal` 트랜스크립트 신뢰** → 결정적 검사 재실행 필요.
- **메모리 pin 미구현** → 캡이 불변식을 축출 가능.

### 6.3 합의점 (양 관점 서명)
1. **검증은 문자열이 아닌 증거에 결속** — 변형 턴의 done은 *재실행된* 검사의 관측 성공(exit status)으로만 해제, 트랜스크립트 자기보고·키워드 금지.
2. **진화는 동결 held-out eval 뒤에서 append-only** — 세대는 pinned·audited baseline, eval set·harness는 mutation surface 밖.
3. **load-bearing 불변식은 예약 예산으로 pin** — OKF 주입이 recency fill 이전에 pinned 불변식 생존 보장.

### 6.4 합의에 따른 필수 수정 (jeo 실코드 대상)
1. `sawVerification`를 실제 exit status에 결속 + 무조건 두 번째-done 통과를 성공-게이트 해제로 대체(변형 턴).
2. `verifyGoal`이 트랜스크립트 채점 대신 결정적 검사 1개 이상 재실행.
3. `memory.ts`에 pin/예약 예산 경로 구현.
4. ouroboros held-out eval harness를 mutation surface에서 제외.
5. 네이티브/순수 parity 테스트를 fail-closed CI 게이트로.

## 7. 결론

합성 철학 **"검증된 게으름의 자기개선 루프"**는 다섯 단일축 철학을 5중 원리로 묶고, 멀티에이전트 검토를 통해 5개 스펙 불변식으로 강화되었다. 속도(지연+네이티브+fast-path), 정확성(증거결속 게이트+회고+race-safe), 메모리(OKF pin+세션스코프+detached distill)의 세 축은 *이음새의 불변식*으로만 동시에 만족된다. codex·claude 관점은 세 합의점에 서명했고, jeo 실코드에는 다섯 가지 구체적 후속 수정이 남는다.

## 관련 문서
- [[index]]
- [[wiki/reports/notebooklm-setup-report]]

<!-- 근거 파일: gajae-code-analysis.md / jeo README.md·AGENTS.md / src/agent/loop-guards.ts·memory.ts·goal-verifier.ts / clawteam architect+critic 서브에이전트 검토 2건 -->
