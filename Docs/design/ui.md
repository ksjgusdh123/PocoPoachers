# UI 시스템

`UIManager` 패널 관리 구조, 씬별 UI 목록, 드래그&드롭 처리 흐름. UI별 세부 게임 로직은 관련 씬 설계 문서 참조.

## 프레임워크

표준 uGUI(Canvas/RectTransform/TextMeshPro) + `UnityEngine.EventSystems`(드래그/클릭) + 새 Input System + DOTween(트윈 연출). UI Toolkit/UXML은 사용하지 않는다.

## UIManager

`Dictionary<UIType, GameObject>` + 열린 순서 목록(`List<UIType> _stack`) 기반 — 진짜 LIFO 스택이라기보단 **열림 순서를 기록하는 평탄한 레지스트리**에 가깝다.

- **ESC:** `HideTop()`이 최근 연 패널을 닫음 → 없으면 `IngameMenu` 표시
- 패널 등록 방식 2가지: (1) `SceneUIRegistrar`(씬에 미리 배치된 패널 — 대부분 `SetActive(false)`로 시작해 `Awake`가 안 불리므로 씬 로드마다 `FindObjectsByType`로 스캔), (2) `UIBase.Awake()` 자가 등록(팝업처럼 런타임 생성되는 패널)
- 공통 팝업: `ShowWarning`/`ShowNotice` 편의 API, `WarningPopupUI`/`NoticePopupUI`
- 패널 열림 여부로 크로스헤어(`CrosshairUI`) 표시 토글

### 베이스 클래스 계층

`UIBase`(Show/Hide/Toggle을 `UIManager`에 위임) → `PopupUIBase`(`SetContent(title, message)`) → `UIFrameBase`(제목+콘텐츠 영역) → `UIFrame`(닫기버튼+딤머, 창형) / `UIPopupFrame`(딤머만, 모달형).

**모든 UI가 `UIBase`를 상속하지는 않는다** — `InventoryUI`, `CraftingTableUI`, `RepairWorkbenchUI`, `GeneratorUI`, `GunEnhancementTableUI`, `MainMenuUI`, `TeamPanelUI` 등은 평범한 `MonoBehaviour`로 부모 패널이 `SetActive`를 직접 제어한다 — 중앙 Show/Hide 스택 참여가 필요한 화면만 `UIBase`를 쓴다.

## UIType 목록

| UIType | 용도 | 구현 상태 |
|--------|------|-----------|
| Inventory | 플레이어 인벤토리 | ✅ |
| Storage | 창고 | ✅ |
| EnhancementTable | 플레이어 스탯 강화 | ✅ |
| GunEnhancementTable | 무기/방어구 강화 | ✅ (과거 "미완" 표기는 최신 상태 아님) |
| RepairWorkbench | 수리 | ✅ |
| CraftingTable | 제작 | ✅ |
| ItemBoxReveal | 아이템 박스 공개 | ✅ |
| EquipContextMenu | 우클릭 장착 메뉴 | ✅ |
| IngameMenu | ESC 인게임 메뉴 | ✅ (호스트 재입장 대기만 TODO) |
| WarningPopup / NoticePopup | 알림 | ✅ |
| JoinCode | 협동 참가 코드 | ✅ |
| Options | 옵션 | ✅ |
| PlanetSelect | 행성 선택 | ✅ |
| ShelterUpgrade | 쉘터 업그레이드 | ✅ |

## 영역별 UI

### Title (`UI/Title/`)

| 클래스 | 역할 |
|--------|------|
| `MainMenuUI` | 새 게임, 로드, 협동, 옵션 |
| `SaveSlotPanelUI` / `SaveSlotButtonUI` | 세이브 슬롯 목록/선택(삭제 지원) |
| `OptionsUI` | 설정 |

### Shelter (`UI/Shelter/`)

| 클래스 | 역할 |
|--------|------|
| `StorageUI` (`InventoryUI` 상속) | 창고 — 페이지네이션 + `ItemType` 필터 |
| `ShelterUpgradeUI` | 쉘터 업그레이드, 재료 부족 시 버튼 비활성 |
| `PlanetSelectUI` / `PlanetSlotUI` | 행성 선택, 잠긴 행성 비활성화 |
| `EnhancementTableUI` | 플레이어 스탯 강화 |
| `GunEnhancementTableUI` | 무기/방어구 강화, 강화 전/후 스탯 미리보기 |
| `RepairWorkbenchUI` | 수리, 재료/전력 비용 색상 표시 |
| `CraftingTableUI` | 제작 — 카테고리 탭 + 레시피 목록 |
| `GeneratorUI` | 전력 바(임계값별 색상), 연료 드롭 슬롯, 투입 전 미리보기(투입 후 % 예상치 표시) |
| `JoinCodeUI` / `TeamPanelUI` | 코옵 참가/초대 코드, 팀 로스터 |

### InGame HUD (`UI/InGame/`)

| 클래스 | 역할 |
|--------|------|
| `HpUI` | HP 바 (`StatBase.OnHpChanged` 바인딩) |
| `VitalUI` | 배터리 바 (`PlayerStat.OnBatteryChanged` 바인딩) |
| `FaintingUI` | 다운 상태 카운트다운(기본 30초), F 홀드 시 30배속 포기 |
| `IngameMenuUI` | ESC 메뉴 |
| `ItemBoxRevealUI` | 아이템 박스 공개 연출 |
| `CrosshairUI`, `ProgressUI` | 조준선, 재장전/채광/아이템 사용 공용 게이지 — [player-combat.md](player-combat.md) |

> `AmmoUI`는 `UI/InGame/HUD`가 아니라 `Game/Weapon/AmmoUI.cs`에 있다 — 무기 스크립트와 함께 배치. 미니맵/컴퍼스 클래스는 존재하지 않는다.
>
> 설계상 필요한 건 전체 미니맵이 아니라 **착지 포드 복귀 방향만 가리키는 최소 화살표**(다른 POI 길찾기는 의도적으로 미지원) — [map-composition.md#5-길찾기--비대칭-설계](map-composition.md#5-길찾기--비대칭-설계) 참고. 아직 미구현.

`RaidResultUI`는 씬 전환 없이 레이드 씬 위에 페이드인되는 결과 오버레이 — [shelter-raid.md](shelter-raid.md#레이드-종료).

### Inventory (`UI/Inventory/`)

| 클래스 | 역할 |
|--------|------|
| `InventoryUI` | 인벤 그리드 |
| `ItemSlotUI` / `WeaponSlotUI` | 슬롯 표시 |
| `DragHandler` | 드래그 시작 — 원본 슬롯 반투명화(0.4), `DragIcon`이 커서 따라감(루트 캔버스 최상단) |
| `BaseDropHandler` 파생 | `InventoryDropHandler`, `EquipDropHandler`, `QuickSlotDropHandler`, `RepairSlotDropHandler`, `GunEnhancementDropHandler` — [inventory-exchange.md](inventory-exchange.md) |
| `SlotInteractionManager` | 슬롯 간 교환·분할·드래그 상태 허브 |
| `EquipContextMenuUI` | 우클릭 장착/파츠 메뉴 |

### World UI (`UI/Common/World/`)

풀링 기반, `WorldUIManager`(`WorldUIType` enum)가 중앙 관리. **인터랙션 프롬프트나 네임플레이트 클래스는 없음** — 전투/상태 피드백 전용:

| 클래스 | 역할 |
|--------|------|
| `HpWorldUI` | 피격 시 표시되는 월드 HP 바, 무피격 3초 후 자동 숨김 |
| `StaminaWorldUI` | 월드 스태미나 바 (풀링 아님, 항상 플레이어 추적) |
| `DamageTextUI` | 데미지 숫자 팝업 (DOTween) |
| `SpeechBubble` | AI 대사 말풍선 |
| `UIScalePulse` | 근접 상호작용 오브젝트 강조 펄스 |
| `LoadingScreenUI` | 로딩 진행률 |

## 툴팁 (`DescriptionUI`)

씬에 단일 인스턴스(`FindAnyObjectByType`). `SlotInteractionManager`의 호버 이벤트로 트리거되어 이름/설명(로컬라이즈)/아이콘/내구도 바/타입별 스탯(무기 RPM·데미지·탄창, 파츠 배율 변화, 방어구 방어율·HP·이동속도, 강화 레벨 포함)을 표시. 캔버스 밖으로 안 나가게 위치 클램프. 미공개(reveal 전) 박스 슬롯은 툴팁·더블클릭 모두 억제.

## 입력 맵 전환

`PlayerInputHandler`가 패널 상태에 따라 Input System 액션 맵 전환:

- **Game** — 이동·전투
- **Inventory** — 인벤토리 조작
- **ItemBox** — 아이템 박스 UI

## 로컬라이제이션

`LocalizedTextUI` + `LocalizationManager` — `localization.csv` key 기반 KO/EN. 언어 설정: `PlayerPrefs`.
