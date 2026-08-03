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

    Tween _revealTween;   // 진행 중인 뒤집기 페이드 (파괴 시 Kill 대상)
    SlotHoverHighlightUI _hoverHighlight;

    public bool isFlip { get; private set; } = true;

    private void Awake()
    {
        originFrameImage = _backFrameImage.sprite;
        originFrameColor = _backFrameImage.color;

        _backFaceSprite ??= Resources.Load<Sprite>(BackFaceSpritePath);
        _hoverHighlight = GetComponent<SlotHoverHighlightUI>();
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
        _revealTween?.Kill();
        _revealTween = _backFrameImage.DOFade(0f, 0.3f)
            .OnComplete(() =>
            {
                _revealTween = null;

                // 0.3초 사이에 카드가 파괴/비활성화될 수 있으므로 콜백에서 다시 확인한다.
                if (_backFrameImage == null || _itemImage == null) return;

                _backFrameImage.sprite = originFrameImage;
                _backFrameImage.color = originFrameColor;
                _itemImage.gameObject.SetActive(true);
                if (_currentSlot != null) _currentSlot.isOpen = true;
                _hoverHighlight?.NotifyRevealed();
            });
    }

    // 트윈이 파괴된 Image를 계속 건드리면 NRE가 나므로 파괴/비활성화 시 정리한다.
    private void OnDisable()
    {
        _revealTween?.Kill();
        _revealTween = null;
    }

    private void OnDestroy()
    {
        _revealTween?.Kill();
        _revealTween = null;
    }
}
