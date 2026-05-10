using System.Collections.Generic;
using UnityEngine;

public class FogOfWarRenderer : MonoBehaviour
{
    [SerializeField] private VisionConfig _visionConfig;
    [SerializeField] private float _groundOffset = 0.05f;
    [SerializeField] private float _overlaySize = 500f;
    [SerializeField] private Color _darkColor = new Color(0f, 0f, 0f, 0.6f);
    [SerializeField] private LayerMask _wallLayer;

    private Transform _fovMeshTrans;
    private Mesh _fovMesh;
    private Transform _overlayTrans;

    private Material _wallMaskMat;
    private readonly List<(MeshRenderer renderer, Material[] origMats)> _wallRenderers = new();

    private void Awake()
    {
        _wallLayer = 1 << LayerMask.NameToLayer("Wall");
        CreateFovMeshObject();
        CreateDarkOverlayObject();
        InitWallStencils();
    }

    private void OnDestroy()
    {
        if (_fovMeshTrans != null) Destroy(_fovMeshTrans.gameObject);
        if (_overlayTrans != null) Destroy(_overlayTrans.gameObject);
        CleanupWallStencils();
    }

    private void Update()
    {
        Vector3 groundPos = new Vector3(transform.position.x, transform.position.y + _groundOffset, transform.position.z);

        _fovMeshTrans.position = groundPos;
        _fovMeshTrans.rotation = Quaternion.identity;
        _overlayTrans.position = groundPos;

        UpdateFovMesh();
    }

    // 씬의 모든 벽 오브젝트에 WallMask 머티리얼을 두 번째 슬롯으로 추가
    private void InitWallStencils()
    {
        _wallMaskMat = new Material(Shader.Find("Custom/WallMask"));

        foreach (var mr in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (((1 << mr.gameObject.layer) & _wallLayer.value) == 0) continue;

            var origMats = mr.sharedMaterials;
            _wallRenderers.Add((mr, origMats));

            var newMats = new Material[origMats.Length + 1];
            origMats.CopyTo(newMats, 0);
            newMats[origMats.Length] = _wallMaskMat;
            mr.sharedMaterials = newMats;
        }
    }

    // 원래 머티리얼 복원
    private void CleanupWallStencils()
    {
        foreach (var (mr, origMats) in _wallRenderers)
        {
            if (mr != null)
                mr.sharedMaterials = origMats;
        }
        _wallRenderers.Clear();
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
        overlayMesh.vertices = new Vector3[] {
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
        float half = _visionConfig.fovAngle * 0.5f;
        int segments = _visionConfig.arcSegments;

        Vector3[] vertices = new Vector3[segments + 2];
        vertices[0] = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float angle = -half + _visionConfig.fovAngle / segments * i;
            Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;

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
        _fovMesh.vertices = vertices;
        _fovMesh.triangles = triangles;
        _fovMesh.RecalculateNormals();
    }
}
