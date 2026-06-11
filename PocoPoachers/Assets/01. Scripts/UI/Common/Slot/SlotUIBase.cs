using UnityEngine;
using UnityEngine.UI;

public abstract class SlotUIBase : MonoBehaviour
{
    [SerializeField] protected Image _icon;

    protected void SetIcon(ItemData data)
    {
        bool hasItem = data != null;
        _icon.sprite = hasItem ? ResourceManager.Instance.LoadSprite(data.icon) : null;
        _icon.gameObject.SetActive(hasItem);
    }
}
