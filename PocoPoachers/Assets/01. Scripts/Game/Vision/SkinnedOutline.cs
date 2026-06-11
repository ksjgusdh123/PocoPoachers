using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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
    readonly List<GameObject> _extraObjects = new();

    static readonly int ColorProp     = Shader.PropertyToID("_OutlineColor");
    static readonly int WidthProp     = Shader.PropertyToID("_OutlineWidth");
    static readonly int SpeedProp     = Shader.PropertyToID("_WaveSpeed");
    static readonly int FrequencyProp = Shader.PropertyToID("_WaveFrequency");
    static readonly int AmplitudeProp = Shader.PropertyToID("_WaveAmplitude");

    void Awake()
    {
        _smrs       = GetComponentsInChildren<SkinnedMeshRenderer>();
        _maskMat    = new Material(Shader.Find("Custom/SkinnedOccludedOutlineMask"));
        _outlineMat = new Material(Shader.Find("Custom/SkinnedOccludedOutline"));
    }

    void OnEnable()
    {
        SyncProperties();
        foreach (var smr in _smrs)
        {
            _extraObjects.Add(CreateExtraSMR(smr, "OutlineMask",     _maskMat));
            _extraObjects.Add(CreateExtraSMR(smr, "OutlineRenderer", _outlineMat));
        }
    }

    void OnDisable()
    {
        foreach (var go in _extraObjects)
            if (go != null) Destroy(go);
        _extraObjects.Clear();
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
        _outlineMat.SetColor(ColorProp,     _color);
        _outlineMat.SetFloat(WidthProp,     _width);
        _outlineMat.SetFloat(SpeedProp,     _waveSpeed);
        _outlineMat.SetFloat(FrequencyProp, _waveFrequency);
        _outlineMat.SetFloat(AmplitudeProp, _waveAmplitude);
    }

    GameObject CreateExtraSMR(SkinnedMeshRenderer src, string goName, Material mat)
    {
        var go  = new GameObject(goName);
        go.transform.SetParent(src.transform.parent, false);

        var dst               = go.AddComponent<SkinnedMeshRenderer>();
        dst.sharedMesh        = src.sharedMesh;
        dst.bones             = src.bones;
        dst.rootBone          = src.rootBone;
        dst.material          = mat;
        dst.shadowCastingMode = ShadowCastingMode.Off;
        dst.receiveShadows    = false;

        return go;
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
