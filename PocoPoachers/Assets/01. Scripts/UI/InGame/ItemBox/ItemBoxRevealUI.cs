using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ItemBoxRevealUI : MonoBehaviour
{
    [SerializeField] float _revealInterval = 0.5f;
    List<ItemRevealCard> _cards;

    public void Open(Inventory inven)
    {
        // 이전 상자의 리빌이 끝나기 전에 다시 열렸다면 남은 순번을 버린다(_cards가 갈아끼워지므로).
        StopAllCoroutines();

        _cards = GetComponentsInChildren<ItemRevealCard>(true).ToList();

        // 현재 용량 밖 슬롯의 카드는 InventoryUI가 비활성으로 두어 아이콘이 이전 상자 것 그대로 남아 있다.
        // 그대로 판정하면 아이템이 있는 줄 알고 보이지도 않는 카드가 리빌 순번을 차지하므로 보이는 칸까지만 본다.
        // (미리 배치된 카드보다 용량이 클 수도 있어 카드 수로도 한 번 더 자른다)
        int count = Mathf.Min(inven.CurrentCapacity, _cards.Count);
        for (int i = 0; i < count; ++i)
            _cards[i].CheckSlotState(inven.Slots[i] as BoxItemSlot);
        for (int i = count; i < _cards.Count; ++i)
            _cards[i].ResetCard();

        UIManager.GetInstance().Show(UIType.ItemBoxReveal);
        StartCoroutine(RevealSequence(count));
    }

    IEnumerator RevealSequence(int count)
    {
        for (int i = 0; i < count; ++i)
        {
            if (!_cards[i].isFlip) continue;

            _cards[i].Reveal();
            yield return new WaitForSeconds(_revealInterval);
        }
    }
}
