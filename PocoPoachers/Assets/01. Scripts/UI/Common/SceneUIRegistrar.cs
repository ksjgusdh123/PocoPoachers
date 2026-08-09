using UnityEngine;

// Player는 씬마다 PlayerSpawner가 새로 Instantiate하는 프리팹이라 씬에 배치된 UI 패널을
// 인스펙터로 직접 참조할 수 없다. 대신 각 UI 패널이 이 컴포넌트로 스스로 UIManager에
// 등록하고, PlayerController 등은 UIManager.GetPanel(UIType)로 조회한다.
// 대부분의 패널은 기본적으로 비활성(SetActive(false)) 상태라 씬 로드 시 Awake가 호출되지
// 않는다. 그래서 UIManager가 씬 로드마다 FindObjectsByType(Include)로 전체를 스캔해
// RegisterSelf를 직접 호출해준다. Awake의 자체 등록은 런타임에 활성 상태로 새로 생성되는
// 패널(예: 팝업)을 위한 보조 경로일 뿐이다.
public class SceneUIRegistrar : MonoBehaviour
{
    [SerializeField] private UIType _uiType;

    [SerializeField, Tooltip("체크하면 이 패널이 열릴 때 UIManager가 패널 뒤에 공용 딤머를 깔고 뒤쪽 클릭을 막는다. 패널이 자체 딤머를 갖고 있으면 체크하지 않는다.")]
    private bool _useSharedDimmer = false;

    // 이 경로로 등록되는 패널은 자식만 토글하는 컨테이너인 경우가 많아 기본값을 끈다.
    // 컨테이너에 블러를 깔면 상시 활성 상태로 화면을 덮고 블러 패스도 계속 돈다.
    [SerializeField, Tooltip("체크하면 이 오브젝트에 뒷배경 블러를 깐다. 자식만 토글하는 컨테이너면 끄고, 안쪽 패널에 직접 붙일 것.")]
    private bool _useBackdropBlur = false;

    public bool UseSharedDimmer => _useSharedDimmer;

    private void Awake()
    {
        RegisterSelf();

        if (_useBackdropBlur) UIRealtimeBackdropBlur.Attach(gameObject);
    }

    public void RegisterSelf() => UIManager.GetInstance().Register(_uiType, gameObject);

    private void OnDestroy()
    {
        var ui = UIManager.ExistingInstance;
        if (ui != null) ui.Unregister(_uiType, gameObject);
    }
}
