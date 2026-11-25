using UnityEngine;

//敵の弾の挙動を制御するクラス。
[RequireComponent(typeof(Rigidbody))]
public class EnemyBullet : MonoBehaviour
{
    //当たったオブジェクトのタグを格納する変数。
    const string PlayerTag = "Player";

    //弾が消えるまでの時間。
    [SerializeField] float m_lifetimeSeconds = 5f;
    //壁などで跳ね返ってよい回数。
    [SerializeField] int m_maxBounceCount = 0;

    //Playerに当たった時に与えるダメージ。
    int m_damageAmount;

    //これまでに何かに当たった回数。
    int m_bounceCount;

    Rigidbody m_rigidbody;

    private void Awake()
    {
        //Rigidbodyコンポーネントを取得する。
        m_rigidbody = GetComponent<Rigidbody>();
    }

    void Start()
    {
        //生成から一定時間後に弾を消す。
        Destroy(gameObject, m_lifetimeSeconds);
    }

    //発射時に敵から呼ばれる。速度とダメージを設定する。
    public void Initialize(float speed,int damageamount)
    {
        m_damageAmount=damageamount;
        //弾の正面咆哮へ速度を加える。
        m_rigidbody.linearVelocity = transform.forward * speed;
    }

    void OnCollisionEnter(Collision collision)
    {
        //Playerに当たったらダメージを与えて弾を消す。
        if (collision.gameObject.CompareTag(PlayerTag)
            &&collision.gameObject.TryGetComponent<IDamageable>(out var target))
        {
            target.TakeDamage(m_damageAmount);
            Destroy(gameObject);
            return;
        }

        //壁などの場合は跳ね返れる回数を超えたら消える。
        m_bounceCount++;
        if (m_bounceCount > m_maxBounceCount) Destroy(gameObject);
    }
}