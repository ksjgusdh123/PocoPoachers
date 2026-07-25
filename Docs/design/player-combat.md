# 플레이어 & 전투

플레이어 컴포넌트, Vital 스탯 수치, 무기 발사·피해 모델, 장비 장착 효과. 인벤·드래그&드롭은 [inventory-exchange.md](inventory-exchange.md). 시야/안개는 [planet-sectors.md](planet-sectors.md).

## 플레이어 컴포넌트

공식 상태 머신은 없다 — `PlayerStat`/`PlayerInputHandler`를 각자 읽는 형제 `MonoBehaviour`들의 조합이다.

| 클래스 | 역할 |
|--------|------|
| `PlayerController` | 상호작용, UI 등록, 인벤·퀵슬롯·장착 초기화, 사망/부활/관전 플로우, `CheckRaidWipe()` |
| `PlayerInputHandler` | Input System 액션 맵 전환 (Game / Inventory / ItemBox) |
| `PlayerMovement` | `CharacterController` 이동, 스프린트, 발소리 `SoundEvent` 발생(호스트만) |
| `PlayerRotation` | 시야 회전 |
| `PlayerDodge` | 구르기 — 무적, 스태미나 소모, 쿨다운 |
| `PlayerStat` (`StatBase` 상속) | HP·스태미나·배터리, 방어구/강화 보너스 적용, 사망 처리 |
| `PlayerEnhancement` | 영구 스탯 강화 — [progression.md](progression.md) |
| `PlayerVision` | FOV + LOS 기반 타겟 가시성 |
| `FogOfWarRenderer` | 렌더텍스처 기반 안개 마스킹 |
| `PlayerItemBoxDropper` | 사망 시 인벤을 `LootBox`로 드롭 |

## 사망·부활 (이벤트 기반, 상태 머신 아님)

`StatBase.OnDie`/`OnRevive` 이벤트로 구동:

1. `PlayerController.HandleDeath()` — 기절 게이지 시작(기본 30초, `FaintingUI`)
2. 기절 중 다른 생존 팀원이 있으면(`HasOtherLivingPlayer`) 부활 가능
3. 시간 초과 시 `FinalizeDeath()` — 인벤 전체를 `LootBox`로 드롭, 장비 전부 해제, 생존 팀원 관전 시작

## Vital 수치 (`PlayerStat`, `Core/Common/StatBase.cs`)

| 스탯 | 기본값 | 변화 |
|------|--------|------|
| HP | 최대 100 | 피해 = `데미지 × (1 - DefenseRate)`. 0 이하 시 사망 |
| Stamina | 최대 100 | 스프린트 중 10/초 소모, 마지막 사용 후 1초 뒤부터 15/초 회복. 구르기 고정 20 소모 |
| Battery | 최대 100 | **생존 지표.** 조건 없이 항상 1초당 1 소모. 0 도달 시 HP와 무관하게 사망 |

- 과적재(`Inventory.CurrentWeight > MaxWeight`): 이동속도 ×0.2, 스태미나 회복 정지
- 기절(다운) 상태: 이동속도 ×0.1
- Vital은 호스트/솔로만 씬 전환 시 저장·복원, 사망 상태면 저장 대신 초기화 — [save.md](save.md)

## 장비가 스탯에 미치는 영향

| 장착 | 클래스 | 효과 |
|------|--------|------|
| 헬멧/갑옷 | `ArmorController` (`Game/Equipment/`) | `DefenseRate`, `MaxHpBonus`, `MoveSpeedMultiplier` 적용. 피격 시 내구도 `-피해/10` 감소 |
| 가방 | `BagController` | 인벤 용량·최대 무게를 `ItemData.EffectValue`만큼 확장. Vital에는 영향 없음 |
| 강화(`PlayerEnhancement`) | 최대 Lv.10 | 레벨당 HP·배터리·스태미나 +10 고정, 이동속도 +0.25 |

방어구 실제 능력치는 `WorldEquipmentManager.GetEnhancedArmorStat`에서 강화 레벨당 +10% 배율로 실시간 계산된다(방어율/HP보너스/이동배율 모두).

## 무기 & 전투

- **히트스캔 아님.** `Bullet`은 매 `Update()`마다 `SphereCastNonAlloc`으로 이동하는 실체 투사체. 데미지 적용은 호스트 전용(`_applyDamage = RoomManager.IsHost`).
- `GunType` enum은 Pistol/AssaultRifle/Shotgun/SniperRifle/SMG 5종을 정의하지만, 실제 발사 로직 클래스는 `SingleGun`(단발 탄) / `ShotgunGun`(펠릿 다발)뿐 — 나머지 타입도 `SingleGun`을 재사용하는 것으로 추정.
- `GunBase.TryShoot()` — 재장전/쿨다운/내구도/탄약 게이트 → 탄약 차감 → 발사 간격 `60/RPM` → 리코일. 발사 시 내구도 감소는 `RoomSync.Durability`로 동기화.
- 스탯은 전부 `GunStatData`(`gun_stat.csv`)에서 오며, 장착 파츠가 `GunBase.RecalculateStat()`에서 곱연산으로 반영.

### 발사체·이펙트

`Bullet` + `BulletPool`, `MuzzleFlash`, `ShellCasingPool`, `BulletDecalPool`

### 무기 관련 HUD

| 클래스 | 위치 | 역할 |
|--------|------|------|
| `CrosshairUI` | `UI/` | 조준선, UI 패널 열림 시 자동 숨김(`UIManager`) |
| `AmmoUI` | `Game/Weapon/AmmoUI.cs` | 탄약 표시 — UI 트리가 아니라 무기 스크립트 옆에 위치 |
| `ProgressUI` | — | 재장전·채광·아이템 사용 공용 게이지 |

## 관련 DataTable

| 테이블 | 용도 |
|--------|------|
| `item.csv` | 아이템 정의, `ItemType` |
| `gun_stat.csv` | 무기 스탯 (Item ID와 1:1 공유) |
| `armor_stat.csv` | 방어구 스탯 (Item ID와 1:1 공유) |
| `gun_part.csv` | 파츠 스탯 (Item ID와 1:1 공유) |
