# Resource manifest — castle-war (Stage 1)

- run-id: 20260809-castle-war-stage1

## Carried in (existing, audited by usage in build) [OBSERVED]

- `Assets/Sprites/` 픽셀아트 Knight/Archer 본체, 블록, 배경 세트 (청/홍 팀 틴트)
- `Assets/Prefabs/` Knight/Archer 본체 + Cannon/Barrel 전용 실루엣 + SpikeTrap/EruptionVent 기믹
- `Assets/Resources/GeneratedUnitFrames/` 런타임 애니메이션 프레임
- TextMesh Pro + bundled Noto Sans KR static SDF atlas (`Assets/Resources/Fonts/`) for WebGL-safe Korean glyph coverage

## To produce (owner per CLAUDE.md §3)

| Resource | Owner tool | Target path | Cycle |
|---|---|---|---|
| 진영 배너/키아트 (청·홍) | Codex CLI | design/concept/ → Assets/Sprites/Factions/ | Stage 3 |
| war-bar HUD 아트 | Codex CLI | design/concept/ → Assets/Sprites/UI/ | Stage 2–3 |
> 영상 경로 변경 [OBSERVED 2026-08-09]: Higgsfield 영상 잡은 유료 플랜
> 전용이라 사용 불가다(`seedance_2_0_mini` → `job_minimum_basic_plan_required`;
> 무료 잔액 23.67 < 최저가 영상 잡 `gemini_omni` 24). 대신 **프레임 합성**
> 경로로 전환한다: 기존 11페이지 웹툰 프롤로그 시나리오를 샷 리스트로 삼아
> `god-tibo-imagen`으로 패널 아트를 그리고, ffmpeg Ken Burns + BGM으로
> 인트로 영상을 조립한다. AI 영상 모델 없이 같은 결과물에 도달한다.

모든 생성 리소스는 `.provenance.json` 동반, 감사 전 `Assets/` 진입 금지.


## Produced in Stage 1 release-art pass

| Resource | Owner tool | Runtime target | Evidence |
|---|---|---|---|
| Social siege preview | god-tibo-imagen + ImageMagick audit/crop | `Assets/WebGLTemplates/CastleWar/social-preview.png` | `.provenance.json` companion; 1200×630; banner glyphs replaced with neutral castle crests |
| PWA/app icon family | god-tibo-imagen + ImageMagick point-resize | `apple-touch-icon.png`, `icon-192.png`, `icon-512.png` | Per-file `.provenance.json`; square audited icon source retained under `design/concept/release/` |
| 32 px favicon | ImageMagick purpose-drawn pixel primitives | `Assets/WebGLTemplates/CastleWar/favicon-32.png` | `.provenance.json`; authored directly at 32×32 |
| 발사/명중/콤보 SFX | rfxgen | `Assets/Resources/Audio/SFX/{launch,impact,combo}.wav` | Imported with Unity `.meta`; focused test confirms short mono 44.1 kHz clips |
| BGM 전장 루프 + 승리/패배 스팅어 | Higgsfield CLI `sonilo_music` + ffmpeg(libvorbis) | `Assets/Resources/Audio/BGM/{battle-loop,victory,defeat}.ogg` | 파일별 `.provenance.json`(프롬프트/모델/SHA-256); ffprobe 측정 60.02s / 10.03s / 10.03s; m4a→OGG 변환 이유는 Unity가 m4a를 소스 포맷으로 받지 않기 때문 |
| Castle facade skin family (Face/Crown/Edge/Base × intact/cracked/heavy) | OpenAI image generation + ImageMagick key/crop | `Assets/Resources/CastleSkin/{role}_{s0|s1|s2}.png` | 12 concept-source `.provenance.json` companions with SHA-256; runtime resolver and facade-neutrality EditMode checks; final WebGL gameplay capture |

## Build artifacts (gitignored here, deployed to pages repo)

- `Builds/WebGL/castle-war/` → `jellyggumi.github.io/games/castle-war/`
