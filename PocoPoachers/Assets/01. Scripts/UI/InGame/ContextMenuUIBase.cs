using UnityEngine;
using UnityEngine.InputSystem;

// 슬롯 우클릭 컨텍스트 메뉴 공통 껍데기 — 위치 잡기, 바깥 클릭 시 닫기, 인벤 닫히면 숨김.
// 자식이 실제 대상(장비 슬롯/인벤 슬롯)과 버튼 동작을 정의한다.
public abstract class ContextMenuUIBase : UIBase
{
    [SerializeField] protected Vector2 _offset = new Vector2(100f, 0f);

    private RectTransform _rectTransform;
    private bool _subscribed;

    protected override void Awake()
    {
        base.Awake();

        _rectTransform = GetComponent<RectTransform>();
    }

    // 씬에 비활성으로 배치된 패널은 Awake가 호출되지 않으므로 Awake에서 구독하면 안 된다.
    // UIManager가 비활성 패널까지 반드시 거치는 등록 경로(RegisterSelf→RegisterToManager)에서
    // 트리거 이벤트를 구독한다. 활성 패널은 Awake·씬 스캔으로 이 경로가 두 번 이상 올 수 있어 가드한다.
    protected override void RegisterToManager()
    {
        base.RegisterToManager();

        if (_subscribed) return;
        _subscribed = true;

        Subscribe();
        UIManager.GetInstance().OnPanelClosed += HandleInventoryClosed;
    }

    // 구독 대상이 DontDestroyOnLoad 싱글톤이라, 씬 전환으로 파괴될 때 해제하지 않으면
    // 파괴된 인스턴스로 이벤트가 들어와 예외가 난다
    protected override void OnDestroy()
    {
        if (_subscribed)
        {
            Unsubscribe();

            var ui = UIManager.GetInstance();
            if (ui != null) ui.OnPanelClosed -= HandleInventoryClosed;

            _subscribed = false;
        }

        base.OnDestroy();
    }

    // 트리거 이벤트 구독/해제 — 자식이 대상별 이벤트에 연결한다
    protected abstract void Subscribe();
    protected abstract void Unsubscribe();

    // 대상 슬롯 옆에 메뉴를 띄운다
    protected void ShowAtPosition(Vector3 anchorWorldPos)
    {
        transform.position = anchorWorldPos + (Vector3)_offset;
        Show();
    }

    private void HandleInventoryClosed(UIType type)
    {
        if (type == UIType.Inventory)
            Hide();
    }

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || _rectTransform == null) return;

        if (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)
        {
            Vector2 mousePos = mouse.position.ReadValue();
            if (!RectTransformUtility.RectangleContainsScreenPoint(_rectTransform, mousePos))
                Hide();
        }
    }
}
