using UnityEngine;

// 튜토리얼에서 F 상호작용 없이 대사를 여는 공용 경로.
// 대사 진행(다음 줄/닫기)과 입력맵 복귀는 DialogueUI가 알아서 처리한다.
public static class TutorialDialogue
{
    public static bool Open(int dialogueId, PlayerController player)
    {
        if (dialogueId <= 0 || player == null) return false;

        var dialogueUI = Object.FindAnyObjectByType<DialogueUI>(FindObjectsInactive.Include);
        if (dialogueUI == null)
        {
            Debug.LogWarning("[TutorialDialogue] 씬에 DialogueUI가 없어 대사를 열지 못했습니다.");
            return false;
        }

        dialogueUI.OpenById(dialogueId, player);
        return true;
    }
}
