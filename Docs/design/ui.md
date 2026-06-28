# UI 시스템

UIManager 패널 스택 구조, 씬별 UI 목록. UI별 세부 로직은 관련 씬 설계 문서 참조.

## UIManager

패널 스택 기반 Show / Hide / Toggle.

- **ESC:** 최상위 패널 닫기 → 없으면 `IngameMenu` 표시
- 공통 팝업: `WarningPopupUI`, `NoticePopupUI`

## UIType 목록

| UIType | 용도 |
|--------|------|
| Inventory | 플레이어 인벤토리 |
| Storage | 창고 |
| EnhancementTable | 플레이어 스탯 강화 |
| GunEnhancementTable | 무기 강화 (미완) |
| RepairWorkbench | 수리 (미완) |
| ItemBoxReveal | 아이템 박스 공개 |
| EquipContextMenu | 우클릭 장착 메뉴 |
| IngameMenu | ESC 인게임 메뉴 |
| WarningPopup / NoticePopup | 알림 |
| JoinCode | 협동 참가 코드 |
| Options | 옵션 |
| PlanetSelect | 행성 선택 |
| ShelterUpgrade | 쉘터 업그레이드 |

## 영역별 UI

### Title (`UI/Title/`)

| 클래스 | 역할 |
|--------|------|
| `MainMenuUI` | 새 게임, 로드, 협동, 옵션 |
| `SaveSlotButtonUI` | 세이브 슬롯 |
| `OptionsUI` | 설정 |

### Shelter (`UI/Shelter/`)

| 클래스 | 역할 |
|--------|------|
| `StorageUI` | 창고 드래그&드롭 |
| `ShelterUpgradeUI` | 쉘터 업그레이드 |
| `PlanetSelectUI` | 행성 선택 |
| `EnhancementTableUI` | 스탯 강화 |
| `RepairWorkbenchUI` | 수리 (표시만) |
| `JoinCodeUI` | 협동 코드 입력 |
| `TeamPanelUI` | 팀 패널·초대 코드 |

### InGame HUD (`UI/InGame/`)

| 클래스 | 역할 |
|--------|------|
| `HpUI` | HP 바 |
| `VitalUI` | 스태미나·배터리 |
| `CrosshairUI` | 조준선 |
| `AmmoUI` | 탄약 |
| `ProgressUI` | 재장전·채광·아이템 사용 게이지 |
| `IngameMenuUI` | ESC 메뉴 |
| `ItemBoxRevealUI` | 아이템 박스 공개 |

### Inventory (`UI/Inventory/`)

| 클래스 | 역할 |
|--------|------|
| `InventoryUI` | 인벤 그리드 |
| `ItemSlotUI` / `WeaponSlotUI` | 슬롯 표시 |
| `DragHandler` | 드래그 시작 |
| `BaseDropHandler` 파생 | Equip, QuickSlot, Repair, GunEnhancement, Inventory |
| `SlotInteractionManager` | 슬롯 간 교환·분할 |
| `EquipContextMenuUI` | 우클릭 장착 |

### World UI (`UI/Common/`)

| 클래스 | 역할 |
|--------|------|
| `HpWorldUI` | 월드 HP 바 |
| `StaminaWorldUI` | 월드 스태미나 |
| `DamageTextUI` | 데미지 숫자 |
| `SpeechBubble` | NPC/AI 대사 |
| `UIScalePulse` | 근접 오브젝트 강조 |
| `LoadingScreenUI` | 로딩 진행률 |

## 입력 맵 전환

`PlayerInputHandler`가 패널 상태에 따라 Input System 액션 맵 전환:

- **Game** — 이동·전투
- **Inventory** — 인벤토리 조작
- **ItemBox** — 아이템 박스 UI

## 로컬라이제이션

`LocalizedTextUI` + `LocalizationManager` — `localization.csv` key 기반 KO/EN.  
언어 설정: PlayerPrefs.
