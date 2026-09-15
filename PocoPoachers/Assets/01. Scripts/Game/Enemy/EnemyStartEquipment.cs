using System.Collections.Generic;
using UnityEngine;

// 스포너와 씬에 직접 배치한 적 모두 CSV의 장비 규칙을 사용한다.
public class EnemyStartEquipment : MonoBehaviour
{
    private bool _equipped;

    private void Start() => EquipFromTable();

    public void EquipFromTable()
    {
        if (!RoomManager.IsHost || _equipped) return;
        var stat = GetComponent<EnemyStat>();
        var data = stat != null ? EnemyTable.Instance.Get(stat.EnemyId) : null;
        if (data == null)
        {
            Debug.LogWarning($"[EnemyStartEquipment] 적 CSV 데이터가 없습니다 ({name}).");
            return;
        }
        _equipped = true;

        int gunId = PickItem(data.GunItemIds, ItemType.Weapon, data.Id);
        if (gunId != 0) GetComponent<AIWeaponController>()?.EquipGun(gunId);

        // 확률 0에서는 절대 장착하지 않고, 1에서는 항상 장착한다.
        float chance = Mathf.Clamp01(data.HelmetSpawnChance);
        if (chance <= 0f || (chance < 1f && Random.value >= chance)) return;
        int helmetId = PickItem(data.HelmetItemIds, ItemType.Helmet, data.Id);
        if (helmetId != 0)
            GetComponent<ArmorController>()?.Equip(ItemTable.Instance.Get(helmetId), 0,
                ItemSpawner.AssignItemUid(helmetId));
    }

    private static int PickItem(string candidates, ItemType type, int enemyId)
    {
        if (string.IsNullOrWhiteSpace(candidates)) return 0;
        var validIds = new List<int>();
        foreach (string token in candidates.Split(';'))
        {
            if (string.IsNullOrWhiteSpace(token)) continue;
            if (!int.TryParse(token.Trim(), out int id) || ItemTable.Instance.Get(id)?.Type != type)
            {
                Debug.LogWarning($"[EnemyStartEquipment] 잘못된 장비 ID: 적={enemyId}, 종류={type}, 값={token}");
                continue;
            }
            if (!validIds.Contains(id)) validIds.Add(id);
        }
        return validIds.Count > 0 ? validIds[Random.Range(0, validIds.Count)] : 0;
    }
}
