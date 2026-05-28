using UnityEngine;

public class ArmorBase : MonoBehaviour
{
    [SerializeField] private ArmorData _armorData;
    public ArmorData ArmorData => _armorData;
}
