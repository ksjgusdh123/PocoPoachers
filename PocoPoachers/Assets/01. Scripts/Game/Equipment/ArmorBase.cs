using UnityEngine;

public class ArmorBase : MonoBehaviour
{
    [SerializeField] private int _itemId;

    public int ItemId => _itemId;
    public ArmorStatData Stat => DataManager.GetArmorStat(_itemId);

    public void SetItemId(int itemId) => _itemId = itemId;
}
