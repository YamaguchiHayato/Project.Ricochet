using UnityEngine;


namespace nsApp
{
    namespace nsActor
    {
        namespace nsPlayer
        {
            /// <summary>
            /// 弾丸の判断と見た目をつなぐクラス。
            /// </summary>
            /// <remarks>
            /// このスクリプトはMVPパターンとして運用中。
            /// </remarks>
            /// <author>Yamaguchi Hayato</author>
            /// <created>2026-11-11</created>
            [RequireComponent(typeof(BulletView))]
            public class BulletPresenter : MonoBehaviour
            {
                [Header("Audio")]
                [SerializeField] private AudioSource _audio; //! SE再生用。
                [SerializeField] private AudioClip _hitWallClip; //! 壁反射（hit_wall.wav）。
                [SerializeField] private AudioClip _explosionClip; //! 爆発（explosion.wav）。
                [SerializeField] private AudioClip _destroyClip; //! 撃破（destroy.wav）。
                [SerializeField] private BulletView _view; //! 見た目。
                [SerializeField] private float _speed = 20.0f; //! 弾速。
                [SerializeField] private float _lifeTime = 3.0f; //! 消えるまでの秒数。今のプレハブに合わせてある。
                private BulletModel _model; //! 弾丸の判断。
                private TankView _owner; //! 発射した戦車。


                /// <summary>
                /// 見た目が未設定なら、同じオブジェクトから取る。
                /// </summary>
                private void Awake()
                {
                    // null の場合、Componentを取得する。
                    if (_view == null)
                        _view = GetComponent<BulletView>();
                }


                /// <summary>
                /// 進行方向と、発射した戦車を渡して判断を作る。
                /// </summary>
                /// <param name="direction">発射方向。</param>
                /// <param name="owner">発射した戦車。</param>
                public void Init(Vector3 direction, TankView owner)
                {
                    _owner = owner;
                    _model = new BulletModel(_view.Position, direction, _speed, _lifeTime);
                    _view.Apply(_model.Position, _model.Direction);
                }


                /// <summary>
                /// 砲口からの予測点を、この弾の速さと寿命で作る。
                /// </summary>
                /// <param name="origin">砲口の座標。</param>
                /// <param name="direction">発射方向。</param>
                /// <param name="owner">発射した戦車。</param>
                /// <returns>反射を含んだ弾道の点。</returns>
                public Vector3[] CreatePath(Vector3 origin, Vector3 direction, TankView owner)
                {
                    // 見た目が未設定なら、同じオブジェクトから取る。
                    if (_view == null)
                        _view = GetComponent<BulletView>();

                    // 弾道の点を作る。反射を含む。
                    return BulletModel.CreatePath(origin, direction, _speed, _lifeTime, _view.Radius, owner);
                }


                /// <summary>
                /// 判断を進め、当たった戦車と自分を消す。
                /// </summary>
                private void FixedUpdate()
                {
                    // まだ発射されていないプレハブは進まない。
                    if (_model == null)
                        return;

                    // このフレーム開始時点の反射回数を覚えておく。
                    int bouncesBefore = _model.Bounces;

                    // このフレームの移動結果を見た目へ反映する。
                    TankView hit = _model.Simulate(Time.fixedDeltaTime, _view.Radius, _owner);
                    _view.Apply(_model.Position, _model.Direction);

                    // 壁・障害物で跳ねたら、反射音を鳴らす。
                    if (_model.Bounces > bouncesBefore && _audio != null && _hitWallClip != null)
                        _audio.PlayOneShot(_hitWallClip);

                    // 戦車に当たったら、その戦車と弾を消す。跳ね返った自分も含む。
                    if (hit != null)
                    {
                        // 弾を消しても音が残るよう、その場でワンショット再生する。
                        Vector3 sePosition = transform.position;
                        if (_destroyClip != null)
                            AudioSource.PlayClipAtPoint(_destroyClip, sePosition);
                        if (_explosionClip != null)
                            AudioSource.PlayClipAtPoint(_explosionClip, sePosition);

                        // 破棄。
                        Destroy(hit.gameObject);
                        Destroy(gameObject);
                        return;
                    }

                    // 弾丸の寿命が尽きたら消す。
                    if (_model.IsExploded)
                        Destroy(gameObject);
                }
            }
        }
    }
}