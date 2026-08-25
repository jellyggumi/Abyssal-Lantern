# 배포 차단 — pages 저장소 푸시 권한 (2026-08-13) — **2026-08-25 해소**

> **해소 기록**: 2026-08-25 세션에서 `gh auth status`가 이 세션의 활성 계정이
> `akillness`(`leeseockmin`이 아님)임을 확인했고, 실제 쓰기 권한을 임시 커밋
> 1개 push→즉시 revert→재push로 검증했다(`gh-pages`에 원상 복구, 최종 HEAD
> `3026af2`). 같은 세션이 `9267b264`(작업 80 이후) 소스를 빌드해
> `9c22e96`으로 정식 배포 완료 — 상세는 `production/task-manifest.md` #81.
> 아래 원문은 당시 상태 기록으로 보존한다.


- 대상 작업: 대장 #52 (가시성 v2 사후 판독)
- 소스 커밋: `682f4b80` (게임 변경 `9bd3494e` + 폰트 베이크 `682f4b80`), **origin 푸시 완료**
- 상태: **빌드 성공·검증 완료, 공개 배포만 막힘**

---

## 1. 무엇이 막혔나

`jellyggumi/jellyggumi.github.io`에 푸시가 거부된다.

```
remote: Permission to jellyggumi/jellyggumi.github.io.git denied to leeseockmin.
fatal: ... The requested URL returned error: 403
```

| 시도 | 결과 |
|---|---|
| HTTPS (osxkeychain 기본 자격증명) | **403** — `leeseockmin`에게 권한 없음 |
| HTTPS + `gh auth git-credential` | **403** — 같은 계정(`gh auth status`: `leeseockmin`, scopes `gist, read:org, repo`) |
| SSH (`git@github.com:...`) | **Permission denied (publickey)** — 이 세션에 키 없음 |
| 소스 저장소(`jellyggumi/castle-war`) 푸시 | **성공** — 차단은 pages 저장소 한정 |

`CLAUDE.md` §6은 "push access confirmed"라 적어두었지만, 그 확인은 **다른 계정**에서
이루어진 것으로 보인다 `[INFERENCE]`. 과거 배포(대장 #37·#42·#48 등)는 동시 세션
(`akillness`)이 수행한 기록이 있다. 이 세션의 활성 계정으로는 재현되지 않는다.

**근거 없이 우회하지 않았다** — 자격증명을 새로 만들거나 리모트를 조용히 바꾸는 것은
`CLAUDE.md` §7(다른 세션 작업 침범 금지)과 권한 경계를 동시에 건드린다.

---

## 2. 빌드는 검증됐다 (배포만 남았다)

| 항목 | 결과 |
|---|---|
| 빌드 | `result=Succeeded`, **96,104,096 bytes** |
| 실제 에러 | `error CS` 0 · 셰이더 에러 0 · `BuildFailedException` 0 |
| 보고된 `errors=2` | **전부 MCP 허브 연결 실패 로그** (`Authorization failed`, `Version handshake failed`) — 게임과 무관 |
| 압축 | 3종 전부 gzip(`1f8b`) + `decompressionFallback=true` (GitHub Pages가 `Content-Encoding`을 안 보내므로 필수) |
| gzip 무결성 | 해제 성공 — data 96,080,003 / wasm 62,452,626 / framework 359,714 bytes |
| 부팅 | 로컬 서빙에서 로딩바 숨김 + progress **100%** + JS 에러 **0** (`qa/evidence/visibility-v2/webgl-boot-title.png`) |
| 글리프 | `FontGlyphAudit` **411/411** (신규 음절 5자 포함 — 두부 없음) |

---

## 3. 배포하는 방법 (권한 있는 세션/사람이 실행)

빌드 산출물은 소스 커밋에서 **재현 가능**하므로 바이너리를 저장소에 넣지 않았다
(준비했던 번들이 363MB였고, 재현 가능한 것을 커밋하는 것은 비용만 남긴다).

```bash
# 1) 소스를 그 커밋에 두고 (에디터는 닫아야 한다 — 프로젝트 락)
cd ~/Desktop/castle-war
git checkout 682f4b80        # 또는 이후 팁

# 2) 빌드
"/Applications/Unity/Hub/Editor/6000.5.6f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -projectPath . -buildTarget WebGL \
  -executeMethod WebGLReleaseBuild.Build -logFile ./webgl-build.log
# 확인: webgl-build.log 에 result=Succeeded, 그리고 error CS 0건

# 3) pages 저장소로 동기화 — 제외 규칙을 반드시 유지한다
git clone https://github.com/jellyggumi/jellyggumi.github.io.git /tmp/pages
rsync -a --delete \
  --exclude '*_DoNotShip*' \
  --exclude '.omc' --exclude '.omc/**' \
  --exclude '.serena' --exclude '.serena/**' \
  --exclude '*.log' \
  Builds/WebGL/castle-war/ /tmp/pages/games/castle-war/

# 4) 산출물만 명시적으로 스테이징 (games/castle-war/Build 만 바뀐다)
cd /tmp/pages && git add -- games/castle-war/Build
git commit -m "castle-war: last shot stays readable — arc, impact marker, turn readback (source 682f4b80)"
git push origin HEAD
```

### 제외 규칙이 왜 규칙인가

- `*_DoNotShip*` — Burst 디버그 정보. 대장 #42가 라이브에서 **404**를 확인해 동기화
  제외가 작동함을 증명했다.
- `.omc/**` — 대장 #28이 **OMC 세션 상태가 빌드 출력에 섞여 공개 사이트로 나갈 뻔한**
  사고를 기록했다. 그 이후 rsync exclude로 원천 차단한다.
- `.serena/**` — 같은 종류(다른 도구의 세션 상태). 이번에 새로 추가했다.

### 배포 후 검증 (대장이 매번 요구하는 것)

1. 서빙 3종의 SHA-256이 로컬 빌드와 일치
2. Pages 빌드 status `built`
3. 부팅 확정 — 로딩바 숨김 + progress 100% + JS 에러 0
4. `*_DoNotShip*` 경로가 **404**

---

## 4. 준비돼 있던 것 (참고)

이 세션이 `/tmp/pages-deploy`에 커밋 `d43d6a7`까지 만들어 두었으나 **푸시하지 못했고,
`/tmp`는 영속되지 않는다.** 그 커밋의 diff는 위 절차가 그대로 재생산한다:

```
games/castle-war/Build/castle-war.data.unityweb        74,904,338 → 75,659,671
games/castle-war/Build/castle-war.wasm.unityweb        15,348,491 → 15,384,818
games/castle-war/Build/castle-war.framework.js.unityweb 81,079 → 81,079
games/castle-war/Build/castle-war.loader.js            1줄 변경
```
