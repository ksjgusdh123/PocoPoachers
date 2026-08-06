# 세이브 시스템

저장 위치, 저장 항목, 저장/로드 트리거. `SaveManager.cs` (`Core/Managers/`) 기준.

## 저장 위치·형식

- `Application.persistentDataPath/save_{slotIndex}.json` — 슬롯별 파일, `JsonUtility` 직렬화
- 슬롯당 `Dictionary<int, GameSaveData> _cache`로 1회 로드 후 메모리 캐시
- 새 슬롯 번호는 `기존 최대값 + 1` (`MainMenuUI.OnClickNewGame`)

## 저장되는 항목 (`GameSaveData`)

| 필드 | 내용 |
|------|------|
| `lastSavedAt` | 타임스탬프. `HasSave` 판정 기준이기도 함 |
| `shelterLevel` | 쉘터 레벨 |
| `inventories` | 키(`"player_inventory"`, `"storage"`)별 `{slotIndex, itemId, amount, uid}` 목록 |
| `equipSlots` | 장착 슬롯 0~4 `{slotIndex, itemId, uid}` (무기 2 + 방어구 2 + 가방 1) |
| `equipment` | `WorldEquipmentManager.SaveData` — uid별 내구도(현재/최대), 탄약(`currentAmmo/maxAmmo/ammoSet`), 파츠(`slotType/partId/partUid`), 강화 레벨. uid 없는 스택형 아이템은 itemId 키로 강화 레벨만 별도 저장 |
| `guestStates` | 호스트 전용. 게스트별 `{playerId, inventory, equipSlots, equipment}` — 게스트는 별도 세이브 파일이 없고 호스트 파일 안에 보관됨 |
| `hasVitals, hp, stamina, battery` | `hasVitals`가 true일 때만 유효. 사망 시 저장하지 않고 `ClearVitals()` — 다음 접속 시 0에서 시작하지 않도록 방지 |
| `questProgress` | `QuestManager.SaveData` — 퀘스트별 진행 상태(Available/InProgress/Completed). 쉘터 레벨처럼 파티 전체가 공유하는 값이라 `guestStates`가 아니라 최상위에 하나만 있음. 호스트 전용 저장(`SaveQuestState`) — [multiplayer.md](multiplayer.md#동기화-미구현-항목) 참고, 아직 게스트 브로드캐스트 패킷 없음 |

> **강화·장착·내구도·Vital 저장은 이미 구현되어 있다.** 과거 문서에 남아있던 "미저장" 기재는 오래된 상태였음.

## 저장되지 않는 항목

| 항목 | 비고 |
|------|------|
| `RaidStats` (경과시간·킬수) | 레이드마다 초기화, 세이브 없음 |
| `GameManager.GainedInventory` / `GiveInventory` | 설정되나 저장/로드 경로 없음 — 죽은 필드 가능성 |
| 통화/재화 | 시스템 자체 없음 |
| 플레이어 월드 좌표 | `SpawnId` 이산 스폰 포인트만 사용 |
| 옵션 설정 | `SaveManager`가 아니라 `PlayerPrefs` (`SoundManager`/`LocalizationManager`가 별도 관리) |
| 월드에 스폰된 아이템 박스 | `ObjectManager._spawnedBoxes` — 씬 전환 시 소멸, 파일로 영속화 안 됨 |

## 저장 트리거 (자동 저장 타이머 없음 — 이벤트/생명주기 기반)

| 트리거 | 대상 | 조건 |
|--------|------|------|
| `PlayerController.OnDestroy` | 인벤·장착 슬롯·장비 상태(내구도/탄약/파츠/강화)·Vital | 호스트/솔로만. 씬 전환·종료마다 발생 |
| `Storage.LateUpdate` (dirty flag) | 창고 인벤 | 슬롯 변경 시 `_isDirty` 세팅, 프레임당 1회 저장. 호스트만 로컬 저장 |
| `ShelterManager.ApplyLevel` | 쉘터 레벨 | 업그레이드 성공 즉시 |
| `MainMenuUI` | 슬롯 메타 | 새 게임(슬롯 생성)·로드(슬롯 선택) 시점 |

`OnApplicationQuit`이나 주기적 오토세이브는 없다 — 정상적인 씬 종료(`OnDestroy`)를 거치지 않고 프로세스가 강제 종료되면 마지막 dirty flush 이후 변경분이 유실될 수 있다.

게스트는 자신의 로컬 세이브 파일을 갖지 않고, 입장 시 호스트가 `SaveManager.GuestRoomState`에 보관된 상태를 `H_GuestRestore`로 복원해준다 — [multiplayer.md](multiplayer.md#late-join-동기화) 참고.

## 알려진 이슈

- `PlayerController.RestoreEquippedSlots`에 디버그 로그 제거 TODO 잔존.
- `IngameMenuUI.OnHostLeft`의 재입장 대기 처리 미구현 — [todo.md](todo.md).

## UI

| 클래스 | 역할 |
|--------|------|
| `SaveSlotButtonUI` | 슬롯 선택 버튼 (삭제 지원) |
| `SaveSlotPanelUI` | 슬롯 목록 패널, `SaveManager.GetAllSlotIndices()` 기반 |
