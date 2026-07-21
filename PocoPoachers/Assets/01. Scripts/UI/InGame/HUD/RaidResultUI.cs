using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// 레이드 종료 결과 오버레이. 현재는 임무 실패(팀 전멸)만 표시한다.
// 이벤트로 활성화되어 페이드 인 → "셸터로 돌아가기" 버튼 클릭 → 페이드 아웃 후 OnFinished를 발생시킨다.
// 실제 씬 복귀는 OnFinished 구독측(호스트)이 수행한다.
//
// 코옵: 복귀 버튼은 호스트에게만 보인다. 게스트는 호스트가 복귀시키면 H_LoadScene으로 따라오므로
// 버튼을 눌러 직접 전환할 필요가 없다. (게스트 화면은 씬 전환 시 로딩 화면으로 덮인다)
//
// 이 오브젝트는 평소 비활성으로 둬도 된다 — ShowFailure에서 스스로 켠다.
[RequireComponent(typeof(CanvasGroup))]
public class RaidResultUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup _group;      // 페이드 대상 (미지정 시 이 오브젝트의 CanvasGroup)
    [SerializeField] private Button _returnButton;    // "셸터로 돌아가기" — 호스트에게만 표시
    [SerializeField] private float _fadeDuration = 0.4f; // 페이드 인/아웃 시간

    // 결과 연출이 끝나(버튼 클릭 → 페이드 아웃) 씬 복귀 준비가 됐을 때
    public event Action OnFinished;

    private bool _isShowing;

    private void Awake()
    {
        if (_group == null) _group = GetComponent<CanvasGroup>();
        if (_returnButton != null)
            _returnButton.onClick.AddListener(OnReturnClicked);

        // 씬에 활성으로 놓였다면 숨긴다. 단 이미 표시 중이면(켜지는 순간 실행되는 Awake) 건드리지 않는다.
        if (!_isShowing)
            gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_returnButton != null)
            _returnButton.onClick.RemoveListener(OnReturnClicked);
    }

    // 임무 실패 표시 시작 — 비활성이었어도 스스로 켠 뒤 페이드 인한다
    public void ShowFailure()
    {
        _isShowing = true;
        gameObject.SetActive(true);

        // 복귀 버튼은 호스트만 — 게스트는 호스트를 따라 이동한다
        if (_returnButton != null)
        {
            _returnButton.gameObject.SetActive(RoomManager.IsHost);
            _returnButton.interactable = true;
        }

        _group.interactable = true;
        _group.blocksRaycasts = true;
        _group.alpha = 0f;
        _group.DOFade(1f, _fadeDuration); // 페이드 인
    }

    private void OnReturnClicked()
    {
        if (!_isShowing) return;
        _isShowing = false;
        if (_returnButton != null) _returnButton.interactable = false;
        StartCoroutine(FadeOutThenFinish());
    }

    private IEnumerator FadeOutThenFinish()
    {
        _group.DOFade(0f, _fadeDuration); // 페이드 아웃
        yield return new WaitForSeconds(_fadeDuration);

        OnFinished?.Invoke();
        gameObject.SetActive(false);
    }
}
