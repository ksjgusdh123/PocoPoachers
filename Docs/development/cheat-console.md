# Cheat Console (개발용 치트 콘솔)

게임 테스트용 **에디터/개발 빌드 전용** 치트 콘솔.

소스: `PocoPoachers/Assets/01. Scripts/Cheats/CheatConsole.cs` (단일 파일, `#if UNITY_EDITOR || DEVELOPMENT_BUILD`로 전체를 감싸 릴리즈 빌드에서 완전히 빠짐)

아이템 ID 참고: [datatable/id-ranges.md](../datatable/id-ranges.md)

## 활성화 조건

- Unity 에디터 (`UNITY_EDITOR`) 또는 Development Build (`DEVELOPMENT_BUILD`)
- `Singleton<CheatConsole>`, `DontDestroyOnLoad` — `Bootstrapper`가 시작 시 생성

## 사용법

| 입력 | 동작 |
|------|------|
| `` ` `` (백틱) | 콘솔 토글 (Input System `Keyboard.current.backquoteKey`) |
| `Esc` | 콘솔 닫기 |
| `Enter` | 명령 실행 |

UI는 IMGUI(`OnGUI`)로 그려지는 720x420 스크롤 로그+입력창.

### 명령어

```
help                    명령 목록
give <itemId> [amount]  아이템 지급 (스택형은 AddItem, 비스택형은 uid 개별 발급)
clear                   인벤토리 비우기
items [limit]           아이템 ID 목록 (기본/상한 1~100)
shelter                 쉘터 레벨/다음 업그레이드 재료 확인
shelter level <n>       쉘터 레벨 강제 설정
shelter upgrade         재료 없이 1레벨 강제 업그레이드
shelter need            다음 업그레이드 재료 지급
god [on|off]            플레이어 무적 토글
```

### 예시

```
give 101 5
items 50
clear
```

## 명령 파싱

리플렉션이 아니라 수동 등록 방식: `Dictionary<string, CheatEntry>`(대소문자 무시)에 `RegisterCommands()`가 명령어→핸들러를 채워 넣는다. 입력은 공백으로 split, 첫 토큰이 명령어, 나머지가 `string[] args`.

## 명령 추가

`CheatConsole.RegisterCommands()`에 한 줄 추가:

```csharp
_commands["hp"] = new CheatEntry { Usage = "hp <value>", Handler = CmdHp };
```

`CmdHp(string[] args)` private 메서드 구현. `help`는 `Usage`를 자동 출력.

## 멀티플레이 동작 — 호스트 게이트 + 로컬 반영 (전용 동기화 패킷 없음)

- 상태를 바꾸는 모든 명령은 `RequireHost()`로 시작 — `RoomManager.IsHost`가 아니면 거부
- `god`는 `FindObjectsByType`로 로컬+원격(`RemotePlayerStat`) 스탯 컴포넌트를 **전부** 찾아 직접 플래그를 설정한다. 별도 RPC가 없어도 되는 이유는 데미지 판정 자체가 호스트 전용(`Bullet._applyDamage`)이라, 호스트가 가진 "모든 플레이어의 스탯 사본"에 플래그를 세팅하는 것만으로 효과가 나기 때문
- `give`/`clear`/`shelter`는 로컬 플레이어의 `Inventory`/`ShelterManager`/`Storage`에만 작용 — 이후 전파는 치트 전용 로직이 아니라 게임의 일반 상태 동기화 경로(인벤/쉘터 레벨 동기화)를 그대로 탄다

## 제한 사항

- 호스트만 상태 변경 명령 사용 가능
- 플레이어 스폰 후 사용 (스폰 전 `Inventory` 참조 없음)
- `give`는 `Inventory.AddItem` 정식 경로 사용 (치트 전용 우회 로직 아님)

## Development Build

**File → Build Settings → Development Build** 체크 후 빌드.
