using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Shooter/Weapon Data")]
public class WeaponDataSO : ScriptableObject
{
    [SerializeField, Min(1)] private int damage = 1;
    [Tooltip("Bullets fired per second.")]
    [SerializeField, Min(0.1f)] private float fireRate = 5f;
    [SerializeField, Min(0.1f)] private float bulletSpeed = 15f;
    [SerializeField, Min(0.01f)] private float bulletLifeTime = 3f;

    public int Damage => Mathf.Max(1, damage);
    public float FireRate => Mathf.Max(0.1f, fireRate);
    public float BulletSpeed => Mathf.Max(0.1f, bulletSpeed);
    public float BulletLifeTime => Mathf.Max(0.01f, bulletLifeTime);
}
