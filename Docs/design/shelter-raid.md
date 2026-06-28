# 쉘터 & 레이드

## 쉘터 (`SC_Shelter`)

거점 허브. 건설 시스템은 없고 **레벨 업그레이드 + 창고 + 워크벤치 상호작용** 중심.

### 주요 오브젝트

| 오브젝트 | 클래스 | 기능 |
|----------|--------|------|
| 업그레이드 단말 | `ShelterUpgradeTerminal` | `ShelterUpgradeUI` 오픈 |
| 창고 | `Storage` | 창고 인벤 ↔ 플레이어 인벤 드래그, 세이브 |
| 강화대 | `EnhancementTable` | 플레이어 스탯 강화 UI |
| 무기 강화대 | `GunEnhancementTable` | 무기 드롭 슬롯 (로직 미완) |
| 수리대 | `RepairWorkbench` | 수리 UI (로직 미완) |
| 우주선 | `Spaceship` | `PlanetSelectUI` → 레이드 출발 |

### 쉘터 업그레이드 (`ShelterManager`)

1. `ShelterTable`에서 다음 레벨 데이터 조회
2. 플레이어 인벤 + 창고 재료 합산 검증 (플레이어 우선 차감)
3. 성공 시 `SaveManager.SaveShelterLevel()`

**shelter.csv 예시:**

| 레벨 | 필요 재료 | 해금 티어 |
|------|-----------|-----------|
| 1 | 없음 | Tier 1 |
| 2 | 철 30 + 돌 20 | Tier 2 |
| 3 | 철 50 + 금 10 | Tier 3 |
| 4 | 금 30 | Tier 4 |

### 행성 잠금

`ShelterManager.IsPlanetUnlocked(planet)` → `CurrentLevel >= planet.NeedShelterLevel`

---

## 레이드 (`SC_Raid_{planetId}`)

### 진입

쉘터 `Spaceship` → `PlanetSelectUI` → `PlanetSlotUI` 클릭 → `LoadPlanetScene(planetId)`

### 행성 데이터 (`planet.csv`)

| ID | 이름 | Tier | 필요 쉘터 Lv | 비고 |
|----|------|------|--------------|------|
| 1001 | 폐기 황무지 | 1 | 1 | 시간 제한 없음 |
| 1002 | 동결 정점 | 2 | 2 | 600초 제한 (미적용) |
| 1003 | 화산 원자로 | 3 | 3 | 600초 제한 (미적용) |

`need_power`, `use_time_limit`, `max_session_time`, `fog_density`, `draw_distance` 필드는 **선택 UI 잠금에 shelter_level만 사용**되고, 레이드 런타임에는 미적용.

### 레이드 콘텐츠

| 시스템 | 클래스 | 설명 |
|--------|--------|------|
| 적 스폰 | `EnemySpawner` | NavMesh 랜덤, 무기/헬멧 랜덤 장착 (호스트만) |
| 아이템 박스 | `ItemSpawner` | 지면 배치, 랜덤 루트, uid 발급, 네트 스폰 |
| 박스 상호작용 | `ItemBox` | Reveal UI, 인벤 교환 |
| 적 드롭 | `EnemyItemBoxDropper` | 사망 시 필드 박스 스폰 |
| 채광 | `BaseOre` | 상호작용 완료 시 광물 아이템 지급 |
| 포털 | `ScenePortal` | 쉘터 / 타이틀 / 테스트 맵 전환 |

### 아이템 박스 흐름

1. `ItemSpawner`가 맵에 박스 배치, 스택 불가 아이템에 uid 부여
2. 플레이어 근접 → `ProximityDetector` → Reveal UI
3. 호스트가 `RoomSync`로 스폰/상태 동기화
4. `ItemBoxRevealUI`에서 아이템 확인 후 인벤으로 이동

### 채광 (`BaseOre`)

- `MineralTable`에서 id로 데이터 로드
- `IInteractable`: 상호작용 → 채광 게이지 → `drop_item_id` / `drop_amount` 지급
- 재상호작용 시 채광 취소
- **1회 상호작용 완료형** (HP 기반 다회 타격 아님)

| Mineral ID | 드롭 아이템 |
|------------|-------------|
| 1 (돌) | ingredient 801 |
| 2 (철) | ingredient 802 |
| 3 (금) | ingredient 803 |

> ID 범위 마이그레이션(3001~)은 [datatable/id-ranges.md](../datatable/id-ranges.md) TODO 참고.

### 창고 세이브

세이브 키: `"storage"` — [save.md](save.md) 참고.
