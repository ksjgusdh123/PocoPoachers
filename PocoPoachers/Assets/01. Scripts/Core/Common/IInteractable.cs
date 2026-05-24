/// <summary>
/// 플레이어가 상호작용할 수 있는 오브젝트 인터페이스.
/// 상호작용 오브젝트는 이 인터페이스를 구현하여 각자의 열기/닫기 로직을 정의한다.
/// </summary>
public interface IInteractable
{
    void OnInteract(PlayerController player);
    void OnInteractExit(PlayerController player);
}
