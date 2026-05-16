using UnityEngine;

public class ShotgunGun : GunBase
{
    protected override void Shoot()
    {
        for (int i = 0; i < _gunData.bulletsPerShot; i++)
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

            BroadcastShoot(_muzzle.position, fireDir);
        }
    }
}

