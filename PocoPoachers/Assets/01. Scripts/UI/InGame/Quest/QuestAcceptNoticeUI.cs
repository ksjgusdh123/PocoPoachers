using DG.Tweening;
using TMPro;
using UnityEngine;

// 퀘스트가 수락되거나 완료되면 잠깐 떴다 사라지는 토스트 알림. 다른 UI 패널처럼 씬에 비활성 상태로 배치해도 된다 -
// UIBase를 상속하면 UIManager가 씬 로드마다 비활성 오브젝트까지 스캔해서 RegisterSelf를 직접
// 호출해주기 때문에(SceneUIRegistrar와 동일 원리) OnEnable 없이도 QuestManager 이벤트 구독이 이뤄진다.
//
// 다만 UIManager.Show/Hide는 의도적으로 안 쓴다 - 그건 패널 열림 스택에 들어가서 전투 중에도
// 크로스헤어를 숨기고 커서를 풀어버리는데(UIManager.RefreshCursor), 이 토스트는 게임플레이를
// 막으면 안 되므로 SetActive/CanvasGroup을 직접 제어한다. UiType은 등록만 되고 스택엔 안 올라간다.
public class QuestAcceptNoticeUI : UIBase
{
    [SerializeField] private TextMeshProUGUI _questNameText;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private float _displayDuration = 3f;
    [SerializeField] private float _fadeDuration = 0.2f;

    private const string AddedMessage = "퀘스트가 추가되었습니다";
    private const string CompletedMessage = "퀘스트가 완료되었습니다";

    protected override UIType UiType => UIType.QuestNotice;

    private CanvasGroup _canvasGroup;
    private Tween _tween;
    private bool _subscribed;

    protected override void Awake()
    {
        // base.Awake()(RegisterSelf → ApplyInitialVisibility)를 일부러 안 부른다: 이 오브젝트는
        // 런타임에 새로 생성되는 패널이 아니라 씬에 미리 배치된 거라 그 "보조 경로"가 필요 없고,
        // 오히려 Show()의 SetActive(true)가 처음으로 이 Awake를 트리거했을 때(비활성 시작 상태)
        // ApplyInitialVisibility가 "열림 스택에 없다"고 판단해 바로 다시 꺼버리는 문제가 생긴다.
        // 등록/구독은 UIManager.RegisterScenePanels가 비활성 상태에서도 RegisterSelf를 직접
        // 호출해주는 정본 경로만으로 충분하다.

        if (!TryGetComponent(out _canvasGroup))
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;

        if (_questNameText == null)
            Debug.LogWarning("[QuestAcceptNoticeUI] _questNameText가 비어있습니다 - 인스펙터에서 QuestName 텍스트를 연결해주세요.", this);
        if (_messageText == null)
            Debug.LogWarning("[QuestAcceptNoticeUI] _messageText가 비어있습니다 - 인스펙터에서 메시지 텍스트를 연결해주세요.", this);
    }

    // UIManager가 씬 로드마다 비활성 오브젝트까지 포함해 이걸 직접 호출해준다 - 그래서 오브젝트가
    // 비활성으로 시작해도(OnEnable이 안 불려도) 구독이 이뤄진다. 스캔이 2번(OnSceneLoaded/Start) 돌거나
    // 나중에 SetActive(true)로 Awake가 또 불릴 수 있어 _subscribed로 중복 구독을 막는다.
    protected override void RegisterToManager()
    {
        base.RegisterToManager();
        if (_subscribed) return;
        _subscribed = true;
        QuestManager.OnQuestStateChanged += HandleQuestStateChanged;
    }

    protected override void UnregisterSelf()
    {
        if (_subscribed)
        {
            _subscribed = false;
            QuestManager.OnQuestStateChanged -= HandleQuestStateChanged;
        }
        base.UnregisterSelf();
    }

    private void HandleQuestStateChanged(int questId, QuestState state)
    {
        string message = state switch
        {
            QuestState.InProgress => AddedMessage,   // Available -> InProgress(수락) 순간
            QuestState.Completed => CompletedMessage, // -> Completed(완료) 순간
            _ => null,
        };
        if (message == null) return;

        var quest = QuestTable.Instance.Get(questId);
        if (quest == null) return;

        Show(quest.QuestName, message);
    }

    private void Show(string questName, string message)
    {
        if (_questNameText != null) _questNameText.text = questName;
        if (_messageText != null) _messageText.text = message;

        gameObject.SetActive(true); // 이 시점에 처음 활성화되면 Awake가 여기서 돌 수 있다 - 문제 없음

        _tween?.Kill();
        _canvasGroup.alpha = 0f;

        _tween = DOTween.Sequence()
            .Append(_canvasGroup.DOFade(1f, _fadeDuration))
            .AppendInterval(_displayDuration)
            .Append(_canvasGroup.DOFade(0f, _fadeDuration))
            .AppendCallback(() => gameObject.SetActive(false))
            .SetUpdate(true); // 일시정지 중에도 재생(UIManager 패널 연출과 동일한 관례)
    }

    // private로 새로 선언하면 UIBase.OnDestroy(UnregisterSelf 호출)를 가려버려서 구독이 안 풀린다 - override 필수.
    protected override void OnDestroy()
    {
        _tween?.Kill();
        base.OnDestroy();
    }
}
