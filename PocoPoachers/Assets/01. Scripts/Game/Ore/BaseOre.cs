using UnityEngine;

// 광물 오브젝트의 베이스. id를 세팅하면 MineralTable에서 정보를 읽어 자신을 구성한다.
public class BaseOre : MonoBehaviour
{
    [SerializeField] private int _id;   // 인스펙터에서 지정하는 광물 id

    private MineralData _data;
    private int _currentHp;

    public int Id => _id;
    public MineralData Data => _data;
    public int CurrentHp => _currentHp;

    private void Start()
    {
        // 인스펙터에서 _id를 세팅해두면 시작 시 자동으로 테이블 정보로 구성
        Setup(_id);
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
        OnSetup();
    }

    // 파생 클래스에서 모델/이펙트 등 추가 세팅이 필요할 때 오버라이드
    protected virtual void OnSetup() { }
}
