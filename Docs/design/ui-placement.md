# 씬별 UI 배치

각 씬에 **어떤 UI 프리팹이 미리 배치돼 있는지**만 다룬다. `UIManager` 구조·`UIType`·테마·Show/Hide 사용법은 [ui.md](ui.md), 열거형 정본은 `UIManager.cs` 참조. 씬에서 직접 만든 UI 오브젝트는 현재 없다 — 모두 프리팹 인스턴스다.

배치된 UI는 대부분 **비활성(`m_IsActive: 0`)으로 시작**한다 — 정상이다. `UIManager`가 씬 로드마다 비활성 오브젝트까지 스캔해 등록한다([ui.md](ui.md#uimanager)).

## 씬 목록

| 씬 | 경로 | 배치 UI |
|----|------|---------|
| `SC_Title` | `Assets/Scenes/00_Core/` | 4개 |
| `SC_Loading` | `Assets/Scenes/00_Core/` | 1개 |
| `SC_RocketShelter` | `Assets/Scenes/01_Shelter/` | 21개 + TotalBagUI |
| `SC_Raid_1001` | `Assets/Scenes/02_Raid/` | 19개 + TotalBagUI |
| `SC_Raid_Desert` | `Assets/Scenes/02_Raid/` | 없음 (맵 지오메트리만) |

프리팹 위치: `Assets/02. Prefabs/UI/{Common,InGame,Shelter,Title,Loading}/`

## TotalBagUI — 인벤토리 패널 루트

`Assets/02. Prefabs/UI/InGame/Inventory/TotalBagUI.prefab`. `SC_RocketShelter`, `SC_Raid_1001` 두 씬의 `UI/Canvas` 아래 인스턴스로 배치돼 있다. **삭제 대상이 아니다.**

- 붙은 컴포넌트: `SceneUIRegistrar` (`_uiType: 0` = `UIType.Inventory`, `_useSharedDimmer: false`)
- 자식: `PlayerBagUI` / `PlayerQuickInventoryUI` / `GunPartUI` (프리팹 안에 중첩 프리팹으로 들어있다)
- 비활성 상태로 시작하는 것이 정상 — 지우면 해당 씬에서 인벤토리가 열리지 않는다
- `Canvas` 자식 중 sibling index 0 — UI 렌더 순서상 가장 아래다. 인스턴스를 새로 넣을 때 순서를 맞춰야 한다

원래는 두 씬에 같은 구조가 씬 오브젝트로 중복돼 있었고, 프리팹으로 통합했다. `UIManager`는 씬 로드마다 `FindObjectsByType<SceneUIRegistrar>(FindObjectsInactive.Include)`로 스캔하므로(`UIManager.cs`) 프리팹 인스턴스도 동일하게 등록된다.

## 씬별 배치 목록

### SC_Title

`TitleUI`, `OptionsUI`, `WarningUI`, `NoticeUI`

`SavePanel`/`SaveSlotButton`은 씬 루트가 아니라 `TitleUI` 내부에 있다. 타이틀 연출은 `TitleScreenPresentationUI`가 담당 — [ui.md](ui.md#title-uititle).

### SC_Loading

`LoadingUI`

### SC_RocketShelter

| 분류 | 프리팹 |
|------|--------|
| 공통 | `MainGameUI`, `WarningUI`, `NoticeUI`, `DescriptionUI` |
| 인벤 | `TotalBagUI` (자식 `PlayerBagUI`, `PlayerQuickInventoryUI`, `GunPartUI`는 프리팹 내부) |
| HUD·메뉴 | `StaminaUI`, `IngameMenuUI`, `EquipContextMenuUI`, `InventoryContextMenuUI`, `ItemBoxUI` |
| 쉘터 기능 | `StorageUI`, `ShelterUpgradeUI`, `CraftingTableUI`, `EnhancementUI`, `GunEnhancementUI`, `RepairWorkbenchUI`, `GeneratorUI`, `CrankGaugeUI`, `PlanetSelectUI` |

**없음:** `TeamPanel`, `JoinPanel`, `OptionsUI`, `FaintingUI`, `ResultUI`. 코옵 패널(`JoinCodeUI`/`TeamPanelUI`)과 옵션은 이 씬에 배치돼 있지 않으므로, 쉘터에서 쓰려면 배치가 필요하다.

### SC_Raid_1001

| 분류 | 프리팹 |
|------|--------|
| 공통 | `MainGameUI`, `WarningUI`, `NoticeUI`, `DescriptionUI` |
| 인벤 | `TotalBagUI` (자식 `PlayerBagUI`, `PlayerQuickInventoryUI`, `GunPartUI`는 프리팹 내부) |
| HUD·메뉴 | `StaminaUI`, `IngameMenuUI`, `EquipContextMenuUI`, `InventoryContextMenuUI`, `ItemBoxUI`, `FaintingUI`, `ResultUI` |
| 쉘터 기능 (레이드에도 배치됨) | `StorageUI`, `CraftingTableUI`, `EnhancementUI`, `GunEnhancementUI`, `RepairWorkbenchUI` |

마지막 행은 의도 미확인이다. 레이드 중 창고·제작·강화·수리를 쓸 수 있어야 하는지는 기획 확인이 필요하고, 아니라면 배치를 걷어낼 후보다.

### SC_Raid_Desert

UI 오브젝트도 `Canvas`도 없다. `Map`, `LandingArea`, `BossArea`, `FirstArea`~`ThirdArea`, `Rocks`/`Trees`/`Deco` 등 맵 지오메트리만 있는 작업 중 씬으로 보인다.

## UI를 수정하기 전에

1. 프리팹 인스턴스인지 씬 오브젝트인지 먼저 확인 — 프리팹 인스턴스는 씬이 아니라 **프리팹을 수정**한다.
2. 여러 씬에 같은 UI가 배치돼 있다. 한 씬만 고치면 다른 씬은 그대로 남는다(위 목록 참조).
3. 패널이 비활성인 것은 정상 상태다. 활성화해서 커밋하지 않는다.
4. `.meta` 파일은 항상 짝을 맞춰 유지한다.
