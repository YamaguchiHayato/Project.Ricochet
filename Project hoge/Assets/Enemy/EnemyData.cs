using UnityEngine;

//敵のパラメータ。
[CreateAssetMenu(fileName = "EnemyData", menuName = "Game/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("体力")]
    [SerializeField] int m_maxHealth = 1;

    [Header("射程")]
    [SerializeField] float m_fireIntervalSeconds = 2f;
    [SerializeField] float m_bulletSpeed = 6f;
    [SerializeField] int m_bulletDamage = 1;

    [Header("移動")]
    [SerializeField] float m_moveSpeed = 2f;
    [SerializeField] float m_fireRange = 8f;

    //Playerの手前で止まる距離。0だとPlayerに重なるまで近づく。
    [SerializeField] float m_stopDistance = 3f;

    /*外部から読み取るためのプロパティ*/
    public int MaxHealth => m_maxHealth;
    public float FireIntervalSeconds => m_fireIntervalSeconds;
    public float BulletSpeed => m_bulletSpeed;
    public int BulletDamage => m_bulletDamage;
    public float MoveSpeed => m_moveSpeed;
    public float FireRange => m_fireRange;
    public float StopDistance => m_stopDistance;
}

