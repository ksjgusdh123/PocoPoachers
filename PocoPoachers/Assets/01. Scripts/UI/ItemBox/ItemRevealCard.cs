using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ItemRevealCard : MonoBehaviour
{
    const string BackFaceSpritePath = "Keyboard_F";

    BoxItemSlot _currentSlot;
    Sprite originFrameImage;
    Color originFrameColor;
    [SerializeField] Image _backFrameImage;
    [SerializeField] Image _itemImage;

    public bool isFlip { get; private set; } = true;

    public void CheckSlotState(BoxItemSlot slot)
    {
        var sprite = Resources.Load<Sprite>(BackFaceSpritePath);
        if (_itemImage.sprite == null || slot.isOpen)
        {
            isFlip = false;
            return;
        }

        if (sprite != null)
        {
            originFrameImage = _backFrameImage.sprite;
            originFrameColor = _backFrameImage.color;
            _backFrameImage.sprite = sprite;
            _currentSlot = slot;
        }
        _itemImage.gameObject.SetActive(false);
        isFlip = true;
    }

    public void Reveal()
    {
        Debug.Log("Reveal");
        _backFrameImage.DOFade(0f, 0.3f)
            .OnComplete(() => 
            {
                _backFrameImage.sprite = originFrameImage;
                _backFrameImage.color = originFrameColor;
                _itemImage.gameObject.SetActive(true);
                _currentSlot.isOpen = true;
            });
    }
}
