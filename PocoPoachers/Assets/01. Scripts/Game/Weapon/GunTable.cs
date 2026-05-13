using System.Collections.Generic;
using UnityEngine;

public class GunTable : MonoBehaviour
{
    public static GunTable Instance { get; private set; }

    [SerializeField] private GunBase[] _gunPrefabs;

    private readonly Dictionary<int, GunBase> _prefabDic = new();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (var prefab in _gunPrefabs)
            _prefabDic[prefab.GunData.itemId] = prefab;
    }

    // 프리팹을 마운트 포인트에 스폰하고 GunBase 반환
    public GunBase Equip(int itemId, Transform mountPoint)
    {
        if (!_prefabDic.TryGetValue(itemId, out GunBase prefab))
        {
            Debug.LogWarning($"[GunTable] itemId {itemId}에 해당하는 총이 없습니다.");
            return null;
        }

        GunBase instance = Instantiate(prefab, mountPoint);
        instance.transform.localPosition = Vector3.zero;
        return instance;
    }
}
