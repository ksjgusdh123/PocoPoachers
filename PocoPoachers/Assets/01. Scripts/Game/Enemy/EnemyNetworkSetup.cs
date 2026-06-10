using UnityEngine;
using UnityEngine.AI;
using Unity.Behavior;

public class EnemyNetworkSetup : MonoBehaviour
{
    private void Awake()
    {
        if (RoomManager.IsHost) return;

        var nav = GetComponent<NavMeshAgent>();
        if (nav != null) nav.enabled = false;

        var behavior = GetComponent<BehaviorGraphAgent>();
        if (behavior != null) behavior.enabled = false;

        var targetDetector = GetComponent<TargetDetector>();
        if (targetDetector != null) targetDetector.enabled = false;

        var weaponController = GetComponent<AIWeaponController>();
        if (weaponController != null) weaponController.enabled = false;

        var rotator = GetComponent<AIRotator>();
        if (rotator != null) rotator.enabled = false;

        var soundDetector = GetComponent<SoundDetector>();
        if (soundDetector != null) soundDetector.enabled = false;

        var dropper = GetComponent<EnemyItemBoxDropper>();
        if (dropper != null) dropper.enabled = false;
    }
}
