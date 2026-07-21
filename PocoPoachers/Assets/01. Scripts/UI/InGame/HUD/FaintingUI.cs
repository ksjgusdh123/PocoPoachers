using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 기절(다운) 상태에서 남은 시간을 게이지 이미지 + 초 텍스트로 표시하는 UI.
// 이 오브젝트는 평소 비활성으로 두고, 기절 시작 시 StartFainting에서 스스로 활성화된다.
// 남은 시간이 0이 되면 완전 사망으로 간주해 OnFaintingComplete를 발생시킨다.
// F키를 꾹 누르는 동안엔 _giveUpDrainMultiplier 배로 빠르게 줄어 바로 사망시킬 수 있다.
//
// 이 스크립트는 표시 + 타이머만 담당한다. 실제 사망 확정(상자 스폰/부활 처리 등)은
// OnFaintingComplete 구독측(사망 시스템)이 수행한다. 구조되면 StopFainting()으로 중단한다.
public class FaintingUI : MonoBehaviour
{
    [SerializeField] private Image _fillImage;           // 남은 시간 비율로 채우는 게이지
    [SerializeField] private TextMeshProUGUI _secondsText; // 남은 시간(초)을 나타내는 텍스트
    [SerializeField] private float _faintDuration = 30f;         // 기절 후 완전 사망까지 시간(초)
    [SerializeField] private float _giveUpDrainMultiplier = 30f; // F 홀드 시 소진 가속 배율

    // 시간이 소진되어 완전 사망이 확정됐을 때 (시간 초과 또는 F 홀드 완료)
    public event Action OnFaintingComplete;

    private float _remaining;
    private bool _isFainting;

    private void Awake()
    {
        // 씬에 활성 상태로 놓였다면 숨긴다. 단 이미 기절이 시작된 상태면 건드리지 않는다
        // (StartFainting이 비활성 오브젝트를 켜는 순간 실행되는 Awake가 다시 꺼버리는 것을 방지).
        if (!_isFainting)
            gameObject.SetActive(false);
    }

    // 기절 시작 — 남은 시간을 최대치로 잡고, 비활성이었어도 스스로 켠 뒤 카운트다운을 시작한다
    public void StartFainting()
    {
        _remaining = _faintDuration;
        _isFainting = true;            // Awake가 다시 숨기지 않도록 먼저 세팅
        gameObject.SetActive(true);    // 평소 비활성이어도 여기서 켜진다 (Update가 돌게)
        Refresh();
    }

    // 구조 등으로 기절 상태가 해제됐을 때 — 표시를 숨기고 카운트다운을 멈춘다
    public void StopFainting()
    {
        _isFainting = false;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!_isFainting) return;

        // F를 누르고 있으면 소진 가속
        float drainRate = IsGiveUpHeld() ? _giveUpDrainMultiplier : 1f;
        _remaining -= Time.deltaTime * drainRate;

        if (_remaining <= 0f)
        {
            _remaining = 0f;
            Refresh();
            Complete();
            return;
        }

        Refresh();
    }

    private bool IsGiveUpHeld()
    {
        var keyboard = Keyboard.current;
        return keyboard != null && keyboard[Key.F].isPressed;
    }

    private void Refresh()
    {
        // 게이지: 남은 시간 비율
        if (_fillImage != null)
            _fillImage.fillAmount = _faintDuration > 0f ? _remaining / _faintDuration : 0f;

        // 텍스트: 남은 초를 올림 표시 — 30초에서 시작해 1까지, 0이 되면 사망
        if (_secondsText != null)
            _secondsText.text = Mathf.CeilToInt(_remaining).ToString();
    }

    private void Complete()
    {
        _isFainting = false;
        gameObject.SetActive(false);
        OnFaintingComplete?.Invoke();
    }
}
