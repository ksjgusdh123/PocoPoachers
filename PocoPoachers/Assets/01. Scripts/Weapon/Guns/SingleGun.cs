using UnityEngine;

public class SingleGun : GunBase
{
    protected override void Shoot()
    {
        Bullet bullet = BulletPool.GetInstance().Get(_gunData.bulletPrefab, _muzzle.position, _muzzle.rotation);
        bullet.Initialize(
            _gunData.bulletSpeed,
            _gunData.damage,
            _gunData.range,
            _muzzle.up,
            () => BulletPool.GetInstance().Release(_gunData.bulletPrefab, bullet)
        );

        var nm = NetworkManager.Instance;
        if (nm != null && nm.IsLoggedIn)
        {
            PacketSender.CShootReq(
                _muzzle.position,
                _muzzle.up.normalized,
                _gunData.bulletSpeed,
                _gunData.damage,
                _gunData.range);
        }
    }
}
