using System.Linq;
using UnityEngine;

// 부수적인 플레이어 관리 클래스
public class PlayerController : MonoBehaviour
{
    [SerializeField] private GameObject PlayerBagUI;
    private DropQuickSlotUI[] _quickSlots;

    private void Start()
    {
        _quickSlots = FindObjectsByType<DropQuickSlotUI>(FindObjectsInactive.Include)
            .OrderBy(s => s.gameObject.name).ToArray();

        var inputHandler = GetComponent<PlayerInputHandler>();
        inputHandler.GoInventory += () => { PlayerBagUI.SetActive(!PlayerBagUI.activeSelf); };
        inputHandler.ItemNumberKey += RegisterItem;
    }

    void RegisterItem(int index)
    {
        _quickSlots[index].TryRegisterItem();
    }
}
