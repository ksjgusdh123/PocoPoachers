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
                PacketSender.CShootReq(
                    _muzzle.position,
                    fireDir,
                    _gunData.bulletSpeed,
                    _gunData.damage,
                    _gunData.range);
            }
        }
    }
}
