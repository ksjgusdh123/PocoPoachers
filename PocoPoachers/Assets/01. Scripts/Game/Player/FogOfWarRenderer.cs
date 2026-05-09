using UnityEngine;

public class FogOfWarRenderer : MonoBehaviour
{
    [SerializeField] private float _detectRange = 15f;
    [SerializeField] private float _fovAngle = 90f;
    [SerializeField] private float _groundOffset = 0.05f;
    [SerializeField] private int _arcSegments = 30;
    [SerializeField] private float _overlaySize = 500f;
    [SerializeField] private Color _darkColor = new Color(0f, 0f, 0f, 0.6f);
    [SerializeField] private LayerMask _wallLayer;

    private Transform _fovMeshTrans;
    private Mesh _fovMesh;
    private Transform _overlayTrans;

    private void Awake()
    {
        _wallLayer = 1 << LayerMask.NameToLayer("Wall");
        CreateFovMeshObject();
        CreateDarkOverlayObject();
    }

    private void OnDestroy()
    {
        if (_fovMeshTrans != null) Destroy(_fovMeshTrans.gameObject);
        if (_overlayTrans != null) Destroy(_overlayTrans.gameObject);
    }

    private void Update()
    {
        Vector3 groundPos = new Vector3(transform.position.x, transform.position.y + _groundOffset, transform.position.z);

        _fovMeshTrans.position = groundPos;
        _fovMeshTrans.rotation = Quaternion.identity;

        _overlayTrans.position = groundPos;

        UpdateFovMesh();
    }

    private void CreateFovMeshObject()
    {
        GameObject go = new GameObject("FovMask");
        _fovMeshTrans = go.transform;

        _fovMesh = new Mesh { name = "FovConeMesh" };

        go.AddComponent<MeshFilter>().mesh = _fovMesh;

        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.material = new Material(Shader.Find("Custom/FovMask"));
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    private void CreateDarkOverlayObject()
    {
        GameObject go = new GameObject("DarkOverlay");
        _overlayTrans = go.transform;

        float s = _overlaySize * 0.5f;
        Mesh overlayMesh = new Mesh { name = "DarkOverlayMesh" };
        overlayMesh.vertices = new Vector3[]
        {
            new(-s, 0, -s), new(-s, 0, s),
            new( s, 0,  s), new( s, 0, -s)
        };
        overlayMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        overlayMesh.RecalculateNormals();

        go.AddComponent<MeshFilter>().mesh = overlayMesh;

        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        Material mat = new Material(Shader.Find("Custom/DarkOverlay"));
        mat.color = _darkColor;
        mr.material = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    private void UpdateFovMesh()
    {
        Vector3 origin = _fovMeshTrans.position;
        float half = _fovAngle * 0.5f;

        // 버텍스: 중심(0) + 호 점들(1 ~ arcSegments+1)
        // _fovMeshTrans.rotation = identity 이므로 로컬 = 월드 오프셋
        Vector3[] vertices = new Vector3[_arcSegments + 2];
        vertices[0] = Vector3.zero;

        for (int i = 0; i <= _arcSegments; i++)
        {
            float angle = -half + _fovAngle / _arcSegments * i;
            Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;

            float dist = Physics.Raycast(origin, dir, out RaycastHit hit, _detectRange, _wallLayer)
                ? hit.distance
                : _detectRange;

            vertices[i + 1] = dir * dist;
        }

        // 삼각형: 중심에서 부채꼴 팬
        int[] triangles = new int[_arcSegments * 3];
        for (int i = 0; i < _arcSegments; i++)
        {
            triangles[i * 3]     = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        _fovMesh.Clear();
        _fovMesh.vertices = vertices;
        _fovMesh.triangles = triangles;
        _fovMesh.RecalculateNormals();
    }
}
