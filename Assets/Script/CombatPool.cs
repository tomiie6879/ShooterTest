using System.Collections.Generic;
using UnityEngine;

// All allocations happen in Awake. An exhausted pool returns null instead of growing.
[DefaultExecutionOrder(-100)]
public class CombatPool : MonoBehaviour
{
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private EnemyHealth enemyPrefab;
    [SerializeField] private int bulletCapacity = 64;
    [SerializeField] private int enemyCapacity = 16;

    private readonly Queue<Bullet> bullets = new Queue<Bullet>();
    private readonly Queue<EnemyHealth> enemies = new Queue<EnemyHealth>();
    private readonly HashSet<Bullet> activeBullets = new HashSet<Bullet>();
    private readonly HashSet<EnemyHealth> activeEnemies = new HashSet<EnemyHealth>();
    private Transform storage;
    private Transform activeRoot;

    public int AvailableBullets => bullets.Count;
    public int AvailableEnemies => enemies.Count;

    private void Awake()
    {
        var activeContainer = new GameObject("Active");
        activeContainer.transform.SetParent(transform, false);
        activeRoot = activeContainer.transform;

        var container = new GameObject("Inactive");
        container.transform.SetParent(transform, false);
        container.SetActive(false);
        storage = container.transform;

        if (bulletPrefab != null)
            for (int i = 0; i < Mathf.Max(1, bulletCapacity); i++)
            {
                var bullet = Instantiate(bulletPrefab, storage);
                bullet.gameObject.SetActive(false);
                bullet.SetPool(this);
                bullets.Enqueue(bullet);
            }

        // Register the enemies already placed by the designer without replacing them.
        var placedEnemies = FindObjectsByType<EnemyHealth>();
        foreach (var enemy in placedEnemies)
        {
            enemy.transform.SetParent(activeRoot, true);
            enemy.SetPool(this);
            activeEnemies.Add(enemy);
        }

        if (enemyPrefab != null)
            for (int i = placedEnemies.Length; i < Mathf.Max(1, enemyCapacity); i++)
            {
                var enemy = Instantiate(enemyPrefab, storage);
                enemy.gameObject.SetActive(false);
                enemy.SetPool(this);
                enemies.Enqueue(enemy);
            }
    }

    public Bullet SpawnBullet(Vector3 position, Quaternion rotation)
    {
        if (bullets.Count == 0) return null;
        var bullet = bullets.Dequeue();
        activeBullets.Add(bullet);
        bullet.transform.SetParent(activeRoot, false);
        bullet.transform.SetPositionAndRotation(position, rotation);
        bullet.gameObject.SetActive(true);
        return bullet;
    }

    public EnemyHealth SpawnEnemy(Vector3 position, Quaternion rotation)
    {
        if (enemies.Count == 0) return null;
        var enemy = enemies.Dequeue();
        activeEnemies.Add(enemy);
        enemy.transform.SetParent(activeRoot, false);
        enemy.transform.SetPositionAndRotation(position, rotation);
        enemy.gameObject.SetActive(true);
        return enemy;
    }

    public void ReturnBullet(Bullet bullet)
    {
        if (bullet == null || !activeBullets.Remove(bullet)) return;
        bullet.gameObject.SetActive(false);
        bullet.transform.SetParent(storage, false);
        bullets.Enqueue(bullet);
    }

    public void ReturnEnemy(EnemyHealth enemy)
    {
        if (enemy == null || !activeEnemies.Remove(enemy)) return;
        enemy.gameObject.SetActive(false);
        enemy.transform.SetParent(storage, false);
        enemies.Enqueue(enemy);
    }
}
