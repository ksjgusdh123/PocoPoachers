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

        for (int i = 0; i < slots.Length; ++i)
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
