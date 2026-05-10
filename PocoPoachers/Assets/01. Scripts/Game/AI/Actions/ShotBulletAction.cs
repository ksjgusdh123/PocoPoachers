using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ShotBullet", story: "[WeaponController] Shot Bullet", category: "Action", id: "9301e66832f04d1eb33d5f64692d3903")]
public partial class ShotBulletAction : Action
{
    [SerializeReference] public BlackboardVariable<AIWeaponController> WeaponController;

    protected override Status OnStart()
    {
        if (WeaponController?.Value == null)
        {
            Debug.LogError("[ShotBulletAction] AIWeaponController가 블랙보드에 할당되지 않았습니다.");
            return Status.Failure;
        }

        if (!WeaponController.Value.HasGun) return Status.Failure;

        WeaponController.Value.TryShoot();
        return Status.Success;
    }
}
