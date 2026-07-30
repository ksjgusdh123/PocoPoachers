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
    private bool _isDepleted;   // 채광이 끝나 더 이상 캘 수 없는 상태

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
        if (_isDepleted) return;        // 고갈된 광물에는 상호작용 안내를 띄우지 않는다
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
        _isDepleted = false;
        OnSetup();
    }

    public void OnInteract(PlayerController player)
    {
        if (_isDepleted) return;              // 이미 캐낸 광물은 재상호작용 불가
        if (_mineCoroutine != null) return;   // 이미 채광 중이면 무시
        HidePulse();                          // 채광 시작 시 펄스 끄기
        OnMineStarted?.Invoke(_mineDuration); // 채광 게이지 표시
        _mineCoroutine = StartCoroutine(MineRoutine(player));
    }

    // 채광 진행: _mineDuration 초 뒤 완료하고 플레이어 상호작용을 해제시킨다.
    private IEnumerator MineRoutine(PlayerController player)
    {
        yield return new WaitForSeconds(_mineDuration);

        _mineCoroutine = null;
        OnMineEnded?.Invoke();                    // 채광 게이지 숨김
        bool given = GiveDrop(player);            // 드롭템을 상호작용한 플레이어 인벤토리에 지급
        player.EndInteraction(this);              // PlayerController가 _currentInteractable을 null로 비움

        // 인벤토리가 가득 차 지급에 실패했다면 광물을 소모하지 않고 남겨둔다.
        if (!given)
        {
            if (_isPlayerNearby) ShowPulse();
            yield break;
        }

        // 채광은 _mineDuration 기반 1회 완료형 상호작용이다(mineral.max_hp는 이 모델에서 쓰이지 않음).
        // 고갈 처리를 하지 않으면 같은 광물을 무한 반복 채광할 수 있다.
        _currentHp = 0;
        Deplete();
    }

    // 고갈 처리: 펄스를 정리하고 오브젝트를 비활성화한다.
    // Destroy 대신 비활성화를 쓰는 이유는 씬에 배치된 광물을 세이브/리스폰에서 되살릴 여지를 남기기 위함이다.
    private void Deplete()
    {
        _isDepleted = true;
        HidePulse();
        gameObject.SetActive(false);
    }

    // 채광 완료 시 드롭템(drop_item_id × drop_amount)을 플레이어 인벤토리에 넣는다. 지급 성공 여부 반환.
    private bool GiveDrop(PlayerController player)
    {
        if (_data == null) return false;

        ItemData dropData = ItemTable.Instance.Get(_data.DropItemId);
        if (dropData == null)
        {
            Debug.LogWarning($"[BaseOre] 드롭 아이템을 찾을 수 없습니다. drop_item_id={_data.DropItemId}");
            return false;
        }

        Inventory inventory = player.PlayerInventory;
        int slotIndex = inventory.CanAddItem(dropData, _data.DropAmount);
        if (slotIndex < 0)
        {
            Debug.Log("[BaseOre] 인벤토리가 가득 차 드롭을 지급하지 못했습니다.");
            return false;
        }

        return inventory.AddItemAtSlot(slotIndex, dropData, _data.DropAmount);
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
