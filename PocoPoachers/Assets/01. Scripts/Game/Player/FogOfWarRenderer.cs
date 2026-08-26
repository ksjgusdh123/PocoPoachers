using UnityEngine;
using UnityEngine.Rendering;

public class FogOfWarRenderer : MonoBehaviour
{
    [SerializeField] private VisionConfig _visionConfig;
    [SerializeField] private float _groundOffset = 0.05f;
    [SerializeField] private float _overlaySize = 500f;
    [SerializeField] private Color _darkColor = new Color(0f, 0f, 0f, 0.6f);
    [SerializeField] private LayerMask _wallLayer;

    [Header("Blur")]
    [SerializeField] private float _blurSize = 3f;
    [SerializeField, Range(1, 4)] private int _blurIterations = 2;

    private Camera _mainCam;
    private RenderTexture _fogMaskRT;
    private RenderTexture _blurTempRT;
    private Material _blurMat;
    private Material _fovMaskMat;
    private Material _depthOnlyMat;
    private Material _darkOverlayMat;
    private Renderer[] _wallRenderers;

    private Transform _fovMeshTrans;
    private Mesh _fovMesh;
    private Transform _circleMeshTrans;
    private Mesh _circleMesh;
    private Transform _overlayTrans;
    private Mesh _overlayMesh;
    private CommandBuffer _cmd;

    // 캐릭터 몸통 회전(transform.forward)은 구르기 중 PlayerDodge가 이동 방향으로 강제로 틀어버려서
    // 시야 기준으로 삼으면 구르기 시작/종료 시점에 홱 튄다. 몸 회전과 분리해 크로스헤어 방향을 직접 쓴다.
    private Vector3 _visionForward = Vector3.forward;

    // Update에서 매 프레임 new 하면 프레임당 4개의 배열이 GC로 흘러가므로 재사용한다.
    private Vector3[] _fovVertices;
    private int[] _fovTriangles;
    private Vector3[] _circleVertices;
    private int[] _circleTriangles;

    private static readonly int FogMaskProp  = Shader.PropertyToID("_FogMask");
    private static readonly int BlurSizeProp = Shader.PropertyToID("_BlurSize");

    private void Awake()
    {
        _wallLayer = 1 << LayerMask.NameToLayer("Wall");
        _mainCam   = Camera.main;

        if (_mainCam == null)
        {
            // 메인 카메라가 없으면 렌더 대상 자체가 없다 — 아래 cullingMask 접근에서 NRE가 나므로 중단한다.
            Debug.LogError("[FogOfWarRenderer] MainCamera를 찾을 수 없어 안개 렌더링을 비활성화합니다.");
            enabled = false;
            return;
        }

        CreateRenderTextures();
        CreateDepthOnlyMaterial();
        RefreshWallRenderers();
        CreateFovMeshObject();
        CreateCircleMeshObject();
        CreateDarkOverlayObject();

        _mainCam.cullingMask &= ~LayerMask.GetMask("FogMask");

        _cmd = new CommandBuffer { name = "FogMaskRender" };
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    private void OnDestroy()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;

        if (_fovMeshTrans    != null) Destroy(_fovMeshTrans.gameObject);
        if (_circleMeshTrans != null) Destroy(_circleMeshTrans.gameObject);
        if (_overlayTrans    != null) Destroy(_overlayTrans.gameObject);

        if (_fogMaskRT  != null) { _fogMaskRT.Release();  Destroy(_fogMaskRT); }
        if (_blurTempRT != null) { _blurTempRT.Release(); Destroy(_blurTempRT); }

        // new Material / new Mesh 로 만든 것은 GameObject를 파괴해도 함께 해제되지 않는다.
        if (_depthOnlyMat   != null) Destroy(_depthOnlyMat);
        if (_blurMat        != null) Destroy(_blurMat);
        if (_fovMaskMat     != null) Destroy(_fovMaskMat);
        if (_darkOverlayMat != null) Destroy(_darkOverlayMat);

        if (_fovMesh     != null) Destroy(_fovMesh);
        if (_circleMesh  != null) Destroy(_circleMesh);
        if (_overlayMesh != null) Destroy(_overlayMesh);

        _cmd?.Release();
    }

    private void Update()
    {
        Vector3 groundPos = new Vector3(
            transform.position.x,
            transform.position.y + _groundOffset,
            transform.position.z);

        _fovMeshTrans.position    = groundPos;
        _fovMeshTrans.rotation    = Quaternion.identity;
        _circleMeshTrans.position = groundPos;
        _overlayTrans.position    = groundPos;

        _visionForward = GetCrosshairForward(groundPos);

        UpdateFovMesh();
        UpdateCircleMesh();
    }

    private void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam)
    {
        if (cam != _mainCam) return;

        _cmd.Clear();
        _cmd.SetRenderTarget(_fogMaskRT);
        _cmd.ClearRenderTarget(true, true, Color.black);
        _cmd.SetViewProjectionMatrices(cam.worldToCameraMatrix, cam.projectionMatrix);

        DrawWallDepth();

        // 부채꼴 FOV + 주변 원형 시야를 같은 RT에 렌더
        _cmd.DrawMesh(_fovMesh,    _fovMeshTrans.localToWorldMatrix,    _fovMaskMat);
        _cmd.DrawMesh(_circleMesh, _circleMeshTrans.localToWorldMatrix, _fovMaskMat);

        Graphics.ExecuteCommandBuffer(_cmd);

        _blurMat.SetFloat(BlurSizeProp, _blurSize);
        for (int i = 0; i < _blurIterations; i++)
        {
            Graphics.Blit(_fogMaskRT, _blurTempRT, _blurMat, 0); // H
            Graphics.Blit(_blurTempRT, _fogMaskRT, _blurMat, 1); // V
        }
    }

    private void CreateRenderTextures()
    {
        _fogMaskRT  = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        _blurTempRT = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
        _fogMaskRT.Create();
        _blurTempRT.Create();
        _blurMat = new Material(Shader.Find("Custom/Blur"));
    }

    private void CreateDepthOnlyMaterial()
    {
        _depthOnlyMat = new Material(Shader.Find("Custom/FogMaskDepthOnly"));
    }

    public void RefreshWallRenderers()
    {
        // 정렬은 필요 없으므로 SortMode.None — 렌더러가 많은 씬에서 InstanceID 정렬 비용을 없앤다.
        var allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        var wallRenderers = new System.Collections.Generic.List<Renderer>();

        foreach (var targetRenderer in allRenderers)
        {
            if (((1 << targetRenderer.gameObject.layer) & _wallLayer.value) == 0)
                continue;

            wallRenderers.Add(targetRenderer);
        }

        _wallRenderers = wallRenderers.ToArray();
    }

    private void DrawWallDepth()
    {
        if (_wallRenderers == null || _depthOnlyMat == null)
            return;

        foreach (var wallRenderer in _wallRenderers)
        {
            if (wallRenderer == null || !wallRenderer.enabled)
                continue;

            _cmd.DrawRenderer(wallRenderer, _depthOnlyMat);
        }
    }

    private void CreateFovMeshObject()
    {
        var go = new GameObject("FovMask");
        go.layer = LayerMask.NameToLayer("FogMask");
        _fovMeshTrans = go.transform;

        _fovMesh = new Mesh { name = "FovConeMesh" };
        go.AddComponent<MeshFilter>().mesh = _fovMesh;

        _fovMaskMat = new Material(Shader.Find("Custom/FovMask"));
        var mr = go.AddComponent<MeshRenderer>();
        mr.material          = _fovMaskMat;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows    = false;
    }

    private void CreateCircleMeshObject()
    {
        var go = new GameObject("CircleMask");
        go.layer = LayerMask.NameToLayer("FogMask");
        _circleMeshTrans = go.transform;

        _circleMesh = new Mesh { name = "CircleMesh" };
        go.AddComponent<MeshFilter>().mesh = _circleMesh;

        var mr = go.AddComponent<MeshRenderer>();
        mr.material          = _fovMaskMat; // 동일한 흰색 셰이더 재사용
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows    = false;
    }

    private void CreateDarkOverlayObject()
    {
        var go = new GameObject("DarkOverlay");
        _overlayTrans = go.transform;

        float s = _overlaySize * 0.5f;
        _overlayMesh = new Mesh { name = "DarkOverlayMesh" };
        _overlayMesh.vertices = new Vector3[]
        {
            new(-s, 0, -s), new(-s, 0, s),
            new( s, 0,  s), new( s, 0, -s)
        };
        _overlayMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        _overlayMesh.RecalculateNormals();

        go.AddComponent<MeshFilter>().mesh = _overlayMesh;

        var mr = go.AddComponent<MeshRenderer>();
        _darkOverlayMat = new Material(Shader.Find("Custom/DarkOverlay"));
        _darkOverlayMat.color = _darkColor;
        _darkOverlayMat.SetTexture(FogMaskProp, _fogMaskRT);
        mr.material          = _darkOverlayMat;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows    = false;
    }

    // 크로스헤어 화면 위치를 플레이어 높이의 지면 평면에 투영해 시야 기준 방향을 구한다.
    // PlayerRotation.RotateTowardMouse와 같은 방식 — 캐릭터 몸 회전과 무관하게 항상 최신 마우스 위치를 따른다.
    private Vector3 GetCrosshairForward(Vector3 origin)
    {
        if (CrosshairUI.Instance == null) return _visionForward;

        Ray ray = _mainCam.ScreenPointToRay(CrosshairUI.Instance.ScreenPosition);
        Plane plane = new Plane(Vector3.up, new Vector3(0f, origin.y, 0f));
        if (!plane.Raycast(ray, out float distance)) return _visionForward;

        Vector3 dir = ray.GetPoint(distance) - origin;
        dir.y = 0f;

        return dir.sqrMagnitude < 0.0001f ? _visionForward : dir.normalized;
    }

    private void UpdateFovMesh()
    {
        Vector3 origin   = _fovMeshTrans.position;
        float   half     = _visionConfig.fovAngle * 0.5f;
        int     segments = _visionConfig.arcSegments;
        if (segments < 1) return;

        // 세그먼트 수가 바뀔 때만 재할당 — 평소에는 기존 배열 내용만 갱신한다.
        if (_fovVertices == null || _fovVertices.Length != segments + 2)
        {
            _fovVertices  = new Vector3[segments + 2];
            _fovTriangles = new int[segments * 3];
            for (int i = 0; i < segments; i++)
            {
                _fovTriangles[i * 3]     = 0;
                _fovTriangles[i * 3 + 1] = i + 1;
                _fovTriangles[i * 3 + 2] = i + 2;
            }
            _fovMesh.Clear();
        }

        _fovVertices[0] = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float   angle = -half + _visionConfig.fovAngle / segments * i;
            Vector3 dir   = Quaternion.Euler(0, angle, 0) * _visionForward;

            float dist = Physics.Raycast(origin, dir, out RaycastHit hit, _visionConfig.detectRange, _wallLayer)
                ? hit.distance
                : _visionConfig.detectRange;

            _fovVertices[i + 1] = dir * dist;
        }

        _fovMesh.vertices  = _fovVertices;
        _fovMesh.triangles = _fovTriangles;
        _fovMesh.RecalculateNormals();
    }

    private void UpdateCircleMesh()
    {
        const int segments = 32;
        float     radius   = _visionConfig.circleRadius;
        Vector3   origin   = _circleMeshTrans.position;

        if (_circleVertices == null)
        {
            _circleVertices  = new Vector3[segments + 2];
            _circleTriangles = new int[segments * 3];
            for (int i = 0; i < segments; i++)
            {
                _circleTriangles[i * 3]     = 0;
                _circleTriangles[i * 3 + 1] = i + 1;
                _circleTriangles[i * 3 + 2] = i + 2;
            }
            _circleMesh.Clear();
        }

        _circleVertices[0] = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float   angle = 360f / segments * i;
            Vector3 dir   = Quaternion.Euler(0, angle, 0) * Vector3.forward;

            float dist = Physics.Raycast(origin, dir, out RaycastHit hit, radius, _wallLayer)
                ? hit.distance
                : radius;

            _circleVertices[i + 1] = dir * dist;
        }

        _circleMesh.vertices  = _circleVertices;
        _circleMesh.triangles = _circleTriangles;
        _circleMesh.RecalculateNormals();
    }
}
