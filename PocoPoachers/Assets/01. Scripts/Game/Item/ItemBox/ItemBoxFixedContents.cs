using System.Collections.Generic;
using UnityEngine;

// 고정 지급 목록. ItemSpawner가 호스트에서 호출하며 자체 Start 초기화는 하지 않는다.
[RequireComponent(typeof(ItemBox))]
[DisallowMultipleComponent]
[AddComponentMenu("Item Box/Fixed Contents (고정 내용물)")]
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, "ItemBoxStartContents")]
public class ItemBoxFixedContents : ItemBoxContents
{
    [System.Serializable]
    public class Entry
    {
        [Tooltip("Item ID — item.csv 참고 (소비 100번대, 무기 200번대, 헬멧 400번대, 탄약 600번대)")]
        public int itemId;

        [Min(1)] public int count = 1;
    }

    [SerializeField] private List<Entry> _items = new();

    [Tooltip("켜면 카드 뒤집기 연출 없이 처음부터 내용물이 보인다")]
    [SerializeField] private bool _skipReveal;

    public void AppendContents(List<int> ids, List<int> counts, List<int> uids, HashSet<int> noReveal)
    {
        if (!RoomManager.IsHost) return;
        if (_items.Count == 0) return;

        foreach (var entry in _items)
        {
            if (entry == null || entry.itemId <= 0) continue;

            if (ItemTable.Instance.Get(entry.itemId) == null)
            {
                Debug.LogWarning($"[ItemBoxFixedContents] 아이템 테이블에 없는 ID입니다 (id={entry.itemId}, {name}).");
                continue;
            }

            if (_skipReveal) noReveal.Add(ids.Count);
            ids.Add(entry.itemId);
            counts.Add(Mathf.Max(1, entry.count));

            // uid는 내구도/강화를 개체별로 추적하는 값이라 0으로 두면 안 된다
            uids.Add(ItemSpawner.AssignItemUid(entry.itemId));
        }

    }
}
