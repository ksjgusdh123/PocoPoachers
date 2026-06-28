# 세이브 시스템

## 저장 위치

`Application.persistentDataPath/save_{slotIndex}.json`

## SaveManager API

| 메서드 | 설명 |
|--------|------|
| `SaveInventory(key, inventory)` | 키별 인벤토리 저장 |
| `LoadInventory(key, inventory)` | 키별 인벤토리 로드 |
| `SaveShelterLevel(level)` | 쉘터 레벨 저장 |
| `LoadShelterLevel()` | 쉘터 레벨 로드 |

## JSON 구조

```json
{
  "lastSavedAt": "<timestamp>",
  "shelterLevel": 1,
  "inventories": [
    {
      "key": "player_inventory",
      "slots": [
        { "slotIndex": 0, "itemId": 101, "amount": 5 }
      ]
    },
    {
      "key": "storage",
      "slots": [ ... ]
    }
  ]
}
```

## 저장되는 항목

| 항목 | 키 / 필드 |
|------|-----------|
| 플레이어 인벤토리 | `player_inventory` |
| 창고 인벤토리 | `storage` |
| 쉘터 레벨 | `shelterLevel` |
| 마지막 저장 시각 | `lastSavedAt` |

## 저장되지 않는 항목

| 항목 | 비고 |
|------|------|
| 플레이어 강화 레벨 | `PlayerEnhancement` |
| 장착 장비 | 무기·방어구·가방 슬롯 |
| HP / 배터리 / 스태미나 | Vital 현재값 |
| 무기 내구도 | `WorldEquipmentManager` |
| 행성 진행도 | — |

## 슬롯 UI

| 클래스 | 역할 |
|--------|------|
| `SaveSlotButtonUI` | 슬롯 선택 버튼 |
| `SaveSlotPanelUI` | 슬롯 패널 |

타이틀 `MainMenuUI`에서 로드 시 `GameManager.ShouldLoadPlayerInventory = true` 설정 후 쉘터 진입 시 `PlayerController`가 로드.

## 자동 저장 시점

쉘터 업그레이드 성공 시 `SaveShelterLevel()` 호출. 인벤토리는 명시적 저장 호출 경로에 따름 (씬 전환·상호작용 시점은 코드 참고).
