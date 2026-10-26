using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private int damage = 1;

    private Rigidbody2D rb;
    private Transform owner;
    private bool hasHit;
    private int shotDamage;
    private CombatPool pool;
    private float expiresAt;
    private bool launched;
    private Collider2D[] bulletColliders;
    private Collider2D[] ignoredColliders;

    public void SetPool(CombatPool value) => pool = value;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bulletColliders = GetComponents<Collider2D>();
    }

    private void OnEnable()
    {
        hasHit = false;
        launched = false;
        owner = null;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    private void OnDisable()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        if (ignoredColliders != null && bulletColliders != null)
            foreach (var ignored in ignoredColliders)
                foreach (var collider in bulletColliders)
                    if (ignored != null && collider != null)
                        Physics2D.IgnoreCollision(collider, ignored, false);
        ignoredColliders = null;
        owner = null;
        launched = false;
    }

    public void Launch(Vector2 direction, float speed, Transform shooter = null)
    {
        LaunchInternal(direction, speed, damage, lifeTime, shooter);
    }

    public void Launch(Vector2 direction, WeaponDataSO data, Transform shooter = null)
    {
        if (data == null)
            throw new System.ArgumentNullException(nameof(data));

        // Snapshot each shot; changing a shared asset cannot change bullets in flight.
        LaunchInternal(direction, data.BulletSpeed, data.Damage, data.BulletLifeTime, shooter);
    }

    private void LaunchInternal(Vector2 direction, float speed, int hitDamage,
        float lifetime, Transform shooter)
    {
        shotDamage = Mathf.Max(1, hitDamage);
        owner = shooter;
        if (owner != null)
        {
            ignoredColliders = owner.GetComponentsInChildren<Collider2D>();
            foreach (var ownCollider in ignoredColliders)
                foreach (var bulletCollider in bulletColliders)
                    Physics2D.IgnoreCollision(bulletCollider, ownCollider);
        }
        rb.linearVelocity = direction.normalized * speed;

        expiresAt = Time.time + Mathf.Max(0.01f, lifetime);
        launched = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Hit(other);
    }

    private void FixedUpdate()
    {
        if (!launched || hasHit) return;
        if (Time.time >= expiresAt)
        {
            ReturnToPool();
            return;
        }
        // Sweep the next step so small targets cannot be skipped at high speed.
        Vector2 step = rb.linearVelocity * Time.fixedDeltaTime;
        if (hasHit || step.sqrMagnitude < 0.000001f)
            return;

        foreach (var hit in Physics2D.RaycastAll(rb.position, step.normalized, step.magnitude))
        {
            if (Hit(hit.collider)) break;
        }
    }

    private bool Hit(Collider2D other)
    {
        if (!launched || hasHit || other == null || other.GetComponentInParent<Bullet>() != null ||
            (owner != null && other.transform.IsChildOf(owner)))
            return false;

        EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();
        // Ignore utility triggers; enemies may use either trigger or solid colliders.
        if (other.isTrigger && enemy == null)
            return false;

        hasHit = true;
        rb.linearVelocity = Vector2.zero;
        if (enemy != null)
            enemy.TakeDamage(shotDamage);
        ReturnToPool();
        return true;
    }

    private void ReturnToPool()
    {
        launched = false;
        if (pool != null) pool.ReturnBullet(this);
        else gameObject.SetActive(false);
    }
}
