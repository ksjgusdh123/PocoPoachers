using TMPro;
using UnityEngine;

// 대화 UI — 열리면 다른 모든 UI를 닫고 이 화면만 띄운다.
public class DialogueUI : UIBase
{
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _dialogueText;

    [Tooltip("대화창이 열릴 때 같이 켜지는 배경(화면 전체를 덮는 어두운 이미지 등). 씬/프리팹에서 직접 배치해서 연결.")]
    [SerializeField] private GameObject _backgroundDim;

    [Header("Test")]
    [SerializeField, Tooltip("테스트용 — 켜두면 씬 시작 직후 아래 이름/대사로 자동으로 열린다. 테스트 끝나면 꺼두세요.")]
    private bool _testAutoOpen;
    [SerializeField] private string _testSpeakerName = "테스트";
    [SerializeField, TextArea] private string _testDialogue = "테스트 대사입니다.";

    protected override UIType UiType => UIType.Dialogue;

    protected override void Awake()
    {
        base.Awake();
        if (_testAutoOpen) Open(_testSpeakerName, _testDialogue);
    }

    // 다른 UI를 전부 닫고 이 대화창만 연다
    public void Open(string speakerName, string dialogue)
    {
        UIManager.GetInstance().HideAll();
        SetContent(speakerName, dialogue);
        Show();
        if (_backgroundDim != null) _backgroundDim.SetActive(true);
    }

    // 이미 열려있는 상태에서 다음 대사로 내용만 갱신할 때
    public void SetContent(string speakerName, string dialogue)
    {
        if (_nameText != null) _nameText.text = speakerName;
        if (_dialogueText != null) _dialogueText.text = dialogue;
    }

    protected override void OnHide()
    {
        if (_backgroundDim != null) _backgroundDim.SetActive(false);
    }
}
