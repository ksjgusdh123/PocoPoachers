using UnityEngine;

public class SingleGun : GunBase
{
    protected override void Shoot()
    {
        Vector3 fireDir = GetFireDirection();

        Bullet bullet = BulletPool.GetInstance().Get(_gunData.bulletPrefab, _muzzle.position, _muzzle.rotation);
        bullet.Initialize(
            _gunData.bulletSpeed,
            _gunData.damage,
            _gunData.range,
            fireDir,
            () => BulletPool.GetInstance().Release(_gunData.bulletPrefab, bullet),
            Owner
        );

        RoomSync.Shoot(_muzzle.position, fireDir, _gunData);
    }
}
