# 쉘터 & 레이드

쉘터 업그레이드·창고·워크벤치, 레이드 진입/종료 흐름, 채광. 성장·강화(강화대·수리대·제작대 내부 로직)는 [progression.md](progression.md).

## 쉘터 (`SC_RocketShelter`)

거점 허브. 건설 시스템은 없고 **레벨 업그레이드 + 창고 + 워크벤치 상호작용** 중심.

### 주요 오브젝트

| 오브젝트 | 클래스 | 기능 |
|----------|--------|------|
| 업그레이드 단말 | `ShelterUpgradeTerminal` | `ShelterUpgradeUI` 오픈 |
| 창고 | `Storage` | 창고 인벤 ↔ 플레이어 인벤 드래그, 세이브 (고정 uid=1) |
| 강화대 | `EnhancementTable` | 플레이어 스탯 강화 UI |
| 무기/방어구 강화대 | `GunEnhancementTable` | 장비 강화 UI — **구현 완료** ([progression.md](progression.md)) |
| 수리대 | `RepairWorkbench` | 수리 UI — **구현 완료** ([progression.md](progression.md#수리)) |
| 제작대 | `CraftingTable` | 제작 UI — **구현 완료** ([progression.md](progression.md#제작-crafting)) |
| 우주선 | `Spaceship` | `PlanetSelectUI` → 레이드 출발 |
| 발전기 | `Generator` | 전력 저장/방전, 연료 투입(`GeneratorFuelTable`). 강화대·수리대·제작대가 모두 이 전력을 소비 |
| 수동 충전기 | `ManualCrank` | F 연타 시 `Generator`에 소량(+2.0, 최대 100.0) 강제 충전 |

### 쉘터 업그레이드 (`ShelterManager`)

1. `ShelterTable`에서 다음 레벨 데이터 조회
2. 플레이어 인벤 + 창고 재료 합산 검증, **플레이어 우선 차감**
3. 성공 시 레벨 갱신 → `SaveManager.SaveShelterLevel()` → `RoomSync.ShelterLevel(...)`로 게스트 브로드캐스트

**shelter.csv 예시:**

| 레벨 | 필요 재료 | 해금 티어 |
|------|-----------|-----------|
| 1 | 없음 | Tier 1 |
| 2 | 철 30 + 돌 20 | Tier 2 |
| 3 | 철 50 + 금 10 | Tier 3 |
| 4 | 금 30 | Tier 4 |

### 행성 잠금

`ShelterManager.IsPlanetUnlocked(planet)` → `CurrentLevel >= planet.NeedShelterLevel` (다른 필드는 미사용 — [planet-sectors.md](planet-sectors.md#실제-구현-상태-코드-확인)).

---

## 레이드 (`SC_Raid_{planetId}`)

### 진입

쉘터 `Spaceship` → `PlanetSelectUI`(잠긴 행성 비활성화) → `PlanetSlotUI` 클릭 → `SceneLoader.LoadPlanetScene(planetId)`. 호스트+게스트 세션이면 클릭 시점에 `H_LoadSceneT`를 먼저 브로드캐스트.

### 행성 데이터 (`planet.csv`)

| ID | 이름 | Tier | 필요 쉘터 Lv | 비고 |
|----|------|------|--------------|------|
| 1001 | 폐기 황무지 | 1 | 1 | 시간 제한 데이터 있음, 런타임 미적용 |
| 1002 | 동결 정점 | 2 | 2 | 동일 |
| 1003 | 화산 원자로 | 3 | 3 | 동일 |
| — | 미지의 심연 (Tier 4) | 4 | — | ❌ `planet.csv` 미등록 |

전체 스펙(안개/가시거리/수직성 테마/광물 티어/위험도)은 [planet-sectors.md](planet-sectors.md) 참고.

### 레이드 콘텐츠

| 시스템 | 클래스 | 설명 |
|--------|--------|------|
| 적 스폰 | `EnemySpawner` | 씬 시작 시 1회, NavMesh 랜덤 위치·무기/헬멧 랜덤 장착 (호스트만). 웨이브·재스폰 없음 |
| 아이템 박스 | `ItemSpawner` | 지면 배치, 랜덤 루트, uid 발급(1000~), 네트 스폰 |
| 박스 상호작용 | `ItemBox` | 근접 시 펄스 UI, Reveal UI, 인벤 교환 |
| 적 드롭 | `EnemyItemBoxDropper` | 사망 시 `LootBox`(자동 소멸형) 스폰 |
| 채광 | `BaseOre` | 아래 참고 |
| 포털 | `ScenePortal` | 쉘터 / 타이틀 / 테스트 맵 전환, 성공 시 결과 UI 경유 가능 |

### 아이템 박스 흐름

1. `ItemSpawner`가 맵에 박스 배치, 스택 불가 아이템에 uid 부여
2. 플레이어 근접 → `ProximityDetector` → 펄스 UI (무기 재장전 중엔 억제)
3. 호스트가 `RoomSync`로 스폰/상태 동기화
4. `ItemBoxRevealUI`에서 아이템 확인 후 인벤으로 이동 — 상세 규칙: [inventory-exchange.md](inventory-exchange.md)

### 채광 (`BaseOre`)

- `MineralTable`에서 id로 데이터 로드, `_currentHp = MaxHp` 설정하지만 **HP는 어디서도 감소하지 않는다** — 필드만 존재하는 죽은 상태
- 실제로는 **고정 시간(기본 2초) 코루틴 상호작용**으로 완료: `OnInteract` → 게이지 → `drop_item_id`/`drop_amount` 지급
- 재상호작용(`OnInteractExit`) 시 채광 취소, 진행도 저장 안 됨
- **채광 완료 후 오브젝트가 파괴/비활성화되지 않는다** — 동일 광물을 반복 채광할 수 있는 것으로 보임(의도 여부 불명, [todo.md](todo.md) 참고)

| Mineral ID | 드롭 아이템 |
|------------|-------------|
| 1 (돌) | ingredient 801 |
| 2 (철) | ingredient 802 |
| 3 (금) | ingredient 803 |

> ID 범위 마이그레이션(3001~) 미완료: [datatable/id-ranges.md](../datatable/id-ranges.md)

### 레이드 종료

레이드 탈출 지점은 `EscapeZone`(`SceneExitBase` 파생) — **살아있는 팀원 전원이 구역 안에 5초** 있어야 발동한다. 다운(구조 대기)된 팀원이 있으면 살려야 하고, 한 명이라도 벗어나면 게이지가 0으로 리셋된다. 판정은 호스트만 하고(위치·생존 상태를 이미 받고 있음), 게스트는 `H_EscapeState`로 게이지와 결과창만 맞춘다. 상호작용으로 즉시 이동하는 포털은 같은 베이스의 `ScenePortal`이다.


**성공(포털 탈출):** `ScenePortal`의 `_showResultUI` 플래그가 true면 `RaidResultUI.ShowSuccess(confirm)` 오버레이 표시 후 전환, false면 즉시 전환. 별도 아이템 회수 임계치 등 추가 조건 없음 — 포털에 진입하면 끝.

**실패(팀 전멸):** `PlayerController.CheckRaidWipe()`가 매 프레임 생존자 확인 → 전원 사망 시 `RaidResultUI.ShowFailure()`. 확인 버튼은 호스트에게만 노출되며 확인 시 쉘터로 전환.

`RaidResultUI`는 씬 전환 없이 현재 레이드 씬 위에 페이드인되는 **오버레이 UI**다 — `RaidStats`(경과시간·킬수)를 표시한다. 저장소에 별도로 존재하는 `SC_Result` 씬 에셋은 이 흐름에서 사용되지 않는 것으로 보인다.

레이드 탈출 후 최종 보스 콘텐츠는 없다 — 보스 전투/클리어 조건 자체가 설계·구현 모두 미확정([todo.md](todo.md)).

### 창고 세이브

세이브 키: `"storage"` — [save.md](save.md) 참고.
