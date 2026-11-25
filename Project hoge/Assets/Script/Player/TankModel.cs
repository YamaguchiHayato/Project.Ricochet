using System;
using UnityEngine;


namespace nsApp
{
    namespace nsActor
    {
        namespace nsPlayer
        {
            /// <summary>
            /// 戦車の移動、照準、発射判断を持つ。
            /// </summary>
            public class TankModel
            {
                private const float MIN_DRIVE_INPUT = 0.01f; //! 移動入力を無視する大きさ。
                private const float MIN_MOVE_DISTANCE = 0.0001f; //! これ以下の移動は無視する。
                private const float HULL_CAST_RADIUS = 0.45f; //! 車体の当たり半径。
                private const float HULL_CAST_HEIGHT = 0.5f; //! 移動判定に使う高さ。弾と同じ。
                private const float HULL_CAST_SKIN = 0.05f; //! 壁に埋まらないように離す距離。
                private const float FLAT = 0.0f; //! 水平にするための値。
                private const string GROUND_NAME_PREFIX = "土台"; //! 床オブジェクト名の先頭。
                public event Action Fired;//! 発射したことを通知するイベント。
                public Vector3 Position => _position;//! 車体の位置。
                public float HullYaw => _hullYaw;//! 車体のヨー角。
                public float TurretYaw => _turretYaw; //! 砲塔のヨー角。
                private readonly float _moveSpeed;     //! 前進速度。
                private readonly float _backMoveSpeed; //! 後退速度。
                private readonly float _rotateSpeed;   //! 車体の回転速度。
                private readonly float _aimSpeed;      //! 砲塔の回転速度。
                private readonly float _shotInterval;  //! 次の弾までの待ち時間。
                private Vector3 _position;             //! 車体の位置。
                private float _hullYaw;                //! 車体のヨー角。
                private float _turretYaw;              //! 砲塔のヨー角。
                private float _targetTurretYaw;        //! 砲塔が向かうヨー角。
                private Vector3 _driveDirection; //! 進む方向。
                private float _lastShotTime = -100.0f; //! 前回発射した時刻。
                private float _rotateInput;            //! 左右回転の入力値。


                /// <summary>
                /// 初期状態とパラメータを受け取る。
                /// </summary>
                public TankModel(Vector3 position, float hullYaw, float moveSpeed, float backMoveSpeed, float rotateSpeed, float aimSpeed,float shotInterval)
                {
                    _position = position;
                    _hullYaw = hullYaw;
                    _turretYaw = hullYaw;
                    _targetTurretYaw = hullYaw;
                    _moveSpeed = moveSpeed;
                    _backMoveSpeed = backMoveSpeed;
                    _rotateSpeed = rotateSpeed;
                    _aimSpeed = aimSpeed;
                    _shotInterval = shotInterval;
                }


                /// <summary>
                /// 砲塔が向かう水平方向を保存する。
                /// </summary>
                public void SetAimDirection(Vector3 direction)
                {
                    // 方向ベクトルのY成分を0にして水平方向のみに制限する。
                    direction.y = 0.0f;
                    if (direction.sqrMagnitude < 0.001f)
                        return;

                    // 方向ベクトルから砲塔の目標ヨー角を計算する。Quaternion.LookRotationを使って方向ベクトルから回転を求め、その回転のY軸の角度を取得する。
                    _targetTurretYaw = Quaternion.LookRotation(direction, Vector3.up).eulerAngles.y;
                }


                /// <summary>
                /// 砲塔を目標方向へ近づける。
                /// </summary>
                public void SimulateAim(float deltaTime)
                {
                    // Mathf.MoveTowardsAngleを使って、現在の砲塔のヨー角から目標のヨー角へ、一定の速度で近づける。
                    // deltaTimeを掛けることでフレームレートに依存しない動作にする。
                    _turretYaw = Mathf.MoveTowardsAngle(_turretYaw, _targetTurretYaw, _aimSpeed * deltaTime);
                }


                /// <summary>
                /// 車体を旋回させ、向いている方向へ進める。
                /// 壁・障害物に当たる場合は、その手前で止める。
                /// </summary>
                /// <param name="deltaTime">経過時間。</param>
                public void SimulateDrive(float deltaTime)
                {
                    // 入力が無いときは動かない。
                    if (_driveDirection.sqrMagnitude < MIN_DRIVE_INPUT)
                        return;

                    // スティックの方向を、車体の前と横に分ける。
                    Vector3 direction = _driveDirection.normalized;
                    Vector3 hullForward = Quaternion.Euler(FLAT, _hullYaw, FLAT) * Vector3.forward;
                    Vector3 hullRight = Quaternion.Euler(FLAT, _hullYaw, FLAT) * Vector3.right;
                    float throttle = Vector3.Dot(hullForward, direction);
                    float steer = Vector3.Dot(hullRight, direction);

                    // 横成分では、その場で回す。位置は動かない。
                    _hullYaw += steer * _rotateSpeed * deltaTime;

                    // 前後入力が無ければ、旋回だけして終わる。
                    if (Mathf.Abs(throttle) < MIN_DRIVE_INPUT)
                        return;

                    // 旋回後の前方向で、このフレームの移動量を求める。
                    hullForward = Quaternion.Euler(FLAT, _hullYaw, FLAT) * Vector3.forward;
                    float speed = throttle >= FLAT ? _moveSpeed : _backMoveSpeed;
                    Vector3 delta = hullForward * (throttle * speed * deltaTime);
                    float moveDist = delta.magnitude;

                    // 移動量がほぼ無ければ何もしない。
                    if (moveDist < MIN_MOVE_DISTANCE)
                        return;

                    // 判定だけ固定の高さで行う。見た目のYはそのまま。
                    Vector3 castOrigin = _position;
                    castOrigin.y = HULL_CAST_HEIGHT;

                    // 進む方向に球体を飛ばして、壁や障害物を調べる。
                    RaycastHit[] hits = Physics.SphereCastAll(castOrigin,HULL_CAST_RADIUS,delta.normalized,moveDist,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);

                    // 近い当たりから順に見る。
                    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                    foreach (RaycastHit hit in hits)
                    {
                        // 開始地点で重なっている当たりは使わない。
                        if (hit.distance <= FLAT)
                            continue;

                        // 床（土台）は移動を止めない。
                        string colliderName = hit.collider.gameObject.name;
                        if (colliderName.StartsWith(GROUND_NAME_PREFIX))
                            continue;

                        // 自分の車体のコライダーは無視する。
                        if (hit.collider.GetComponentInParent<TankView>() != null)
                            continue;

                        // 壁・障害物の手前で止める。
                        float allowed = Mathf.Max(FLAT, hit.distance - HULL_CAST_SKIN);
                        _position += delta.normalized * allowed;
                        return;
                    }

                    // 何も無ければ、そのまま進む。
                    _position += delta;
                }


                /// <summary>
                /// 待ち時間が過ぎていれば発射を通知する。
                /// </summary>
                public bool TryFire(float time)
                {
                    // 前回発射からの経過時間がショット間隔より短い場合は発射できない。
                    if (time < _lastShotTime + _shotInterval)
                        return false;

                    // 発射可能な場合は、前回発射時刻を更新し、Firedイベントを通知する。
                    _lastShotTime = time;
                    Fired?.Invoke();
                    return true;
                }


                /// <summary>
                /// 砲塔の水平な前方向を返す。
                /// </summary>
                public Vector3 GetTurretForward()
                {
                    // Quaternion.Eulerを使って、砲塔のヨー角から回転を作り、その回転をVector3.forwardに掛けることで、砲塔の前方向のベクトルを取得する。
                    return Quaternion.Euler(0.0f, _turretYaw, 0.0f) * Vector3.forward;
                }


                /// <summary>
                ///  車体の前方向を返す。
                /// </summary>
                /// <param name="diredction"> 進行方向を管理するベクトル。</param>
                public void SetDrive(Vector3 diredction)
                {
                    // 進行方向のY成分を0にして水平方向のみに制限する。
                    diredction.y = 0.0f;
                    _driveDirection = diredction;
                }
            }
        }
    }
}