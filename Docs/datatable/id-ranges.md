# DataTable ID 범위 정의

전체 테이블 간 ID 충돌 방지를 위한 범위 규칙.

CSV 원본: `PocoPoachers/DataTable/`

---

## ID 범위 할당

| 범위 | 테이블 | 비고 |
|---|---|---|
| `100 ~ 199` | Item — 소비 아이템 | |
| `200 ~ 299` | Item — 무기 / GunStat | GunStat ID = Item ID (1:1 연결) |
| `300 ~ 399` | Item — 상자/오브젝트 | |
| `400 ~ 499` | Item — 헬멧 / ArmorStat | ArmorStat ID = Item ID (1:1 연결) |
| `500 ~ 599` | Item — 갑옷 / ArmorStat | ArmorStat ID = Item ID (1:1 연결) |
| `600 ~ 699` | Item — 탄약 | |
| `700 ~ 799` | Item — 가방 | |
| `800 ~ 899` | Item — 재료 아이템 (채굴 드롭) | |
| `1001 ~ 1999` | Planet | 씬 이름 컨벤션: `SC_Raid_{id}` |
| `2001 ~ 2999` | Enemy | ⚠️ TODO: 현재 1,2,3 → 2001,2002,2003 으로 변경 필요 |
| `3001 ~ 3999` | Mineral (광물 오브젝트) | ⚠️ TODO: 현재 1,2,3 → 3001,3002,3003 으로 변경 필요 |
| `4001 ~ 4999` | Shelter | |
| `5001 ~ 5999` | EnhancementCost (PlayerEnhancement 강화 재료) | stat(EnhancementStatType 이름) + level 조합당 1행 |
| `6001 ~ 6999` | RepairCost (수리 재료) | item_id(수리 대상 무기/헬멧/갑옷 ID)당 1행, 고정 비용 |

---

## TODO

Enemy·Mineral ID 마이그레이션 등 미완 작업은 [design/todo.md](../design/todo.md)에서 통합 관리합니다.

### Enemy ID 변경 (→ 2001 범위)

- `PocoPoachers/DataTable/enemy.csv` id: 1→2001, 2→2002, 3→2003
- `enemy.json` 동일하게 수정 (또는 테이블 제너레이터 재실행)
- 씬 내 **Enemy 프리팹**의 `EnemyStat._enemyId` 인스펙터 값 동일하게 수정 필요

### Mineral ID 변경 (→ 3001 범위)

- `PocoPoachers/DataTable/mineral.csv` id: 1→3001, 2→3002, 3→3003
- `mineral.json` 동일하게 수정 (또는 테이블 제너레이터 재실행)
- 씬 내 **BaseOre** 오브젝트의 `_id` 인스펙터 값 동일하게 수정 필요

---

## 씬 이름 컨벤션

| 테이블 | 규칙 | 예시 |
|---|---|---|
| Planet | `SC_Raid_{id}` | SC_Raid_1001, SC_Raid_1002, SC_Raid_1003 |

---

## 의도적 ID 공유 (변경 금지)

`GunStatData`와 `ArmorStatData`는 대응하는 `ItemData`와 ID를 공유한다.
코드에서 `ItemTable.Get(id)`로 아이템 정보, `GunStatTable.Get(id)`로 스탯을 동시에 조회하는 구조.
