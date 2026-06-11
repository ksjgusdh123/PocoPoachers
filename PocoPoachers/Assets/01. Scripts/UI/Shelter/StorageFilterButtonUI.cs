using UnityEngine;
using UnityEngine.UI;

// 각 필터 버튼에 붙이는 스크립트
public class StorageFilterButtonUI : MonoBehaviour
{
    [SerializeField] private ItemType _filterType;
    [SerializeField] private StorageUI _storageUI;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        _storageUI.SetFilter((int)_filterType);
    }
}
