using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private EnemyDataSO enemyData;
    [SerializeField, Min(0.01f)] private float hitFlashDuration = 0.15f;
    [SerializeField, HideInInspector] private int maxHealth = 3;
    private SpriteRenderer[] sprites;
    private float hitFlashUntil;
    private bool isFlashing;
    private CombatPool pool;
    private Collider2D[] colliders;
    private bool[] initialColliderStates;
    private Rigidbody2D rb;

    public void SetPool(CombatPool value) => pool = value;
    public int CurrentHealth { get; private set; }
    public EnemyDataSO Data => enemyData;
    public int MaxHealth => enemyData != null ? enemyData.MaxHP : Mathf.Max(1, maxHealth);
    public float MoveSpeed => enemyData != null ? enemyData.MoveSpeed : 0f;
    public float DetectRange => enemyData != null ? enemyData.DetectRange : 0f;
    public int Damage => enemyData != null ? enemyData.Damage : 0;
    public int ScoreReward => enemyData != null ? enemyData.ScoreReward : 0;
    public event System.Action<EnemyHealth> Died;

    private void Awake()
    {
        // Current health belongs to this instance, never to the shared asset.
        CurrentHealth = MaxHealth;
        sprites = GetComponentsInChildren<SpriteRenderer>(true);
        colliders = GetComponentsInChildren<Collider2D>(true);
        initialColliderStates = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
            initialColliderStates[i] = colliders[i].enabled;
        rb = GetComponent<Rigidbody2D>();
        SetColor(Color.white);
    }

    private void OnEnable()
    {
        CurrentHealth = MaxHealth;
        isFlashing = false;
        hitFlashUntil = 0f;
        SetColor(Color.white);
        for (int i = 0; i < colliders.Length; i++)
            if (colliders[i] != null)
                colliders[i].enabled = initialColliderStates[i];
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private void LateUpdate()
    {
        if (!isFlashing)
            return;

        if (Time.time >= hitFlashUntil)
        {
            isFlashing = false;
            SetColor(Color.white);
        }
        else
        {
            SetColor(enemyData != null ? enemyData.Color : Color.red);
        }
    }

    private void OnDisable()
    {
        isFlashing = false;
        SetColor(Color.white);
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private void SetColor(Color color)
    {
        if (sprites == null)
            return;

        foreach (var sprite in sprites)
            if (sprite != null)
                sprite.color = color;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || CurrentHealth <= 0)
            return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        // Consecutive hits restart the flash without overlapping timers.
        hitFlashUntil = Time.time + Mathf.Max(0.01f, hitFlashDuration);
        isFlashing = true;
        SetColor(enemyData != null ? enemyData.Color : Color.red);
        if (CurrentHealth == 0)
        {
            foreach (var collider in GetComponentsInChildren<Collider2D>())
                collider.enabled = false;
            Died?.Invoke(this);
            if (pool != null) pool.ReturnEnemy(this);
            else gameObject.SetActive(false);
        }
    }
}
