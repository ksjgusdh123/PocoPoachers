using System.Collections.Generic;
using UnityEngine;

public class DamageTextPool : MonoBehaviour
{
    public static DamageTextPool Instance { get; private set; }

    [SerializeField] private DamageTextUI _prefab;
    [SerializeField] private int _initialSize = 10;
    [SerializeField] private Vector3 _spawnOffset = new Vector3(0f, 1.5f, 0f);

    private readonly Queue<DamageTextUI> _pool = new();

    private void Awake()
    {
        Instance = this;
        for (int i = 0; i < _initialSize; i++)
            Return(CreateNew());
    }

    public DamageTextUI Get()
    {
        var text = _pool.Count > 0 ? _pool.Dequeue() : CreateNew();
        text.gameObject.SetActive(true);
        return text;
    }

    public void Return(DamageTextUI text)
    {
        text.gameObject.SetActive(false);
        _pool.Enqueue(text);
    }

    private DamageTextUI CreateNew()
    {
        var obj = Instantiate(_prefab, transform);
        obj.gameObject.SetActive(false);
        return obj;
    }

    public static void Show(float damage, Vector3 worldPosition)
    {
        Instance.Get().Play(damage, worldPosition + Instance._spawnOffset);
    }
}
