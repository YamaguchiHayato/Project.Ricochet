using UnityEngine;

//ボス。
public class BossEnemy:ChaserEnemy
{
    //体力がこの割合以下になったら強化状態になる。
    const float EnragedHealthRatio = 0.5f;

    //強化状態での発射間隔の倍率。
    const float EnragedIntervalMultiplier = 0.5f;

    //強化状態かどうか。
    bool IsEnraged => (float)CurrentHealth / MaxHealth <= EnragedHealthRatio;

    //発射間隔を上書きする。
    protected override float CurrentFireIntervalSeconds
        => IsEnraged ? m_data.FireIntervalSeconds * EnragedIntervalMultiplier
        : m_data.FireIntervalSeconds;
}