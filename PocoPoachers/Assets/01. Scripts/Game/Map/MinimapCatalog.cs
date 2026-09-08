using UnityEngine;

// 씬 이름으로 그 씬의 MinimapCaptureData를 찾는 목록.
// Resources 폴더에 MinimapCatalog 이름으로 하나 두면 인스펙터 연결 없이 자동 로드된다(UITheme와 같은 방식).
// 목록은 MinimapCaptureWindow가 촬영할 때마다 자동으로 채운다.
[CreateAssetMenu(fileName = "MinimapCatalog", menuName = "Minimap/Minimap Catalog")]
public class MinimapCatalog : ScriptableObject
{
    public const string ResourcePath = "MinimapCatalog";

    [SerializeField] private MinimapCaptureData[] _entries;

    private static MinimapCatalog _default;

    public static MinimapCatalog Default
    {
        get
        {
            if (_default == null) _default = Resources.Load<MinimapCatalog>(ResourcePath);
            return _default;
        }
    }

    public static MinimapCaptureData For(string sceneName)
    {
        MinimapCatalog catalog = Default;
        return catalog != null ? catalog.Find(sceneName) : null;
    }

    public MinimapCaptureData Find(string sceneName)
    {
        if (_entries == null || string.IsNullOrEmpty(sceneName)) return null;

        foreach (MinimapCaptureData entry in _entries)
        {
            if (entry != null && entry.SceneName == sceneName) return entry;
        }
        return null;
    }

#if UNITY_EDITOR
    // 이미 있으면 아무것도 안 하고, 없으면 끝에 추가한다. 호출 측에서 SetDirty/SaveAssets를 처리한다.
    public bool Register(MinimapCaptureData data)
    {
        if (data == null) return false;

        _entries ??= new MinimapCaptureData[0];
        foreach (MinimapCaptureData entry in _entries)
        {
            if (entry == data) return false;
        }

        var grown = new MinimapCaptureData[_entries.Length + 1];
        _entries.CopyTo(grown, 0);
        grown[_entries.Length] = data;
        _entries = grown;
        return true;
    }
#endif
}
