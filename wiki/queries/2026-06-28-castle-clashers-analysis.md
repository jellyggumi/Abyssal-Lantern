# Castle Clashers Analysis & Unity Implementation Design Query

- **Query**: 루트경로를 여기로 잡고, 다음 $scrapling https://play.google.com/store/apps/details?id=com.epicoro.castleclashers&hl=ko 게임을 분석하고, $deep-research 게임을 unity로 구성하기위한 요소를 상세하게 파악해. $game-build-log-triage unity 리소스도 검색하고 찾아서 mvp 작성할꺼야. $brainstorm-ideas-existing 게임적인 요소와 연출, 에니메이션을 모두 파악해서 구현계획을 설계하자.
- **Date**: 2026-06-28
- **Linked Report**: [[wiki/reports/castle-clashers-analysis-and-design]]
- **Linked Source**: [[wiki/sources/castle-clashers]]

## Summary of Response

1. **Game Analysis**: Scraped and analyzed the Google Play Store page for Castle Clashers (com.epicoro.castleclashers). Identified core loop, mechanics, UI/UX, progression, and victory conditions.
2. **Unity Architecture**: Designed a physics-based destructible castle system (BFS flood-fill for structural integrity), drag-to-aim launching mechanics, and a unit state machine (Knight, Archer, Bomber).
3. **Unity Resources**: Recommended packages (URP, Cinemachine, Input System, TextMeshPro) and asset store resources (castle tilesets, fantasy character sprites, cartoon particle FX, casual game SFX).
4. **Brainstorming & Game Feel**: Designed visual effects (launch trails, impact explosions, screen shake), animations (idle, launch, flight, attack, hurt/die), and juice (hit stop, floating damage numbers, camera zoom/pan).
5. **Implementation Roadmap**: Created a detailed 5-phase roadmap spanning 12 days to build the MVP.
