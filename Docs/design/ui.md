# UI 시스템

`UIManager` 패널 관리 구조, 디자인 토큰(`UITheme`), 씬별 UI 목록, 드래그&드롭 처리 흐름. UI별 세부 게임 로직은 관련 씬 설계 문서 참조.

## 프레임워크

표준 uGUI(Canvas/RectTransform/TextMeshPro) + `UnityEngine.EventSystems`(드래그/클릭) + 새 Input System + DOTween(트윈 연출). UI Toolkit/UXML은 사용하지 않는다. 텍스트 폰트는 `TextMesh Pro/Fonts/NotoSansKR-Medium SDF` 하나로 통일.

## 디자인 시스템 (`UITheme`)

색상·타이포·모션 값을 프리팹마다 흩뿌리지 않고 `Assets/Resources/UITheme.asset` 한 곳에 모은다. `UITheme.Default`가 `Resources.Load`로 자동 로드되므로 인스펙터 연결은 선택 사항(비우면 기본 테마).

| 컴포넌트 | 위치 | 역할 |
|----------|------|------|
| `UITheme` | `UI/Common/Theme/` | 토큰 단일 출처(ScriptableObject) — 팔레트, 그래픽 역할색, 타이포 스케일, 버튼 `ColorBlock`/모션, 슬롯 호버, `SpacingGrid` |
| `ThemedGraphicUI` | `UI/Common/Theme/` | `Image`/`Graphic` 색을 역할(`Accent`/`Surface`/`SlotSurface`/`ProgressFill`/`HealthFill`/`StaminaFill`/`Danger`)로 지정. `_keepAlpha`로 원본 알파 유지 |
| `ThemedTextUI` | `UI/Common/Theme/` | TMP 크기와 Auto Size 범위를 타이포 역할(`Caption`/`Body`/`Subtitle`/`Title`/`Display`)에 맞춤. **색은 건드리지 않음**(등급·경고색이 기능적 의미를 가짐) |
| `ThemedSelectableUI` | `UI/Common/Theme/` | Slider/Dropdown/InputField/Toggle 등 버튼 외 위젯 상태색 통일 |
| `ThemedButtonUI` | `UI/Common/Button/` | 버튼 `ColorBlock`(Primary/Secondary/Danger) + 호버 확대·누름 축소 모션. `SetUpdate(true)`로 `timeScale 0`에서도 반응 |
| `SlotHoverHighlightUI` | `UI/Common/Slot/` | 아이템 슬롯 호버 시 테두리 강조 + 살짝 확대. 테두리는 자식에서 `SlotFrame`/`Frame`/`Outline` 등 이름으로 자동 탐색하고, 그런 자식이 없으면 **자기 오브젝트의 `Graphic`을 테두리로 사용**한다(루트 Image 자체가 슬롯 프레임인 `WeaponSlotUI`/`GunPartSlot` 등). 드롭 타깃(`BaseDropHandler`)에는 전부 붙어 있어야 하며, 중첩으로 두 번 붙으면 확대가 겹친다 |

**왜 `ButtonNormal`이 흰색이 아닌가:** `ColorTint`는 스프라이트 색에 곱연산이므로 기본값을 약간 어둡게(`#DCDCDC`) 두고 호버에서 흰색으로 올려야 "밝아지는" 피드백이 생긴다.

### 에디터 도구 (`UI/Editor/UIThemeMigrationEditor.cs`)

`Assets/02. Prefabs/UI` 아래 프리팹을 일괄 처리한다. `UITheme.DesignSystemVersion`이 낮으면 에디터 로드 시 자동 1회 적용.

| 메뉴 | 동작 |
|------|------|
| `PocoPoachers/UI/Apply Design System` | 텍스트에 `ThemedTextUI`·위젯에 `ThemedSelectableUI` 부착, 레이아웃 spacing/padding을 `SpacingGrid` 배수로 정렬 |
| `PocoPoachers/UI/Audit Design System` | 미적용 텍스트/위젯, 과대 폰트, 잘못된 Auto Size 범위, 그리드 밖 여백 수를 리포트(수정 없음) |

## UIManager

`Dictionary<UIType, GameObject>` + 열린 순서 목록(`List<UIType> _stack`) 기반 — 진짜 LIFO 스택이라기보단 **열림 순서를 기록하는 평탄한 레지스트리**에 가깝다.

- **ESC:** 매 프레임 키 폴링이 아니라 Input System 액션(`_cancelAction`, 기본 `<Keyboard>/escape`)으로 받음 → `HideTop()`이 최근 연 패널을 닫고, 열린 패널이 없으면 `IngameMenu` 표시. 리바인딩·게임패드 바인딩 확장 가능
- 패널 등록 방식 2가지: (1) `SceneUIRegistrar`(씬에 미리 배치된 패널 — 대부분 `SetActive(false)`로 시작해 `Awake`가 안 불리므로 씬 로드마다 `FindObjectsByType(Include)`로 스캔. `Awake`/`sceneLoaded`/`Start` 3회 스캔으로 매니저 생성 순서·에디터 직접 실행 누락을 방어), (2) `UIBase.Awake()` 자가 등록(팝업처럼 런타임 생성되는 패널)
- 같은 `UIType`을 다른 오브젝트가 덮어쓰면 경고 로그. `Unregister(type, owner)`로 소유자가 일치할 때만 해제해 파괴 순서에 따른 등록 유실을 막음
- 공통 팝업: `ShowWarning`/`ShowNotice` 편의 API, `WarningPopupUI`/`NoticePopupUI`. 팝업이 씬에 없으면 조용히 무시하지 않고 경고 로그
- 패널 열림 여부로 크로스헤어(`CrosshairUI`) 표시 토글
- `OnPanelOpened`/`OnPanelClosed` 이벤트 — `UISoundManager`, `ContextMenuUIBase` 등이 구독

### 그리기 순서 · 딤머 · 연출

- **순서:** 씬 하이어라키가 아니라 **열린 순서** 기준. 자체 `Canvas`가 있으면 `overrideSorting` + `sortingOrder = _panelSortingOrderBase(100) + index`, 없으면 형제 순서를 올림. 드래그 아이콘처럼 항상 최상단이어야 하는 요소는 `UIManager.OverlaySortingOrder`(1000)
- **딤머:** 패널마다 따로 두지 않고 `UIManager`가 공용 `SharedDimmer`(런타임 생성, 색은 `UITheme.Dimmer`) 하나를 가장 위쪽 요청 패널 바로 뒤에 배치해 뒤쪽 클릭 차단. 요청 여부는 `UIBase._useSharedDimmer` / `SceneUIRegistrar._useSharedDimmer`
- **연출:** 열릴 때 페이드인 + `OutBack` 스케일(기본 0.12초, 0.94배에서 시작), 닫힐 때 페이드아웃(0.08초). 둘 다 `SetUpdate(true)`로 일시정지 중에도 재생. 닫힘 연출 중 다시 열면 연출을 취소하고 `blocksRaycasts`를 복구하며, 중간에 끊긴 알파·스케일은 `RestorePanelVisual`이 원복

### 베이스 클래스 계층

`UIBase`(Show/Hide/Toggle을 `UIManager`에 위임 + `OnShow`/`OnHide` 훅) → `PopupUIBase`(`SetContent(title, message)`) / `ContextMenuUIBase`(우클릭 메뉴 — 위치 잡기, 바깥 클릭 시 닫기, 인벤 닫히면 숨김).
프레임 계층은 별개: `UIFrameBase`(제목+콘텐츠 영역) → `UIFrame`(닫기버튼+딤머, 창형) / `UIPopupFrame`(딤머만, 모달형).

`UIBase` 인스펙터 옵션: `_startVisible`(씬 로드 시 열린 상태 유지), `_useSharedDimmer`(공용 딤머 사용). 갱신 로직은 `OnEnable`이 아니라 `OnShow`에 두면 열림 순서·이벤트 발행 시점이 보장된다. 씬에 비활성으로 배치된 패널은 `Show()`가 활성화하는 순간 `Awake`가 처음 도는데, 이미 열림 스택에 있으면 초기 비활성 처리를 건너뛴다(`IsInOpenStack`).

**모든 UI가 `UIBase`를 상속하지는 않는다** — `InventoryUI`, `CraftingTableUI`, `RepairWorkbenchUI`, `GeneratorUI`, `GunEnhancementTableUI`, `EnhancementTableUI`, `MainMenuUI`, `TeamPanelUI` 등은 평범한 `MonoBehaviour`로 부모 패널이 `SetActive`를 직접 제어한다 — 중앙 Show/Hide 스택 참여가 필요한 화면만 `UIBase`를 쓴다.

## UIType 목록

| UIType | 용도 | 구현 상태 |
|--------|------|-----------|
| Inventory | 플레이어 인벤토리 | ✅ |
| Storage | 창고 | ✅ |
| EnhancementTable | 플레이어 스탯 강화 | ✅ |
| GunEnhancementTable | 무기/방어구 강화 | ✅ |
| RepairWorkbench | 수리 | ✅ |
| CraftingTable | 제작 | ✅ |
| Generator | 발전기(연료 투입·전력) | ✅ |
| ItemBoxReveal | 아이템 박스 공개 | ✅ |
| EquipContextMenu | 장비 슬롯 우클릭 메뉴 | ✅ |
| InventoryContextMenu | 인벤 슬롯 우클릭 메뉴 | ✅ |
| MainGameUI | 인게임 HUD 루트 | ✅ |
| IngameMenu | ESC 인게임 메뉴 | ✅ (호스트 재입장 대기만 TODO) |
| WarningPopup / NoticePopup | 알림 | ✅ |
| VotePopup | 팀 이동 수락 대기 (`VotePopupUI` — 게스트는 수락/거절, 호스트는 대기+취소) | ✅ |
| JoinCode | 협동 참가 코드 | ✅ |
| Options | 옵션 | ✅ |
| PlanetSelect | 행성 선택 | ✅ |
| ShelterUpgrade | 쉘터 업그레이드 | ✅ |

## UI 사운드 (`UISoundManager`)

`SoundManager`에 SFX 재생을 위임하는 얇은 중계 계층. 각 UI 스크립트가 사운드를 직접 호출하지 않게 이벤트 구독으로 처리한다.

| 트리거 | SFX |
|--------|-----|
| 버튼 호버 / 클릭 | `ui_hover` / `PlayButtonClick()` |
| 슬롯 드래그 시작 | `ui_slot_click` |
| 아이템 배치 성공 / 실패 | `ui_item_place` / `ui_item_place_fail` |
| Inventory 패널 열림 / 닫힘 | `ui_inventory_open` / `ui_inventory_close` |

호버 판정은 매 프레임 전체 레이캐스트가 아니라 **포인터가 실제로 움직였거나 클릭한 프레임만** 검사하고 `PointerEventData` 인스턴스를 재사용한다.

## 영역별 UI

### Title (`UI/Title/`)

| 클래스 | 역할 |
|--------|------|
| `MainMenuUI` | 새 게임, 로드, 협동, 옵션 |
| `TitleScreenPresentationUI` | 진입 시 브랜드→메뉴 순차 페이드/슬라이드 연출. `unscaledDeltaTime` 기반이며 완료 후 첫 메뉴에 포커스. 연출이 중단돼도 메뉴 입력은 잠그지 않음 |
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
| `CraftingTableUI` | 제작 — 카테고리 탭 + 레시피 목록(엔트리는 파괴하지 않고 재사용, `SetSiblingIndex`로 순서 유지) |
| `GeneratorUI` | 전력 바(임계값별 색상), 연료 드롭 슬롯, 투입 전 미리보기(투입 후 % 예상치 표시) |
| `CrankGaugeUI` | 수동 크랭크(F) 시 발전기를 열지 않고도 잠깐 떴다 사라지는 전력 게이지 피드백 |
| `JoinCodeUI` / `TeamPanelUI` | 코옵 참가/초대 코드, 팀 로스터 |

### InGame HUD (`UI/InGame/`)

| 클래스 | 역할 |
|--------|------|
| `HpUI` | HP 바 (`StatBase.OnHpChanged` 바인딩) |
| `VitalUI` | 배터리 바 (`PlayerStat.OnBatteryChanged` 바인딩) |
| `FaintingUI` | 다운 상태 카운트다운(기본 30초), F 홀드 시 30배속 포기 |
| `IngameMenuUI` | ESC 메뉴 |
| `EquipContextMenuUI` / `InventoryContextMenuUI` | 장비/인벤 슬롯 우클릭 메뉴 (`ContextMenuUIBase` 파생) |
| `GunPartPanelUI` | 파츠 장착 고정 패널 — `ItemInfoPanel` 상속, 드롭 슬롯 + 프리뷰 총 + 호스트/게스트 동기화 |
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
| `ItemSlotUI` / `WeaponSlotUI` | 슬롯 표시 (호버 피드백은 `SlotHoverHighlightUI`) |
| `DragHandler` | 드래그 시작 — 원본 슬롯 반투명화(0.4), `DragIcon`이 커서 따라감 |
| `DragIcon` | 루트 `Canvas` 직속으로 옮기고 자체 `Canvas.sortingOrder = UIManager.OverlaySortingOrder`(1000)로 항상 모든 패널 위에 그림 |
| `BaseDropHandler` 파생 | `InventoryDropHandler`, `EquipDropHandler`, `QuickSlotDropHandler`, `RepairSlotDropHandler`, `GunEnhancementDropHandler`, `GunPartDropHandler`, `GeneratorFuelDropHandler` — [inventory-exchange.md](inventory-exchange.md) |
| `SlotInteractionManager` | 슬롯 간 교환·분할·드래그 상태 허브 (`OnDragBegin`/`OnItemPlaced`/`OnItemPlaceFailed` 이벤트 발행) |

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

## 아이템 정보 표시 (`ItemInfoPanel`)

표시 로직(이름/설명/아이콘/내구도 바/타입별 스탯/강화 레벨/파츠 아이콘)은 `ItemInfoPanel` 하나에 모으고, **켜고 끄는 시점만** 파생 클래스가 정한다.

| 클래스 | 여는 방식 |
|--------|-----------|
| `DescriptionUI` | 호버 툴팁. 씬에 단일 인스턴스(`FindAnyObjectByType`), `SlotInteractionManager` 호버 이벤트로 트리거. 캔버스 밖으로 안 나가게 위치 클램프 |
| `GunPartPanelUI` | 버튼으로 열고 닫는 고정 패널 |

호버 핸들러가 `FindAnyObjectByType<DescriptionUI>`로 툴팁만 집어내야 하므로 고정 패널과 **타입을 분리**해 둔다. 스탯 한 줄은 `StatRowUI` 프리팹이고, 정렬·높이는 `VerticalLayoutGroup` + `ContentSizeFitter`가 처리한다. 행은 **파괴하지 않고 재사용**한다(부족하면 추가 생성, 남으면 비활성화) — 슬롯 사이로 마우스를 움직일 때마다 `Instantiate`/`Destroy`가 반복되면 GC 스파이크가 생긴다. 표시 항목: 무기 RPM·데미지·탄창, 파츠 배율 변화, 방어구 방어율·HP·이동속도. 미공개(reveal 전) 박스 슬롯은 툴팁·더블클릭 모두 억제.

## 입력 맵 전환

`PlayerInputHandler`가 패널 상태에 따라 Input System 액션 맵(`PlayerInputMapType`)을 전환:

- **Game** — 이동·전투
- **Shelter** — 쉘터 이동(전투 입력 없음)
- **Inventory** — 인벤토리 조작
- **ItemBox** — 아이템 박스 UI

씬 이름으로 기본 게임플레이 맵을 판별하고(`GameplayMap`: 쉘터→`Shelter`, 그 외→`Game`), `PlayerInput.Default Map` 설정에 의존하지 않도록 `Start`에서 덮어쓴다. 상호작용을 닫으면 이 맵으로 복귀한다.

**ESC는 이 맵들과 무관** — `UIManager`가 자체 `InputAction`으로 직접 처리한다(위 UIManager 섹션 참고).

## 로컬라이제이션

`LocalizedTextUI` + `LocalizationManager` — `localization.csv` key 기반 KO/EN. 언어 설정: `PlayerPrefs`. CSV를 고치면 `Tools/Generator/Tables`로 `Generated/DataTable`·`_Data/Resources/JsonData`를 다시 만들어야 한다. CSV 값의 `\n`은 생성 시 실제 개행으로 변환된다.

규칙 두 가지:

- **정적 라벨에만 붙인다.** `LocalizedTextUI`는 `OnEnable`과 언어 변경 이벤트에서 `_text.text`를 덮으므로, 코드가 런타임에 채우는 텍스트(제목 `Txt_Title`, 수량/레벨/비용 표시 등)에 붙이면 값이 지워진다. 그런 텍스트는 코드에서 `LocalizationManager.GetString`을 호출한다.
- **키를 비워두지 않는다.** 빈 키는 `GetString("")`이 그대로 `""`를 돌려주므로 텍스트를 지운다. 키가 없으면 컴포넌트를 붙이지 않는다.

프리팹에 남겨두는 텍스트는 편집 중 확인용 placeholder이므로 CSV의 KO 값과 같게 유지하면 인스펙터가 오해를 부르지 않는다.
