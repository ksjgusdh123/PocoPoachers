using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 타이틀 진입 시 브랜드와 메뉴를 짧고 절제된 모션으로 노출한다.
// Time.timeScale과 무관하게 동작하며 완료 후 첫 메뉴에 키보드/게임패드 포커스를 준다.
[DisallowMultipleComponent]
public sealed class TitleScreenPresentationUI : MonoBehaviour
{
    [Header("Reveal Targets")]
    [SerializeField] private CanvasGroup _brandGroup;
    [SerializeField] private RectTransform _brandBlock;
    [SerializeField] private CanvasGroup _menuGroup;
    [SerializeField] private RectTransform _menuPanel;
    [SerializeField] private Selectable _defaultSelection;

    [Header("Timing")]
    [SerializeField, Min(0.1f)] private float _brandDuration = 0.55f;
    [SerializeField, Min(0f)] private float _menuDelay = 0.16f;
    [SerializeField, Min(0.1f)] private float _menuDuration = 0.42f;
    [SerializeField, Min(0f)] private float _travelDistance = 24f;

    private Coroutine _revealRoutine;
    private Vector2 _brandTargetPosition;
    private Vector2 _menuTargetPosition;

    private void OnEnable()
    {
        if (_brandGroup == null || _brandBlock == null || _menuGroup == null || _menuPanel == null)
            return;

        _brandTargetPosition = _brandBlock.anchoredPosition;
        _menuTargetPosition = _menuPanel.anchoredPosition;

        PrepareForReveal();
        _revealRoutine = StartCoroutine(RevealRoutine());
    }

    private void OnDisable()
    {
        if (_revealRoutine != null)
        {
            StopCoroutine(_revealRoutine);
            _revealRoutine = null;
        }

        RestoreFinalState();
    }

    private void PrepareForReveal()
    {
        _brandGroup.alpha = 0f;
        _brandGroup.interactable = false;
        _brandGroup.blocksRaycasts = false;
        _brandBlock.anchoredPosition = _brandTargetPosition + Vector2.up * (_travelDistance * 0.5f);

        _menuGroup.alpha = 0f;
        // 연출이 도메인 리로드나 비활성화로 중단돼도 메뉴 입력은 잠그지 않는다.
        _menuGroup.interactable = true;
        _menuGroup.blocksRaycasts = true;
        _menuPanel.anchoredPosition = _menuTargetPosition + Vector2.down * _travelDistance;
    }

    private IEnumerator RevealRoutine()
    {
        float elapsed = 0f;
        Vector2 brandStart = _brandBlock.anchoredPosition;
        Vector2 menuStart = _menuPanel.anchoredPosition;

        while (true)
        {
            elapsed += Time.unscaledDeltaTime;

            float brandProgress = Mathf.Clamp01(elapsed / _brandDuration);
            float menuProgress = Mathf.Clamp01((elapsed - _menuDelay) / _menuDuration);
            float easedBrand = EaseOutCubic(brandProgress);
            float easedMenu = EaseOutCubic(menuProgress);

            _brandGroup.alpha = easedBrand;
            _brandBlock.anchoredPosition = Vector2.LerpUnclamped(brandStart, _brandTargetPosition, easedBrand);

            _menuGroup.alpha = easedMenu;
            _menuPanel.anchoredPosition = Vector2.LerpUnclamped(menuStart, _menuTargetPosition, easedMenu);

            if (brandProgress >= 1f && menuProgress >= 1f) break;
            yield return null;
        }

        RestoreFinalState();
        SelectDefaultMenuItem();
        _revealRoutine = null;
    }

    private void RestoreFinalState()
    {
        if (_brandGroup != null)
        {
            _brandGroup.alpha = 1f;
            _brandGroup.interactable = true;
            _brandGroup.blocksRaycasts = true;
        }

        if (_menuGroup != null)
        {
            _menuGroup.alpha = 1f;
            _menuGroup.interactable = true;
            _menuGroup.blocksRaycasts = true;
        }

        if (_brandBlock != null) _brandBlock.anchoredPosition = _brandTargetPosition;
        if (_menuPanel != null) _menuPanel.anchoredPosition = _menuTargetPosition;
    }

    private void SelectDefaultMenuItem()
    {
        if (_defaultSelection == null || !_defaultSelection.IsActive() || !_defaultSelection.IsInteractable())
            return;

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null)
            eventSystem.SetSelectedGameObject(_defaultSelection.gameObject);
    }

    private static float EaseOutCubic(float value)
    {
        float inverse = 1f - value;
        return 1f - inverse * inverse * inverse;
    }
}
