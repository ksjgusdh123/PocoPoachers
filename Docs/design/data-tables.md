# DataTable 연동

CSV→FlatBuffer→C# 파이프라인, 테이블 클래스 사용법. ID 범위 규칙은 [datatable/id-ranges.md](../datatable/id-ranges.md).

## 파이프라인

```
PocoPoachers/DataTable/*.csv
    ↓ (제너레이터)
Resources/JsonData/*.json
    ↓ (런타임 로드)
Generated/DataTable/*Table.cs
```

제너레이터: `Core/Editor/` (테이블·패킷 생성)

## ID 규칙

상세 범위: [datatable/id-ranges.md](../datatable/id-ranges.md)

**의도적 ID 공유:** `ItemData.id` == `GunStatData.id` == `ArmorStatData.id` (1:1)

## 테이블별 사용처

| CSV | 생성 클래스 | 주요 필드 | 사용처 |
|-----|-------------|-----------|--------|
| `item.csv` | `ItemTable`, `ItemData` | type, effect, max_stack, prefab | 인벤, 장착, 스폰, UI |
| `gun_stat.csv` | `GunStatTable`, `GunStatData` | damage, rpm, magazine, spread | `GunBase`, AI |
| `armor_stat.csv` | `ArmorStatTable`, `ArmorStatData` | defense_rate, hp_bonus | 방어구 장착 |
| `enemy.csv` | `EnemyTable`, `EnemyData` | max_hp, move_speed, ranges | `EnemyStat`, AI |
| `mineral.csv` | `MineralTable`, `MineralData` | drop_item_id, drop_amount | `BaseOre` |
| `planet.csv` | `PlanetTable`, `PlanetData` | tier, need_shelter_level | 행성 선택 (잠금만) |
| `shelter.csv` | `ShelterTable`, `ShelterData` | need_item1/2 | `ShelterManager` |
| `enhancement_cost.csv` | `EnhancementCostTable` | stat, level, need_item | `PlayerEnhancement` |
| `repair_cost.csv` | `RepairCostTable` | item_id, need_item | `RepairWorkbenchUI` |
| `localization.csv` | `LocalizationTable` | key, ko, en | `LocalizationManager` |
| `sound.csv` | `SoundTable`, `SoundData` | key, type, path | `SoundManager` |
| `skill.csv` | `SkillTable`, `SkillData` | skill, cooldown, speed, distance, duration, power, clip_name (범용 컬럼, 스킬마다 재해석) | `SkillManager`, `DodgeRollSkill`, `RetreatSkill`, `HealSkill` |

## ItemType

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

## EffectType (소모품)

`HP`, `Hunger`, `Thirst`, `Stamina` — `ItemUseSystem`에서 처리.

## SoundType

`BGM`, `SFX`, `UI` — `SoundManager` / `UISoundManager` 분기.

## 미연동 데이터

`planet.csv`의 런타임 필드:

| 필드 | 의도 | 현재 |
|------|------|------|
| `need_power` | 행성 진입 전력 요구 | 미적용 |
| `use_time_limit` | 시간 제한 사용 여부 | 미적용 |
| `max_session_time` | 세션 최대 시간 | 미적용 |
| `fog_density` | 안개 밀도 | 미적용 |
| `draw_distance` | 시야 거리 | 미적용 |

## 네트워크 패킷

FlatBuffer 스키마 → `Generated/FlatBuffer/` — [multiplayer.md](multiplayer.md) 참고.
