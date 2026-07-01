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

    private WeaponController _playerWeaponController;
    [SerializeField] private GameObject PlayerBagUI;
    [SerializeField] private GameObject PlayerMainGameUI;
    [SerializeField] private GameObject boxUI;
    [SerializeField] private GameObject StorageUI;
    [SerializeField] private GameObject EnhancementTableUI;
    [SerializeField] private GameObject GunEnhancementTableUI;
    [SerializeField] private GameObject RepairWorkbenchUI;
    [SerializeField] private GameObject CraftingTableUI;
    [SerializeField] private GunPartUI _gunPartPanel;   // 비활성으로 둬도 됨 (이벤트로 열림)
    [SerializeField] private CameraController _cameraController;
    [SerializeField] private float _useItemDuration = 1.5f;

    // IInteractable이 접근하는 멤버
    public Inventory PlayerInventory => _inventory;
    public GameObject BoxUI => boxUI;
    public GameObject GetStorageUI => StorageUI;
    public GameObject GetEnhancementTableUI => EnhancementTableUI;
    public GameObject GetGunEnhancementTableUI => GunEnhancementTableUI;
    public GameObject GetRepairWorkbenchUI => RepairWorkbenchUI;
    public GameObject GetCraftingTableUI => CraftingTableUI;
    public InventoryUI PlayerBagInventoryUI => _playerBagInventoryUI;

    private Inventory _inventory;
    private InventoryUI _playerBagInventoryUI;
    private PlayerStat _playerStat;
    private SaveManager _saveManager;
    private PlayerInputHandler _inputHander;
    private QuickSlotDropHandler[] _quickSlots;
    private GunPartDropHandler[] _gunPartSlots;
    private readonly List<GameObject> _interactObjects = new();
    private IInteractable _currentInteractable;
    private Coroutine _useCoroutine;

    private const string PlayerSaveKey = "player_inventory";

    private void Awake()
    {
        _inventory = GetComponent<Inventory>();
    }

    private void Start()
    {
        _playerWeaponController = GetComponent<WeaponController>();
        _saveManager = SaveManager.GetInstance();

        _playerStat = GetComponent<PlayerStat>();
        if (_playerStat != null)
            _playerStat.OnDie += HandleDeath;

        var gm = GameManager.GetInstance();
        if (gm.ShouldLoadPlayerInventory)
        {
            _saveManager.LoadInventory(PlayerSaveKey, _inventory);
            gm.SetLoadPlayerInventory(false);
        }

        BindPlayerInventoryUI();

        _quickSlots = FindObjectsByType<QuickSlotDropHandler>(FindObjectsInactive.Include)
            .OrderBy(s => s.gameObject.name).ToArray();

        InitQuickSlots();
        InitGunPartSlots();

        _inputHander = GetComponent<PlayerInputHandler>();
        _inputHander.GoInventory += ShowInventory;
        _inputHander.RegisterItemNumberKey += RegisterItem;
        _inputHander.ConsumeItemNumberKey += StartConsuming;
        _inputHander.StartInteraction += Interaction;
        _inputHander.CancelItemUse += CancelConsuming;

        if (_cameraController == null)
            _cameraController = FindObjectOfType<CameraController>();
        if (_cameraController != null)
            _cameraController.SetTarget(transform);

        var ui = UIManager.GetInstance();
        ui.Register(UIType.Inventory, PlayerBagUI);
        ui.Register(UIType.Storage, StorageUI);
        ui.Register(UIType.EnhancementTable, EnhancementTableUI);
        ui.Register(UIType.GunEnhancementTable, GunEnhancementTableUI);
        ui.Register(UIType.RepairWorkbench, RepairWorkbenchUI);
        ui.Register(UIType.CraftingTable, CraftingTableUI);
        ui.Register(UIType.ItemBoxReveal, boxUI);
        ui.OnPanelOpened += OnPanelOpened;
        ui.OnPanelClosed += OnPanelClosed;

        SlotInteractionManager.GetInstance().OnGunPartRequest += OnGunPartRequested;

        InitEquipSlots();
    }

    private void BindPlayerInventoryUI()
    {
        if (PlayerBagUI == null || _inventory == null) return;
        _playerBagInventoryUI = PlayerBagUI.GetComponentInChildren<InventoryUI>(true);
        _playerBagInventoryUI?.Bind(_inventory);
    }

    private void InitQuickSlots()
    {
        var quickSlotInventory = GetComponent<QuickSlotInventory>();

        int count = Mathf.Min(_quickSlots.Length, quickSlotInventory.SlotCount);
        for (int i = 0; i < count; i++)
            _quickSlots[i].Init(_playerBagInventoryUI, quickSlotInventory, quickSlotInventory.StartIndex + i);
    }

    // 총기 파츠 슬롯에 로컬 플레이어 인벤 UI 연결 (해제 시 인벤토리 반납용). 대상 총은 패널이 SetGun으로 주입.
    private void InitGunPartSlots()
    {
        _gunPartSlots = FindObjectsByType<GunPartDropHandler>(FindObjectsInactive.Include);
        foreach (var handler in _gunPartSlots)
            handler.BindInventoryUI(_playerBagInventoryUI);
    }

    // 무기 우클릭 "파츠 장착" → 해당 총으로 파츠 패널 열기 (패널은 비활성으로 둬도 됨)
    private void OnGunPartRequested(GunBase gun)
    {
        if (_gunPartPanel != null)
            _gunPartPanel.Open(gun);
    }

    private void InitEquipSlots()
    {
        var weaponController = GetComponent<WeaponController>();
        var armorController = GetComponent<PlayerArmorController>();
        var bagController = GetComponent<BagController>();

        foreach (var handler in PlayerBagUI.GetComponentsInChildren<EquipDropHandler>(true))
        {
            if (handler.SlotIndex <= 1) handler.SetController(weaponController);
            else if (handler.SlotIndex <= 3) handler.SetController(armorController);
            else handler.SetController(bagController);
        }
    }

    // 사망 시 메인 인벤토리 비우기 + 장착 무기/방어구/가방 모두 해제
    private void HandleDeath()
    {
        _inventory?.Clear();

        foreach (var equip in GetComponents<EquipableController>())
            equip.UnequipAll();
    }

    private void OnDestroy()
    {
        if (_playerStat != null)
            _playerStat.OnDie -= HandleDeath;

        if (_inventory != null)
            _saveManager?.SaveInventory(PlayerSaveKey, _inventory);

        if (_inputHander != null)
        {
            _inputHander.GoInventory -= ShowInventory;
            _inputHander.RegisterItemNumberKey -= RegisterItem;
            _inputHander.ConsumeItemNumberKey -= StartConsuming;
            _inputHander.StartInteraction -= Interaction;
            _inputHander.CancelItemUse -= CancelConsuming;
        }

        var slotManager = SlotInteractionManager.GetInstance();
        if (slotManager != null)
            slotManager.OnGunPartRequest -= OnGunPartRequested;

        var ui = UIManager.GetInstance();
        if (ui == null) return;
        ui.Unregister(UIType.Inventory);
        ui.Unregister(UIType.Storage);
        ui.Unregister(UIType.EnhancementTable);
        ui.Unregister(UIType.ItemBoxReveal);
        ui.OnPanelOpened -= OnPanelOpened;
        ui.OnPanelClosed -= OnPanelClosed;
    }

    private void OnPanelOpened(UIType type)
    {
        if (type == UIType.Inventory)
            PlayerMainGameUI.SetActive(false);
        else if (type != UIType.IngameMenu && type != UIType.EnhancementTable)
            return;

        LockCamera(true);
        _inputHander.SwitchInputActionMap(type == UIType.EnhancementTable
            ? PlayerInputMapType.ItemBox
            : PlayerInputMapType.Inventory);
    }

    private void OnPanelClosed(UIType type)
    {
        // ESC로 UI가 닫혔을 때 _currentInteractable 정리
        if (type == UIType.PlanetSelect || type == UIType.ShelterUpgrade)
        {
            if (_currentInteractable != null)
            {
                _currentInteractable.OnInteractExit(this);
                _currentInteractable = null;
            }
            return;
        }

        if (type == UIType.Inventory)
            _gunPartPanel?.Close();

        if (type != UIType.Inventory && type != UIType.IngameMenu && type != UIType.EnhancementTable) return;
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
        if (_playerWeaponController != null && _playerWeaponController.IsReloading) return;

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
    /// 상호작용 오브젝트가 스스로 상호작용을 끝낼 때 호출한다. (예: 채광 완료)
    /// 호출한 오브젝트가 현재 상호작용 중인 대상일 때만 해제한다.
    /// </summary>
    public void EndInteraction(IInteractable interactable)
    {
        if (_currentInteractable != interactable) return;
        _currentInteractable = null;
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
