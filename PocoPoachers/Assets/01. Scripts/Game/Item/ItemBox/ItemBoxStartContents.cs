using System.Collections.Generic;
using UnityEngine;

// 씬에 직접 배치한 상자에 지정한 아이템을 미리 채워 넣는다.
// 상자 내용물은 ItemSpawner가 BoxLootTable을 굴려서 Initialize로 넣어주는 구조라,
// 스포너를 거치지 않고 씬에 놓인 상자는 빈 채로 남는다. 그 몫을 대신하는 컴포넌트.
//
// BoxLootTable은 타입별 확률로만 뽑아서 "이 아이템을 정확히" 지정할 수 없다 — 그래서 여기서 직접 넣는다.
// ItemSpawner.SpawnInitBoxes와 동일하게 호스트에서만 채운다(솔로 플레이도 호스트다).
[RequireComponent(typeof(ItemBox))]
public class ItemBoxStartContents : MonoBehaviour
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

    private void Start()
    {
        if (!RoomManager.IsHost) return;
        if (_items.Count == 0) return;

        var ids = new List<int>();
        var counts = new List<int>();
        var uids = new List<int>();

        foreach (var entry in _items)
        {
            if (entry == null || entry.itemId == 0) continue;

            if (ItemTable.Instance.Get(entry.itemId) == null)
            {
                Debug.LogWarning($"[ItemBoxStartContents] 아이템 테이블에 없는 ID입니다 (id={entry.itemId}, {name}).");
                continue;
            }

            ids.Add(entry.itemId);
            counts.Add(Mathf.Max(1, entry.count));

            // uid는 내구도/강화를 개체별로 추적하는 값이라 0으로 두면 안 된다
            uids.Add(ItemSpawner.AssignItemUid(entry.itemId));
        }

        if (ids.Count == 0) return;

        GetComponent<ItemBox>().Initialize(ids.ToArray(), counts.ToArray(), uids.ToArray(), skipReveal: _skipReveal);
    }
}
