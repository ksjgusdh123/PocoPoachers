# 멀티플레이

P2P 아키텍처 개요, 입장 흐름, 인원 제한, 동기화 현황. 패킷 구조·추가 절차는 [network-packets.md](../development/network-packets.md).

## 아키텍처

```
[클라이언트] ──TCP──▶ [마스터 서버]  로그인, 방 생성/참가 (매치메이킹만)
[호스트] ◀────UDP P2P (Star)────▶ [게스트 1]
                    └───────────▶ [게스트 2]
```

- **마스터 서버 (TCP):** 로그인, 하트비트, 방 생성/참가만 담당 — 게임플레이 트래픽은 전혀 거치지 않음 (`NetworkManager`, `Server/Server.sln`)
- **P2P 게임 (UDP):** STUN(`stun.l.google.com:19302`)으로 공인 엔드포인트 획득 후 UDP 홀펀칭(`UdpHolePuncher`). 같은 LAN이면 사설 IP로 바로 연결
- **스타 토폴로지:** 게스트는 서로 통신하지 않고 오직 호스트와만 UDP로 연결 (`Server/Server/Net/Packet/PacketHandler/PacketHandler.Room.cs` 주석에 명시)
- **최대 인원: 호스트 1 + 게스트 2 = 총 3명.** `Room.MaxGuests = 2`(`Server/Server/Game/Room/Room.cs`) — 4인이 아니다

## 권위 모델 (기능별로 다름)

| 영역 | 권위 |
|------|------|
| 전투 데미지, 아이템 박스, 내구도, 탄약/파츠, HP 클램프 | **호스트 권위** — 호스트가 계산·검증(`GuestValidator.ClampGuestHp` 등) 후 결과를 통보 |
| 이동 | **게스트 자기보고 + 호스트 중계** — 호스트는 `G_Move`를 검증 없이 그대로 적용하고 `H_Move`로 재전파(사실상 클라이언트 권위) |
| 적 AI | **100% 호스트 시뮬레이션** — 게스트→호스트 방향의 `G_Enemy*` 패킷이 없어 게스트 측 예측/보정이 전혀 없음 |
| 스탯(`StatSync`) | 게스트 자기보고, 호스트가 가볍게 클램프만 수행 (완전 재계산은 아님) |
| 파티 버프 오라 (`PartyBuffRegistry`) | **호스트 중계, 완전 권위 아님** — 켜짐/꺼짐만 `G/H_PartyBuff`로 전파, "누가 범위 안인지"는 각 클라가 위치로 로컬 계산(재전송 없음) — [player-combat.md](player-combat.md#파티-버프-오라-partybuffregistry--partybuffreceiver) |
| 화로(`Furnace`) | **호스트 권위** — 투입/제련/지급 전부 호스트, 게스트는 진행 게이지만 로컬로 이어 셈 — [network-packets.md](../development/network-packets.md#쉘터-화로-furnace) |

## 핵심 클래스

| 클래스 | 역할 |
|--------|------|
| `NetworkManager` | TCP 연결, 로그인, RTT, `DontDestroyOnLoad` |
| `RoomManager` | 호스트/게스트, 6자리 세션 코드, late-join 동기화, 타임아웃 30초 / 킵얼라이브 5초 |
| `RoomSync` | 이동·사격·장착·내구도·스탯·적·아이템 패킷 송신 헬퍼 |
| `ObjectManager` | 원격 플레이어·아이템 박스 스폰/디스폰 |
| `RemotePlayerStat` | 원격 플레이어 HP·스탯 반영 (`ApplyNetworkStats`) |
| `GuestValidator` | 호스트 측 게스트 패킷 검증 (HP 클램프, 자가 부활/과회복 차단, 장비 소유 확인) |
| `UdpReliable` | UDP 신뢰 전송 (seq·ACK·재전송, 시그널 `0x03`/`0x04`) |

## 싱글플레이

`RoomManager.StartLocalHost()` — 코드에 `// TEMP` 주석이 달린 폴백. 마스터 서버 연결 실패 시 UDP/TCP 없이 즉시 호스트 모드로 진행. `RoomSync.IsSolo`가 true면 게임 패킷 자체를 보내지 않는다.

## 연결 흐름

### 호스트 (새 게임)

1. `NetworkManager` 마스터 서버 TCP 로그인
2. `RoomManager.StartAsHost()` — STUN으로 공인 IP/포트 획득 → `C_CreateRoom` → `S_CreateRoom`(6자리 코드)
3. 실패 시 `StartLocalHost()` 폴백
4. `SC_RocketShelter` 로드

### 게스트 (협동 참가)

1. TCP 로그인
2. `JoinCodeUI`에서 6자리 코드 입력 → `C_JoinRoom` (방이 이미 2명이면 `S_JoinRoom{Success=false}`)
3. 호스트 NetInfo 수신 → UDP 홀펀칭(`StartUdpPunch`) → 게임 세션 시작

## Late-join 동기화 (구현됨)

게스트 입장(`OnRoomJoined` 또는 조기 패킷 수신 시 `TryRegisterLateGuest`) 시 `SendWorldStateToGuest`가 순서대로 전송:

1. `H_GuestJoined` (기존 플레이어 정보 + 신규 게스트를 기존 플레이어에 통지)
2. `SendHostEquipToGuest` — 호스트 자신의 무기·방어구·가방 + 내구도
3. `H_ShelterLevel`
4. `SendAllPlayerStatsToGuest` — 전원의 HP/스태미나/배터리/방어력

씬 전환 후에는 게스트의 `G_SceneReady` 수신을 기다렸다가(`HandleGuestSceneReady`) 호스트 장비 재전송 + `SendGuestRoomRestore`(저장된 인벤/장착 복원, 내구도·탄약·파츠는 제외 — 재장착 시 `H_Durability`/`H_GunState`로 자연 갱신) + `SendWorldObjectsToGuest`(적·아이템 박스 스냅샷)를 보낸다. 호스트 스폰이 끝나기 전에 게스트가 준비되면 `_sceneReadyGuests`에 대기시켰다가 스폰 완료 다음 프레임에 일괄 전송 — 로딩 중 유실을 막기 위함.

## UI 연동

| UI | 기능 |
|----|------|
| `TeamPanelUI` | 호스트 초대 코드 생성/복사, 팀 슬롯 표시 |
| `JoinCodeUI` | 게스트 코드 참가 |
| `IngameMenuUI` | ESC 메뉴, 호스트/게스트 이탈 |

## 동기화 미구현 항목

| 항목 | 위치 | 설명 |
|------|------|------|
| 총기 발사 사운드 미동기화 | `Combat.fbs` / `PacketHandler.Combat.cs` | `G_Shoot`엔 `sound_range`가 있으나 `H_Shoot`엔 없음 — 다른 게스트에게 전파 안 됨 |
| 발자국 사운드 미전달 | — | — |
| 게스트 이탈 시 호스트 재입장 대기 | `IngameMenuUI.OnHostLeft` | `onCancel`이 빈 스텁 (TODO 주석) |
| 퀘스트 진행 신뢰 검증 없음 | `PacketHandler.Quest.cs` (`OnG_QuestSubmit`) | Accept/Submit/Complete 전부 동기화됨(2026-08) — `G_QuestAccept`/`H_QuestAccept`, `G_QuestSubmit`/`H_QuestSubmit`, `G_QuestComplete`/`H_QuestComplete`, `RoomSync.QuestAccept`/`QuestSubmit`/`QuestComplete`. Accept/Complete는 상태 확정형이라 멱등이라 트리거 쪽이 낙관적으로 먼저 적용(`ShelterManager.TryUpgrade`와 동일 패턴). Submit은 누적값이라 멱등이 아니라서 게스트는 로컬 미적용, 호스트가 `AddSubmitted` 적용 후 브로드캐스트한 `H_QuestSubmit`을 받을 때만 반영(이중 집계 방지) — `QuestDescriptionUI.OnClickAction` 참고. 다만 호스트는 게스트가 보낸 제출 수량을 검증 없이 그대로 믿는다(`G_Move`와 동일한 신뢰 수준) — 실제 인벤토리 보유 여부 확인 안 함 |
| 퀘스트 late-join 스냅샷 없음 | `RoomManager.SendWorldStateToGuest` | 새로 들어온 게스트는 그 전에 있었던 Accept/Submit/Complete 내역을 못 받음 — `H_ShelterLevel`처럼 접속 시 `QuestManager` 전체 상태를 보내는 스냅샷 패킷이 필요 |
| 퀘스트 보상 지급 정책 | `QuestDescriptionUI.GrantReward` | 정책: **완료 버튼을 누른 사람만** 보상을 받음(파티 전원 지급 아님). 보상 지급은 네트워크 동기화 대상이 아니라 순수 로컬 동작 — `H_QuestComplete`/`G_QuestComplete` 핸들러는 상태(`QuestManager.Complete`)만 맞추고 보상은 절대 지급하지 않는다. `RewardItems`가 여러 아이템이면 전부 로컬 인벤토리에 `AddItem` — 인벤토리가 가득 차면 `Inventory.AddItem`이 들어가는 만큼만 넣고 초과분은 유실(별도 처리 없음) |
| 플레이어 이름 UI 입력 | `NetworkManager` | 현재 `"Player"` 고정 |
| `RoomSync.GunAmmoSave` 디버그 로그 | `RoomSync.cs` | 제거 예정 TODO 잔존 |

## 제한 사항

- 치트 명령(`give`, `clear`, `god` 등)은 호스트만 사용 가능 — [cheat-console.md](../development/cheat-console.md)
- 최대 인원 3명(호스트+게스트 2) — README 등에 "4인"으로 기재된 과거 표기는 오류

## 서버

마스터 서버 구현: `Server/Server.sln` — 클라이언트 `NetworkManager`와 FlatBuffer 프로토콜 연동. 코드 생성 시 클라이언트와 동일 스키마를 서버 쪽에도 동시 출력한다.

## 관련 문서

- 패킷 상세·추가 절차: [network-packets.md](../development/network-packets.md)
- 아이템 교환 규칙: [inventory-exchange.md](inventory-exchange.md)
