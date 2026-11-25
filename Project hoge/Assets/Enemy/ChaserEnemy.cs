using UnityEngine;
using UnityEngine.AI;

//追従処理。Playerに向かって移動して射程に入ったら弾を撃つ。
[RequireComponent(typeof(NavMeshAgent))]
public class ChaserEnemy:EnemyBase
{
    //壁を避けて移動するためのコンポーネント。
    NavMeshAgent m_navMeshAgent;

    protected override void Start()
    {
        //EnemyBase側の初期化。
        base.Start();

        m_navMeshAgent = GetComponent<NavMeshAgent>();

        //移動速度はEnemyDataで管理。
        m_navMeshAgent.speed = m_data.MoveSpeed;

        //Playerの手前で止まる距離をEnemyDataから設定する。
        m_navMeshAgent.stoppingDistance = m_data.StopDistance;

        //向きの制御はEnemyBaseのAimAtPlayerにさせる。
        //ナビメッシュにも回転させると向きが取り合いになるため切る。
        m_navMeshAgent.updateRotation = false;
    }

    //毎フレームPlayerの位置情報を目的地にして追いかける。
    protected override void OnUpdateBehavior()
    {
        m_navMeshAgent.SetDestination(m_playerTransform.position);
    }

    //Playerとの距離が射程内で、かつ射線が通ってるときだけ撃つ。
    protected override bool CanFire()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, m_playerTransform.position);
        return distanceToPlayer <= m_data.FireRange&&HasLineOfSight();
    }
}
