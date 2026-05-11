using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "HasBullet", story: "[WeaponController] 의 총알이 있는지 확인", category: "Conditions", id: "578444d1a7aa5bef82fbee5b44f23d71")]
public partial class HasBulletCondition : Condition
{
    [SerializeReference] public BlackboardVariable<AIWeaponController> WeaponController;

    public override bool IsTrue()
    {
        if (WeaponController?.Value == null) return false;
        if (!WeaponController.Value.HasGun) return false;

        return WeaponController.Value.Gun.CurrentAmmo > 0;
    }
}
