using UnityEngine;

// NPC와 상호작용(F)하면 지정된 대사부터 DialogueUI를 연다.
// 대사 진행(다음 줄 넘기기/닫기)과 입력맵 전환은 DialogueUI가 직접 처리하므로,
// 여기서는 대화를 연 즉시 일반 월드 상호작용을 종료 처리한다.
// 대사가 끝나면 DialogueUI가 이 대사의 npc_id에 걸린 퀘스트 목록을 띄운다 - 퀘스트가 없으면 그냥 닫힌다.
public class NpcDialogueInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private int _startDialogueId;

    [Tooltip("켜면 대사가 끝나도 퀘스트 목록을 띄우지 않는다. 퀘스트와 무관한 안내용 NPC에 쓴다")]
    [SerializeField] private bool _skipQuestMenu;

    private DialogueUI _dialogueUI;

    private void Awake()
    {
        _dialogueUI = FindAnyObjectByType<DialogueUI>(FindObjectsInactive.Include);
    }

    public void OnInteract(PlayerController player)
    {
        // 대화를 못 열더라도 상호작용은 반드시 끝내야 한다 —
        // 안 그러면 PlayerController가 이 NPC를 계속 붙들고 있어 F가 한 번 걸러 먹힌다.
        if (_dialogueUI == null)
        {
            Debug.LogWarning($"[NpcDialogueInteractable] 씬에 DialogueUI가 없어 대사를 열지 못했습니다. ({name})", this);
            player.EndInteraction(this);
            return;
        }

        _dialogueUI.OpenById(_startDialogueId, player, !_skipQuestMenu);
        player.EndInteraction(this);
    }

    public void OnInteractExit(PlayerController player) { }
}
