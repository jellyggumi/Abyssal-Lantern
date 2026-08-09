# Resource manifest — castle-war (Stage 1)

- run-id: 20260809-castle-war-stage1

## Carried in (existing, audited by usage in build) [OBSERVED]

- `Assets/Sprites/` 픽셀아트 유닛/블록/배경 세트 (3 유닛 × 팀 틴트)
- `Assets/Prefabs/` 유닛/블록/기믹 프리팹 (SpikeTrap, EruptionVent 등)
- `Assets/Resources/GeneratedUnitFrames/` 런타임 애니메이션 프레임
- TextMesh Pro + KoreanFontSupport 동적 폰트

## To produce (owner per CLAUDE.md §3)

| Resource | Owner tool | Target path | Cycle |
|---|---|---|---|
| 진영 배너/키아트 (청·홍) | Codex CLI | design/concept/ → Assets/Sprites/Factions/ | Stage 3 |
| war-bar HUD 아트 | Codex CLI | design/concept/ → Assets/Sprites/UI/ | Stage 2–3 |
| BGM 전장 루프 + 스팅어 2종 | Gemini (playwriter) | Assets/Resources/Audio/BGM/ | Stage 3 |
| SFX 발사/명중/붕괴 세트 | rfxgen | Assets/Resources/Audio/SFX/ | Stage 3 |

모든 생성 리소스는 `.provenance.json` 동반, 감사 전 `Assets/` 진입 금지.

## Build artifacts (gitignored here, deployed to pages repo)

- `Builds/WebGL/castle-war/` → `jellyggumi.github.io/games/castle-war/`
