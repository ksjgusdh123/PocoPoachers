using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ItemRevealCard : MonoBehaviour
{
    const string BackFaceSpritePath = "Keyboard_F";

    Sprite originFrameImage;
    Color originFrameColor;
    [SerializeField] Image _backFrameImage;
    [SerializeField] Image _itemImage;

    public bool isFlip { get; private set; } = true;

    public void CheckSlotState()
    {
        var sprite = Resources.Load<Sprite>(BackFaceSpritePath);
        if (_itemImage.sprite == null)
        {
            isFlip = false;
            return;
        }

        if (sprite != null)
        {
            originFrameImage = _backFrameImage.sprite;
            originFrameColor = _backFrameImage.color;
            _backFrameImage.sprite = sprite;
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
            });
    }
}
