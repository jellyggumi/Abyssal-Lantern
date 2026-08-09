# Defect register — castle-war

| ID | Sev | Defect | Evidence | Status | Response |
|---|---|---|---|---|---|
| D-001 | S1 | WebGL 빌드에서 모든 한글 HUD 문자열이 tofu(□)로 렌더 — `Font.GetPathsToOSFonts()`가 브라우저에서 빈 배열이라 KoreanFontSupport 폴백 생성 불가 | `qa/evidence/20260809-webgl-korean-tofu-before.png` (https://jellyggumi.github.io/games/castle-war/ 2026-08-09) | fix-in-progress | Noto Sans KR 서브셋 번들 + KoreanFontAssetBuilder + KoreanFontSupport 번들 우선 로드. D-002로 인해 정적 베이크로 전환 |
| D-002 | S1 | D-001 1차 수정(동적 아틀라스)의 WebGL 빌드가 로드 말미에 `RangeError: Maximum call stack size exceeded` (WASM invoke_viii↔invoke_iiii 상호 재귀) — 유력 원인: 싱글스레드 WASM의 얕은 스택에서 CJK 글리프 온디맨드 SDF 래스터화. codex-uiux 세션이 로컬 검증에서 발견, **배포 차단됨(라이브는 e3ceeac 유지)** | `qa/evidence/20260809-webgl-wasm-rangeerror-headless.png`, `qa/evidence/20260809-webgl-wasm-rangeerror-realchrome.png` (headless+실크롬 동일 재현) | fix-in-progress | KoreanFontAssetBuilder를 정적 베이크로 재작성: 빌드마다 소스에서 한글 347자+기호 추출 → TryAddCharacters → AtlasPopulationMode.Static 저장. 런타임 래스터화 제거. 재빌드 후 로컬 서버+Playwright(콘솔 에러 캡처 포함) 검증 통과 시에만 배포 |
