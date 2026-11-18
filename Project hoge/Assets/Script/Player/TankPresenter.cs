using UnityEngine;

namespace nsApp
{
    namespace nsActor
    {
        namespace nsPlayer
        {
            /// <summary>
            /// タンクの見た目と判断をつなぐクラス。
            /// </summary>
            public class TankPresenter : MonoBehaviour
            {
                [Header("Reference")]
                [SerializeField] private TankView _view;    //! 見た目。
                [SerializeField] private Camera _aimCamera; //! 画面を映しているカメラ。
                [Header("Move")]
                [SerializeField] private float _moveSpeed = 5.0f;     //! 前進速度。
                [SerializeField] private float _backMoveSpeed = 3.0f; //! 後退速度。
                [SerializeField] private float _rotateSpeed = 100.0f; //! 車体の回転速度。
                [Header("Aim")]
                [SerializeField] private float _aimSpeed = 720.0f; //! 砲塔の回転速度。
                [Header("Shot")]
                [SerializeField] private float _shotInterval = 0.4f; //! 次の弾までの待ち時間。
                private TankModel _model; //! 戦車の判断。


                /// <summary>
                /// 戦車の判断を初期化する。
                /// </summary>
                private void Awake()
                {
                    // 戦車の判断を初期化する。
                    _model = new TankModel
                     (
                        _view.HullPosition,
                        _view.HullYaw,
                        _moveSpeed,
                        _backMoveSpeed,
                        _rotateSpeed,
                        _aimSpeed,
                        _shotInterval
                    );
                }


                /// <summary>
                /// 発射通知の購読を開始する。
                /// </summary>
                private void OnEnable()
                {
                    _model.Fired += OnFired;
                }


                /// <summary>
                /// 発射通知の購読を終了する。   
                /// </summary>
                private void OnDisable()
                {
                    _model.Fired -= OnFired;
                }


                /// <summary>
                /// 車体の移動と砲塔の回転を計算する。
                /// </summary>
                private void Update()
                {
                    // 入力を読み取って戦車の判断に反映する。
                    _model.SetDrive(ReadDriveDirection());
                    // マウスが指している方向を読み取って戦車の判断に反映する。
                    _model.SetAimDirection(ReadAimDirection());
                    // 砲塔の回転を計算して View に反映する。
                    _model.SimulateAim(Time.deltaTime);
                    // 砲塔の回転結果を View に反映する。
                    _view.ApplyTurret(_model.TurretYaw);
                    // 弾道予測の点を更新し、確認用の線を View に描かせる。
                    Vector3[] path = _view.PredictBulletPath(_model.GetTurretForward()); _view.DrawPredictedPath(path);
                    // マウスの左クリックで弾を発射する。
                    if (Input.GetMouseButtonDown(0))
                        _model.TryFire(Time.time);
                }


                /// <summary>
                /// 車体の移動を計算する。
                /// </summary>
                private void FixedUpdate()
                {
                    // 車体の移動を計算して View に反映する。
                    _model.SimulateDrive(Time.fixedDeltaTime);
                    _view.ApplyHull(_model.Position, _model.HullYaw);
                }


                /// <summary>
                /// 弾を発射する。
                /// </summary>
                private void OnFired()
                {
                    // 弾を発射する。
                    _view.SpawnBullet(_model.GetTurretForward());
                }


                /// <summary>
                /// 画面に対する進行方向を読む。
                /// </summary>
                /// <returns></returns>
                private Vector3 ReadDriveDirection()
                {
                    // 画面上の入力を読み取る。W が奥、S が手前、A が左、D が右。
                    float x = 0.0f;
                    float z = 0.0f;

                    // 対応するキー。
                    if (Input.GetKey(KeyCode.D))
                        x += 1.0f;
                    if (Input.GetKey(KeyCode.A))
                        x -= 1.0f;
                    if (Input.GetKey(KeyCode.W))
                        z += 1.0f;
                    if (Input.GetKey(KeyCode.S))
                        z -= 1.0f;

                    // カメラが無いときは、入力をそのまま使う。
                    if (_aimCamera == null)
                        return new Vector3(x, 0.0f, z);

                    // カメラの水平な前と右を、画面の奥と右にする。
                    Vector3 forward = _aimCamera.transform.forward;
                    Vector3 right = _aimCamera.transform.right;
                    forward.y = 0.0f;
                    right.y = 0.0f;
                    return right.normalized * x + forward.normalized * z;
                }


                /// <summary>
                /// マウスが指している方向を読む。
                /// </summary>
                /// <returns></returns>
                private Vector3 ReadAimDirection()
                {
                    // マウスが指している方向を計算する。
                    if (_aimCamera == null)
                        return Vector3.zero;

                    // マウスの位置からレイを作成し、地面との交点を計算する。
                    Ray ray = _aimCamera.ScreenPointToRay(Input.mousePosition);
                    Plane ground = new Plane(Vector3.up, Vector3.zero);
                    if (!ground.Raycast(ray, out float distance))
                        return Vector3.zero;

                    // 交点から砲塔の前方向を計算する。
                    Vector3 hit = ray.GetPoint(distance);
                    Vector3 direction = hit - _view.HullPosition;
                    direction.y = 0.0f;
                    return direction;
                }
            }
        }
    }
}