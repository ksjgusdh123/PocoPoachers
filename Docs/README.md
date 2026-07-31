# PocoPoachers 문서

Escape From Duckov 스타일 **탈출·파밍·성장** 게임. 쉘터 거점 → 행성 레이드 → 파밍·전투 → 복귀·성장 루프.

| 경로 | 설명 |
|------|------|
| `PocoPoachers/` | Unity 클라이언트 |
| `Server/` | 마스터(매치메이킹) 서버 (`Server.sln`) |
| `Docs/` | 프로젝트 문서 |

생존 지표 **배터리**(시간 감소, 0 시 사망). 협동은 호스트 권위 P2P, **최대 호스트 1 + 게스트 2 = 3인**(4인 아님).

---

## 작업별 읽을 문서

**해당 작업 행 1~2개만** 읽을 것. `Docs/design/` 일괄 열람 금지.

| 작업 | 읽을 문서 |
|------|-----------|
| 게임 컨셉·비전·엔딩 | [design/overview.md](design/overview.md) |
| 레이드 맵 배치 규칙 (POI·동선·길찾기) | [design/map-composition.md](design/map-composition.md) |
| 처음 / 범위·미구현 | [design/todo.md](design/todo.md) |
| 씬·매니저·플로우 | [design/game-flow.md](design/game-flow.md) |
| 플레이어·전투·장비 | [design/player-combat.md](design/player-combat.md) |
| 인벤·창고·아이템 박스 | [design/inventory-exchange.md](design/inventory-exchange.md) |
| 쉘터·레이드·채광 | [design/shelter-raid.md](design/shelter-raid.md) |
| 행성/섹터 스펙 (안개·가시거리·위험도) | [design/planet-sectors.md](design/planet-sectors.md) |
| 적 AI | [design/enemy-ai.md](design/enemy-ai.md) |
| 멀티플레이 (개요) | [design/multiplayer.md](design/multiplayer.md) |
| 패킷 추가·디버깅 | [development/network-packets.md](development/network-packets.md) |
| CSV·fbs·제너레이터 | [development/code-generators.md](development/code-generators.md) + [datatable/id-ranges.md](datatable/id-ranges.md) |
| 맵 자동 생성 (기획 배경) | [design/map-generation.md](design/map-generation.md) |
| 맵 자동 생성 (사용법·팔레트·트러블슈팅) | [development/map-generator.md](development/map-generator.md) |
| DataTable 연동 | [design/data-tables.md](design/data-tables.md) |
| 강화·수리·제작 | [design/progression.md](design/progression.md) |
| 세이브 | [design/save.md](design/save.md) |
| UI · 디자인 시스템(테마·타이포·버튼) | [design/ui.md](design/ui.md) |
| 씬별 UI 배치 (어느 씬에 어떤 UI 프리팹이 있는지) | [design/ui-placement.md](design/ui-placement.md) |
| 치트 콘솔 | [development/cheat-console.md](development/cheat-console.md) |
| 에이전트 규칙 | [agents.md](agents.md) |

---

## 구현 현황

| 영역 | 상태 | 비고 |
|------|------|------|
| 타이틀 → 쉘터 → 레이드 루프 | ✅ | |
| 플레이어 이동·전투·인벤·장착 | ✅ | |
| 호스트-게스트 협동 (UDP P2P, 호스트+게스트 2) | ✅ | |
| 적 AI + 네트워크 동기화 | ✅ | 호스트 전용 시뮬레이션, 게스트 예측 없음 |
| 쉘터 업그레이드 + 창고 | ✅ | |
| 플레이어 스탯 강화 | ✅ | 최대 Lv.10 |
| 무기/방어구 강화 | ✅ | 최대 Lv.3 — 과거 "미구현" 기재는 오래된 정보였음 |
| 수리 | ✅ | 과거 "미구현" 기재는 오래된 정보였음 |
| 제작 (Crafting) | ✅ | 과거 "구현 없음" 기재는 오래된 정보였음 |
| 세이브 (인벤·쉘터레벨·장착·강화·내구도·Vital) | ✅ | 과거 문서보다 저장 범위가 넓음 |
| 채광 | ⚠️ | 1회 완료형 상호작용이지만 오브젝트가 소멸하지 않아 반복 채광 가능 |
| 행성 런타임 규칙 (전력/시간제한/안개/가시거리) | ❌ | 데이터만 존재, 선택 UI 잠금(`need_shelter_level`)만 실제 사용 |
| 맵 자동 생성 | ⚠️ | 에디터 도구로는 동작, 런타임 게임플레이 미연동·시드 없음 |
| 레이드 결과 화면 | ✅ | `RaidResultUI` 오버레이로 구현 (별도 `SC_Result` 씬은 미사용) |
| 레이드 탈출 | ✅ | 포털 진입만으로 종료, 추가 조건 없음 |
| 레이드 보스 | ❌ | 미구현 |

✅ 완료 · ⚠️ 부분 · ❌ 미구현 — 상세 작업: [design/todo.md](design/todo.md)

---

## 소스 코드 위치

| 기능 | 경로 |
|------|------|
| DataTable CSV | `PocoPoachers/DataTable/` |
| ID 범위 규칙 | [datatable/id-ranges.md](datatable/id-ranges.md) |
| 치트 콘솔 | `PocoPoachers/Assets/01. Scripts/Cheats/CheatConsole.cs` |
| 마스터 서버 | `Server/Server.sln` |
