using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 레이드 종료 결과 오버레이 — 탈출 성공 / 임무 실패(팀 전멸) 공용.
// ShowSuccess/ShowFailure로 활성화되어 페이드 인 → 확정 버튼 클릭 → 페이드 아웃 후,
// 호출측이 넘긴 onConfirm(씬 전환 등)을 실행한다. 무엇을 할지는 UI가 정하지 않고 호출측이 정한다.
//
// 버튼 노출 규칙 (버튼은 성공/실패 공용 1개):
//  - 성공(탈출): 상호작용한 로컬 플레이어가 확정 → 버튼을 모두에게 노출.
//  - 실패(전멸): 복귀는 호스트만 트리거 → 버튼을 호스트에게만 노출(게스트는 호스트를 따라 이동).
//
// 이 오브젝트는 평소 비활성으로 둬도 된다 — Show 시 스스로 켠다.
[RequireComponent(typeof(CanvasGroup))]
public class RaidResultUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup _group;         // 페이드 대상 (미지정 시 이 오브젝트의 CanvasGroup)
    [SerializeField] private GameObject _successPanel;   // 탈출 성공 패널
    [SerializeField] private GameObject _failurePanel;   // 임무 실패 패널
    [SerializeField] private Button _confirmButton;      // 확정 버튼 (성공/실패 공용)
    [SerializeField] private TextMeshProUGUI _timeText;  // 진행 시간 표시
    [SerializeField] private TextMeshProUGUI _killText;  // 처치 수 표시
    [SerializeField] private float _fadeDuration = 0.4f;

    private Action _onConfirm;
    private bool _isShowing;

    private void Awake()
    {
        if (_group == null) _group = GetComponent<CanvasGroup>();
        if (_confirmButton != null) _confirmButton.onClick.AddListener(OnConfirmClicked);

        if (!_isShowing)
            gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_confirmButton != null) _confirmButton.onClick.RemoveListener(OnConfirmClicked);

        // 진행 중인 페이드 트윈이 파괴된 CanvasGroup을 계속 건드리면 NRE가 난다.
        if (_group != null) _group.DOKill();
    }

    // 탈출 성공 표시. onConfirm: 확정 시 실행할 동작(예: 셸터로 이동)
    public void ShowSuccess(Action onConfirm) => Show(success: true, onConfirm, buttonVisible: true);

    // 임무 실패 표시. onConfirm: 확정 시 실행할 동작. 버튼은 호스트에게만.
    public void ShowFailure(Action onConfirm) => Show(success: false, onConfirm, buttonVisible: RoomManager.IsHost);

    private void Show(bool success, Action onConfirm, bool buttonVisible)
    {
        _onConfirm = onConfirm;
        _isShowing = true;
        gameObject.SetActive(true);

        if (_successPanel != null) _successPanel.SetActive(success);
        if (_failurePanel != null) _failurePanel.SetActive(!success);

        if (_confirmButton != null)
        {
            _confirmButton.gameObject.SetActive(buttonVisible);
            _confirmButton.interactable = true;
        }

        // 진행 통계 표시 — 집계를 멈추고 최종 값을 반영
        var stats = RaidStats.Instance;
        stats.StopRaid();
        if (_timeText != null) _timeText.text = FormatTime(stats.ElapsedTime);
        if (_killText != null) _killText.text = stats.Kills.ToString();

        _group.interactable = true;
        _group.blocksRaycasts = true;
        _group.DOKill();                   // 이전 페이드가 남아 있으면 알파가 튀므로 먼저 정리
        _group.alpha = 0f;
        _group.DOFade(1f, _fadeDuration); // 페이드 인
    }

    private void OnConfirmClicked()
    {
        if (!_isShowing) return;
        _isShowing = false;
        if (_confirmButton != null) _confirmButton.interactable = false;
        StartCoroutine(FadeOutThenConfirm());
    }

    private IEnumerator FadeOutThenConfirm()
    {
        _group.DOKill();                   // 진행 중인 페이드 인과 겹치지 않게 정리
        _group.DOFade(0f, _fadeDuration); // 페이드 아웃
        yield return new WaitForSeconds(_fadeDuration);

        _group.DOKill();                   // 비활성화 이후 콜백이 남지 않도록 정리
        gameObject.SetActive(false);
        _onConfirm?.Invoke();
    }

    // 초 → "mm:ss"
    private static string FormatTime(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        return $"{total / 60:00}:{total % 60:00}";
    }
}
