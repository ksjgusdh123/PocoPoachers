using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ItemBoxRevealUI : MonoBehaviour
{
    [SerializeField] float _revealInterval = 0.5f;
    List<ItemRevealCard> _cards;

    public void Open()
    {
        _cards = GetComponentsInChildren<ItemRevealCard>(true).ToList();
        StartCoroutine(RevealSequence());
    }

    IEnumerator RevealSequence()
    {
        foreach (var card in _cards)
        {
            if(card.isFlip) card.Reveal();
            yield return new WaitForSeconds(_revealInterval);
        }
    }
}
