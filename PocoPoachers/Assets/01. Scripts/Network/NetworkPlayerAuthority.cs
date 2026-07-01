using UnityEngine;

public static class NetworkPlayerAuthority
{
    public static bool TryGetGuestGun(int guestId, out GunBase gun)
    {
        gun = null;
        if (!ObjectManager.Instance.TryGet(ObjectKind.Player, guestId, out var worldObj))
            return false;

        var mount = worldObj.GetComponent<WeaponMount>();
        if (mount == null) return false;

        for (int i = 0; i < 2; i++)
        {
            var g = mount.GetGun(i);
            if (g != null)
            {
                gun = g;
                return true;
            }
        }

        return false;
    }

    public static bool GuestOwnsItemUid(int guestId, int itemUid)
    {
        if (itemUid == 0) return false;
        if (!ObjectManager.Instance.TryGet(ObjectKind.Player, guestId, out var worldObj))
            return false;

        var weaponMount = worldObj.GetComponent<WeaponMount>();
        if (weaponMount != null)
        {
            for (int i = 0; i < 2; i++)
            {
                var gun = weaponMount.GetGun(i);
                if (gun != null && gun.Uid == itemUid) return true;
            }
        }

        var armor = worldObj.GetComponent<ArmorMount>()?.GetArmor();
        if (armor != null && armor.Uid == itemUid) return true;

        return false;
    }

    public static float SanitizeGuestHp(StatBase stat, float hp, float maxHp)
    {
        maxHp = Mathf.Max(maxHp, 1f);
        hp = Mathf.Clamp(hp, 0f, maxHp);

        if (stat == null) return hp;

        const float maxHealDelta = 100f;
        if (hp > stat.CurrentHp + maxHealDelta)
            hp = stat.CurrentHp;

        return hp;
    }
}
