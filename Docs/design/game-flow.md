# 게임 플로우 & 코어 매니저

## 씬 목록

| 씬 | 용도 | 로드 API |
|----|------|----------|
| `SC_Title` | 타이틀·메인 메뉴 | `SceneLoader.LoadTitleScene()` |
| `SC_Loading` | 비동기 로딩 중간 씬 | 모든 전환 시 경유 |
| `SC_Shelter` | 거점(쉘터) | `SceneLoader.LoadShelterScene()` |
| `SC_Raid_{planetId}` | 행성별 레이드 | `SceneLoader.LoadPlanetScene(id)` |
| `SC_Raid_1001` | 레이드 테스트 | `SceneLoader.LoadRaidTestScene()` |

**미연동:** `SC_Result`(결과 화면), `SC_Raid_Test`, `SC_ShelterTest` — 에셋만 존재.

## 씬 전환 흐름

```
SC_Title ──(새 게임/로드)──▶ SC_Loading ──▶ SC_Shelter
SC_Shelter ──(우주선)──────▶ SC_Loading ──▶ SC_Raid_{id}
SC_Raid ──(포털)───────────▶ SC_Loading ──▶ SC_Shelter / SC_Title
```

- 타이틀 복귀 시 `RoomManager.LeaveRoom()`, `NetworkManager.LeaveGame()` 호출
- `LoadingSceneController`가 `AsyncOperation` 진행률을 로딩 UI에 표시

## 플레이어 스폰

`SpawnId` enum: `None`, `FromTitle`, `FromShelter`, `FromRaid`

`PlayerSpawner`가 `GameManager.PendingSpawnId`와 일치할 때만 플레이어 프리팹을 생성한다.

| 진입 경로 | SpawnId |
|-----------|---------|
| 타이틀에서 새 게임/로드 | `FromTitle` |
| 쉘터 → 레이드 | `FromShelter` |
| 레이드 → 쉘터 | `FromRaid` |

## Bootstrapper 초기화 순서

`Core/Managers/Bootstrapper.cs` — `Awake`에서 싱글톤 순차 생성:

1. `MainThreadDispatcher`
2. `RoomManager`
3. `GameManager`
4. `SceneLoader`
5. `SaveManager`
6. `ShelterManager`
7. `SoundManager`
8. `LocalizationManager`
9. `UISoundManager`
10. (DEV) `CheatConsole`

## 주요 매니저

### GameManager

- 플레이어 프리팹 참조
- 씬 전환 시 인벤토리 로드 플래그 (`ShouldLoadPlayerInventory`)
- 스폰 ID, 선택 행성 ID
- 레이드 복귀 시 인벤 diff (`GiveInventory`, `GainedInventory`)

### SceneLoader

씬 이름 상수(`SceneName`)와 비동기 로딩 진입점을 제공한다.

### SaveManager / ShelterManager

각각 [save.md](save.md), [shelter-raid.md](shelter-raid.md) 참고.

## 타이틀 메뉴 (`MainMenuUI`)

| 버튼 | 동작 |
|------|------|
| 새 게임 | 슬롯 할당 → 마스터 서버 로그인 → 호스트 방 생성 (실패 시 로컬 호스트) |
| 로드 | 세이브 슬롯 선택 → 인벤 로드 플래그 ON → 동일 연결 흐름 |
| 협동 참가 | `JoinCodeUI` 6자리 코드 입력 |
| 성공 후 | `SC_Shelter` 로드, `FromTitle` 스폰 |
