# PocoPoachers 문서

Escape From Duckov 스타일 **탈출·파밍·성장** 게임. 쉘터 거점 → 행성 레이드 → 파밍·전투 → 복귀·성장 루프.

| 경로 | 설명 |
|------|------|
| `PocoPoachers/` | Unity 클라이언트 |
| `Server/` | 서버 (`Server.sln`) |
| `Docs/` | 프로젝트 문서 |

생존 지표 **배터리**(시간 감소, 0 시 사망). 협동은 호스트 권위 P2P(최대 4인).

---

## 작업별 읽을 문서

**해당 작업 행 1~2개만** 읽을 것. `Docs/design/` 일괄 열람 금지.

| 작업 | 읽을 문서 |
|------|-----------|
| 처음 / 범위·미구현 | [design/todo.md](design/todo.md) |
| 씬·매니저·플로우 | [design/game-flow.md](design/game-flow.md) |
| 플레이어·전투·장비 | [design/player-combat.md](design/player-combat.md) |
| 인벤·창고·아이템 박스 | [design/inventory-exchange.md](design/inventory-exchange.md) |
| 쉘터·레이드·채광 | [design/shelter-raid.md](design/shelter-raid.md) |
| 적 AI | [design/enemy-ai.md](design/enemy-ai.md) |
| 멀티플레이 (개요) | [design/multiplayer.md](design/multiplayer.md) |
| 패킷 추가·디버깅 | [development/network-packets.md](development/network-packets.md) |
| CSV·fbs·제너레이터 | [development/code-generators.md](development/code-generators.md) + [datatable/id-ranges.md](datatable/id-ranges.md) |
| DataTable 연동 | [design/data-tables.md](design/data-tables.md) |
| 강화·수리·성장 | [design/progression.md](design/progression.md) |
| 세이브 | [design/save.md](design/save.md) |
| UI | [design/ui.md](design/ui.md) |
| 치트 콘솔 | [development/cheat-console.md](development/cheat-console.md) |
| 에이전트 규칙 | [agents.md](agents.md) |

---

## 구현 현황

| 영역 | 상태 | 비고 |
|------|------|------|
| 타이틀 → 쉘터 → 레이드 루프 | ✅ | |
| 플레이어 이동·전투·인벤·장착 | ✅ | |
| 호스트-게스트 협동 (UDP P2P) | ✅ | |
| 적 AI + 네트워크 동기화 | ✅ | |
| 쉘터 업그레이드 + 창고 | ✅ | |
| 플레이어 스탯 강화 | ✅ | |
| 세이브 (인벤 + 쉘터 레벨) | ⚠️ | 강화·장착·Vital 미저장 |
| 채광 | ✅ | 1회 상호작용 완료형 |
| 수리 / 무기 강화 | ⚠️ | UI만 |
| 행성 런타임 규칙 / 제작 / `SC_Result` | ❌ | |

✅ 완료 · ⚠️ 부분 · ❌ 미구현 — 상세 작업: [design/todo.md](design/todo.md)

---

## 소스 코드 위치

| 기능 | 경로 |
|------|------|
| DataTable CSV | `PocoPoachers/DataTable/` |
| ID 범위 규칙 | [datatable/id-ranges.md](datatable/id-ranges.md) |
| 치트 콘솔 | `PocoPoachers/Assets/01. Scripts/Cheats/CheatConsole.cs` |
