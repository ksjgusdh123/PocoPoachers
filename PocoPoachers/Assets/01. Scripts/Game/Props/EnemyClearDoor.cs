using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 지정한 적을 전부 처치하면 자동으로 열리는 문. 열린 뒤에는 계속 열려 있는다.
// frame_door_1/2/3처럼 character_nearby(bool)로 열림/닫힘이 갈리는 Animator를 쓴다.
//
// 접근 감지는 하지 않는다 — 같은 문에 AutoDoor가 붙어 있으면 다가갈 때 열려버리니 떼어낼 것.
//
// 처치 판정은 이벤트 구독이 아니라 주기적인 확인으로 한다. 적은 죽으면 파괴되기도 하고
// 원격에서 동기화로 사라지기도 해서, 그냥 "없거나 죽었으면 처치"로 보는 쪽이 안전하다.
[RequireComponent(typeof(Animator))]
public class EnemyClearDoor : MonoBehaviour
{
    private static readonly int CharacterNearby = Animator.StringToHash("character_nearby");

    [Tooltip("이 적들을 모두 처치하면 문이 열린다")]
    [SerializeField] private List<StatBase> _enemies = new();

    [Tooltip("클리어 확인 주기(초)")]
    [SerializeField] private float _checkInterval = 0.3f;

    // 첫 프레임부터 닫힌 모습이 되도록 애니메이터를 미리 진행시킬 시간.
    // Animator Controller의 시작 상태가 열림이면 SetBool만으로는 닫히는 애니메이션이 보이므로,
    // 한 번에 감아버려서 처음부터 닫힌 상태로 서 있게 한다.
    private const float SnapToClosedTime = 10f;

    private Animator _animator;
    private bool _opened;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _animator.SetBool(CharacterNearby, false);
        _animator.Update(SnapToClosedTime);
    }

    private IEnumerator Start()
    {
        if (_enemies.Count == 0)
        {
            // 잠긴 채로 길이 막히는 것보다는 열어두는 쪽이 낫다
            Debug.LogWarning($"[EnemyClearDoor] 대상 적이 비어 있어 문을 바로 엽니다 (door={name}).");
            Open();
            yield break;
        }

        var wait = new WaitForSeconds(_checkInterval);
        while (!_opened)
        {
            if (AllDead()) Open();
            yield return wait;
        }
    }

    private bool AllDead()
    {
        foreach (var enemy in _enemies)
        {
            // 파괴된 적은 null로 잡힌다 (Unity의 null 비교)
            if (enemy == null) continue;

            // HP로 판단하면 안 된다 — 비활성 상태로 배치된 적은 Awake가 아직 안 돌아
            // CurrentHp가 0이라 죽은 것으로 오인된다. IsDead만 본다.
            if (!enemy.IsDead) return false;
        }
        return true;
    }

    private void Open()
    {
        if (_opened) return;
        _opened = true;

        _animator.SetBool(CharacterNearby, true);
    }
}
