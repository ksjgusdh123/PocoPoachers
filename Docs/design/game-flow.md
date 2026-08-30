# 게임 플로우 & 코어 매니저

씬 목록·전환 API, 부트스트랩 초기화 순서, 코어 매니저 역할. 각 씬의 상세 시스템은 해당 설계 문서 참조. 세이브는 [save.md](save.md).

## 씬 목록

| 씬 (상수) | 실제 씬 이름 | 용도 | 로드 API |
|-----------|--------------|------|----------|
| `SceneName.Title` | `SC_Title` | 타이틀·메인 메뉴 | `SceneLoader.LoadTitleScene()` |
| `SceneName.CharacterCreate` | `SC_CharacterCreate` | 새 게임 — 닉네임 입력(추후 외형 커스터마이징) | `SceneManager.LoadScene` (로딩 씬 미경유) |
| `SceneName.Loading` | `SC_Loading` | 비동기 로딩 중간 씬 | 모든 전환이 경유 |
| `SceneName.Shelter` | `SC_RocketShelter` | 거점(쉘터) | `SceneLoader.LoadShelterScene()` |
| — | `SC_Raid_{planetId}` | 행성별 레이드 (동적 조합) | `SceneLoader.LoadPlanetScene(id)` |
| — | `SC_Raid_1001` | 레이드 테스트 | `SceneLoader.LoadRaidTestScene()` |

> 상수 이름은 `Shelter`지만 실제 씬 파일명은 `SC_RocketShelter`다. 씬 이름 문자열로 검색할 때 혼동 주의.

**존재하지만 미연동:** `SC_Raid_Test`, `SC_ShelterTest` — 정리 여부 미정 ([todo.md](todo.md)). `SC_Result` 씬은 실제로 사용된다 — 레이드 성공·실패 모두 이 씬으로 전환되고, 그 안의 `ResultSceneController`가 `RaidResultUI`를 표시한다 ([shelter-raid.md](shelter-raid.md#레이드-종료)).

## 씬 전환 흐름

```
SC_Title ──(새 게임)──▶ SC_CharacterCreate ──(닉네임 확정)──▶ SC_Loading ──▶ SC_RocketShelter
SC_Title ──(로드)─────────────────────────────────────────▶ SC_Loading ──▶ SC_RocketShelter
SC_RocketShelter ──(우주선)─▶ SC_Loading ──▶ SC_Raid_{id}
SC_Raid ──(탈출 성공/팀 전멸)─▶ SC_Result ──(확인)──▶ SC_Loading ──▶ SC_RocketShelter
```

두 가지 전환 경로가 있다:

| 경로 | 클래스 | 용도 | 코옵 전파 |
|------|--------|------|-----------|
| `SceneLoader.LoadXxxScene()` | `Core/Scene/SceneLoader.cs` | 로컬 진입점 (타이틀 메뉴, 행성 선택) | 호출자가 직접 `H_LoadScene` 브로드캐스트 |
| `SceneTransition.Go(sceneName, spawnId)` | `Core/Scene/SceneTransition.cs` | 게임플레이 중 전환 (포털, 팀 전멸) | 호스트가 자동으로 게스트에 `H_LoadSceneT` 전송 후 로컬 전환 |

둘 다 최종적으로 `LoadViaLoadingScreen` → `ObjectManager.Clear()` → `SC_Loading` 로드로 수렴한다. `LoadingSceneController`가 `AsyncOperation` 진행률을 로딩 UI에 표시하고 100%에서 `allowSceneActivation`을 true로 바꾼다.

타이틀 복귀 시 `RoomManager.LeaveRoom()`, `NetworkManager.LeaveGame()` 호출.

## 루프 배선 (코드 근거)

- **타이틀 → 캐릭터 생성:** `MainMenuUI.OnClickNewGame()` — 슬롯 할당/닉네임 확정 없이 `SC_CharacterCreate`로만 이동한다. 슬롯 생성은 닉네임 확정 시점으로 미뤄져, 도중에 취소해도 빈 세이브가 남지 않는다.
- **캐릭터 생성 → 쉘터:** `CharacterCreateUI.OnClickConfirm()` — `AllocateNewSlot()` → `SaveNickname()` → 장비/퀘스트 상태 초기화 → `NetworkConnectFlow.Run` 후 `RoomManager.StartAsHost()`(연결 실패 시 `StartLocalHost()` 폴백).
- **메뉴 → 쉘터:** `RoomManager.Awake`가 `OnGameStarted += LoadShelterIfOnMenu` 구독. 호스트/솔로 세션이 `SC_Title`(로드/협동) 또는 `SC_CharacterCreate`(새 게임)에서 시작되면 `SpawnId.FromTitle` 설정 후 `SceneLoader.LoadShelterScene()`.
- **쉘터 → 레이드:** `PlanetSlotUI.OnClick()` — 선택 행성/스폰ID 설정, 호스트+게스트 있으면 `H_LoadSceneT` 브로드캐스트, `SceneLoader.LoadPlanetScene(planetId)`.
- **레이드 → 결과 (탈출 성공):** `Game/Map/EscapeZone.cs` — 살아있는 팀원 전원이 구역에 5초 체류 시 `Complete()`. 포드 호송 연출 후 `RaidResultCarry.Set(success: true, ...)` → `SceneTransition.Go(SceneName.Result)`로 `SC_Result`로 전환. 프로덕션 레이드 씬에서는 이 경로만 쓰이고, `_showResultUI` 플래그로 현재 씬 위에 바로 오버레이를 띄우는 `ScenePortal` 대체 경로는 테스트/튜토리얼 씬 전용이다.
- **레이드 → 결과 (팀 전멸):** `PlayerController.CheckRaidWipe()` — `Update()`마다 씬이 `SC_Raid_`로 시작하고 생존자가 없는지 확인(모든 클라 로컬 판정). 없으면 포드 호송 연출 후 `RaidResultCarry.Set(success: false, ...)`, 호스트만 `SceneTransition.Go(SceneName.Result)` 트리거(게스트는 `H_LoadSceneT`로 따라옴). `_raidWipeHandled` 플래그로 씬당 1회만 처리.
- **결과 → 쉘터:** `SC_Result`의 `ResultSceneController`가 `RaidResultUI`를 표시, 확인(성공은 각자, 실패는 호스트만) 시 `RaidResultCarry`에 저장된 목적지로 재전환 — [shelter-raid.md](shelter-raid.md#레이드-종료).
- **레이드 통계:** `RaidStats`는 `RoomManager.OnSceneLoaded`에서 씬이 `SC_Raid_`로 시작하면 자동 시작, 아니면 자동 종료.

## 플레이어 스폰

`SpawnId` enum (`Game/Map/SpawnId.cs`): `None`, `FromTitle`, `FromShelter`, `FromRaid`

`PlayerSpawner`는 씬에 배치된 스폰 포인트의 `SpawnId`가 `GameManager.PendingSpawnId`와 일치할 때만 `GameManager.PlayerPrefab`을 생성하고 `PendingSpawnId`를 `None`으로 초기화한다.

## Bootstrapper 초기화 순서

`Core/Managers/Bootstrapper.cs::Awake` — 싱글톤(`Singleton<T>`, `DontDestroyOnLoad`) 강제 생성 순서:

1. `MainThreadDispatcher`
2. `RoomManager`
3. `GameManager`
4. `SceneLoader`
5. `SaveManager`
6. `ShelterManager`
7. `SoundManager`
8. `LocalizationManager`
9. `UISoundManager`
10. (에디터/개발 빌드) `CheatConsole`

## 주요 매니저

| 클래스 | 역할 |
|--------|------|
| `GameManager` | `PlayerPrefab` 참조, `PendingSpawnId`, `SelectedPlanetId`. `GainedInventory`/`GiveInventory` 필드도 있으나 설정만 되고 소비하는 코드가 확인되지 않음 — 죽은 배선일 가능성 |
| `SceneLoader` | 씬 이름 상수 + 비동기 로딩 진입점 (로컬 전용, 코옵 전파 없음) |
| `SceneTransition` | 코옵 인지 전환 (`Go`) — 호스트가 게스트에 씬 전환을 강제 |
| `ObjectManager` | 네트워크 오브젝트(원격 플레이어·아이템 박스) 레지스트리, 이동 보간. 씬 전환마다 `Clear()` |
| `ShelterManager` | 쉘터 레벨, 업그레이드 로직 — [shelter-raid.md](shelter-raid.md) |
| `SaveManager` | 세이브/로드 — [save.md](save.md) |
| `DataManager` | `ItemTable`/`GunStatTable`/`ArmorStatTable`/`EnemyTable` 4개만 감싸는 정적 래퍼. 나머지 25개 테이블은 각 `XxxTable.Instance` 직접 호출 — [data-tables.md](data-tables.md) |

## 타이틀 메뉴 (`MainMenuUI`)

| 버튼 | 동작 |
|------|------|
| 새 게임 | 새 세이브 슬롯 할당 → 마스터 서버 로그인 → `RoomManager.StartAsHost()` (연결 실패 시 `StartLocalHost()` 폴백) |
| 로드 | `SaveSlotPanelUI`에서 슬롯 선택 → 해당 슬롯 활성화 후 동일 연결 흐름 |
| 협동 참가 | `JoinCodeUI` 6자리 코드 입력 → `RoomManager.StartAsGuest(code)` |
| 성공 후 | `SC_RocketShelter` 로드, `FromTitle` 스폰 |

`RoomManager.StartLocalHost()`는 마스터 서버 연결 실패 시 쓰는 **임시 폴백**으로 코드에 명시(`// TEMP`) — UDP/TCP 없이 즉시 호스트 모드로 진행.

## 관련 문서

- [multiplayer.md](multiplayer.md) — 방 생성/참가, 최대 인원(호스트+게스트 2명)
- [save.md](save.md) — 저장 시점과 저장 항목
- [shelter-raid.md](shelter-raid.md) — 레이드 진입/종료 상세
