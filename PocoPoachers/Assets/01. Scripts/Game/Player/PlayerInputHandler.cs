using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using static UnityEngine.Rendering.DebugUI;

public enum PlayerInputMapType
{
    Game,
    Inventory,
    ItemBox,
    Shelter,
    Dialogue
}

[RequireComponent(typeof(PlayerInput))]
public class PlayerInputHandler : MonoBehaviour
{

    public Vector2 MoveInput { get; private set; }
    public bool IsSprintPressed { get; private set; }
    public bool IsFirePressed { get; private set; }
    public bool IsReloadPressed { get; private set; }
    public bool IsAimPressed { get; private set; }

    public event Action GoInventory;
    public event Action StartInteraction;
    public event Action DialogueAdvance;
    public event Action<int> WeaponSwitch;
    public event Action<int> RegisterItemNumberKey;
    public event Action<int> ConsumeItemNumberKey;
    public event Action Dodge;
    public event Action CancelItemUse;
    public event Action CancelReload;

    private PlayerInput _inputMap;
    private readonly Key[] _weaponKeys = { Key.Digit1, Key.Digit2 };
    private readonly Key[] _numberKeys = { Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8 };
    private PlayerInputMapType _inputType;

    // 이 플레이어가 있는 씬의 기본 게임플레이 맵 — 상호작용(창고 등)을 닫고 복귀할 맵.
    // 이동해 온 씬으로 자동 판별한다: 쉘터=Shelter(전투 입력 없음), 그 외=Game.
    public PlayerInputMapType GameplayMap =>
        SceneName.IsShelter(SceneManager.GetActiveScene().name)
            ? PlayerInputMapType.Shelter
            : PlayerInputMapType.Game;

    public void SwitchToGameplayMap() => SwitchInputActionMap(GameplayMap);

    // 입력 액션 콜백 처리 도중(같은 프레임) 맵을 바로 바꾸면, 그 입력을 떼는(release) 이벤트를
    // 새로 바뀐 맵 쪽이 놓쳐서 다음 입력이 "새로 눌림"으로 안 잡히는 경우가 있다.
    // 이번 입력 이벤트 처리가 끝난 뒤(다음 프레임)로 복귀를 미룬다.
    public void SwitchToGameplayMapNextFrame() => StartCoroutine(SwitchToGameplayMapNextFrameRoutine());

    private IEnumerator SwitchToGameplayMapNextFrameRoutine()
    {
        yield return null;
        SwitchToGameplayMap();
    }

    private void Awake()
    {
        _inputMap = GetComponent<PlayerInput>();
    }

    private void Start()
    {
        // PlayerInput.Default Map 설정에 의존하지 않고, 이 플레이어의 게임플레이 맵으로 시작을 통일한다.
        // PlayerInput의 OnEnable(기본 맵 활성화) 이후인 Start에서 덮어써야 안전하다.
        SwitchToGameplayMap();
    }

    public void SwitchInputActionMap(PlayerInputMapType type)
    {
        _inputType = type;
        _inputMap.SwitchCurrentActionMap(type.ToString());
    }

    // PlayerInput 컴포넌트가 Move 액션 발생 시 자동으로 호출
    private void OnMove(InputValue value)
    {
        MoveInput = value.Get<Vector2>();
    }

    private void OnSprint(InputValue value)
    {
        IsSprintPressed = value.isPressed;
    }

    private void OnFire(InputValue value)
    {
        IsFirePressed = value.isPressed;
    }

    private void OnReload(InputValue value)
    {
        IsReloadPressed = value.isPressed;
    }

    private void OnAim(InputValue value)
    {
        IsAimPressed = value.isPressed;
    }

    void OnGoInventory(InputValue value)
    {
        if (value.isPressed)
        {
            if (_inputType == PlayerInputMapType.Inventory) SwitchToGameplayMap();
            else SwitchInputActionMap(PlayerInputMapType.Inventory);
            GoInventory?.Invoke();
        }
    }

    void OnInteraction(InputValue value)
    {
        if (value.isPressed) StartInteraction?.Invoke();
    }

    // Dialogue 맵 전용 — Interaction과 이름을 다르게 둬서, 대화 중 F가 월드 상호작용으로 재소비되지 않는다
    void OnAdvance(InputValue value)
    {
        if (value.isPressed) DialogueAdvance?.Invoke();
    }

    void OnDodge(InputValue value)
    {
        if (value.isPressed) Dodge?.Invoke();
    }

    void OnItemNumberKey(InputValue value)
    {
        var keyboard = Keyboard.current;
        if (null == keyboard) return;

        for (int i = 0; i < _numberKeys.Length; i++)
        {
            if (keyboard[_numberKeys[i]].wasPressedThisFrame)
            {
                if (_inputType == GameplayMap) ConsumeItemNumberKey?.Invoke(i);
                else RegisterItemNumberKey?.Invoke(i);
                break;
            }
        }
    }

    void OnCancelItemUse(InputValue value)
    {
        if (value.isPressed) CancelItemUse?.Invoke();
    }

    private void Update()
    {
        if (Keyboard.current[Key.X].wasPressedThisFrame)
            CancelReload?.Invoke();
    }

    void OnChangeGun()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        for (int i = 0; i < _weaponKeys.Length; i++)
        {
            if (keyboard[_weaponKeys[i]].wasPressedThisFrame)
            {
                WeaponSwitch?.Invoke(i);
                break;
            }
        }
    }

}
