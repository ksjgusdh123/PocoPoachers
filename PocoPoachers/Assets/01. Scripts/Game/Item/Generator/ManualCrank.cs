using UnityEngine;

public class ManualCrank : MonoBehaviour, IInteractable
{
    // F 연타마다 매번 충전되도록, 상호작용 즉시 스스로 종료해 PlayerController의 토글-닫힘 동작을 우회한다.
    public void OnInteract(PlayerController player)
    {
        Generator.Instance?.CrankCharge();
        CrankGaugeUI.Instance?.Show();
        player.EndInteraction(this);
    }

    public void OnInteractExit(PlayerController player)
    {
    }
}
