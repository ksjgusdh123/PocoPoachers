using UnityEngine;

// 씬에 직접 배치한 적에게 시작 장비를 입힌다.
// 적의 장비는 프리팹이 아니라 EnemySpawner가 스폰 직후 코드로 입혀주는 구조라,
// 스포너를 거치지 않고 씬에 놓인 적은 맨손으로 남는다. 그 몫을 대신하는 컴포넌트.
//
// EnemySpawner.SpawnAll과 동일하게 호스트에서만 장착한다(솔로 플레이도 호스트다).
public class EnemyStartEquipment : MonoBehaviour
{
    [Tooltip("장착할 무기의 Item ID (200번대). 0이면 안 들림")]
    [SerializeField] private int _gunItemId;

    [Tooltip("장착할 헬멧의 Item ID (400번대). 0이면 안 씌움")]
    [SerializeField] private int _helmetItemId;

    private void Start()
    {
        if (!RoomManager.IsHost) return;

        EquipGun();
        EquipHelmet();
    }

    private void EquipGun()
    {
        if (_gunItemId == 0) return;

        var weaponController = GetComponent<AIWeaponController>();
        if (weaponController == null)
        {
            Debug.LogWarning($"[EnemyStartEquipment] AIWeaponController가 없어 무기를 못 들립니다 ({name}).");
            return;
        }

        weaponController.EquipGun(_gunItemId);
    }

    private void EquipHelmet()
    {
        if (_helmetItemId == 0) return;

        var armorController = GetComponent<ArmorController>();
        if (armorController == null)
        {
            Debug.LogWarning($"[EnemyStartEquipment] ArmorController가 없어 헬멧을 못 씌웁니다 ({name}).");
            return;
        }

        var itemData = ItemTable.Instance.Get(_helmetItemId);
        if (itemData == null)
        {
            Debug.LogWarning($"[EnemyStartEquipment] 아이템 테이블에 없는 헬멧 ID입니다 (id={_helmetItemId}, {name}).");
            return;
        }

        // uid는 강화/내구도를 개체별로 추적하는 값이라 0으로 두면 안 된다
        armorController.Equip(itemData, 0, ItemSpawner.AssignItemUid(itemData.id));
    }
}
