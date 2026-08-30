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

## 플레이어 스킬 (`PlayerSkillManager`)

적 AI 스킬([enemy-ai.md](enemy-ai.md#ai-스킬-skillmanager))과 이름·구조는 닮았지만 **완전히 분리된 시스템**(코드 공유 없음). `IPlayerSkill`/`PlayerSkillBase`/`PlayerSkillContext`/`PlayerSkillFactory`/`PlayerSkillId` 패턴.

- `PlayerSkillManager`가 플레이어당 1개, **3슬롯** 장착, 쿨다운·지속시간·활성 슬롯을 중앙 관리(`IPlayerSkill.CanUse`는 그 외 조건만 판단)
- 입력: `PlayerInputHandler.SkillUse` 이벤트(슬롯 인덱스), HUD 표기는 `Shift+1/2/3`
- 장착 슬롯은 `SaveManager.SaveSkillSlots`로 로컬 저장(씬 재생성 대비), 저장 없으면 프리팹의 시작 스킬로 폴백

### 해금(Unlock) — 2단계 게이트

`PlayerSkillData.Unlock.cs`가 `player_skill.csv`의 조건 컬럼을 해석:

1. **해금**: `unlock_stat`(강화 스탯 이름) + `unlock_level` — `PlayerEnhancement.GetStatLevel(stat) >= unlock_level`이면 해금. 조건이 비어있으면 항상 해금.
2. **획득**: `need_item_id` + `need_item_count` — 해금된 스킬을 재료 소모로 실제 보유(`_ownedSkills`)해야 장착 가능. 조건이 없으면 해금 즉시 보유.
3. 장착 가능 여부 = 해금 **AND** 보유. 장착 UI: `SkillEquipPanel`(전체 목록 + 잠김/보유 상태 + 상세) — `SkillEquipUI`(단축키로 여닫는 창)와 강화대 스킬 탭이 같은 패널을 공유.

> ⚠️ **데이터 미기재:** 현재 `player_skill.csv` 18개 행 전부 `unlock_stat`/`need_item_id`가 비어 있어(0), 해금·재료 소모 로직은 완성돼 있지만 실제로는 모든 스킬이 조건 없이 해금+보유 상태다.

### 스킬 목록 (`player_skill.csv`, id 10001~)

| id | 이름 | 유형 | 요약 |
|----|------|------|------|
| 10001 | 대시 | 액티브(`LocksMovement`) | 이동 방향으로 짧게 슬라이드, 무적 없음 |
| 10002 | 즉시 장전 | 액티브(버프) | 지속시간 동안 재장전 즉시 완료 |
| 10003 | 무한 탄약 | 액티브(버프) | 지속시간 동안 탄약 미소모 (호스트 총알 판정용 탄약 사본 고갈 방지를 위해 0.5초 간격 재보고) |
| 10004 | 추가탄 드론 | 액티브(버프) | `CombatDrone` 프리팹을 실제 스폰(연출 아님) — 명중마다 유도탄 추가 발사, 판정은 호스트만 |
| 10005 | 확정 헤드샷 | 액티브(버프) | 지속시간 동안 모든 사격이 헤드샷 판정 |
| 10006 | 크리 데미지 증가 | 액티브(버프) | 크리티컬 배율 상승, `StatBase.CritMultiplier`→즉시 `StatSync` |
| 10007 | 사거리 증가 | 액티브(버프) | 사거리 배율 상승, `StatSync` 경로 공유 |
| 10008 | 수류탄 | 액티브(즉발) | 크로스헤어 지면 좌표에 투척 — 호스트는 직접 실행, 게스트는 로컬 예측+요청 |
| 10009 | 은신 | 액티브(버프) | 반투명 + AI 탐지 제외, 발사 시 즉시 해제 |
| 10010 | 무한 스태미나 | 액티브(버프) | 스태미나 미소모, 네트워크 동기화 불필요 |
| 10011 | 무적 | 액티브(버프) | `StatBase.SetInvincible` — 구르기와 같은 `H_Invincible` 채널 공유 |
| 10012 | 반사 | 액티브(버프) | 무적 + 피탄 반사(`Bullet`이 관통 대신 반사) |
| 10013 | 도발 | 액티브(즉발, 범위) | 반경 내 적 전체를 시전자로 강제 타겟팅. AI 판정은 호스트 전용이라 게스트는 요청만 |
| 10014 | 강철 피부 | **패시브** | 장착만으로 스탯 보너스(`passive_stat`/`passive_value`) — `PlayerEnhancement`에 등록돼 강화와 같은 경로로 반영 |
| 10015 | 행운의 사격 | 액티브(버프) | 명중 시 확률로 배수 데미지, 굴림은 호스트 `Bullet`이 수행 |
| 10016 | 공격 강화 오라 | 액티브(버프) | **파티 오라** — 아래 참고 |
| 10017 | 방어 강화 오라 | 액티브(버프) | **파티 오라** |
| 10018 | 가속 오라 | 액티브(버프) | **파티 오라** |

### 파티 버프 오라 (`PartyBuffRegistry` / `PartyBuffReceiver`)

공격/방어/속도 오라 3종은 시전자 반경(현재 CSV 기준 8.0, 지속 10초, 쿨다운 30초) 안의 아군(본인 포함)에게 배율/가산 보너스를 준다.

- **켜짐/꺼짐만 네트워크로 알린다.** "누가 지금 범위 안인가"는 전송하지 않고, 위치는 이미 서로 동기화돼 보이므로 **각 클라이언트가 매 0.25초(`PartyBuffReceiver.CheckInterval`)마다 스스로 거리 계산**한다
- 전파는 호스트 완전 권위가 아니라 **중계**: `G_PartyBuff`(게스트→호스트, 등록 후 나머지에 재중계) / `H_PartyBuff`(호스트→각 클라)
- 같은 종류 버프가 중첩되면 최댓값만 적용(합산 아님)
- 공격력·방어 보너스는 데미지 판정 주체(호스트)에 반영하기 위해 `StatSync`로 전파, 이동속도는 로컬 전용이라 전파 불필요
- 오라 시각 효과(`AuraMeshEffect`)는 화면에 보이는 모두가 각자 로컬로 계산 — 별도 통신 없음

## 관련 DataTable

| 테이블 | 용도 |
|--------|------|
| `item.csv` | 아이템 정의, `ItemType` |
| `gun_stat.csv` | 무기 스탯 (Item ID와 1:1 공유) |
| `armor_stat.csv` | 방어구 스탯 (Item ID와 1:1 공유) |
| `gun_part.csv` | 파츠 스탯 (Item ID와 1:1 공유) |
| `player_skill.csv` | 플레이어 스킬 정의·수치·해금 조건 (id 10001~) |
