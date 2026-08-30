# DataTable ID 범위 정의

전체 테이블 간 ID 충돌 방지를 위한 범위 규칙. CSV 원본: `PocoPoachers/DataTable/`

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
| `800 ~ 899` | Item — 재료 아이템 | 801~806 = 채굴/연료 원석(801=돌, 802=철, 803=금), 851~855 = 화로 제련 결과물(주괴) |
| `900 ~ 999` | Item — 총기 파츠 / GunPart | GunPart ID = Item ID (1:1 연결) |
| `1001 ~ 1999` | Planet | 씬 이름 컨벤션: `SC_Raid_{id}`. 현재 1001~1003만 등록, Tier 4(섹터04) 미등록 |
| `2001 ~ 2999` | Enemy | ⚠️ **여전히 미마이그레이션** — CSV상 현재 값은 `1,2,3` (검증일 기준 최신 코드에서도 확인됨) |
| `3001 ~ 3999` | Mineral (광물 오브젝트) | ⚠️ **여전히 미마이그레이션** — CSV상 현재 값은 `1,2,3` (검증일 기준 최신 코드에서도 확인됨) |
| `4001 ~ 4999` | Shelter | |
| `5001 ~ 5999` | EnhancementCost (PlayerEnhancement 강화 재료) | stat(EnhancementStatType 이름) + level 조합당 1행 |
| `6001 ~ 6999` | RepairCost (수리 재료) | item_id(수리 대상 무기/헬멧/갑옷 ID)당 1행, 고정 비용 |
| `7001 ~ 7999` | Skill (AI 스킬) | `skill` 컬럼(Dodge/Retreat/Heal)이 동작 종류, `SkillManager._skillIds`로 AI별 할당 |
| `8001 ~ 8999` | ItemEnhancementCost (장비/파츠 강화 재료) | item_id + level 조합당 1행, 최대 레벨 3 |
| `9001 ~ 9999` | Quest | `npc_id`는 Dialogue처럼 참조용 정수(전용 NPC 테이블 없음). `goal_item_ids`/`reward_item_ids`는 `\|`로 구분한 Item ID 목록(콤마는 CSV 파서가 컬럼 구분자로 먹어서 못 씀), 대응하는 `*_counts`와 인덱스로 짝짓는다 — `QuestData.GoalItems`/`RewardItems`(손으로 쓴 partial, `QuestData.Parsed.cs`)가 파싱해서 `(itemId, count)` 목록으로 돌려줌. 완료 시 보상 지급 로직은 아직 연결 안 됨. `dialogue_choice.csv`의 `accept_quest_id`(0=없음)로 대화 선택지에서 `QuestManager.Accept` 호출 가능(`DialogueUI.SelectChoice`) |
| `10001 ~ 10999` | PlayerSkill (플레이어 스킬) | `skill` 컬럼이 `PlayerSkillId` enum 문자열(Dash/InstantReload/... 18종). `unlock_stat`+`unlock_level`(해금 조건)·`need_item_id`+`need_item_count`(획득 재료) 컬럼이 있으나 현재 전부 미기재(0/빈값) — 모든 스킬이 조건 없이 해금+보유 상태. AI용 `skill.csv`(7001~)와는 별개 테이블 |
| Item ID 공유 | CraftingRecipe (제작 레시피) | id = result_item_id (1:1), `CraftingRecipeTable.Get(itemId)`로 조회 |
| Item ID 공유 | FurnaceRecipe (화로 제련) | id = **투입** 광석의 Item ID (1:1), `FurnaceRecipeTable.Get(itemId)`로 결과 아이템(result_item_id)과 소요 시간(smelt_seconds) 조회. 레시피가 없는 아이템은 화로에 넣을 수 없다 — 돌(801)이 여기 해당 |
| Item ID 공유 | GeneratorFuel (발전기 연료) | id = 연료 아이템의 Item ID (1:1), `GeneratorFuelTable.Get(itemId)`로 전력 보충값(power_seconds) 조회. 연료 아이템은 800~899(Ingredient) 범위 사용 |

---

## 씬 이름 컨벤션

| 테이블 | 규칙 | 예시 |
|---|---|---|
| Planet | `SC_Raid_{id}` | SC_Raid_1001, SC_Raid_1002, SC_Raid_1003 |

주의: 쉘터 씬은 이 컨벤션과 무관하게 파일명이 `SC_RocketShelter`다(코드 상수명은 `SceneName.Shelter`) — [game-flow.md](../design/game-flow.md).

---

## 의도적 ID 공유 (변경 금지)

`GunStatData`와 `ArmorStatData`는 대응하는 `ItemData`와 ID를 공유한다.
코드에서 `ItemTable.Get(id)`로 아이템 정보, `GunStatTable.Get(id)`로 스탯을 동시에 조회하는 구조.

`CraftingRecipeData`도 동일 패턴을 따른다. id = 제작 결과 아이템의 Item ID.
`CraftingRecipeTable.Get(itemId)`로 해당 아이템의 레시피를 조회. 레시피가 없으면 null.

`GeneratorFuelData`도 동일 패턴을 따른다. id = 연료 아이템의 Item ID.
`GeneratorFuelTable.Get(itemId)`로 해당 아이템의 전력 보충값(power_seconds)을 조회. 연료가 아니면 null.

`GunPartData`도 동일 패턴을 따른다. id = 파츠 아이템의 Item ID.
`GunPartTable.Get(itemId)`로 해당 파츠의 슬롯/호환/스탯 정보를 조회.

`FurnaceRecipeData`도 동일 패턴이지만 id가 **결과가 아니라 투입** 아이템 ID다.
`FurnaceRecipeTable.Get(oreItemId)`로 그 광석을 녹였을 때 나오는 결과와 소요 시간을 조회.
`CraftingRecipe`(id = 결과)와 방향이 반대이므로 혼동 주의.
