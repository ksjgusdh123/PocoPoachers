using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ItemRevealCard : MonoBehaviour
{
    const string BackFaceSpritePath = "Keyboard_F";

    private static Sprite _backFaceSprite;

    BoxItemSlot _currentSlot;
    Sprite originFrameImage;
    Color originFrameColor;
    [SerializeField] Image _backFrameImage;
    [SerializeField] Image _itemImage;

    public bool isFlip { get; private set; } = true;

    private void Awake()
    {
        originFrameImage = _backFrameImage.sprite;
        originFrameColor = _backFrameImage.color;

        _backFaceSprite ??= Resources.Load<Sprite>(BackFaceSpritePath);
    }

    public void CheckSlotState(BoxItemSlot slot)
    {
        if (_itemImage.sprite == null || slot.isOpen || slot.skipReveal)
        {
            isFlip = false;
            return;
        }

        if (_backFaceSprite != null)
        {
            _backFrameImage.sprite = _backFaceSprite;
            _currentSlot = slot;
        }
        _itemImage.gameObject.SetActive(false);
        isFlip = true;
    }

    public void Reveal()
    {
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
