using System;
using System.Collections;
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
    [SerializeField] private CameraController _cameraController;
    [SerializeField] private float _useItemDuration = 1.5f;

    private Inventory _inventory;
    private PlayerInputHandler _inputHander;
    private QuickSlotDropHandler[] _quickSlots;
    private GameObject _interactObject;
    private Coroutine _useCoroutine;

    private void Start()
    {
        _inventory = GetComponent<Inventory>();
        _quickSlots = FindObjectsByType<QuickSlotDropHandler>(FindObjectsInactive.Include)
            .OrderBy(s => s.gameObject.name).ToArray();

        _inputHander = GetComponent<PlayerInputHandler>();
        _inputHander.GoInventory += ShowInventory;
        _inputHander.RegisterItemNumberKey += RegisterItem;
        _inputHander.ConsumeItemNumberKey += StartConsuming;
        _inputHander.StartInteraction += Interaction;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject != gameObject) _interactObject = other.gameObject;
    }

    private void OnTriggerExit(Collider other)
    {
        _interactObject = null;
    }

    void Interaction()
    {
        if (_interactObject != null && _interactObject.TryGetComponent<Inventory>(out var inven))
        {
            ShowInventory();
            boxUI.SetActive(!boxUI.activeSelf);
            if (boxUI.activeSelf)
            {
                _inventory.InteractionInventory = inven;
                inven.InteractionInventory = _inventory;
                boxUI.GetComponentInChildren<InventoryUI>()?.Bind(inven);
                boxUI.GetComponent<ItemBoxRevealUI>().Open(inven);
                _interactObject.GetComponent<ItemBox>()?.MarkOpened();
                _inputHander.SwitchInputActionMap(PlayerInputMapType.ItemBox);
            }
            else
            {
                _inventory.InteractionInventory = null;
                inven.InteractionInventory = null;
                _inputHander.SwitchInputActionMap(PlayerInputMapType.Game);
                UIManager.GetInstance().ChangeMouseCursor(true);
            }
        }
    }

    public void LockCamera(bool locked)
    {
        _cameraController.SetLocked(locked);
    }

    void ShowInventory()
    {
        PlayerBagUI.SetActive(!PlayerBagUI.activeSelf);
        bool isOpen = PlayerBagUI.activeSelf;
        PlayerMainGameUI.SetActive(!isOpen);
        LockCamera(isOpen);
        UIManager.GetInstance().ChangeMouseCursor(!isOpen);
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
