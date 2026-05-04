using UnityEngine;

public class ShotgunGun : GunBase
{
    protected override void Shoot()
    {
        var nm = NetworkManager.Instance;

        for (int i = 0; i < _gunData.bulletsPerShot; i++)
        {
            Vector3 fireDir = GetFireDirection();

            Bullet bullet = BulletPool.GetInstance().Get(_gunData.bulletPrefab, _muzzle.position, _muzzle.rotation);
            bullet.Initialize(
                _gunData.bulletSpeed,
                _gunData.damage,
                _gunData.range,
                fireDir,
                () => BulletPool.GetInstance().Release(_gunData.bulletPrefab, bullet)
            );

            if (nm != null && nm.IsLoggedIn)
            {
                var p = _muzzle.position;
                PacketBuilder.Send(new C_ShootReqT
                {
                    Origin      = new Vec3T { X = p.x,       Y = p.y,       Z = p.z },
                    Direction   = new Vec3T { X = fireDir.x,  Y = fireDir.y,  Z = fireDir.z },
                    BulletSpeed = _gunData.bulletSpeed,
                    Damage      = _gunData.damage,
                    MaxRange    = _gunData.range,
                }, C_ShootReq.Pack, PacketType.C_ShootReq);
            }
        }
    }
}
