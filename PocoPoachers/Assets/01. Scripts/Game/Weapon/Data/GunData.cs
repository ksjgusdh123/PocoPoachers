using UnityEngine;

public enum FireMode { Single, Auto }

[CreateAssetMenu(fileName = "GunData", menuName = "Weapon/GunData")]
public class GunData : ScriptableObject
{
    [Header("기본 스펙")]
    public float damage = 10f;
    public float fireRate = 1f;
    public int magazineSize = 30;
    public float reloadTime = 2f;

    [Header("탄환")]
    public float bulletSpeed = 20f;
    public float range = 50f;
    public GameObject bulletPrefab;

    [Header("발사 모드")]
    public FireMode fireMode = FireMode.Single;
}
