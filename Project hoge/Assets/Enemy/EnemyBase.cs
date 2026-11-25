using System;
using UnityEngine;

//敵の基底クラス。敵の共通処理をまとめる。
public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    //敵が倒されたときに通知されるイベント。
    public static event Action<EnemyBase> Died;

    //Playerを探すためのタグ。
    const string PlayerTag = "Player";

    //敵のパラメーター。
    [SerializeField] protected EnemyData m_data;

    //弾の発射位置。
    [SerializeField] Transform m_firePoint;

    //撃つ弾のプレハブ。
    [SerializeField] EnemyBullet m_bulletPrefab;

    //射線を遮るものととして扱うレイヤー。
    [SerializeField] LayerMask m_obstacleMask;

    //狙う対象の位置情報。
    protected Transform m_playerTransform;

    //現在の体力。
    int m_currentHealth;

    //前回撃ってからの経過時間。
    float m_fireTimerSeconds;

    //外部から体力を取得するためのプロパティ。
    public int CurrentHealth => m_currentHealth;
    public int MaxHealth => m_data.MaxHealth;

    //発射間隔。
    protected virtual float CurrentFireIntervalSeconds => m_data.FireIntervalSeconds;

    protected virtual void Start()
    {
        //体力を最大にして開始。
        m_currentHealth = m_data.MaxHealth;

        //Playerを探す。
        var playerObject = GameObject.FindWithTag(PlayerTag);
        if (playerObject != null) m_playerTransform = playerObject.transform;
    }

    protected virtual void Update()
    {
        //Playerがいない間は何もしない。
        if (m_playerTransform == null) return;

        AimAtPlayer();
        OnUpdateBehavior();
        UpdateFireTimer();
    }

    //種類ごとのフレーム処理、
    protected virtual void OnUpdateBehavior() { }

    //弾を打てるかどうかを判定する。
    protected virtual bool CanFire() => true;

    //銃口からPlayerまでの間に障害物がないかを検知する。
    protected bool HasLineOfSight()
    {
        Vector3 origin = m_firePoint.position;
        Vector3 toPlayer = m_playerTransform.position - origin;

        //障害物のレイヤーが当たったら射線が遮られてる。
        bool isBlocked = Physics.Raycast(origin, toPlayer.normalized, toPlayer.magnitude,
                                       m_obstacleMask, QueryTriggerInteraction.Ignore);

        return !isBlocked;
    }

    //発射間隔。
    void UpdateFireTimer()
    {
        m_fireTimerSeconds += Time.deltaTime;

        //まだ時間が経っていない場合は何もしない。
        if (m_fireTimerSeconds < CurrentFireIntervalSeconds || !CanFire()) return;

        m_fireTimerSeconds = 0f;
        Fire();
    }

    //playerの方向を向く。
    void AimAtPlayer()
    {
        Vector3 directionToPlayer = m_playerTransform.position - transform.position;

        //水平方向だけ。
        directionToPlayer.y = 0f;

        //ほぼ同じ位置にいるときは向かない。
        if (directionToPlayer.sqrMagnitude < 0.001f) return;

        transform.rotation = Quaternion.LookRotation(directionToPlayer);
    }

    //銃口の位置向きで弾生成し、速度を与える。
    void Fire()
    {
        //発射方向を計算。
        Vector3 fireDirection = m_playerTransform.position - m_firePoint.position;
        fireDirection.y = 0f;
        fireDirection.Normalize();

        //水平方向を向いた回転を作成。
        Quaternion bulletRotation = Quaternion.LookRotation(fireDirection);

        //生成して初期化。
        EnemyBullet bullet = Instantiate(m_bulletPrefab, m_firePoint.position, bulletRotation);
        bullet.Initialize(m_data.BulletSpeed, m_data.BulletDamage);
    }

    //ダメージを受ける。
    public void TakeDamage(int damageAmount)
    {
        m_currentHealth -= damageAmount;
        if (m_currentHealth <= 0) Die();
    }

    //倒されたときの処理。
    void Die()
    {
        Died?.Invoke(this);
        Destroy(gameObject);
    }
}