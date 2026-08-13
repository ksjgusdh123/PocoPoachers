using UnityEngine;

// 튜토리얼에서 가야 할 지점을 야광 박스로 표시하고, 플레이어가 들어오면 다음 대사를 연다.
// 도착하면 스스로 꺼지고 _next를 켜서 다음 지점으로 이어진다 — 체인 중간 지점들은 씬에서 꺼둔다.
//
// 도착 판정은 트리거가 아니라 거리 비교로 한다. 플레이어가 CharacterController라
// 트리거 이벤트가 콜라이더/레이어 설정에 따라 안 들어올 수 있어서다.
// 표식은 런타임에 스스로 만든다. 에디터에서는 기즈모로 위치를 확인한다.
public class TutorialWaypoint : MonoBehaviour
{
    [SerializeField] private int _dialogueId;

    [Tooltip("도착 후 켜질 다음 지점 (비어 있으면 여기서 끝)")]
    [SerializeField] private TutorialWaypoint _next;

    [Tooltip("도착으로 판정할 거리(수평 기준, m)")]
    [SerializeField] private float _reachRadius = 1.5f;

    [Header("표식")]
    [SerializeField] private Vector3 _size = new Vector3(2f, 2f, 2f);

    [Tooltip("상자 모서리 막대의 두께(m)")]
    [SerializeField] private float _edgeThickness = 0.08f;

    [SerializeField] private Color _color = new Color(0.35f, 1f, 0.7f, 0.8f);
    [SerializeField] private float _pulseSpeed = 2f;
    [SerializeField] private float _pulseAlpha = 0.15f;
    [SerializeField] private float _spinSpeed = 25f;

    private Transform _visual;
    private Material _material;
    private PlayerController _player;
    private bool _reached;

    private void Awake() => BuildVisual();

    private void OnDestroy()
    {
        // new Material로 만든 인스턴스는 GC 대상이 아니라 직접 파괴해야 한다
        if (_material != null) Destroy(_material);
    }

    private void Update()
    {
        Animate();

        if (_reached) return;

        if (_player == null)
        {
            _player = FindAnyObjectByType<PlayerController>();
            if (_player == null) return;
        }

        Vector3 delta = _player.transform.position - transform.position;
        delta.y = 0f;
        if (delta.sqrMagnitude > _reachRadius * _reachRadius) return;

        Reach();
    }

    private void Reach()
    {
        _reached = true;

        // 여기까지 온 진행을 죽어도 잃지 않도록 부활 지점을 갱신한다
        _player.GetComponent<PlayerRespawnPoint>()?.SetPoint(transform.position);

        TutorialDialogue.Open(_dialogueId, _player);

        if (_next != null) _next.gameObject.SetActive(true);
        gameObject.SetActive(false);
    }

    private void Animate()
    {
        if (_visual == null) return;

        _visual.Rotate(Vector3.up, _spinSpeed * Time.deltaTime, Space.Self);

        if (_material == null) return;

        Color color = _color;
        color.a = Mathf.Max(0f, _color.a + Mathf.Sin(Time.time * _pulseSpeed) * _pulseAlpha);
        _material.color = color;
    }

    // 속은 비우고 모서리 12개만 얇은 막대로 세워 상자 윤곽을 만든다
    private void BuildVisual()
    {
        _material = MakeGlowMaterial();

        var root = new GameObject("WaypointGlow");
        _visual = root.transform;
        _visual.SetParent(transform, false);
        _visual.localPosition = new Vector3(0f, _size.y * 0.5f, 0f);

        Vector3 half = _size * 0.5f;
        float t = _edgeThickness;

        // 모서리가 코너에서 벌어지지 않도록 막대 길이를 두께만큼 늘려 겹친다
        for (int i = 0; i < 4; i++)
        {
            float sy = (i & 1) == 0 ? -1f : 1f;
            float sz = (i & 2) == 0 ? -1f : 1f;
            CreateEdge(new Vector3(0f, half.y * sy, half.z * sz), new Vector3(_size.x + t, t, t));
        }

        for (int i = 0; i < 4; i++)
        {
            float sx = (i & 1) == 0 ? -1f : 1f;
            float sz = (i & 2) == 0 ? -1f : 1f;
            CreateEdge(new Vector3(half.x * sx, 0f, half.z * sz), new Vector3(t, _size.y + t, t));
        }

        for (int i = 0; i < 4; i++)
        {
            float sx = (i & 1) == 0 ? -1f : 1f;
            float sy = (i & 2) == 0 ? -1f : 1f;
            CreateEdge(new Vector3(half.x * sx, half.y * sy, 0f), new Vector3(t, t, _size.z + t));
        }
    }

    private void CreateEdge(Vector3 localPosition, Vector3 localScale)
    {
        var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.name = "Edge";

        // 표식이 이동/사격을 막지 않도록 콜라이더는 떼어낸다
        var barCollider = bar.GetComponent<Collider>();
        if (barCollider != null) Destroy(barCollider);

        bar.transform.SetParent(_visual, false);
        bar.transform.localPosition = localPosition;
        bar.transform.localScale = localScale;

        var renderer = bar.GetComponent<Renderer>();
        renderer.sharedMaterial = _material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    // RescueBeamEffect의 잔상 머티리얼과 같은 방식 — URP Unlit을 투명으로 돌려 쓴다
    private Material MakeGlowMaterial()
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = _color };

        material.SetFloat("_Surface", 1f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // 가산 합성 — 야광처럼 밝게
        material.SetInt("_ZWrite", 0);
        material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        return material;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(_color.r, _color.g, _color.b, 1f);
        Gizmos.DrawWireCube(transform.position + new Vector3(0f, _size.y * 0.5f, 0f), _size);
        Gizmos.DrawWireSphere(transform.position, _reachRadius);
    }
}
