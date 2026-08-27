using UnityEngine;

/// <summary>
/// 창고 오브젝트. ItemBox와 달리 Reveal 연출 없이 바로 인벤토리 UI를 열어준다.
/// 고정 UID로 ObjectManager에 자기등록해서 기존 박스 교환 패킷(G_ItemGain/G_ItemExchange/H_ItemBoxUpdate)을
/// 그대로 사용한다. 내용물은 호스트만 저장/복원하고, 게스트는 씬 준비 후 스냅샷(H_ItemSpawn)으로 받는다.
/// </summary>
public class Storage : MonoBehaviour, IInteractable
{
    public const int STORAGE_UID = 1; // 필드 박스 uid(1000~)와 겹치지 않는 예약값

    [SerializeField] private string _saveKey = "storage";

    public Inventory StorageInventory => _inventory;
    private Inventory _inventory;
    private InventoryUI _storageUI;
    private bool _isDirty;

    private void Awake()
    {
        _inventory = GetComponent<Inventory>();

        var storageUI = FindAnyObjectByType<StorageUI>(FindObjectsInactive.Include);
        _storageUI = storageUI?.GetComponent<InventoryUI>();
        if (storageUI != null) storageUI.gameObject.SetActive(false);

        // 호스트/게스트 공통 — uid 기반 패킷이 이 오브젝트를 찾을 수 있게 등록.
        // WorldObject가 붙으면서 InventoryUI.IsBox가 true가 되어 UI 조작이 자동으로 네트워크 경로를 탄다
        ObjectManager.Instance?.RegisterSceneObject(ObjectKind.ItemBox, STORAGE_UID, gameObject);
    }

    // 같은 GameObject의 Inventory.Awake가 슬롯을 채운 뒤 실행되도록 Start에서 초기화한다.
    // (동일 오브젝트라도 컴포넌트 간 Awake 순서는 보장되지 않아, Awake에서 슬롯을 참조하면 0개일 수 있음)
    private void Start()
    {
        _storageUI?.Bind(_inventory);

        // 게스트는 로컬 세이브 대신 호스트가 보내주는 스냅샷으로 채운다 (로컬 파일과 갈라짐 방지)
        if (!RoomManager.IsHost) return;

        SaveManager.GetInstance().LoadInventory(_saveKey, _inventory);

        // 로드 이후의 변경(호스트 조작 + 게스트 요청 반영)부터 저장 대상
        foreach (var slot in _inventory.Slots)
            slot.OnChanged += MarkDirty;
        _inventory.ChangeInventory += MarkDirty;

        // 게스트 입장 스냅샷(SendWorldObjectsToGuest) 대상에 포함
        var pos = transform.position;
        ObjectManager.Instance?.RegisterSpawnedBox(new H_ItemSpawnT
        {
            Uid      = STORAGE_UID,
            TypeId   = 0,
            Pos      = new Vec3T { X = pos.x, Y = pos.y, Z = pos.z },
            Rotation = transform.eulerAngles.y,
        });
    }

    private void MarkDirty() => _isDirty = true;

    // 스왑 등으로 슬롯 이벤트가 한 프레임에 여러 번 발생해도 프레임 끝에 한 번만 저장
    private void LateUpdate()
    {
        if (!_isDirty) return;
        _isDirty = false;
        SaveManager.GetInstance()?.SaveInventory(_saveKey, _inventory);
    }

    private void OnDestroy()
    {
        ObjectManager.Instance?.UnregisterSceneObject(ObjectKind.ItemBox, STORAGE_UID);
    }

    public void OnInteract(PlayerController player)
    {
        SoundManager.GetInstance().PlaySfx("sfx_storage_open");

        player.SetInventoryOpen(true);

        var storageUI = player.GetStorageUI;
        storageUI.SetActive(true);

        player.PlayerInventory.InteractionInventory = _inventory;
        _inventory.InteractionInventory = player.PlayerInventory;

        GetComponent<ItemBoxAnimation>()?.SetOpen(true);
        player.SwitchInputMap(PlayerInputMapType.ItemBox);
    }

    public void OnInteractExit(PlayerController player)
    {
        SoundManager.GetInstance().PlaySfx("sfx_storage_close");

        player.PlayerInventory.InteractionInventory = null;
        _inventory.InteractionInventory = null;

        player.GetStorageUI.SetActive(false);
        player.SetInventoryOpen(false);

        GetComponent<ItemBoxAnimation>()?.SetOpen(false);

        player.SwitchToGameplayInputMap();
        UIManager.GetInstance().ChangeMouseCursor(true);
        // 저장은 슬롯 변경 이벤트(LateUpdate) 기반으로 호스트만 수행 — 게스트가 로컬 파일에 덮어쓰지 않도록 여기서는 저장하지 않는다
    }
}
