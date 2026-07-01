# TODO (향후 기획·구현)

미구현·부분 구현·코드 내 `TODO`를 한곳에 모은 목록입니다.  
구현 현황 요약: [../README.md#구현-현황](../README.md#구현-현황)

**우선순위:** P0 필수 · P1 중요 · P2 개선·후순위

---

## P0 — 게임플레이 핵심

### 수리 시스템

현재: `RepairWorkbenchUI` — 재료 표시만, 수리 버튼 미동작.

- [ ] `OnClickRepair()` 수리 로직 구현 (`RepairWorkbenchUI.cs`)
- [ ] 인벤 슬롯·장착 무기 **내구도 표시** (`? / ?` → 실제 값)
- [ ] 재료 차감 + 내구도 복구 (`RepairCostTable`, `WorldEquipmentManager` 연동)
- [ ] 수리 불가 조건 처리 (내구도 만땅, 재료 부족, 수리 대상 아님)

관련: [progression.md](progression.md) · `repair_cost.csv` · `RepairWorkbenchUI.cs:53,86`

### 무기 강화

현재: `GunEnhancementTable` 상호작용 + 드롭 슬롯만 존재.

- [ ] `GunEnhancementTableUI` (또는 동등 UI) 구현
- [ ] 강화 비용 테이블 설계·연동 (현재 전용 CSV 없음)
- [ ] 강화 로직 (내구도/스탯 상승 등 기획 확정 필요)
- [ ] `GunEnhancementDropHandler` ↔ 강화 실행 연결

관련: [progression.md](progression.md) · `GunEnhancementTable.cs`

### 세이브 확장

현재: 플레이어 인벤 + 창고 + 쉘터 레벨만 저장.

- [ ] `PlayerEnhancement` 강화 레벨 저장/로드
- [ ] 장착 장비 (무기·헬멧·갑옷·가방) 저장/로드
- [ ] Vital 현재값 (HP·배터리·스태미나) 저장 여부 기획 확정 후 구현
- [ ] `WorldEquipmentManager` 내구도(uid별) 저장/로드
- [ ] 자동 저장 시점 정의 (쉘터 복귀, 업그레이드, 종료 등)

관련: [save.md](save.md) · `SaveManager.cs`

---

## P1 — 레이드·데이터

### 행성 런타임 규칙 (`planet.csv`)

데이터는 있으나 레이드 진입 후 미적용.

| 필드 | 의도 | 작업 |
|------|------|------|
| `need_power` | 진입 전력 요구 | [ ] 검증 로직 |
| `use_time_limit` | 시간 제한 on/off | [ ] 플래그 처리 |
| `max_session_time` | 세션 제한 시간 | [ ] 타이머 + 실패/퇴장 |
| `fog_density` | 안개 밀도 | [ ] `FogOfWarRenderer` 연동 |
| `draw_distance` | 시야 거리 | [ ] `PlayerVision` 연동 |

관련: [shelter-raid.md](shelter-raid.md) · [data-tables.md](data-tables.md)

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

## P1 — 멀티플레이

### 기능 버그

- [x] **게스트 입장 시 장비 내구도 미전달** (`RoomManager.SendHostEquipToGuest`) — ItemUid + H_Durability 전송
- [x] **게스트 입장 시 아이템 박스 ItemUid 누락** (`RoomManager.SendWorldStateToGuest`) — H_ItemSpawnT.ItemUids 전송
- [x] **게스트 입장 시 방어구/가방 장착 상태 누락** (`SendHostEquipToGuest`) — ArmorMount·BagMount H_Equip 포함
- [x] **게스트 입장 시 호스트 HP/스탯 초기값 누락** (`SendAllPlayerStatsToGuest`) — late join 시 H_StatSync 즉시 전송
- [x] **장비 능력치 미동기화** — `ApplyRemoteArmorStats`, `ApplyNetworkStats`로 MaxHp·방어력 반영
- [x] **Sandbag 파괴 미동기화** (`Sandbag.HandleDie`) — H_SandbagDestroy 브로드캐스트
- [x] **쉘터 업그레이드 미동기화** (`RoomSync.ShelterLevel`) — H_ShelterLevel 브로드캐스트
- [x] **H_ConsumeItemResult 핸들러 비어있음** (`OnH_ConsumeItemResult`) — `ApplyConsumableEffect` 연동

### 비주얼

- [ ] **총기 발사 사운드 미동기화** (`PacketHandler.Combat.cs OnH_Shoot`) — H_ShootT에 SoundId 추가 후 3D 재생
- [ ] 발자국 사운드 미전달

### UX

- [ ] 게스트 이탈 시 **호스트 재입장 대기** 처리 (`IngameMenuUI.cs:100`)
- [ ] 플레이어 이름 UI 입력 (`NetworkManager.cs` — 현재 `"Player"` 고정)

관련: [multiplayer.md](multiplayer.md)

---

## P2 — 신규 시스템

### 제작 (Crafting)

- [ ] 기획: 레시피 구조, 재료 소스, UI 위치
- [ ] `craft` 테이블 (또는 기존 테이블 확장) 설계
- [ ] 워크벤치 상호작용 + UI 구현

### 결과 화면

- [ ] `SC_Result` 씬 연동 (레이드 클리어/사망/시간 초과)
- [ ] `SceneLoader` 진입점 추가
- [ ] 획득 요약, 경험/보상 표시 기획

### 레이드 탈출·보스

핵심 루프 4단계(탈출·보스) 미구현.

- [ ] 최종 보스 전투/클리어 조건 정의
- [ ] 탈출 포털 vs 우주선 복귀 흐름 정리
- [ ] `SC_Result` 또는 쉘터 직행 분기

관련: [game-flow.md](game-flow.md) · [shelter-raid.md](shelter-raid.md)

---

## P2 — 개선·폴리시

### 채광

현재: 1회 상호작용 완료형. `MineralTable.max_hp` 미사용.

- [ ] HP 기반 다회 채굴 여부 기획 확정
- [ ] 채굴 도구/무기 연동 필요 시 설계

### 내구도 UX

런타임 내구도는 `WorldEquipmentManager` + 네트 동기화 존재. UI 미연결.

- [ ] 인벤·장비 슬롯 내구도 바 표시
- [ ] 수리대·무기 강화대와 데이터 연결

### 기타 코드 TODO

- [ ] `PacketGenerator` 생성 스텁 핸들러 실제 구현 (`PacketGenerator.cs:242,251`) — 신규 패킷 추가 시

### 테스트 씬 정리

- [ ] `SC_Raid_Test`, `SC_ShelterTest` 용도 문서화 또는 제거
- [ ] `SC_Raid_1001` 테스트 씬 vs `LoadPlanetScene` 통합 검토

---

## 완료 시 체크리스트

항목 완료 후:

1. 이 파일에서 `[ ]` → `[x]` 갱신
2. 해당 기획 문서의 ⚠️/❌ 상태 수정
3. [구현 현황](../README.md#구현-현황) 표 갱신
