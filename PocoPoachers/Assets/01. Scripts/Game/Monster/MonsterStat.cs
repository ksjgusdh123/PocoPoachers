using UnityEngine;

public class MonsterStat : StatBase
{
    [SerializeField] private int _monsterId;

    private void Awake()
    {
        var data = MonsterTable.Instance.Get(_monsterId);
        if (data == null)
        {
            Debug.LogError($"[MonsterStat] ID {_monsterId}에 해당하는 몬스터 데이터가 없습니다.");
            return;
        }
        Initialize(data);
    }

    public void Initialize(MonsterData data)
    {
        MaxHp = data.Hp;
        CurrentHp = MaxHp;
        Debug.Log($"{MaxHp} - 현재 체력 세팅 완료");
    }
}
