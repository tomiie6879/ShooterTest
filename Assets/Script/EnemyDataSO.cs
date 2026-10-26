using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Shooter/Enemy Data")]
public class EnemyDataSO : ScriptableObject
{
    [SerializeField] private int maxHP = 3;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float detectRange = 5f;
    [SerializeField] private int damage = 1;
    [SerializeField] private int scoreReward = 10;
    [Tooltip("Hit flash color. Enemies are white when not taking damage.")]
    [SerializeField] private Color color = UnityEngine.Color.red;

    public int MaxHP => Mathf.Max(1, maxHP);
    public float MoveSpeed => Mathf.Max(0f, moveSpeed);
    public float DetectRange => Mathf.Max(0f, detectRange);
    public int Damage => Mathf.Max(0, damage);
    public int ScoreReward => Mathf.Max(0, scoreReward);
    public Color Color => color;
}
