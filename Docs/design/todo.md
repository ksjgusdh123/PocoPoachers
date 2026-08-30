# TODO (향후 기획·구현)

미구현·부분 구현·코드 내 발견된 이상 동작을 한곳에 모은 목록. 코드 전수 분석(2026-07) 기준으로 재작성됨 — **수리·무기 강화·제작은 이미 구현되어 있어 과거 목록에서 제외됨.** 2026-08 후속 조사로 플레이어 스킬·파티 버프·화로 항목 추가([player-combat.md](player-combat.md), [progression.md](progression.md) 참고), 레이드 결과 화면(SC_Result) 관련 오래된 문서 오기는 수정 완료.

구현 현황 요약: [../README.md#구현-현황](../README.md#구현-현황)

**우선순위:** P0 필수 · P1 중요 · P2 개선·후순위

---

## P0 — 데이터 정합성

### DataTable ID 마이그레이션

ID 범위 규칙과 실제 데이터 불일치. 상세: [datatable/id-ranges.md](../datatable/id-ranges.md)

**Enemy (→ 2001~)**

- [ ] `enemy.csv` id: 1,2,3 → 2001,2002,2003
- [ ] `enemy.json` 갱신 (제너레이터 재실행)
- [ ] 씬 Enemy 프리팹 `EnemyStat._enemyId` 수정

**Mineral (→ 3001~)**

- [ ] `mineral.csv` id: 1,2,3 → 3001,3002,3003
- [ ] `mineral.json` 갱신
- [ ] 씬 `BaseOre._id` 수정

---

## P1 — 레이드·행성

### 행성 런타임 규칙 (`planet.csv`)

데이터는 있으나 레이드 진입 후 미적용 (`tier`도 포함해 확인된 것보다 더 많은 필드가 미사용):

| 필드 | 의도 | 작업 |
|------|------|------|
| `tier` | 행성 티어 | [ ] 사용처 결정 (표시용? 배율용?) |
| `need_power` | 진입 전력 요구 | [ ] 검증 로직 |
| `use_time_limit` | 시간 제한 on/off | [ ] 플래그 처리 |
| `max_session_time` | 세션 제한 시간 | [ ] 타이머 + 실패/퇴장, `RaidStats`와 연동 |
| `fog_density` | 안개 밀도 | [ ] `FogOfWarRenderer`/`VisionConfig` 행성별 파라미터화 |
| `draw_distance` | 시야 거리 | [ ] `PlayerVision`/`VisionConfig` 연동 |

관련: [shelter-raid.md](shelter-raid.md) · [planet-sectors.md](planet-sectors.md) · [data-tables.md](data-tables.md)

### enemy.csv 탐지 필드 미배선

- [ ] `EnemyStat.Awake()`가 `detect_range`/`forget_range`/`fov_angle`/`attack_range`를 읽지 않음 — 현재는 장착 무기 사거리(`AIWeaponController.UpdateBlackboardGunStat`)가 탐지 사거리를 대신 결정. 의도된 설계인지 확인 필요, CSV 컬럼이 죽은 데이터라면 제거 또는 배선 필요

관련: [enemy-ai.md](enemy-ai.md#데이터-배선-갭)

### 맵 배치 규칙 (단일 착지 포드 + 복귀 컴퍼스)

설계 확정: [map-composition.md](map-composition.md). 요약 — 섹터당 탈출 지점은 스폰과 동일한 포드 1개(다중 엑스필 아님), 길찾기는 복귀 방향만 안내.

- [ ] 스폰 포인트와 연동된 단일 포드 컴포넌트 정의 (현재 `ScenePortal`은 다중 배치 가능한 범용 트리거라 "스폰과 동일 지점"이라는 개념이 없음)
- [ ] 포드 복귀 방향만 가리키는 최소 HUD 화살표(컴퍼스) 구현 — 다른 POI 길찾기는 의도적으로 제외
- [ ] `EnemySpawner` 스폰 위치를 포드로부터의 거리 기준 클러스터로 재구성 (현재는 씬 시작 시 고정 배열 1회 스폰, 거리/존 개념 없음)

### 섹터 04(미지의 심연) 미등록

- [ ] `planet.csv`에 Tier 4 행 추가, 보스 코어 소비 조건 설계·구현

### 채광 오브젝트가 소멸하지 않음

- [ ] `BaseOre` 채광 완료 후 오브젝트가 파괴/비활성화되지 않아 반복 채광이 가능해 보임 — 의도 확인 후 (a) 1회성이면 소멸/비활성 처리, (b) 다회성이 목표면 `MineralTable.max_hp` 기반 HP 차감 로직 구현

관련: [shelter-raid.md](shelter-raid.md#채광-baseore)

---

## P1 — 스킬·화로

### player_skill.csv 해금·재료 조건 데이터 미기재

- [ ] 해금(`unlock_stat`/`unlock_level`)·획득(`need_item_id`/`need_item_count`) 로직은 완성됐으나 18개 스킬 전부 값이 비어있어(0) 실제로는 조건 없이 전부 해금+보유 상태 — 밸런스 데이터 채우기

관련: [player-combat.md](player-combat.md#해금unlock--2단계-게이트)

### 화로가 발전기 전력을 소비하지 않음

- [ ] 다른 워크벤치(수리대/강화대/제작대)는 모두 `Generator` 전력을 소비하는데 `Furnace`만 참조하지 않음 — 의도 확인 후 (a) 의도적 예외면 문서에만 명시, (b) 아니면 전력 소비 로직 추가

관련: [progression.md](progression.md#화로-제련)

### 원석(804~806) 채광원 없음

- [ ] 석탄/우라늄/레드 플라즈마 원석이 `item.csv`·`generator_fuel.csv`·`furnace_recipe.csv`엔 정의돼 있으나 `mineral.csv`에 대응 채광 오브젝트가 없어 레이드에서 획득 불가(치트로만 획득 가능) — 채광 오브젝트 추가 또는 다른 획득 경로 설계

관련: [progression.md](progression.md#재료-아이템-흐름)

---

## P1 — 멀티플레이

### 기능 버그

- [ ] **총기 발사 사운드 범위 미동기화** (`Combat.fbs`, `PacketHandler.Combat.cs`) — `G_Shoot`엔 `sound_range`가 있으나 `H_Shoot`엔 없어 다른 게스트에게 전파 안 됨
- [ ] 발자국 사운드 미전달

### UX

- [ ] 게스트 이탈 시 **호스트 재입장 대기** 처리 (`IngameMenuUI.OnHostLeft` — `onCancel`이 빈 스텁)
- [ ] 플레이어 이름 UI 입력 (`NetworkManager.cs` — 현재 `"Player"` 고정)

### 코드 정리

- [ ] `RoomSync.GunAmmoSave` 디버그 로그 제거 (`// TODO: 디버그 후 제거`)
- [ ] `PlayerController.RestoreEquippedSlots` 디버그 로그 제거
- [ ] `CraftingTableUI.HasRecipesOfType()` 죽은 코드 제거 또는 카테고리 버튼 활성화 판정에 사용
- [ ] `GunEnhancementTableUI`의 강화 배율 미리보기 공식이 `WorldEquipmentManager`와 중복 구현됨 — 공용 헬퍼로 통합 고려

관련: [multiplayer.md](multiplayer.md) · [network-packets.md](../development/network-packets.md)

---

## P2 — 맵 자동 생성

- [ ] 생성 결과(적 스폰 지점 등)를 실제 게임플레이 스폰 시스템과 연결 — 현재 완전히 미연동
- [ ] 재현 가능한 시드 옵션 추가 (현재 `Random` 무시드)
- [ ] Road Map 레이어
- [ ] `NavMeshModifier` 자동 부착 (배치 오브젝트 장애물 처리)
- [ ] 더미 프리팹 → 실제 아트 프리팹 교체

관련: [map-generation.md](map-generation.md) · [development/map-generator.md](../development/map-generator.md)

---

## P2 — 신규 시스템

### 레이드 보스 및 엔딩

핵심 루프 마지막 단계인 보스·엔딩 전부 미구현. 코어 경제와 최종 요구 자원은 [overview.md](overview.md#4-엔딩--보스-코어-경제-확정)에서 확정됨.

- [ ] 섹터 02·03·04 고유 보스 전투와 클리어 조건 구현 (섹터 01은 보스 없음)
- [ ] 보스 처치 시 코어 2개 드롭 및 섹터 04 진입 시 코어 1개 소비 로직 구현
- [ ] 엔딩 요구 자원(보스 코어 3개 + 오메가 광물 50개) 적립·검증 로직 구현
- [ ] 유한자원생성기 4단계 부품 조립과 중앙 콘솔의 지구 복귀 엔딩 구현

관련: [overview.md](overview.md) · [map-composition.md](map-composition.md) · [game-flow.md](game-flow.md) · [shelter-raid.md](shelter-raid.md)

> 레이드 결과 화면 자체는 `RaidResultUI` 오버레이로 이미 구현되어 있음 — 과거 문서의 "`SC_Result` 씬 미연동"은 오해였음(별도 씬을 쓰지 않는 방식으로 이미 대체 구현됨). 미사용 `SC_Result` 씬 에셋의 처리 여부만 결정하면 됨.

---

## P2 — 개선·폴리시

### 내구도 UX

런타임 내구도는 `WorldEquipmentManager` + 네트 동기화 존재, 툴팁에도 표시됨. 인벤 슬롯 아이콘 자체에 내구도 바가 있는지는 추가 확인 필요.

### 인벤토리 무게 제한

- [ ] `Inventory.AddItem`/`CanAddItem`이 `MaxWeight`를 강제하지 않음 — UI 표시용으로만 쓰이는지, 실제 제약으로 만들지 결정

관련: [inventory-exchange.md](inventory-exchange.md)

### 기타 코드 TODO

- [ ] `PacketGenerator` 생성 스텁 핸들러 실제 구현 — 신규 패킷 추가 시마다 반복 확인 필요

### 테스트 씬 정리

- [ ] `SC_Raid_Test`, `SC_ShelterTest`, `SC_Result` 용도 문서화 또는 제거
- [ ] `SC_Raid_1001` 테스트 씬 vs `LoadPlanetScene` 통합 검토

---

## 완료 시 체크리스트

항목 완료 후:

1. 이 파일에서 `[ ]` → `[x]` 갱신
2. 해당 기획 문서의 ⚠️/❌ 상태 수정
3. [구현 현황](../README.md#구현-현황) 표 갱신
