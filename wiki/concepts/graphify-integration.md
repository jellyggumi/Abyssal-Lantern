# Graphify Integration

## Summary

Graphify는 코드/URL 기반 지식 그래프 빌더로, llm-wiki vault와 함께 사용할 수 있다.

## 핵심 개념

- `graphify add <url>` — URL을 fetch하여 `raw/`에 저장하고 `graphify-out/graph.json`을 업데이트
- `graphify query "topic"` — BFS/DFS 방식으로 그래프를 탐색하여 질문에 답변
- `graphify explain "node"` — 특정 노드와 이웃 노드를 평이한 언어로 설명
- `graphify update` — 코드 파일 전용 (markdown 미지원)

## llm-wiki와의 관계

| 도구 | 역할 |
|------|------|
| `ingest-url.sh` | Scrapling 기반 raw 캡처 + `wiki/sources/` 스텁 생성 |
| `graphify add` | URL 캡처 + 자동 그래프 빌드 (vault root에서 실행) |
| `graphify query` | 그래프 기반 질문 탐색 (graph.json 필요, vault root에서 실행) |
| `index.md` + grep | 소규모 vault 검색 (그래프 없이도 충분) |

## 권장 워크플로

소규모 vault: `ingest-url.sh` → `index.md`/grep으로 검색  
대규모 vault: `graphify add` 병행 사용 → `graphify query`로 의미 기반 탐색

## Related Pages

- [[index]]
