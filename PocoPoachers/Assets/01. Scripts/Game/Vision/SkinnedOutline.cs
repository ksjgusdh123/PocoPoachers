using UnityEngine;

// 캐릭터 모델 루트(mini simple demo_01)에 붙이면
// 자식의 모든 SkinnedMeshRenderer에 Mask + Outline 머티리얼을 추가
public class SkinnedOutline : MonoBehaviour
{
    [Header("Outline")]
    [SerializeField, ColorUsage(true, true)] Color _color = Color.yellow;
    [SerializeField, Range(0f, 0.1f)]  float _width     = 0.02f;

    [Header("Wave")]
    [SerializeField, Range(0f, 10f)]   float _waveSpeed     = 3f;
    [SerializeField, Range(0f, 20f)]   float _waveFrequency = 5f;
    [SerializeField, Range(0f, 0.05f)] float _waveAmplitude = 0.05f;

    SkinnedMeshRenderer[] _smrs;
    Material _maskMat;
    Material _outlineMat;
    Material[][] _originalMats;

    static readonly int ColorProp     = Shader.PropertyToID("_OutlineColor");
    static readonly int WidthProp     = Shader.PropertyToID("_OutlineWidth");
    static readonly int SpeedProp     = Shader.PropertyToID("_WaveSpeed");
    static readonly int FrequencyProp = Shader.PropertyToID("_WaveFrequency");
    static readonly int AmplitudeProp = Shader.PropertyToID("_WaveAmplitude");

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
            mats[^2] = _maskMat;
            mats[^1] = _outlineMat;
            _smrs[i].materials = mats;
        }
    }

    void OnDisable()
    {
        for (int i = 0; i < _smrs.Length; i++)
            _smrs[i].materials = _originalMats[i];
    }

    // Inspector에서 값 변경 시 Play 모드에서도 즉시 반영
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
        _outlineMat.SetColor(ColorProp,     _color);
        _outlineMat.SetFloat(WidthProp,     _width);
        _outlineMat.SetFloat(SpeedProp,     _waveSpeed);
        _outlineMat.SetFloat(FrequencyProp, _waveFrequency);
        _outlineMat.SetFloat(AmplitudeProp, _waveAmplitude);
    }

    public void SetOutline(Color color, float width)
    {
        _color = color;
        _width = width;
        SyncProperties();
    }

    public void SetWave(float speed, float frequency, float amplitude)
    {
        _waveSpeed     = speed;
        _waveFrequency = frequency;
        _waveAmplitude = amplitude;
        SyncProperties();
    }
}
