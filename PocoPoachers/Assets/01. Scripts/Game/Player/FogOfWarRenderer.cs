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
    private Material _darkOverlayMat;

    private Transform _fovMeshTrans;
    private Mesh _fovMesh;
    private Transform _overlayTrans;
    private CommandBuffer _cmd;

    private static readonly int FogMaskProp  = Shader.PropertyToID("_FogMask");
    private static readonly int BlurSizeProp = Shader.PropertyToID("_BlurSize");

    private void Awake()
    {
        _wallLayer = 1 << LayerMask.NameToLayer("Wall");
        _mainCam   = Camera.main;

        CreateRenderTextures();
        CreateFovMeshObject();
        CreateDarkOverlayObject();

        _mainCam.cullingMask &= ~LayerMask.GetMask("FogMask");

        _cmd = new CommandBuffer { name = "FogMaskRender" };
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    private void OnDestroy()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;

        if (_fovMeshTrans != null) Destroy(_fovMeshTrans.gameObject);
        if (_overlayTrans != null) Destroy(_overlayTrans.gameObject);

        if (_fogMaskRT  != null) { _fogMaskRT.Release();  Destroy(_fogMaskRT); }
        if (_blurTempRT != null) { _blurTempRT.Release(); Destroy(_blurTempRT); }

        _cmd?.Release();
    }

    private void Update()
    {
        Vector3 groundPos = new Vector3(
            transform.position.x,
            transform.position.y + _groundOffset,
            transform.position.z);

        _fovMeshTrans.position = groundPos;
        _fovMeshTrans.rotation = Quaternion.identity;
        _overlayTrans.position = groundPos;

        UpdateFovMesh();
    }

    // 메인 카메라 렌더 직전에 호출 → 정확한 VP 행렬 사용 가능
    private void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam)
    {
        if (cam != _mainCam) return;

        // FovMesh → _fogMaskRT
        _cmd.Clear();
        _cmd.SetRenderTarget(_fogMaskRT);
        _cmd.ClearRenderTarget(false, true, Color.black);
        _cmd.SetViewProjectionMatrices(
            cam.worldToCameraMatrix,
            cam.projectionMatrix);
        _cmd.DrawMesh(_fovMesh, _fovMeshTrans.localToWorldMatrix, _fovMaskMat);
        Graphics.ExecuteCommandBuffer(_cmd);

        // Gaussian blur
        _blurMat.SetFloat(BlurSizeProp, _blurSize);
        for (int i = 0; i < _blurIterations; i++)
        {
            Graphics.Blit(_fogMaskRT, _blurTempRT, _blurMat, 0); // H
            Graphics.Blit(_blurTempRT, _fogMaskRT, _blurMat, 1); // V
        }
    }

    private void CreateRenderTextures()
    {
        _fogMaskRT  = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
        _blurTempRT = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
        _fogMaskRT.Create();
        _blurTempRT.Create();
        _blurMat = new Material(Shader.Find("Custom/Blur"));
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

    private void CreateDarkOverlayObject()
    {
        var go = new GameObject("DarkOverlay");
        _overlayTrans = go.transform;

        float s = _overlaySize * 0.5f;
        var overlayMesh = new Mesh { name = "DarkOverlayMesh" };
        overlayMesh.vertices = new Vector3[]
        {
            new(-s, 0, -s), new(-s, 0, s),
            new( s, 0,  s), new( s, 0, -s)
        };
        overlayMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        overlayMesh.RecalculateNormals();

        go.AddComponent<MeshFilter>().mesh = overlayMesh;

        var mr = go.AddComponent<MeshRenderer>();
        _darkOverlayMat = new Material(Shader.Find("Custom/DarkOverlay"));
        _darkOverlayMat.color = _darkColor;
        _darkOverlayMat.SetTexture(FogMaskProp, _fogMaskRT);
        mr.material          = _darkOverlayMat;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows    = false;
    }

    private void UpdateFovMesh()
    {
        Vector3 origin   = _fovMeshTrans.position;
        float   half     = _visionConfig.fovAngle * 0.5f;
        int     segments = _visionConfig.arcSegments;

        Vector3[] vertices = new Vector3[segments + 2];
        vertices[0] = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float   angle = -half + _visionConfig.fovAngle / segments * i;
            Vector3 dir   = Quaternion.Euler(0, angle, 0) * transform.forward;

            float dist = Physics.Raycast(origin, dir, out RaycastHit hit, _visionConfig.detectRange, _wallLayer)
                ? hit.distance
                : _visionConfig.detectRange;

            vertices[i + 1] = dir * dist;
        }

        int[] triangles = new int[segments * 3];
        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3]     = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        _fovMesh.Clear();
        _fovMesh.vertices  = vertices;
        _fovMesh.triangles = triangles;
        _fovMesh.RecalculateNormals();
    }
}
