using System;
using System.Collections;

// 새 게임 진입 — 새 슬롯을 만들어 닉네임을 기록하고 호스트 세션을 연다.
// 세션이 열리면 RoomManager.LoadShelterIfOnMenu가 쉘터로 넘긴다.
// 캐릭터 생성 확정(CharacterCreateUI)과 생성 건너뛰기(MainMenuUI)가 같은 경로를 타도록 한 곳에 모았다.
public static class NewGameStart
{
    // nickname이 비어 있으면(캐릭터 생성 건너뛰기) 슬롯 번호를 붙인 기본 이름을 쓴다.
    // onCancel: 연결 실패 경고창을 취소했을 때 — 호출측이 잠갔던 버튼을 되돌리는 용도
    public static IEnumerator Run(string nickname, Action onCancel)
    {
        var save = SaveManager.GetInstance();
        save.AllocateNewSlot();
        save.SaveNickname(string.IsNullOrWhiteSpace(nickname) ? DefaultNickname(save.ActiveSlot) : nickname);
        save.LoadEquipmentState(); // 새 슬롯: 장비 상태 초기화 + uid 카운터 리셋
        save.LoadQuestState();     // 새 슬롯: 퀘스트 진행 상태 초기화

        var loc = LocalizationManager.GetInstance();
        yield return NetworkConnectFlow.Run(
            onSuccess: () => RoomManager.Instance.StartAsHost(),
            onFail: () => UIManager.GetInstance().ShowWarning(
                loc.GetString("network.connect_failed_title"),
                loc.GetString("network.local_play_fallback"),
                onConfirm: () => RoomManager.Instance.StartLocalHost(),
                onCancel: onCancel));
    }

    // 슬롯 번호를 붙여 여러 슬롯이 같은 이름으로 겹치지 않게 한다
    static string DefaultNickname(int slotIndex) =>
        $"{LocalizationManager.GetInstance().GetString("character.default_nickname")} {slotIndex + 1}";
}
