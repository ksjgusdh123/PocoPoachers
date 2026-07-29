using UnityEngine;

// 씬/프리팹에 배치된 UI 패널의 공통 베이스.
// - UIManager에 자기 자신을 등록하고 열림/닫힘 훅(OnShow/OnHide)을 제공한다.
// - 등록의 정본 경로는 UIManager가 씬 로드마다 비활성 오브젝트까지 스캔해 RegisterSelf를
//   호출해주는 쪽이고, Awake의 등록은 런타임에 새로 생성되는 패널을 위한 보조 경로다.
public abstract class UIBase : MonoBehaviour
{
    [SerializeField, Tooltip("체크하면 씬 로드 시 열린 상태로 둔다. 기본은 닫힌 상태.")]
    private bool _startVisible = false;

    protected abstract UIType UiType { get; }

    protected virtual void Awake()
    {
        RegisterSelf();
    }

    protected virtual void OnDestroy()
    {
        if (UIManager.GetInstance() == null) return;
        UnregisterSelf();
    }

    public void RegisterSelf()
    {
        RegisterToManager();
        ApplyInitialVisibility();
    }

    // 등록 방식만 파생 클래스가 바꿀 수 있게 분리한다. RegisterSelf 자체를 오버라이드하면
    // 초기 활성 처리(ApplyInitialVisibility)가 함께 빠져버리기 때문이다.
    protected virtual void RegisterToManager() => UIManager.GetInstance().Register(UiType, gameObject);

    protected virtual void UnregisterSelf() => UIManager.GetInstance().Unregister(UiType, gameObject);

    // 씬에 비활성으로 배치된 패널은 UIManager.Show()가 활성화하는 순간 Awake가 처음 호출된다.
    // 이때 무조건 SetActive(false)를 하면 열자마자 닫혀버리므로, 이미 열림 스택에 올라가 있으면
    // 활성 상태를 건드리지 않는다.
    private void ApplyInitialVisibility()
    {
        if (_startVisible) return;
        if (UIManager.GetInstance().IsInOpenStack(UiType)) return;
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }

    public void Show()   => UIManager.GetInstance().Show(UiType);
    public void Hide()   => UIManager.GetInstance().Hide(UiType);
    public void Toggle() => UIManager.GetInstance().Toggle(UiType);

    // UIManager가 패널을 열거나 닫은 직후 호출한다. 갱신 로직은 OnEnable 대신 여기에 두면
    // 열림 순서와 이벤트 발행 시점이 보장된다.
    protected virtual void OnShow() { }
    protected virtual void OnHide() { }

    internal void NotifyShown()  => OnShow();
    internal void NotifyHidden() => OnHide();
}
