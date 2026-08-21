# DataTable 연동

CSV→JSON→C# 파이프라인, 테이블 클래스 사용법. ID 범위 규칙은 [datatable/id-ranges.md](../datatable/id-ranges.md). 생성기 동작은 [development/code-generators.md](../development/code-generators.md).

## 파이프라인

```
PocoPoachers/DataTable/*.csv
    ↓ (Tools → Generator → Tables)
Assets/_Data/Resources/JsonData/*.json  +  Generated/DataTable/*Table.cs, *Data.cs
    ↓ (런타임 Resources.Load)
XxxTable.Instance.Get(id) / .All
```

각 CSV마다 독립적으로 `{Name}Data`(POCO) + `{Name}Table`(지연 로딩 싱글톤, `Dictionary<int, Data>`) 한 쌍이 생성된다 — 공유 제네릭 `Table<T>` 베이스는 없다. `type`/`*_type`/`*_mode` 컬럼은 자동으로 enum(`ItemType`, `GunType`, `FiringMode`, `SlotType`, `SoundType`, `EffectType` 등) 파일도 함께 생성한다.

게임 코드 접근은 두 경로가 섞여 있다: `DataManager`(`Core/Managers/`)는 `ItemTable`/`GunStatTable`/`ArmorStatTable`/`EnemyTable` 4개만 감싸는 얇은 정적 래퍼이고, 나머지 25개 테이블은 각 `XxxTable.Instance`를 코드에서 직접 호출한다.

두 테이블(`GunStatData`)은 손으로 작성한 partial 확장 파일이 생성 파일과 같은 폴더에 공존한다: `GunStatData.Color.cs`(총구 색 구조체), `GunStatData.Clone.cs`(`Clone()` 헬퍼) — 재생성해도 지워지지 않는다.

## ID 규칙

상세 범위: [datatable/id-ranges.md](../datatable/id-ranges.md)

**의도적 ID 공유:** `ItemData.id` == `GunStatData.id` == `ArmorStatData.id` == `GunPartData.id`(1:1). `CraftingRecipeData.id`/`GeneratorFuelData.id`도 결과/연료 아이템의 Item ID와 공유.

## CSV 전체 목록 (17개, `PocoPoachers/DataTable/`)

| CSV | 생성 클래스 | 주요 필드 | 사용처 |
|-----|-------------|-----------|--------|
| `item.csv` | `ItemTable`, `ItemData` | type, effect_type, effect_value, max_stack, weight, prefab | 인벤, 장착, 스폰, UI (69행) |
| `gun_stat.csv` | `GunStatTable`, `GunStatData` | gun_type, damage, rpm, spread 등 30컬럼 | `GunBase`, AI |
| `armor_stat.csv` | `ArmorStatTable`, `ArmorStatData` | defense_rate, max_hp_bonus, move_speed_multiplier | 방어구 장착 |
| `gun_part.csv` | `GunPartTable`, `GunPartData` | slot_type, compatible_gun_types, spread_multiplier | 파츠 장착 |
| `enemy.csv` | `EnemyTable`, `EnemyData` | max_hp, defense_rate, move_speed, detect/forget_range, fov_angle, attack_range | `EnemyStat` (탐지 관련 컬럼은 현재 미사용 — [enemy-ai.md](enemy-ai.md#데이터-배선-갭)) |
| `mineral.csv` | `MineralTable`, `MineralData` | max_hp(미사용), drop_item_id, drop_amount | `BaseOre` |
| `planet.csv` | `PlanetTable`, `PlanetData` | tier(미사용), need_shelter_level(사용), need_power/use_time_limit/max_session_time/fog_density/draw_distance(전부 미사용) | 행성 선택 — [planet-sectors.md](planet-sectors.md) |
| `shelter.csv` | `ShelterTable`, `ShelterData` | need_item_ids, need_item_counts, unlocked_planet_tier | `ShelterManager` — need_item은 `\|` 구분 목록, `ShelterData.Parsed.cs`의 `NeedItems`가 파싱 |
| `skill.csv` | `SkillTable`, `SkillData` | skill, cooldown, speed, distance, duration, power, clip_name (범용 컬럼, 스킬마다 재해석) | `SkillManager`, `DodgeRollSkill`, `RetreatSkill`, `HealSkill` |
| `character_level_cost.csv` | `CharacterLevelCostTable` | level, need_item1/2, stat_points | `PlayerEnhancement` (기체 레벨업 비용 + 레벨당 지급 스탯 포인트) |
| `item_enhancement_cost.csv` | `ItemEnhancementCostTable` | item_id, level, need_item1/2 | `GunEnhancementTableUI` |
| `repair_cost.csv` | `RepairCostTable` | item_id, need_item1/2 | `RepairWorkbenchUI` |
| `crafting_recipe.csv` | `CraftingRecipeTable` | result_item_id, result_count, need_item1~3 | `CraftingTableUI` |
| `generator_fuel.csv` | `GeneratorFuelTable`, `GeneratorFuelData` | id(=연료 item_id), power_seconds | `Generator.TryInsertFuel` |
| `quest.csv` | `QuestTable`, `QuestData` | npc_id, npc_name, name, description, goal_item_ids, goal_item_counts, reward_item_ids, reward_item_counts | `QuestListUI`/`QuestDescriptionUI` — goal/reward는 `\|` 구분 Item ID 목록(복수 아이템 지원), `QuestData.Parsed.cs`(손으로 쓴 partial)의 `GoalItems`/`RewardItems`가 파싱. 완료 시 보상 지급은 미구현 |
| `sound.csv` | `SoundTable`, `SoundData` | key, type, path (숫자 id 없음, key 기반) | `SoundManager` |
| `localization.csv` | `LocalizationTable` | key, ko, en | `LocalizationManager` |

## ItemType 목표 ID 범위

| 값 | ID 범위 (목표) | 예시 |
|----|----------------|------|
| Consumable | 100~199 | 구급약병 101 |
| Weapon | 200~299 | M1911 201 |
| ItemBox | 300~399 | — |
| Helmet | 400~499 | — |
| Armor | 500~599 | — |
| Bullet | 600~699 | — |
| Bag | 700~799 | — |
| Ingredient | 800~899 | 돌 801, 철 802, 금 803 |
| GunPart | 900~999 | — |

## EffectType (소모품)

`HP`, `Hunger`, `Thirst`, `Stamina` — `ItemUseSystem`에서 처리.

## SoundType

`BGM`, `SFX`, `UI` — `SoundManager` / `UISoundManager` 분기.

## 미연동 데이터 (검증됨)

`planet.csv`의 런타임 필드 중 실제로 읽히는 건 `id`/`planet_name`/`need_shelter_level`/`icon` 뿐이다:

| 필드 | 의도 | 현재 |
|------|------|------|
| `tier` | 행성 티어 표시 | 미적용 (아무 코드도 참조 안 함) |
| `need_power` | 행성 진입 전력 요구 | 미적용 |
| `use_time_limit` | 시간 제한 사용 여부 | 미적용 |
| `max_session_time` | 세션 최대 시간 | 미적용 (`RaidStats`는 표시만, 상한 비교 없음) |
| `fog_density` | 안개 밀도 | 미적용 (고정 `VisionConfig` 애셋 하나가 전 씬 공통 적용) |
| `draw_distance` | 시야 거리 | 미적용 |

`enemy.csv`의 `detect_range`/`forget_range`/`fov_angle`도 `EnemyStat`이 읽지 않는다 — 실제 탐지 사거리는 장착 무기 사거리로 런타임에 덮어써진다.

수직성 테마·광물 매장 티어·위험도/스폰 캡은 `planet.csv`에 대응 컬럼조차 없음 — 전체 설계 기준: [planet-sectors.md](planet-sectors.md)

## 네트워크 패킷

FlatBuffer 스키마 → `Generated/FlatBuffer/` — 별도 생성기(`PacketGenerator`), CSV 파이프라인과 무관. [multiplayer.md](multiplayer.md), [network-packets.md](../development/network-packets.md) 참고.
