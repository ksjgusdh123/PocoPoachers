using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ItemBoxRevealUI : MonoBehaviour
{
    [SerializeField] float _revealInterval = 0.5f;
    List<ItemRevealCard> _cards;

    public void Open(Inventory inven)
    {
        _cards = GetComponentsInChildren<ItemRevealCard>(true).ToList();

        BoxItemSlot[] slots = inven.Slots.Cast<BoxItemSlot>().ToArray();

        // 슬롯 수가 미리 배치된 카드 수보다 많을 수 있음 (예: 상자 용량이 동적으로 늘어난 경우) — 넘치는 슬롯은 리빌 연출 없이 넘어감
        int count = Mathf.Min(slots.Length, _cards.Count);
        for (int i = 0; i < count; ++i)
        {
            _cards[i].CheckSlotState(slots[i]);
        }

        UIManager.GetInstance().Show(UIType.ItemBoxReveal);
        StartCoroutine(RevealSequence());
    }

    IEnumerator RevealSequence()
    {
        foreach (var card in _cards)
        {
            if (card.isFlip)
            {
                card.Reveal();
                yield return new WaitForSeconds(_revealInterval);
            }
        }
    }
}
