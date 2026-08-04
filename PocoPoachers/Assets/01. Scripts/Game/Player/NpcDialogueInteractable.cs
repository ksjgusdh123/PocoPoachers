using UnityEngine;

// NPC와 상호작용(F)하면 지정된 대사부터 DialogueUI를 연다.
// 대사 진행(다음 줄 넘기기/닫기)과 입력맵 전환은 DialogueUI가 직접 처리하므로,
// 여기서는 대화를 연 즉시 일반 월드 상호작용을 종료 처리한다.
public class NpcDialogueInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private int _startDialogueId;

    private DialogueUI _dialogueUI;

    private void Awake()
    {
        _dialogueUI = FindAnyObjectByType<DialogueUI>(FindObjectsInactive.Include);
    }

    public void OnInteract(PlayerController player)
    {
        if (_dialogueUI == null) return;

        _dialogueUI.OpenById(_startDialogueId, player);
        player.EndInteraction(this);
    }

    public void OnInteractExit(PlayerController player) { }
}
