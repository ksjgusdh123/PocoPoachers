using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    [SerializeField] private GameObject GeneratorUI;
    [SerializeField] private GameObject FurnaceUI;
    [SerializeField] private GunPartPanelUI _gunPartPanel;   // 비활성으로 둬도 됨 (이벤트로 열림)
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
    public GameObject GetGeneratorUI => GeneratorUI;
    public GameObject GetFurnaceUI => FurnaceUI;
    public InventoryUI PlayerBagInventoryUI => _playerBagInventoryUI;
    public PlayerInputHandler InputHandler => _inputHandler;

    private Inventory _inventory;
    private InventoryUI _playerBagInventoryUI;
    private PlayerStat _playerStat;
    private FaintingUI _faintingUI;
    private PlayerRespawnPoint _respawnPoint; // 체크포인트가 등록돼 있으면 사망 대신 부활시킨다 (튜토리얼)
    private bool _isFainting;   // 기절(다운) 상태 진행 중 여부
    private bool _finalized;    // 완전 사망 처리(상자 스폰)를 이미 실행했는지 — 중복 방지
    private bool _raidWipeHandled; // 레이드 전멸 → 결과 씬 이동을 이미 처리했는지 (씬당 1회)
    private bool _spectating;      // 완전 사망 후 동료 시점 관전 중인지
    private StatBase _spectateStat; // 현재 관전 중인 동료의 스탯 (사망/이탈 시 다른 동료로 전환)
    private SaveManager _saveManager;
    private PlayerInputHandler _inputHandler;
    private QuickSlotDropHandler[] _quickSlots;
    private QuickSlotInventory _quickSlotInventory;
    private bool _guestCarryRestored;
    private GunPartDropHandler[] _gunPartSlots;
    private EquipDropHandler[] _equipHandlers;
    private readonly List<GameObject> _interactObjects = new();
    private IInteractable _currentInteractable;
    private Coroutine _useCoroutine;

    private const string PlayerSaveKey = "player_inventory";

    // 장착 슬롯 인덱스 — SaveManager.EquipSlotEntry / ApplyRoomRestore / GatherEquippedSlots와 일치해야 함
    private const int WeaponSlot0  = 0;
    private const int WeaponSlot1  = 1;
    private const int ArmorSlot    = 2;
    private const int BagSlot      = 4;

    private void Awake()
    {
        _inventory = GetComponent<Inventory>();
    }

    private void Start()
    {
        var ui = UIManager.GetInstance();
        if (PlayerBagUI == null) PlayerBagUI = ui.GetPanel(UIType.Inventory);
        if (PlayerMainGameUI == null) PlayerMainGameUI = ui.GetPanel(UIType.MainGameUI);
        if (boxUI == null) boxUI = ui.GetPanel(UIType.ItemBoxReveal);
        if (StorageUI == null) StorageUI = ui.GetPanel(UIType.Storage);
        if (EnhancementTableUI == null) EnhancementTableUI = ui.GetPanel(UIType.EnhancementTable);
        if (GunEnhancementTableUI == null) GunEnhancementTableUI = ui.GetPanel(UIType.GunEnhancementTable);
        if (RepairWorkbenchUI == null) RepairWorkbenchUI = ui.GetPanel(UIType.RepairWorkbench);
        if (CraftingTableUI == null) CraftingTableUI = ui.GetPanel(UIType.CraftingTable);
        if (GeneratorUI == null) GeneratorUI = ui.GetPanel(UIType.Generator);
        if (FurnaceUI == null) FurnaceUI = ui.GetPanel(UIType.Furnace);

        if (_gunPartPanel == null) _gunPartPanel = FindAnyObjectByType<GunPartPanelUI>(FindObjectsInactive.Include);

        _playerWeaponController = GetComponent<WeaponController>();
        _saveManager = SaveManager.GetInstance();

        _playerStat = GetComponent<PlayerStat>();
        if (_playerStat != null)
        {
            _playerStat.OnDie += HandleDeath;
            _playerStat.OnRevive += HandleRevive;
        }

        _faintingUI = FindAnyObjectByType<FaintingUI>(FindObjectsInactive.Include);
        if (_faintingUI != null)
            _faintingUI.OnFaintingComplete += FinalizeDeath;

        _respawnPoint = GetComponent<PlayerRespawnPoint>();

        // 코옵 게스트는 개인 세이브를 쓰지 않는다 — 방 세계(호스트 소유)에서 상태를 받는다.
        // 호스트/솔로만 자기 개인 세이브에서 복원한다(호스트 세이브 = 방 세계).
        if (RoomManager.IsHost)
        {
            // 장비를 먼저 복원한다 — 가방이 인벤토리 용량을 확장하므로, 인벤 로드보다 앞서야 확장 슬롯 아이템이 정상 적재된다.
            RestoreEquippedSlots();
            _saveManager.LoadInventory(PlayerSaveKey, _inventory);
        }
        else if (GuestStateCarry.HasPending)
        {
            RestoreGuestCarry();
        }

        BindPlayerInventoryUI();

        _quickSlots = FindObjectsByType<QuickSlotDropHandler>(FindObjectsInactive.Include)
            .OrderBy(s => s.gameObject.name).ToArray();

        InitQuickSlots();
        InitGunPartSlots();

        // UI 바인딩(InitQuickSlots)이 끝난 뒤여야 slot.OnChanged로 아이콘까지 갱신된다
        if (RoomManager.IsHost)
            _quickSlotInventory.Import(_saveManager.LoadQuickSlots());
        else if (GuestStateCarry.HasPending)
            _quickSlotInventory.Import(GuestStateCarry.QuickSlots);

        _inputHandler = GetComponent<PlayerInputHandler>();
        _inputHandler.GoInventory += ShowInventory;
        _inputHandler.GoQuest += ShowQuest;
        _inputHandler.ToggleMinimap += ShowMinimap;
        _inputHandler.ToggleSkillPanel += ToggleSkill;
        _inputHandler.RegisterItemNumberKey += RegisterItem;
        _inputHandler.ConsumeItemNumberKey += StartConsuming;
        _inputHandler.StartInteraction += Interaction;
        _inputHandler.CancelItemUse += CancelConsuming;

        if (_cameraController == null)
            _cameraController = FindObjectOfType<CameraController>();
        if (_cameraController != null)
            _cameraController.SetTarget(transform);

        ui.OnPanelOpened += OnPanelOpened;
        ui.OnPanelClosed += OnPanelClosed;

        SlotInteractionManager.GetInstance().OnGunPartRequest += OnGunPartRequested;
        SlotInteractionManager.GetInstance().OnInventoryGunPartRequest += OnInventoryGunPartRequested;
        SlotInteractionManager.GetInstance().OnInventoryItemUseRequest += StartConsumingInventory;

        InitEquipSlots();

        // 저장된 활력치(체력/스태미나/배터리) 복원 — 없으면(새 게임) PlayerStat.Awake의 최대치 유지.
        // 게스트는 개인 세이브를 쓰지 않으므로 호스트/솔로만 복원한다.
        if (RoomManager.IsHost && _playerStat != null && _saveManager.TryLoadVitals(out float savedHp, out float savedStamina, out float savedBattery))
            _playerStat.RestoreVitals(savedHp, savedStamina, savedBattery);
    }

    private void BindPlayerInventoryUI()
    {
        if (PlayerBagUI == null || _inventory == null) return;
        _playerBagInventoryUI = PlayerBagUI.GetComponentInChildren<InventoryUI>(true);
        _playerBagInventoryUI?.Bind(_inventory);
    }

    private void InitQuickSlots()
    {
        _quickSlotInventory = GetComponent<QuickSlotInventory>();

        int count = Mathf.Min(_quickSlots.Length, _quickSlotInventory.SlotCount);
        for (int i = 0; i < count; i++)
            _quickSlots[i].Init(_playerBagInventoryUI, _quickSlotInventory, _quickSlotInventory.StartIndex + i);
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

    // 인벤토리(미장착) 무기 우클릭 "파츠창 보기" → 임시 총으로 파츠 패널 열기
    private void OnInventoryGunPartRequested(int itemId, int uid)
    {
        if (_gunPartPanel != null)
            _gunPartPanel.OpenForItem(itemId, uid);
    }

    private void InitEquipSlots()
    {
        var weaponController = GetComponent<WeaponController>();
        var armorController = GetComponent<PlayerArmorController>();
        var bagController = GetComponent<BagController>();

        _equipHandlers = PlayerBagUI.GetComponentsInChildren<EquipDropHandler>(true);
        foreach (var handler in _equipHandlers)
        {
            if (handler.SlotIndex <= WeaponSlot1) handler.SetController(weaponController);
            else if (handler.SlotIndex <= ArmorSlot) handler.SetController(armorController);
            else handler.SetController(bagController);

            // 복원된 장착 상태를 슬롯 표시에 반영 (드래그드롭이 아닌 경로로 장착됐으므로 명시적 동기화 필요)
            handler.SyncDisplayToEquipped();
        }
    }

    // 저장된 장착 슬롯 구성을 복원한다. WorldEquipmentManager(static)에 uid별 내구도/장탄수/파츠가 남아 있어
    // 각 컨트롤러의 Equip 안에서 함께 복원된다. 가방 복원 시 인벤토리 용량이 확장되므로 인벤 로드 전에 호출한다.
    private void RestoreEquippedSlots()
    {
        var entries = _saveManager?.LoadEquipSlots();
        if (entries == null) return;

        foreach (var e in entries)
            EquipRestored(e.slotIndex, e.itemId, e.uid);
    }

    // 게스트가 직전 씬에서 넘긴 자기 상태를 복원한다. 장착이 인벤 용량을 확장하므로 순서는 세이브 복원과 동일하게 장착 먼저.
    private void RestoreGuestCarry()
    {
        _guestCarryRestored = true;

        if (GuestStateCarry.Equips != null)
            foreach (var e in GuestStateCarry.Equips)
                EquipRestored(e.slotIndex, e.itemId, e.uid);

        if (_inventory == null) return;

        foreach (var it in GuestStateCarry.Inventory)
        {
            var data = ItemTable.Instance.Get(it.itemId);
            if (data == null) continue;
            _inventory.AddItemAtSlot(it.slotIndex, data, it.amount, it.uid);
        }
    }

    private List<SaveManager.SlotSaveEntry> GatherInventorySlots()
    {
        var entries = new List<SaveManager.SlotSaveEntry>();
        if (_inventory == null) return entries;

        for (int i = 0; i < _inventory.CurrentCapacity; i++)
        {
            var slot = _inventory.Slots[i];
            if (slot.IsEmpty) continue;

            entries.Add(new SaveManager.SlotSaveEntry
            {
                slotIndex = i,
                itemId    = slot.ItemData.id,
                amount    = slot.Amount,
                uid       = slot.Uid,
            });
        }
        return entries;
    }

    // 슬롯 인덱스 → 담당 컨트롤러 매핑을 한 곳에 모은다 (세이브 복원/방 세계 복원/게스트 캐리오버 공용)
    private void EquipRestored(int slotIndex, int itemId, int uid)
    {
        var data = ItemTable.Instance.Get(itemId);
        if (data == null) return;

        EquipableController controller =
            slotIndex <= WeaponSlot1 ? (EquipableController)GetComponent<WeaponController>() :
            slotIndex <= ArmorSlot   ? GetComponent<PlayerArmorController>() : GetComponent<BagController>();

        controller?.Equip(data, slotIndex, uid);
    }

    // 코옵 게스트 복원 — 호스트가 보낸 방 세계 상태(로드아웃+인벤)를 로컬에 적용한다.
    // 장착이 controller.Equip → G_Equip을 보내 호스트가 총 상태(H_Durability/H_GunState)를 되돌려주게 한다.
    // 가방 장착이 인벤 용량을 확장하므로 장착을 인벤 채우기보다 먼저 한다(RestoreEquippedSlots와 동일).
    public void ApplyRoomRestore(List<GuestEquipEntryT> equips, List<GuestInvEntryT> inventory, List<GuestInvEntryT> quickSlots)
    {
        // 로컬 캐리오버로 이미 복원했다면 호스트가 늦게 보낸 스냅샷은 무시한다 — 적용하면 중복 지급이 된다
        if (_guestCarryRestored) return;

        if (equips != null)
            foreach (var e in equips)
            {
                if (e == null) continue;
                EquipRestored(e.SlotIndex, e.ItemId, e.ItemUid);
            }

        if (inventory != null && _inventory != null)
            foreach (var it in inventory)
            {
                if (it == null) continue;
                var data = ItemTable.Instance.Get(it.ItemId);
                if (data == null) continue;
                _inventory.AddItemAtSlot(it.SlotIndex, data, it.Amount, it.ItemUid);
            }

        if (quickSlots != null && _quickSlotInventory != null)
        {
            var entries = new List<SaveManager.SlotSaveEntry>();
            foreach (var qs in quickSlots)
            {
                if (qs == null) continue;
                entries.Add(new SaveManager.SlotSaveEntry { slotIndex = qs.SlotIndex, itemId = qs.ItemId, amount = qs.Amount, uid = qs.ItemUid });
            }
            _quickSlotInventory.Import(entries);
        }

        InitEquipSlots();
    }

    // 현재 장착 중인 슬롯 구성을 수집한다. 복원과 동일하게 컨트롤러에서 직접 읽어 UI(_equipHandlers) 유무와 무관하게 동작한다.
    // 슬롯 인덱스는 복원의 매핑(0~1 무기, 2~3 방어구, 그 외 가방)과 일치시킨다.
    private List<SaveManager.EquipSlotEntry> GatherEquippedSlots()
    {
        var result = new List<SaveManager.EquipSlotEntry>();
        var seenUids = new HashSet<int>();

        var weapon = GetComponent<WeaponController>();
        var armor = GetComponent<PlayerArmorController>();
        var bag = GetComponent<BagController>();

        if (weapon != null)
        {
            AddEquipEntry(result, seenUids, weapon, WeaponSlot0);
            AddEquipEntry(result, seenUids, weapon, WeaponSlot1);
        }
        if (armor != null) AddEquipEntry(result, seenUids, armor, ArmorSlot); // 방어구(헬멧) — 단일
        if (bag != null)   AddEquipEntry(result, seenUids, bag,   BagSlot);

        return result;
    }

    private void AddEquipEntry(List<SaveManager.EquipSlotEntry> list, HashSet<int> seenUids, EquipableController controller, int slotIndex)
    {
        int itemId = controller.GetEquippedId(slotIndex);
        if (itemId == 0) return;

        int uid = controller.GetEquippedUid(slotIndex);
        if (uid != 0 && !seenUids.Add(uid)) return; // 같은 인스턴스가 여러 슬롯에서 보고되면 한 번만

        list.Add(new SaveManager.EquipSlotEntry { slotIndex = slotIndex, itemId = itemId, uid = uid });
    }

    // 사망 시 인벤토리+장착 아이템을 상자에 담아 스폰(PlayerItemBoxDropper) + 장착 무기/방어구/가방 모두 해제
    // 상자 스폰이 먼저 실행돼야 장착 중이던 아이템을 조회할 수 있음
    // 단, 살아있는 다른 플레이어가 있으면 구출 가능하므로 상자를 만들지 않고 인벤토리/장비를 그대로 둔다
    private void Update()
    {
        // 기절 중 다른 살아있는 플레이어가 모두 사망하면 구조가 불가능하므로 즉시 완전 사망시킨다
        if (_isFainting && !HasOtherLivingPlayer())
        {
            Debug.Log("[PlayerController] 기절 중 다른 플레이어 전원 사망 — 구조 불가로 즉시 완전 사망");
            FinalizeDeath();
        }

        UpdateSpectate();
        CheckRaidWipe();
    }

    private bool HasOtherLivingPlayer()
        => ObjectManager.Instance != null && ObjectManager.Instance.HasLivingPlayerExcept(_playerStat);

    // 레이드 씬에서 살아있는 플레이어가 하나도 없으면(팀 전멸) 결과 씬으로 보낸다.
    // 전멸 판정은 모든 클라이언트가 로컬로 하고(스탯 동기화로 각자 판정), 씬 이동은 탈출과 마찬가지로
    // 호스트만 트리거한다(게스트는 H_LoadScene으로 따라옴). 결과 정보는 각자 자기 Carry에 담는다 —
    // 게스트가 기본값(성공)을 쓰면 실패인데 성공 화면이 뜬다.
    private void CheckRaidWipe()
    {
        if (_raidWipeHandled) return;
        if (!SceneManager.GetActiveScene().name.StartsWith("SC_Raid_")) return;

        // 로컬+원격 통틀어 살아있는 플레이어가 하나라도 있으면 아직 전멸이 아니다
        if (ObjectManager.Instance == null || ObjectManager.Instance.HasLivingPlayerExcept(null)) return;

        _raidWipeHandled = true;
        Debug.Log("[PlayerController] 레이드 팀 전멸 — 포드 호송 연출 후 결과 씬으로 이동");

        StartCoroutine(CoGoResultAfterRescueBeams());
    }

    // 마지막 팀원의 포드 호송 연출이 끝난 뒤 결과 씬으로 넘어간다 — 연출이 중간에 잘리지 않게.
    // 원격 팀원 몫의 포드도 각 클라이언트가 로컬 RescueBeamEffect로 재생하고 끝나면 스스로 파괴되므로,
    // 씬에 남은 이펙트가 사라질 때까지 기다리면 추가 패킷 없이 전원의 연출 종료를 알 수 있다.
    private IEnumerator CoGoResultAfterRescueBeams()
    {
        yield return null; // 마지막 사망자의 이펙트가 생성될 한 프레임을 준다

        var poll = new WaitForSeconds(0.1f);
        while (FindAnyObjectByType<RescueBeamEffect>() != null)
            yield return poll;

        RaidResultCarry.Set(success: false, SceneName.Shelter, SpawnId.FromRaid);

        if (RoomManager.IsHost)
            SceneTransition.Go(SceneName.Result, SpawnId.None);
    }

    private void HandleDeath()
    {
        // 부활 지점이 등록된 씬(튜토리얼)에서는 기절/호송/관전 대신 마지막 체크포인트에서 다시 시작한다.
        // 상자 드롭과 장비 해제는 FinalizeDeath에 있으므로 이 경로로 빠지면 소지품을 잃지 않는다.
        if (_respawnPoint != null && _respawnPoint.HasPoint)
        {
            _respawnPoint.Respawn();
            return;
        }

        // HP/배터리 0 → 기절 상태로 진입. FaintingUI 게이지(30초) 시작.
        // 시간 초과 또는 F 홀드로 게이지가 소진되면 OnFaintingComplete → FinalizeDeath로 완전 사망한다.
        // 그 사이 살아있는 동료가 구조하면 OnRevive → StopFainting으로 취소된다.
        // 기절 중 동료가 전원 사망하면 Update에서 감지해 즉시 FinalizeDeath로 이어진다.
        //
        // 단, 사망 시점에 이미 구조해 줄 동료가 없으면 게이지를 띄우지 않고 곧바로 완전 사망으로 넘어간다.
        // (Update에 맡기면 한 프레임이지만 게이지가 떴다 사라진다)
        if (_faintingUI != null && HasOtherLivingPlayer())
        {
            _isFainting = true;
            _faintingUI.StartFainting();
            return;
        }

        // 게이지 UI가 없거나(안전장치) 구조가 불가능하면 기절 없이 즉시 최종 사망 처리
        FinalizeDeath();
    }

    // 완전 사망 확정 — 인벤/장착 아이템을 상자에 담아 스폰(호스트)하고 장착을 모두 해제한다.
    // 상자 스폰이 먼저 실행돼야 장착 중이던 아이템을 조회할 수 있음. 중복 실행은 _finalized로 막는다.
    private void FinalizeDeath()
    {
        if (_finalized) return;
        _finalized = true;
        _isFainting = false;

        // 탈출 구역 집결 인원에서 빠지려면 "다운"이 아니라 "완전 사망"임을 알려야 한다
        PlayerDeathTracker.MarkFinalized(NetworkManager.Instance?.MyPlayerId ?? 0);

        // 게임 시간은 "자신이 완전히 사망한 시점"까지만 기록한다 (이후 관전 시간은 미포함)
        RaidStats.Instance.StopRaid();

        _faintingUI?.StopFainting();

        GetComponent<PlayerItemBoxDropper>()?.SpawnLootBox();

        foreach (var equip in GetComponents<EquipableController>())
            equip.UnequipAll();

        PlayRescueBeam();
    }

    // 위에서 포드가 내려와 빔으로 플레이어를 호송하는 연출. 끝나면 관전 모드로 전환한다.
    // 연출 동안 카메라가 상승하는 플레이어를 따라가지 않도록 위치 추적을 잠시 꺼둔다.
    // 로컬 재생과 별개로 RoomSync를 통해 팀원 화면에도 같은 연출이 재생되도록 알린다.
    private void PlayRescueBeam()
    {
        _cameraController?.SetFollowPosition(false);

        var effect = new GameObject("RescueBeamEffect").AddComponent<RescueBeamEffect>();
        effect.Play(transform, () =>
        {
            _cameraController?.SetFollowPosition(true);
            BeginSpectate();
        });

        RoomSync.RescueBeamPlay();
    }

    // 탈출 확정 — 사망 때와 같은 포드 호송 연출을 재생하고, 끝나면 결과창 콜백을 부른다.
    // 연출 중에는 조작을 막는다(빔에 실린 채 걸어 나가지 못하게).
    public void PlayEscapeBeam(Action onFinished)
    {
        _inputHandler?.SwitchInputActionMap(PlayerInputMapType.Inventory);
        LockCamera(true);
        _cameraController?.SetFollowPosition(false);

        var effect = new GameObject("RescueBeamEffect").AddComponent<RescueBeamEffect>();
        effect.Play(transform, () =>
        {
            _cameraController?.SetFollowPosition(true);
            onFinished?.Invoke();
        });
    }

    // 구조되어 부활하면 기절 게이지를 중단하고 상태를 초기화한다 (다음 사망을 다시 처리할 수 있도록)
    private void HandleRevive()
    {
        _isFainting = false;
        _finalized = false;
        _faintingUI?.StopFainting();

        PlayerDeathTracker.Clear(NetworkManager.Instance?.MyPlayerId ?? 0);

        // 관전 중이었다면 해제하고 카메라를 자신에게 되돌린다
        _spectating = false;
        _spectateStat = null;
        _cameraController?.SetSpectate(false);
        _cameraController?.SetTarget(transform);
    }

    // 완전 사망 후 살아있는 동료 시점으로 카메라를 옮긴다 (관전 시작)
    private void BeginSpectate()
    {
        _spectating = true;
        // 관전 모드: 이펙트 잠금 + 스무딩 강화로 원격 보간 떨림을 완화한다
        _cameraController?.SetSpectate(true);
        RetargetSpectate();
    }

    // 관전 중 현재 대상이 죽거나 사라졌으면 다른 살아있는 동료로 카메라를 옮긴다
    private void UpdateSpectate()
    {
        if (!_spectating) return;

        if (_spectateStat != null && !_spectateStat.IsDead && _spectateStat.CurrentHp > 0f)
            return; // 현재 대상이 아직 살아있으면 유지

        RetargetSpectate();
    }

    private void RetargetSpectate()
    {
        if (_cameraController == null) return;

        var living = ObjectManager.Instance?.GetLivingPlayer(_playerStat);
        if (living == null) return; // 살아있는 동료 없음 → 팀 전멸(CheckRaidWipe가 셸터로 복귀)

        _spectateStat = living.GetComponent<StatBase>();
        _cameraController.SetTarget(living.transform);
    }

    private void OnDestroy()
    {
        if (_playerStat != null)
        {
            _playerStat.OnDie -= HandleDeath;
            _playerStat.OnRevive -= HandleRevive;
        }

        if (_faintingUI != null)
            _faintingUI.OnFaintingComplete -= FinalizeDeath;


        // 코옵 게스트는 개인 세이브에 저장하지 않는다(방 세계는 호스트가 저장). 호스트/솔로만 자기 개인 세이브에 저장.
        if (RoomManager.IsHost)
        {
            if (_inventory != null)
                _saveManager?.SaveInventory(PlayerSaveKey, _inventory);

            if (_quickSlotInventory != null)
                _saveManager?.SaveQuickSlots(_quickSlotInventory.Export());

            // 장착 슬롯 구성(어떤 아이템이 몇 번 슬롯에 장착됐는지)을 저장 — 인벤토리와 함께 씬 전환/종료 시 영속화
            var equipSlots = GatherEquippedSlots();
            _saveManager?.SaveEquipSlots(equipSlots);

            // 씬 전환/종료 시점에 uid별 장비 상태(내구도/장탄수/파츠)를 함께 영속화
            _saveManager?.SaveEquipmentState();

            // 퀘스트 진행 상태(파티 공유)도 같은 시점에 영속화
            _saveManager?.SaveQuestState();

            // 활력치(체력/스태미나/배터리)를 영속화해 맵 전환 시 유지한다.
            // 단 사망 상태면 저장값을 버려(ClearVitals) 다음 스폰이 0 체력으로 시작하지 않게 한다.
            if (_playerStat != null)
            {
                if (_playerStat.IsDead || _playerStat.CurrentHp <= 0f)
                    _saveManager?.ClearVitals();
                else
                    _saveManager?.SaveVitals(_playerStat.CurrentHp, _playerStat.CurrentStamina, _playerStat.CurrentBattery);
            }
        }
        else
        {
            // 게스트는 세이브 파일 대신 메모리로 다음 씬에 넘기고(복원용), 같은 내용을 호스트 세이브에 올린다(오토세이브용)
            var inventorySlots = GatherInventorySlots();
            var equipSlots = GatherEquippedSlots();
            var quickSlots = _quickSlotInventory?.Export();

            GuestStateCarry.Store(inventorySlots, equipSlots, quickSlots);
            RoomSync.GuestSnapshot(inventorySlots, equipSlots, quickSlots);
        }

        if (_inputHandler != null)
        {
            _inputHandler.GoInventory -= ShowInventory;
            _inputHandler.GoQuest -= ShowQuest;
            _inputHandler.ToggleMinimap -= ShowMinimap;
            _inputHandler.ToggleSkillPanel -= ToggleSkill;
            _inputHandler.RegisterItemNumberKey -= RegisterItem;
            _inputHandler.ConsumeItemNumberKey -= StartConsuming;
            _inputHandler.StartInteraction -= Interaction;
            _inputHandler.CancelItemUse -= CancelConsuming;
        }

        var slotManager = SlotInteractionManager.GetInstance();
        if (slotManager != null)
        {
            slotManager.OnGunPartRequest -= OnGunPartRequested;
            slotManager.OnInventoryGunPartRequest -= OnInventoryGunPartRequested;
            slotManager.OnInventoryItemUseRequest -= StartConsumingInventory;
        }

        var ui = UIManager.GetInstance();
        if (ui == null) return;
        ui.OnPanelOpened -= OnPanelOpened;
        ui.OnPanelClosed -= OnPanelClosed;
    }

    // 플레이어는 씬마다 새로 스폰되지만 HUD는 씬에 배치돼 있어, 캐시해 둔 참조가 이전 씬의
    // 파괴된 오브젝트일 수 있다. 파괴됐으면 현재 씬 패널로 다시 잡는다.
    // (Unity의 == null은 파괴된 오브젝트에도 true를 돌려준다 — ?. 로는 걸러지지 않는다)
    private void SetMainGameUIActive(bool active)
    {
        if (PlayerMainGameUI == null)
        {
            var manager = UIManager.GetInstance();
            PlayerMainGameUI = manager != null ? manager.GetPanel(UIType.MainGameUI) : null;
        }

        if (PlayerMainGameUI != null) PlayerMainGameUI.SetActive(active);
    }

    private void OnPanelOpened(UIType type)
    {
        if (type == UIType.Inventory || type == UIType.Minimap)
            SetMainGameUIActive(false);
        else if (type != UIType.IngameMenu && type != UIType.EnhancementTable && type != UIType.VotePopup &&
                 type != UIType.Skill)
            return;

        LockCamera(true);
        _inputHandler.SwitchInputActionMap(type switch
        {
            UIType.EnhancementTable => PlayerInputMapType.ItemBox,
            UIType.Minimap => PlayerInputMapType.Minimap,
            _ => PlayerInputMapType.Inventory,
        });
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

        if (type != UIType.Inventory && type != UIType.IngameMenu && type != UIType.EnhancementTable &&
            type != UIType.Minimap && type != UIType.VotePopup && type != UIType.Skill) return;
        if (UIManager.GetInstance().IsAnyPanelOpen) return;

        if (type == UIType.Inventory || type == UIType.Minimap)
            SetMainGameUIActive(true);

        LockCamera(false);
        _inputHandler.SwitchToGameplayMap();
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

        // OnInteract보다 먼저 대입해야, OnInteract 내부에서 EndInteraction(this)를 호출해
        // 즉시 상호작용을 끝내려는 경우(예: NpcDialogueInteractable)가 제대로 동작한다.
        // 순서가 반대면 EndInteraction의 "지금 추적 중인 게 이거 맞나" 가드에 걸려 무시되고,
        // 바로 다음 줄에서 다시 덮어써져 버린다.
        _currentInteractable = interactable;
        interactable.OnInteract(this);
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
        _inputHandler.SwitchInputActionMap(type);
    }

    /// <summary>
    /// 상호작용 종료 후 이 플레이어의 기본 게임플레이 맵으로 복귀 (Raid=Game, 쉘터=Shelter)
    /// </summary>
    public void SwitchToGameplayInputMap()
    {
        _inputHandler.SwitchToGameplayMap();
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

    void ShowMinimap()
    {
        var ui = UIManager.GetInstance();
        if (ui.IsOpen(UIType.Minimap))
        {
            ui.Hide(UIType.Minimap);
            return;
        }

        // DialogueUI.Open()과 동일한 패턴 — 열려있던 다른 패널을 전부 닫고 미니맵만 띄운다
        ui.HideAll();
        ui.Show(UIType.Minimap);
    }

    void ToggleSkill() => UIManager.GetInstance().Toggle(UIType.Skill);

    void ShowQuest()
    {
        bool wasOpen = UIManager.GetInstance().GetPanel(UIType.Quest)?.activeSelf ?? false;
        UIManager.GetInstance().Toggle(UIType.Quest);
        LockCamera(!wasOpen); // 열리는 거면 잠그고(마우스 오프셋 정지), 닫히는 거면 원복
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

    // 아이템별 사용 효과음(item.csv의 use_sound) — 사용 시간 동안 깔리도록 시작 시점에 낸다.
    // 사용을 취소하면 소리도 끊어야 하므로 정지 가능한 경로로 재생한다.
    private void PlayUseSound(ItemData item)
    {
        if (item == null) return;
        SoundManager.GetInstance()?.PlayCancelableSfx(item.UseSound);
    }

    // 입력 측에서 호출: 사용 취소
    public void CancelConsuming()
    {
        if (_useCoroutine == null) return;

        StopCoroutine(_useCoroutine);
        _useCoroutine = null;
        SoundManager.GetInstance()?.StopCancelableSfx();
        OnUseCancelled?.Invoke();
    }

    private IEnumerator UseItemRoutine(int index)
    {
        OnUseStarted?.Invoke(_useItemDuration);
        PlayUseSound(_quickSlots[index].DroppedItemData);

        yield return new WaitForSeconds(_useItemDuration);

        _quickSlots[index].ConsumeItem();
        _useCoroutine = null;
    }

    // 인벤토리 컨텍스트 메뉴의 "사용" — 퀵슬롯과 동일하게 사용 시간 UI를 거쳐 소비한다
    private void StartConsumingInventory(ItemData item, Inventory inventory)
    {
        if (item == null || inventory == null) return;
        if (_useCoroutine != null) return; // 이미 사용 중이면 무시

        _useCoroutine = StartCoroutine(UseInventoryItemRoutine(item, inventory));
    }

    private IEnumerator UseInventoryItemRoutine(ItemData item, Inventory inventory)
    {
        OnUseStarted?.Invoke(_useItemDuration);
        PlayUseSound(item);

        yield return new WaitForSeconds(_useItemDuration);

        ItemUseSystem.TryUse(item, inventory);
        _useCoroutine = null;
    }
}
