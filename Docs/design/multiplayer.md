# 멀티플레이

P2P 아키텍처 개요, 입장 흐름, 동기화 현황. 패킷 구조는 [network-packets.md](../development/network-packets.md).

## 아키텍처

```
[클라이언트] ──TCP──▶ [마스터 서버]  로그인, 방 생성/참가
[호스트] ◀────UDP P2P────▶ [게스트]  게임 상태 동기화
```

- **마스터 서버 (TCP):** 로그인, 하트비트, 방 생성/참가 (`NetworkManager`, FlatBuffer)
- **P2P 게임 (UDP):** STUN + UDP 홀펀칭 (`RoomManager`, `UdpSession`, `StunClient`)
- **호스트 권위:** 적 AI, 데미지, 아이템 박스, 내구도는 호스트가 계산

## 핵심 클래스

| 클래스 | 역할 |
|--------|------|
| `NetworkManager` | TCP 연결, 로그인, RTT, DontDestroyOnLoad |
| `RoomManager` | 호스트/게스트, 세션 코드, 게스트 동기화, 타임아웃 30초 |
| `RoomSync` | 이동·사격·장착·내구도·스탯·적·아이템 패킷 전송 |
| `ObjectManager` | 원격 플레이어·아이템 박스 스폰/디스폰 |
| `RemotePlayerStat` | 원격 플레이어 HP 동기화 |

## 싱글플레이

`RoomManager.StartLocalHost()` — UDP 없이 즉시 호스트 모드로 진행.

## 연결 흐름

### 호스트 (새 게임)

1. `NetworkManager` 마스터 서버 TCP 로그인
2. `RoomManager` 방 생성 → 6자리 초대 코드 발급
3. 실패 시 `StartLocalHost()` 폴백
4. `SC_Shelter` 로드

### 게스트 (협동 참가)

1. TCP 로그인
2. `JoinCodeUI`에서 6자리 코드 입력 → 방 참가
3. UDP 홀펀칭 후 게임 세션 시작

## 동기화 패킷 (FlatBuffer)

### 플레이어

| 패킷 | 방향 | 내용 |
|------|------|------|
| `G/H_Move` | 양방향 | 위치·회전 |
| `G/H_Shoot` | 양방향 | 발사 |
| `G/H_Equip` | 양방향 | 장비 변경 |
| `G/H_Durability` | 호스트→게스트 | 무기 내구도 |
| `G/H_StatSync` | 호스트→게스트 | HP 등 스탯 |
| `G/H_Leave` | — | 퇴장 |

### 적

`H_EnemySpawn`, `H_EnemyMove`, `H_EnemyHit`, `H_EnemyDie`

### 아이템

`G_ItemGain`, `G_ItemExchange`, `H_ItemSpawn`, `H_ItemDespawn`, `H_ItemBoxUpdate`

### 방

`C/S_CreateRoom`, `C/S_JoinRoom`, `H_GuestJoined`

## UI 연동

| UI | 기능 |
|----|------|
| `TeamPanelUI` | 호스트 초대 코드 생성/복사, 팀 슬롯 4명 |
| `JoinCodeUI` | 게스트 코드 참가 |
| `IngameMenuUI` | ESC 메뉴, 호스트/게스트 이탈 |

## 동기화 미구현 항목

### 기능 버그

| 항목 | 위치 | 설명 |
|------|------|------|
| 게스트 입장 시 장비 내구도 미전달 | `RoomManager.cs:314` | ItemUid=0 전달 → H_Durability 적용 안됨 |
| 게스트 입장 시 아이템 박스 ItemUid 누락 | `RoomManager.cs:303` | H_ItemSpawnT의 ItemUids 필드 미전송 |
| 게스트 입장 시 방어구/가방 장착 상태 누락 | `RoomManager.cs:314` | WeaponMount 2슬롯만 전송, ArmorMount·BagMount 누락 |
| 게스트 입장 시 호스트 HP/스탯 초기값 누락 | `RoomManager.cs:260` | 2초 주기 StatSync 전까지 기본값 표시 |
| 장비 능력치 미동기화 | — | MaxHp 보너스·이동속도 배수 로컬만 적용 (defense 제외) |
| Sandbag 파괴 미동기화 | `Game/Props/Sandbag.cs:16` | 게스트 화면에서 Sandbag 파괴 안됨 |
| 쉘터 업그레이드 미동기화 | `ShelterManager.cs:44` | 행성 잠금 해제 조건이 양측에서 다름 |
| H_ConsumeItemResult 핸들러 비어있음 | `PacketHandler.Item.cs:89` | 소비 아이템 VFX/사운드/애니메이션 원격 미재생 |

### 비주얼/피드백

| 항목 | 위치 | 설명 |
|------|------|------|
| 총기 발사 사운드 미동기화 | `PacketHandler.Combat.cs` | OnH_Shoot에서 SFX 재생 없음 |
| 발자국 사운드 미전달 | — | — |

## 제한 사항

- 치트 명령 (`give`, `clear`)은 호스트만 사용 가능
- 게스트 이탈 시 호스트 재입장 대기 처리 TODO (`IngameMenuUI`)

## 서버

마스터 서버 구현: `Server/Server.sln` — 클라이언트 `NetworkManager`와 FlatBuffer 프로토콜 연동.

## 관련 문서

- 패킷 상세·추가 절차: [development/network-packets.md](../development/network-packets.md)
- 아이템 교환 규칙: [inventory-exchange.md](inventory-exchange.md)
