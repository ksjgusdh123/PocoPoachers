using UnityEngine;

// 상호작용(F)으로 즉시 발동하는 이탈 지점. 목적지·연출 처리는 SceneExitBase가 담당한다.
public class ScenePortal : SceneExitBase, IInteractable
{
    public void OnInteract(PlayerController player) => Exit();

    public void OnInteractExit(PlayerController player) { }
}
