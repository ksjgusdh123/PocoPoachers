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
    bool _originCached;
    [SerializeField] Image _backFrameImage;
    [SerializeField] Image _itemImage;

    Tween _revealTween;   // 진행 중인 뒤집기 페이드 (파괴 시 Kill 대상)
    SlotHoverHighlightUI _hoverHighlight;

    public bool isFlip { get; private set; }

    private void Awake() => CacheOrigin();

    // 용량 밖 슬롯의 카드는 비활성이라 Awake 없이 먼저 조회될 수 있으므로 처음 필요한 시점에 한 번만 잡는다.
    private void CacheOrigin()
    {
        if (_originCached) return;

        originFrameImage = _backFrameImage.sprite;
        originFrameColor = _backFrameImage.color;

        _backFaceSprite ??= Resources.Load<Sprite>(BackFaceSpritePath);
        _hoverHighlight = GetComponent<SlotHoverHighlightUI>();
        _originCached = true;
    }

    public void CheckSlotState(BoxItemSlot slot)
    {
        // 이 카드 UI는 여러 상자가 돌려쓴다. 이전 상자에서 남은 뒷면 상태를 먼저 지우고 판정한다.
        ResetCard();

        if (slot == null || _itemImage.sprite == null || slot.isOpen || slot.skipReveal) return;

        if (_backFaceSprite != null)
        {
            _backFrameImage.sprite = _backFaceSprite;
            _currentSlot = slot;
        }
        _itemImage.gameObject.SetActive(false);
        isFlip = true;
    }

    // 리빌 도중 상자를 닫으면 트윈이 Kill돼 프레임이 뒷면(또는 반투명)인 채로 남는다.
    // 다음에 열 때 그 칸이 빈 칸/이미 공개된 칸이면 Reveal이 돌지 않아 복구될 기회가 없으므로 여기서 되돌린다.
    public void ResetCard()
    {
        CacheOrigin();

        _revealTween?.Kill();
        _revealTween = null;
        _currentSlot = null;
        isFlip = false;

        if (_backFrameImage == null) return;
        _backFrameImage.sprite = originFrameImage;
        _backFrameImage.color = originFrameColor;
    }

    public void Reveal()
    {
        _revealTween?.Kill();
        _revealTween = _backFrameImage.DOFade(0f, 0.3f)
            .OnComplete(() =>
            {
                _revealTween = null;
                isFlip = false;

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
