using UnityEngine;


namespace nsApp
{
    namespace nsActor
    {
        namespace nsPlayer
        {
            /// <summary>
            /// 弾丸の見た目を制御するインターフェース。
            /// </summary>
            public interface IBulletView
            {
                Vector3 Position { get; } //! 弾丸の座標。
                float Radius { get; } //! ワールド空間での半径。

                /// <summary>
                ///　弾丸の座標と進行方向を見た目に反映する。
                /// </summary>
                /// <param name="position">The position at which to apply the operation.</param>
                /// <param name="direction">The direction in which to apply the operation.</param>
                void Apply(Vector3 position, Vector3 direction);
            }


            /// <summary>
            /// 弾丸の見た目を制御するクラス。
            /// </summary>
            /// <remarks>
            /// このスクリプトはMVPパターンとして運用中。
            /// </remarks>
            /// <author>Yamaguchi Hayato</author>
            /// <created>2026-11-11</created>
            [RequireComponent(typeof(Rigidbody))]
            [RequireComponent(typeof(SphereCollider))]
            public class BulletView : MonoBehaviour, IBulletView
            {
                public Vector3 Position => transform.position; //! 弾丸の座標。
                private const float MinScale = 0.001f; //! 半径の計算に使うスケールの下限。
                private SphereCollider _sphere; //! 弾丸の当たり判定。半径の取得に使う。


                /// <summary>
                /// 物理の反発は使わず、Presenter が座標を書き込む。
                /// </summary>
                private void Awake()
                {
                    // Rigidbody の重力と反発を無効化する。Presenter が座標を書き込むので、物理の反発は使わない。
                    Rigidbody body = GetComponent<Rigidbody>();
                    body.useGravity = false;
                    body.isKinematic = true;
                    body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;

                    // 弾丸の当たり判定を無効化する。判定は Model の SphereCast に任せる。
                    _sphere = GetComponent<SphereCollider>();
                    _sphere.enabled = false;
                }


                /// <summary>
                /// 弾丸のワールド空間での半径を返す。
                /// </summary>
                public float Radius
                {
                    // ワールド空間での半径を返す。スケールが小さすぎると SphereCast が無効になるので、下限を設ける。
                    get
                    {
                        // 無効化したコライダーからも、ローカル半径は読める。
                        SphereCollider sphere = _sphere != null ? _sphere : GetComponent<SphereCollider>();
                        Vector3 scale = transform.localScale;
                        float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));

                        // スケールが小さすぎると、SphereCast が無効になるので、下限を設ける。
                        if (maxScale < MinScale)
                            maxScale = 1.0f;

                        // ローカル半径にスケールを掛けて、ワールド空間での半径を返す。
                        return sphere.radius * maxScale;
                    }
                }


                /// <summary>
                /// 判断が決めた座標と進行方向を、見た目へ反映する。
                /// </summary>
                /// <param name="position">弾丸の座標。</param>
                /// <param name="direction">弾丸の進行方向。</param>
                public void Apply(Vector3 position, Vector3 direction)
                {
                    // 弾丸の座標を反映する。
                    transform.position = position;
                    // 弾丸の進行方向を反映する。Y成分を0にして水平方向のみに制限する。
                    direction.y = 0.0f;
                    if (direction.sqrMagnitude < 0.001f)
                        return;

                    // 方向ベクトルから回転を求め、弾丸の向きを反映する。Quaternion.LookRotationを使って方向ベクトルから回転を求める。
                    transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                }
            }
        }
    }
}