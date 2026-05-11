using UnityEngine;

// 캐릭터 모델 루트(mini simple demo_01)에 붙이면
// 자식의 모든 SkinnedMeshRenderer에 Mask + Outline 머티리얼을 추가
public class SkinnedOutline : MonoBehaviour
{
    [SerializeField] Color _color = Color.yellow;
    [SerializeField, Range(0f, 0.1f)] float _width = 0.02f;

    SkinnedMeshRenderer[] _smrs;
    Material _maskMat;
    Material _outlineMat;
    Material[][] _originalMats;

    static readonly int ColorProp = Shader.PropertyToID("_OutlineColor");
    static readonly int WidthProp  = Shader.PropertyToID("_OutlineWidth");

    void Awake()
    {
        _smrs         = GetComponentsInChildren<SkinnedMeshRenderer>();
        _maskMat      = new Material(Shader.Find("Custom/SkinnedOccludedOutlineMask"));
        _outlineMat   = new Material(Shader.Find("Custom/SkinnedOccludedOutline"));
        _originalMats = new Material[_smrs.Length][];

        for (int i = 0; i < _smrs.Length; i++)
            _originalMats[i] = _smrs[i].materials;
    }

    void OnEnable()
    {
        SyncProperties();
        for (int i = 0; i < _smrs.Length; i++)
        {
            var mats = new Material[_originalMats[i].Length + 2];
            _originalMats[i].CopyTo(mats, 0);
            mats[^2] = _maskMat;    // Queue=Transparent    (Mask 먼저)
            mats[^1] = _outlineMat; // Queue=Transparent+1  (Outline 나중)
            _smrs[i].materials = mats;
        }
    }

    void OnDisable()
    {
        for (int i = 0; i < _smrs.Length; i++)
            _smrs[i].materials = _originalMats[i];
    }

    void OnValidate()
    {
        if (_outlineMat == null) return;
        SyncProperties();
    }

    void OnDestroy()
    {
        if (_maskMat    != null) Destroy(_maskMat);
        if (_outlineMat != null) Destroy(_outlineMat);
    }

    void SyncProperties()
    {
        _outlineMat.SetColor(ColorProp, _color);
        _outlineMat.SetFloat(WidthProp,  _width);
    }

    public void SetOutline(Color color, float width)
    {
        _color = color;
        _width = width;
        SyncProperties();
    }
}
