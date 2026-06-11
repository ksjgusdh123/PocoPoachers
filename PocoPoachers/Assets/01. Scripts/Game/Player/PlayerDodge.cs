using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerDodge : MonoBehaviour
{
    [SerializeField] private float _dodgeSpeed = 10f;
    [SerializeField] private float _dodgeDuration = 0.5f;
    [SerializeField] private float _cooldown = 1f;
    [SerializeField] private float _dodgeStaminaCost = 25f;

    [SerializeField] private float _recoveryDuration = 0.4f;

    public bool IsRolling { get; private set; }
    public bool IsRecovering { get; private set; }
    public bool IsInvincible { get; private set; }

    private PlayerInputHandler _inputHandler;
    private CharacterController _characterController;
    private Animator _animator;
    private PlayerStat _playerStat;
    private WeaponController _weaponController;
    private float _lastDodgeTime = float.NegativeInfinity;

    private void Awake()
    {
        _inputHandler = GetComponent<PlayerInputHandler>();
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();
        _playerStat = GetComponent<PlayerStat>();
        _weaponController = GetComponent<WeaponController>();
    }

    private void Start()
    {
        _inputHandler.Dodge += TryDodge;
    }

    private void OnDestroy()
    {
        _inputHandler.Dodge -= TryDodge;
    }

    private void TryDodge()
    {
        if (IsRolling) return;
        if (Time.time < _lastDodgeTime + _cooldown) return;
        if (_playerStat != null && !_playerStat.UseStamina(_dodgeStaminaCost)) return;

        _weaponController?.CancelReload();

        Vector2 input = _inputHandler.MoveInput;
        Vector3 dodgeDir = input.sqrMagnitude > 0.01f
            ? new Vector3(input.x, 0f, input.y).normalized
            : transform.forward;

        StartCoroutine(DodgeRoutine(dodgeDir));
    }

    private IEnumerator DodgeRoutine(Vector3 direction)
    {
        IsRolling = true;
        IsInvincible = true;
        _lastDodgeTime = Time.time;
        transform.rotation = Quaternion.LookRotation(direction);
        _weaponController?.SetCurrentGunVisible(false);
        _animator.SetTrigger("Roll");
        _animator.SetLayerWeight(1, 0f);

        float elapsed = 0f;
        while (elapsed < _dodgeDuration)
        {
            _characterController.Move(direction * _dodgeSpeed * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        _animator.SetLayerWeight(1, 1f);
        _weaponController?.SetCurrentGunVisible(true);
        IsInvincible = false;
        IsRolling = false;
        IsRecovering = true;
        yield return new WaitForSeconds(_recoveryDuration);
        IsRecovering = false;
    }
}
