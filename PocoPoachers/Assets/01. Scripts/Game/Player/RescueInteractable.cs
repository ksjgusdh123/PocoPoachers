using UnityEngine;

// 구출(다운) 상태가 된 플레이어 오브젝트에 부착 — 다른 플레이어가 F로 상호작용하면 구출 로그를 남긴다
// 기존 상호작용 흐름(PlayerController.Interaction → IInteractable.OnInteract)을 그대로 사용
public class RescueInteractable : MonoBehaviour, IInteractable
{
    public bool IsDowned { get; private set; }

    public void SetDowned(bool downed) => IsDowned = downed;

    // 대상 오브젝트를 구출 상태로 표시 (없으면 컴포넌트 추가)
    public static RescueInteractable MarkDowned(GameObject go)
    {
        if (!go.TryGetComponent<RescueInteractable>(out var rescue))
            rescue = go.AddComponent<RescueInteractable>();
        rescue.SetDowned(true);
        return rescue;
    }

    public void OnInteract(PlayerController player)
    {
        if (!IsDowned) return;
        Debug.Log("구출중");
    }

    public void OnInteractExit(PlayerController player) { }
}
