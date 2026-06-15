using System;
using System.Collections;
using UnityEngine;

// 광물 오브젝트의 베이스. id를 세팅하면 MineralTable에서 정보를 읽어 자신을 구성한다.
public class BaseOre : MonoBehaviour, IInteractable
{
    [SerializeField] private int _id;             // 인스펙터에서 지정하는 광물 id
    [SerializeField] private float _mineDuration = 2f;   // 채광에 걸리는 시간(초)
    [SerializeField] private ProximityDetector _proximityDetector;   // 플레이어 근접 감지

    private MineralData _data;
    private int _currentHp;
    private Coroutine _mineCoroutine;
    private UIScalePulse _pulseUI;
    private bool _isPlayerNearby;

    public int Id => _id;
    public MineralData Data => _data;
    public int CurrentHp => _currentHp;

    // ProgressUI가 구독하는 채광 진행 이벤트 (GunBase 재장전 이벤트와 동일 패턴)
    public static event Action<float> OnMineStarted;   // 채광 시작 (소요 시간 전달)
    public static event Action OnMineEnded;            // 채광 완료/취소

    private void Awake()
    {
        if (_proximityDetector != null)
        {
            _proximityDetector.OnEnter += OnPlayerEntered;
            _proximityDetector.OnExit += OnPlayerExited;
        }
    }

    private void OnDestroy()
    {
        if (_proximityDetector != null)
        {
            _proximityDetector.OnEnter -= OnPlayerEntered;
            _proximityDetector.OnExit -= OnPlayerExited;
        }
    }

    private void Start()
    {
        // 인스펙터에서 _id를 세팅해두면 시작 시 자동으로 테이블 정보로 구성
        Setup(_id);
    }

    private void OnPlayerEntered()
    {
        _isPlayerNearby = true;
        if (_mineCoroutine == null) ShowPulse();   // 채광 중이 아닐 때만 표시
    }

    private void OnPlayerExited()
    {
        _isPlayerNearby = false;
        HidePulse();
    }

    // 펄스 UI 표시 (ItemBox와 동일 방식)
    private void ShowPulse()
    {
        if (_pulseUI != null) return;   // 중복 생성 방지
        _pulseUI = WorldUIManager.Instance.Create<UIScalePulse>(WorldUIType.ScalePulse, transform);
    }

    private void HidePulse()
    {
        _pulseUI?.Release();
        _pulseUI = null;
    }

    // id로 테이블에서 광물 정보를 읽어 자신을 세팅한다. (런타임 스폰 시에도 호출 가능)
    public void Setup(int id)
    {
        _id = id;
        _data = MineralTable.Instance.Get(id);
        if (_data == null)
        {
            Debug.LogError($"[BaseOre] 광물 데이터를 찾을 수 없습니다. id={id}");
            return;
        }

        _currentHp = _data.MaxHp;
        OnSetup();
    }

    public void OnInteract(PlayerController player)
    {
        if (_mineCoroutine != null) return;   // 이미 채광 중이면 무시
        HidePulse();                          // 채광 시작 시 펄스 끄기
        OnMineStarted?.Invoke(_mineDuration); // 채광 게이지 표시
        _mineCoroutine = StartCoroutine(MineRoutine(player));
    }

    // 채광 진행: _mineDuration 초 뒤 완료하고 플레이어 상호작용을 해제시킨다.
    private IEnumerator MineRoutine(PlayerController player)
    {
        Debug.Log("채광중");

        yield return new WaitForSeconds(_mineDuration);

        _mineCoroutine = null;
        // TODO: 채광 완료 처리(체력 차감/드롭 등)는 다음 단계에서
        Debug.Log("채광 완료");
        OnMineEnded?.Invoke();         // 채광 게이지 숨김
        player.EndInteraction(this);   // PlayerController가 _currentInteractable을 null로 비움

        if (_isPlayerNearby) ShowPulse();   // 완료 후 근처에 있으면 펄스 재등장
    }

    public void OnInteractExit(PlayerController player)
    {
        // 채광 도중 다시 상호작용하면 취소
        if (_mineCoroutine != null)
        {
            StopCoroutine(_mineCoroutine);
            _mineCoroutine = null;
            OnMineEnded?.Invoke();   // 채광 게이지 숨김
        }

        if (_isPlayerNearby) ShowPulse();   // 취소 후 근처에 있으면 펄스 재등장
    }

    // 파생 클래스에서 모델/이펙트 등 추가 세팅이 필요할 때 오버라이드
    protected virtual void OnSetup() { }
}
