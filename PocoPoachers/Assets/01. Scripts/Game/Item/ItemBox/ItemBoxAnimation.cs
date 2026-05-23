using UnityEngine;

public class ItemBoxAnimation : MonoBehaviour
{
    private Animator _animator;
    private static readonly int IsOpenHash = Animator.StringToHash("IsOpen");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void SetOpen(bool isOpen)
    {
        _animator.SetBool(IsOpenHash, isOpen);
    }
}
