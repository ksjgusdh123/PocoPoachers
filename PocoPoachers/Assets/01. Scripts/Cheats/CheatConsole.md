# Cheat Console (개발용 치트 콘솔)

게임 테스트용 **개발 빌드 전용** 치트 콘솔입니다.

## 활성화 조건

- Unity 에디터 (`UNITY_EDITOR`)
- Development Build (`DEVELOPMENT_BUILD`)

## 파일

| 파일 | 역할 |
|------|------|
| `CheatConsole.cs` | 콘솔 UI + 명령 등록/실행 + 아이템 치트 |
| `Bootstrapper.cs` | 시작 시 `CheatConsole.GetInstance()` |

## 사용법

| 입력 | 동작 |
|------|------|
| `` ` `` | 콘솔 토글 |
| `Esc` | 콘솔 닫기 |
| `Enter` | 명령 실행 |

### 명령어

```
help                  명령 목록
give <itemId> [amount]  아이템 지급
clear                 인벤토리 비우기
items [limit]         아이템 ID 목록
shelter               쉘터 레벨/재료 확인
shelter level <n>     쉘터 레벨 설정
shelter upgrade       재료 없이 1레벨 업
shelter need          다음 업그레이드 재료 지급
```

### 예시

```
give 101 5
items 50
clear
```

## 명령 추가

`CheatConsole.RegisterCommands()`에 한 줄 추가:

```csharp
_commands["hp"] = new CheatEntry { Usage = "hp <value>", Handler = CmdHp };
```

`CmdHp(string[] args)` private 메서드 구현. `help`는 `Usage`를 자동 출력.

## 제한 사항

- **호스트만** `give` / `clear` 사용 가능
- 플레이어 스폰 후 사용
- `Inventory.AddItem` 경로 사용 (정식 로직과 동일)
- Shelter 등 `_inventory` 미연결 씬은 `PlayerController.BindPlayerInventoryUI()`로 UI 바인딩

## Development Build

**File → Build Settings → Development Build** 체크 후 빌드.
