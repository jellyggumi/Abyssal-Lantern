# Castle Busters — 20+ 반복 개선 로드맵
**Status**: Initiated  
**Framework**: $bmad-gds (Game Design Specification) × $product-strategy × $spec-stack  
**Goal**: 게임의 완결성, 안정성, 수익성 극대화 + 발전 방향 제시  
**Target**: 20+ 반복 사이클 (Iteration 1–25)

---

## 📋 평가 기준 (Evaluation Metrics)

### 1️⃣ 완결성 (Completeness)
- ✅ 코어 게임 루프 (Launch → Action → Resolve)
- ✅ 모든 유닛 타입 동작 (Knight, Archer, Bomber)
- ✅ 모든 기믹 기능 (Moving, Buff/Debuff, Castle Core)
- ✅ 승패 조건 명확성
- ✅ 필요한 UI 요소 완비

### 2️⃣ 안정성 (Stability)
- ✅ 컴파일 에러/경고 0개
- ✅ 런타임 예외 없음
- ✅ FPS 안정성 (목표: 60 FPS)
- ✅ 메모리 안정성 (<500MB)
- ✅ 엣지 케이스 핸들링

### 3️⃣ 게임성 (Game Feel)
- ✅ 입력 반응성 (<50ms 지연)
- ✅ 시각적 피드백 (파티클, 애니메이션, 화면 흔들림)
- ✅ 애니메이션 부드러움 (Idle, Attack, Launch, Land)
- ✅ 카메라 프레이밍
- ✅ 타이밍 & 리듬감

### 4️⃣ 수익성 (Revenue Potential)
- 🎯 세션 길이: 3–8분 (참여도)
- 🎯 재도전 의욕 (승패 균형, 난이도)
- 🎯 난이도 곡선 (조기 승리 → 중반 도전 → 고급 난제)
- 🎯 콘텐츠 깊이 (유닛, 스테이지, 업그레이드)

---

## 🔄 반복 사이클 구조 (20–25 Iterations)

### **Phase 1: 분석 & 기준선 수립 (Cycles 1–5)**
**목표**: 현재 게임 상태 파악, 문제점 식별

| Cycle | Focus | 산출물 |
|-------|-------|---------|
| 1 | 빌드 & 컴파일 상태 검증 | Compile log, Runtime baseline |
| 2 | 코어 기계 검증 (각 유닛, 기믹 동작) | Mechanics test report |
| 3 | 플레이테스트 × 30게임 (기초 통계) | Player win rate, avg duration, FPS |
| 4 | 밸런스 분석 & 사용성 검토 | Balance sheet, Issue list (Top 5) |
| 5 | 개선 제안 & 우선순위 결정 | Improvement proposal (Cycles 6–10 plan) |

### **Phase 2: 플레이테스트 & 미세 개선 (Cycles 6–12)**
**목표**: 게임성 개선, 밸런싱, 애니메이션 폴리시

| Cycle | Focus | 주요 작업 |
|-------|-------|----------|
| 6–7 | 유닛 밸런싱 (데미지, 속도, 쿨타임) | Damage tuning, Speed balancing |
| 8–9 | 애니메이션 & VFX 최적화 | Timing sync, Particle intensity |
| 10–11 | 난이도 곡선 (AI 스케일링) | AI behavior tuning |
| 12 | 플레이테스트 검증 (50게임) | Statistics validation |

### **Phase 3: 콘텐츠 & 설계 완성 (Cycles 13–18)**
**목표**: 최종 콘텐츠 구성, 다양성 확보

| Cycle | Focus | 산출물 |
|-------|-------|---------|
| 13–14 | 추가 기믹/스테이지 검토 | Scope decision (v1.0 vs v2.0) |
| 15–16 | UI/UX 최종 폴리시 | Control clarity, Visual hierarchy |
| 17–18 | 최종 밸런싱 패스 | Final statistics |

### **Phase 4: 완성 & 로드맵 수립 (Cycles 19–25)**
**목표**: 빌드 완성, 문서화, 다음 단계 제시

| Cycle | Focus | 산출물 |
|-------|-------|---------|
| 19–20 | 최종 QA & 버그 fix | Build validation |
| 21–22 | 문서 & README 작성 | Portfolio-ready docs |
| 23–24 | 수익화 전략 & 시퀄 로드맵 | Monetization proposal, Roadmap v2.0 |
| 25+ | 최종 보고서 & 아카이빙 | Learnings summary |

---

## 📊 데이터 수집 & 추적

### 각 Cycle별 기본 데이터

- Player Win Rate: X%
- Average Game Duration: X분
- Most Used Unit: [Unit name]
- FPS Average: X (min/max range)
- Memory Peak: X MB
- Critical Issues: [Count]
- Reported Improvements: [Count]


### 파일 구조

wiki/reports/
├── castle-busters-cycle-1.md      (Compile & Baseline)
├── castle-busters-cycle-2.md      (Mechanics)
├── castle-busters-cycle-3.md      (Playtest 30×)
├── castle-busters-cycle-4.md      (Balance Analysis)
├── castle-busters-cycle-5.md      (Improvement Plan)
├── castle-busters-cycle-6-10.md   (Phase 2 Summary)
├── castle-busters-cycle-11-15.md  (Phase 3 Summary)
├── castle-busters-cycle-16-20.md  (Phase 4 Summary)
└── castle-busters-final-status.md (Completion Report)


---

## ✅ 성공 기준 (Definition of Done)

- ✅ **20+ 사이클** 완료 & 문서화
- ✅ **게임 플레이**능 (크래시 0, 심각한 버그 0)
- ✅ **플레이어 승률** ±20% 범위 내 균형
- ✅ **성능 안정** (60 FPS, <500MB)
- ✅ **README & 문서** 포트폴리오 완성도
- ✅ **로드맵** v2.0 / 시퀄 방향 제시

---

## 🎯 다음 단계

1. **Cycle 1 시작**: 프로젝트 상태 스캔 & 컴파일 검증
2. **Executor** 역할: 실제 플레이테스트 & 데이터 수집
3. **Architect** 역할: 개선 전략 수립
4. **Critic** 역할: 품질 검증 & 피드백

**시작 일자**: 지금  
**예상 완료**: 25 사이클 (병렬 작업 시 단축 가능)

