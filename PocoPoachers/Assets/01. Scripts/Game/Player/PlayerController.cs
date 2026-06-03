using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // ItemUseProgressUI가 구독해서 슬라이더를 채운다
    public static event Action<float> OnUseStarted;  // 사용 시작 (사용 시간 전달)
    public static event Action OnUseCancelled;        // 사용 취소

    [SerializeField] private GameObject PlayerBagUI;
    [SerializeField] private GameObject PlayerMainGameUI;
    [SerializeField] private GameObject boxUI;
    [SerializeField] private GameObject StorageUI;
    [SerializeField] private CameraController _cameraController;
    [SerializeField] private float _useItemDuration = 1.5f;

    // IInteractable이 접근하는 멤버
    public Inventory PlayerInventory => _inventory;
    public GameObject BoxUI => boxUI;
    public GameObject GetStorageUI => StorageUI;

    private Inventory _inventory;
    private PlayerInputHandler _inputHander;
    private QuickSlotDropHandler[] _quickSlots;
    private readonly List<GameObject> _interactObjects = new();
    private IInteractable _currentInteractable;
    private Coroutine _useCoroutine;

    private const string PlayerSaveKey = "player_inventory";

    private void Start()
    {
        _inventory = GetComponent<Inventory>();

        var gm = GameManager.GetInstance();
        if (gm.ShouldLoadPlayerInventory)
        {
            SaveManager.GetInstance().LoadInventory(PlayerSaveKey, _inventory);
            gm.SetLoadPlayerInventory(false);
        }

        _quickSlots = FindObjectsByType<QuickSlotDropHandler>(FindObjectsInactive.Include)
            .OrderBy(s => s.gameObject.name).ToArray();

        _inputHander = GetComponent<PlayerInputHandler>();
        _inputHander.GoInventory += ShowInventory;
        _inputHander.RegisterItemNumberKey += RegisterItem;
        _inputHander.ConsumeItemNumberKey += StartConsuming;
        _inputHander.StartInteraction += Interaction;
        _inputHander.CancelItemUse += CancelConsuming;

        var ui = UIManager.GetInstance();
        ui.Register(UIType.Inventory, PlayerBagUI);
        ui.Register(UIType.Storage, StorageUI);
        ui.Register(UIType.ItemBoxReveal, boxUI);
        ui.OnPanelOpened += OnPanelOpened;
        ui.OnPanelClosed += OnPanelClosed;
    }

    private void OnDestroy()
    {
        if (_inventory != null)
            SaveManager.GetInstance()?.SaveInventory(PlayerSaveKey, _inventory);

        var ui = UIManager.GetInstance();
        if (ui == null) return;
        ui.Unregister(UIType.Inventory);
        ui.Unregister(UIType.Storage);
        ui.Unregister(UIType.ItemBoxReveal);
        ui.OnPanelOpened -= OnPanelOpened;
        ui.OnPanelClosed -= OnPanelClosed;
    }

    private void OnPanelOpened(UIType type)
    {
        if (type == UIType.Inventory)
            PlayerMainGameUI.SetActive(false);
        else if (type != UIType.IngameMenu)
            return;

        LockCamera(true);
        _inputHander.SwitchInputActionMap(PlayerInputMapType.Inventory);
    }

    private void OnPanelClosed(UIType type)
    {
        if (type != UIType.Inventory && type != UIType.IngameMenu) return;
        if (UIManager.GetInstance().IsAnyPanelOpen) return;

        if (type == UIType.Inventory)
            PlayerMainGameUI.SetActive(true);

        LockCamera(false);
        _inputHander.SwitchInputActionMap(PlayerInputMapType.Game);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject != gameObject)
            _interactObjects.Add(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        _interactObjects.Remove(other.gameObject);
    }

    void Interaction()
    {
        var weapon = GetComponent<WeaponController>();
        if (weapon != null && weapon.IsReloading) return;

        // 현재 열린 인터랙션이 있으면 닫기
        if (_currentInteractable != null)
        {
            _currentInteractable.OnInteractExit(this);
            _currentInteractable = null;
            return;
        }

        GameObject nearest = GetNearestInteractable();
        if (nearest == null) return;
        if (!nearest.TryGetComponent<IInteractable>(out var interactable)) return;

        interactable.OnInteract(this);
        _currentInteractable = interactable;
    }

    /// <summary>
    /// 플레이어 가방 UI 열기/닫기 (IInteractable에서 직접 상태 지정 시 사용)
    /// </summary>
    public void SetInventoryOpen(bool open)
    {
        if (open) UIManager.GetInstance().Show(UIType.Inventory);
        else UIManager.GetInstance().Hide(UIType.Inventory);
    }

    /// <summary>
    /// 입력 맵 전환 (IInteractable에서 호출)
    /// </summary>
    public void SwitchInputMap(PlayerInputMapType type)
    {
        _inputHander.SwitchInputActionMap(type);
    }

    public void LockCamera(bool locked)
    {
        _cameraController.SetLocked(locked);
    }

    private GameObject GetNearestInteractable()
    {
        _interactObjects.RemoveAll(o => o == null);

        if (_interactObjects.Count == 1)
            return _interactObjects[0].TryGetComponent<IInteractable>(out _) ? _interactObjects[0] : null;

        GameObject nearest = null;
        float minDist = float.MaxValue;

        foreach (var obj in _interactObjects)
        {
            if (!obj.TryGetComponent<IInteractable>(out _)) continue;
            float dist = Vector3.Distance(transform.position, obj.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = obj;
            }
        }
        return nearest;
    }

    void ShowInventory()
    {
        var weapon = GetComponent<WeaponController>();
        if (weapon != null && weapon.IsReloading) return;
        UIManager.GetInstance().Toggle(UIType.Inventory);
    }

    void RegisterItem(int index)
    {
        _quickSlots[index].TryRegisterItem();
    }

    // 입력 측에서 호출: 사용 시작
    public void StartConsuming(int index)
    {
        if (!_quickSlots[index].IsSetted) return;
        if (_useCoroutine != null) return; // 이미 사용 중이면 무시

        _useCoroutine = StartCoroutine(UseItemRoutine(index));
    }

    // 입력 측에서 호출: 사용 취소
    public void CancelConsuming()
    {
        if (_useCoroutine == null) return;

        StopCoroutine(_useCoroutine);
        _useCoroutine = null;
        OnUseCancelled?.Invoke();
    }

    private IEnumerator UseItemRoutine(int index)
    {
        OnUseStarted?.Invoke(_useItemDuration);

        yield return new WaitForSeconds(_useItemDuration);

        _quickSlots[index].ConsumeItem();
        _useCoroutine = null;
    }
}
