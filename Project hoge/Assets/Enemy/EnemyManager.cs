using System;
using UnityEngine;

//シーン内の敵の生存数を管理する。
public class EnemyManager: MonoBehaviour
{
    //全ての敵が倒された時に通知されるイベント。
    public event Action AllEnemiesDefeated;

    //現在生きている敵の数。
    int m_aliveEnemyCount;

    void Start()
    {
        //シーン開始時に、配置されている敵の数を数える。
        m_aliveEnemyCount = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None).Length;
    }

    //無効・破棄されたときに受け取りをやめる。
    void OnDisable()
    {
        EnemyBase.Died -= HandleEnemyDied;
    }
     
    //敵が1体倒されるたびに呼ばれる。
    void HandleEnemyDied(EnemyBase defeatedEnemy)
    {
        m_aliveEnemyCount--;

        //全滅したら通知する。
        if(m_aliveEnemyCount<=0)
        {
            AllEnemiesDefeated?.Invoke();
        }
    }
}