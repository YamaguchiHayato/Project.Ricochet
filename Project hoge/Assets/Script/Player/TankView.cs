using UnityEngine;

namespace nsApp
{
    namespace nsActor
    {
        namespace nsPlayer
        {
            /// <summary>
            /// タンクの見た目を制御するインターフェース。
            /// </summary>
            public interface ITankView
            {
                Vector3 HullPosition { get; } //! 車体の位置。
                float HullYaw { get; }  //! 車体のヨー角。
                void ApplyHull(Vector3 position, float yaw);
                void ApplyTurret(float yaw);
                void SpawnBullet(Vector3 direction);
            }

            /// <summary>
            ///  タンクの見た目を制御するクラス。
            /// </summary>
            [RequireComponent(typeof(Rigidbody))]
            public class TankView : MonoBehaviour, ITankView
            {
                [Header("Hull")]
                [SerializeField] private Rigidbody _hull; //! 車体のRigidbody。
                [Header("Turret")]
                [SerializeField] private Transform _turret; //! 砲塔。
                [SerializeField] private Transform _muzzle; //! 弾の出現位置。
                [Header("Bullet")]
                [SerializeField] private BulletPresenter _bulletPrefab; //! 発射する弾。
                public Vector3 HullPosition => _hull.position; //! 車体の位置。                public Vector3 MuzzlePosition => _muzzle.position; //! 弾の出現位置。
                public float HullYaw => _hull.rotation.eulerAngles.y; //! 車体のヨー角。
                private const float BarrelYawOffset = 180.0f; //! 砲塔のヨー角のオフセット。


                /// <summary>
                /// 砲塔の前方向を返す。Y成分は0にして水平方向のみに制限する。
                /// </summary>
                public Vector3 BarrelDirection
                {
                    // 砲塔の前方向を返す。
                    get
                    {
                        // 
                        Vector3 direction = _turret.forward;
                        direction.y = 0.0f;

                        // 
                        if (direction.sqrMagnitude < 0.001f)
                            return Vector3.forward;

                        // 方向ベクトルを正規化して返す。
                        return direction.normalized;
                    }
                }


                /// <summary>
                /// 車体の位置とヨー角を反映する。
                /// </summary>
                /// <param name="position"></param>
                /// <param name="yaw"></param>
                public void ApplyHull(Vector3 position, float yaw)
                {
                    // 車体の位置とヨー角を設定する。
                    _hull.MovePosition(position);
                    _hull.MoveRotation(Quaternion.Euler(0.0f, yaw, 0.0f));
                }


                /// <summary>
                /// 砲塔のヨー角を反映する。
                /// </summary>
                /// <param name="yaw"></param>
                public void ApplyTurret(float yaw)
                {
                    // 砲塔の回転を設定する。
                    _turret.rotation = Quaternion.Euler(0.0f, yaw + BarrelYawOffset, 0.0f);
                }


                /// <summary>
                /// 弾を発射する。
                /// </summary>
                /// <param name="direction"></param>
                public void SpawnBullet(Vector3 direction)
                {
                    // 弾の見た目も、進む方向へ向ける。
                    direction.y = 0.0f;
                    if (direction.sqrMagnitude < 0.001f)
                        direction = Vector3.forward;

                    BulletPresenter bullet = Instantiate(_bulletPrefab, _muzzle.position, Quaternion.LookRotation(direction.normalized, Vector3.up));
                    bullet.Init(direction, this);
                }


                /// <summary>
                /// 今の砲口から、弾と同じ規則の弾道を返す。
                /// </summary>
                /// <param name="direction"></param>
                public Vector3[] PredictBulletPath(Vector3 direction)
                {
                    // プレハブが無いときは、砲口だけを返す。
                    if (_bulletPrefab == null || _muzzle == null)
                        return new[] { transform.position };

                    return _bulletPrefab.CreatePath(_muzzle.position, direction, this);
                }


                /// <summary>
                /// 予測の点を、確認用の線で描く。
                /// </summary>
                /// <param name="path"></param>
                public void DrawPredictedPath(Vector3[] path)
                {
                    // Game 画面の点線は UI が描く。ここは Scene ビューでの確認用。
                    if (path == null)
                        return;

                    // 点を線で結ぶ。Debug.DrawLine は Scene ビューでしか見えない。
                    for (int i = 1; i < path.Length; i++)
                        Debug.DrawLine(path[i - 1], path[i], Color.white);
                }
            }
        }
    }
}