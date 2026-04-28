using UnityEngine;

public class SingleGun : GunBase
{
    protected override void Shoot()
    {
        Vector3 direction = GetAimDirection();
        Bullet bullet = BulletPool.GetInstance().Get(_gunData.bulletPrefab, _muzzle.position, Quaternion.LookRotation(direction));
        bullet.Initialize(
            _gunData.bulletSpeed,
            _gunData.damage,
            _gunData.range,
            direction,
            () => BulletPool.GetInstance().Release(_gunData.bulletPrefab, bullet)
        );
    }
}
