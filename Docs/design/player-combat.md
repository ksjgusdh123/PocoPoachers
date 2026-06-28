# 플레이어 & 전투

플레이어 컴포넌트, 이동·전투 로직, 장비 장착 슬롯, Vital 스탯. 인벤·드래그&드롭은 [inventory-exchange.md](inventory-exchange.md).

## 플레이어 컴포넌트

| 클래스 | 역할 |
|--------|------|
| `PlayerController` | 상호작용, UI 등록, 인벤·퀵슬롯·장착 초기화 |
| `PlayerInputHandler` | Input System 맵 전환 (Game / Inventory / ItemBox) |
| `PlayerMovement` | CharacterController 이동, 스프린트, 네트 동기화 |
| `PlayerRotation` | 시야 회전 |
| `PlayerDodge` | 구르기 (무적 0.5초, 스태미나 20 소모) |
| `PlayerStat` | HP·스태미나·배터리, 방어구·강화 보너스, 사망 처리 |
| `PlayerEnhancement` | 영구 스탯 강화 (별도 문서: [progression.md](progression.md)) |
| `PlayerVision` | FOV + LOS 기반 타겟 가시성 |
| `FogOfWarRenderer` | URP CommandBuffer 안개 오브 워 |

## Vital 시스템

| 스탯 | 용도 |
|------|------|
| HP | 전투 피해 |
| Stamina | 스프린트·구르기 소모 |
| Battery | 시간 경과 감소, 0 시 사망 |

이동 속도 = 기본 속도 × 무기 이동 배율 × 아이템 사용 배율 × 강화 보너스

## 소모품 사용 (`ItemUseSystem`)

| EffectType | 효과 |
|------------|------|
| HP | 체력 회복 |
| Hunger / Thirst | 배터리 충전 |
| Stamina | 스태미나 회복 |

- 퀵슬롯 1~9: 등록 + 홀드 사용 (1.5초 게이지) → `QuickSlotInventory`
- 사용 중 이동 속도 감소 적용

## 인벤토리

### Inventory

- 슬롯 기반, 스택, 용량 확장/축소, 정렬
- `ChangeInventory` 이벤트로 UI 갱신
- 세이브 키: `"player_inventory"`

### QuickSlotInventory

플레이어 가방 하위 슬롯 범위를 퀵슬롯으로 사용.

### ItemSlot / BoxItemSlot

아이템 박스 전용 reveal 플래그 지원.

## 장비 슬롯

| 슬롯 인덱스 | 타입 | 클래스 |
|-------------|------|--------|
| 0~1 | 무기 (2슬롯) | `WeaponController` |
| 2 | 헬멧 | `PlayerArmorController` |
| 3 | 갑옷 | `PlayerArmorController` |
| 4 | 가방 | `BagController` → 인벤 용량 확장 |

- `GunStatData` / `ArmorStatData`는 `ItemData`와 ID 1:1 공유
- `WorldEquipmentManager`: 호스트 전용 uid별 내구도 상태

## 무기 & 전투

### GunBase 계열

- `SingleGun`, `ShotgunGun` — `FiringMode`: Single / Auto
- `GunStatData`: 데미지, RPM, 탄창, 재장전, 스프레드, 조준 FOV, 리코일, 사운드 범위
- 발사 시 내구도 감소 → `RoomSync.Durability` 동기화

### 발사체·이펙트

`Bullet` + `BulletPool`, `MuzzleFlash`, `ShellCasingPool`, `BulletDecalPool`

### HUD

`CrosshairUI`, `AmmoUI`, `ProgressUI` (재장전·채광·아이템 사용 게이지)

## 시야 (Vision)

- `PlayerVision`: FOV + Line of Sight로 `VisionTarget` 렌더러 on/off
- `FogOfWarRenderer`: 미탐색 영역 마스킹
- 행성별 `fog_density` / `draw_distance`는 **데이터만 존재**, 런타임 미적용

## 관련 DataTable

| 테이블 | 용도 |
|--------|------|
| `item.csv` | 아이템 정의, ItemType |
| `gun_stat.csv` | 무기 스탯 (Item ID와 동일) |
| `armor_stat.csv` | 방어구 스탯 (Item ID와 동일) |
