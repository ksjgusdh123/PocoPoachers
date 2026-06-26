using UnityEngine;

public class ShotgunGun : GunBase
{
    protected override void Shoot()
    {
        for (int i = 0; i < _stat.PelletCount; i++)
        {
            Vector3 fireDir = GetFireDirection();

            Bullet bullet = BulletPool.GetInstance().Get(_bulletPrefab, _muzzle.position, _muzzle.rotation);
            bullet.Initialize(
                _stat.BulletSpeed,
                _stat.Damage,
                _stat.BulletRange,
                fireDir,
                () => BulletPool.GetInstance().Release(_bulletPrefab, bullet),
                Owner,
                _stat.MuzzleColor
            );

            RoomSync.Shoot(_muzzle.position, fireDir, _stat);
        }
    }
}
