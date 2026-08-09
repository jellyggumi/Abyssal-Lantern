# Defect register — castle-war

| ID | Sev | Defect | Evidence | Status | Response |
|---|---|---|---|---|---|
| D-001 | S1 | WebGL 빌드에서 모든 한글 HUD 문자열이 tofu(□)로 렌더 — `Font.GetPathsToOSFonts()`가 브라우저에서 빈 배열이라 KoreanFontSupport 폴백 생성 불가 | `qa/evidence/20260809-webgl-korean-tofu-before.png` (https://jellyggumi.github.io/games/castle-war/ 2026-08-09) | fix-in-progress | Noto Sans KR 서브셋 번들 + KoreanFontAssetBuilder(에디터 생성 동적 TMP 에셋, TMP 폴백 등록) + KoreanFontSupport 번들 우선 로드. 재빌드 후 동일 절차 스크린샷으로 재검증 |
