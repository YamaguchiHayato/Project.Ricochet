using System.Collections.Generic; // ジェネリックコンテンツを扱うため。
using UnityEngine;


namespace nsApp
{
    namespace nsActor
    {
        namespace nsPlayer
        {
            /// <summary>
            /// 弾丸の座標/反射角/止まる条件を管理。
            /// </summary>
            /// <remarks>
            /// このスクリプトはMVPパターンとして運用中。
            /// </remarks>
            /// <author>Yamaguchi Hayato</author>
            /// <created>2026-11-11</created>
            public class BulletModel
            {
                public Vector3 Position => _position; //! 弾丸の座標。
                public Vector3 Direction => _direction; //! 弾丸の進行方向。
                public bool IsExploded => _age >= _lifeTime; //! 弾丸が爆発したかどうか。寿命を終えていればtrue。
                public int Bounces => _bounces; //! 壁で反射した回数。
                private const float FlatNormalLimit = 0.5f; //! 地面とみなす法線のY成分。
                private const float PathSkin = 0.05f; //! 壁に埋まらないように離す距離。
                private const int MaxSegments = 48; //! 角で無限に反射しないための区間数。
                private const float Flat = 0.0f; //! 水平にするための値。
                private const float MinDirectionSqr = 0.001f; //! 方向が無いとみなす長さの二乗。
                private const float MinStep = 0.001f; //! これ以下の移動は無視する。
                private const float MinRadius = 0.01f; //! 判定に使う半径の下限。
                private Vector3 _position; //! 弾丸の座標。
                private Vector3 _direction; //! 弾丸の水平な進行方向。
                private readonly float _speed; //! 弾速。
                private readonly float _lifeTime; //! 消えるまでの秒数。
                private int _bounces; //! 壁で反射した回数。
                private float _age; //! 生成からの秒数。


                /// <summary>
                /// 1回の判定で進んだ結果。
                /// </summary>
                private struct Advance
                {
                    public Vector3 Position; //! 進んだ先の座標。
                    public Vector3 Direction; //! 進んだあとの進行方向。
                    public float Used; //! この判定で進んだ距離。
                    public int Bounces; //! この判定が終わった時点の反射回数。
                    public TankView Tank; //! 当たった戦車。無ければnull。
                }


                /// <summary>
                /// 初期位置とPrefabで決めた速さと寿命を受け取る。
                /// </summary>
                /// <param name="position">生成時の座標。</param>
                /// <param name="direction">発射方向。</param>
                /// <param name="speed">弾速。</param>
                /// <param name="lifeTime">消えるまでの秒数。</param>
                public BulletModel(Vector3 position, Vector3 direction, float speed, float lifeTime)
                {
                    // 進行方向は水平面だけを使う。
                    _position = position;
                    direction.y = Flat;
                    _direction = direction.sqrMagnitude < MinDirectionSqr ? Vector3.forward : direction.normalized;
                    _speed = speed;
                    _lifeTime = lifeTime;
                }


                /// <summary>
                /// このフレームの距離だけ進め、当たった戦車を返す。
                /// </summary>
                /// <param name="deltaTime">経過時間。</param>
                /// <param name="radius">弾丸の半径。</param>
                /// <param name="owner">発射した戦車。</param>
                /// <returns>当たった戦車。無ければnull。</returns>
                public TankView Simulate(float deltaTime, float radius, TankView owner)
                {
                    // 寿命を終えていれば、これ以上進まない。
                    float remainingLife = _lifeTime - _age;
                    if (remainingLife <= Flat)
                        return null;

                    // このフレームで進める距離は、残り寿命を超えない。
                    float distance = Mathf.Min(_speed * deltaTime, _speed * remainingLife);
                    TankView hit = Fly(ref _position, ref _direction, ref _bounces, distance, radius, owner);
                    _age += deltaTime;
                    return hit;
                }


                /// <summary>
                /// 発射位置から、実弾と同じ規則の点の列を返す。弾は生成しない。
                /// </summary>
                /// <param name="origin">砲口の座標。</param>
                /// <param name="direction">発射方向。</param>
                /// <param name="speed">弾速。</param>
                /// <param name="lifeTime">消えるまでの秒数。</param>
                /// <param name="radius">弾丸の半径。</param>
                /// <param name="owner">発射した戦車。</param>
                /// <returns>反射を含んだ弾道の点。</returns>
                public static Vector3[] CreatePath(Vector3 origin, Vector3 direction, float speed, float lifeTime, float radius, TankView owner)
                {
                    // 水平方向だけを進める。
                    direction.y = Flat;
                    if (direction.sqrMagnitude < MinDirectionSqr)
                        return new[] { origin };

                    // 反射を含めた弾道の点を作る。
                    const float drawHeight = 0.5f;
                    Vector3 position = origin;
                    position.y = drawHeight;

                    // 進行方向は水平面だけを使う。
                    direction = direction.normalized;
                    int bounces = 0;
                    float remaining = speed * lifeTime;
                    List<Vector3> points = new List<Vector3>();
                    points.Add(position);

                    // 反射を含めて、指定距離を進める。
                    for (int segment = 0; segment < MaxSegments && remaining > MinStep; segment++)
                    {
                        // 
                        int bouncesBefore = bounces;
                        Advance step = AdvanceOnce(position, direction, remaining, radius, bounces, owner);
                        if (step.Used <= MinStep)
                            break;

                        // 
                        position = step.Position;
                        position.y = drawHeight;
                        direction = step.Direction;
                        bounces = step.Bounces;
                        remaining -= step.Used;
                        points.Add(position);

                        // 戦車に当たったら、そこで止める。
                        if (step.Tank != null || bounces == bouncesBefore)
                            break;
                    }

                    return points.ToArray();
                }

                /// <summary>
                /// 指定距離を、壁の反射まで含めて進める。
                /// </summary>
                /// <param name="position">現在の座標。</param>
                /// <param name="direction">現在の進行方向。</param>
                /// <param name="bounces">現在の反射回数。</param>
                /// <param name="distance">進める距離。</param>
                /// <param name="radius">弾丸の半径。</param>
                /// <param name="owner">発射した戦車。</param>
                /// <returns>当たった戦車。無ければnull。</returns>
                private static TankView Fly(ref Vector3 position, ref Vector3 direction, ref int bounces, float distance, float radius, TankView owner)
                {
                    return Fly(ref position, ref direction, ref bounces, distance, radius, owner, out _);
                }


                /// <summary>
                /// 指定距離を進め、消費した距離も返す。
                /// </summary>
                /// <param name="position">現在の座標。</param>
                /// <param name="direction">現在の進行方向。</param>
                /// <param name="bounces">現在の反射回数。</param>
                /// <param name="distance">進める距離。</param>
                /// <param name="radius">弾丸の半径。</param>
                /// <param name="owner">発射した戦車。</param>
                /// <param name="usedTotal">消費した距離。</param>
                /// <returns>当たった戦車。無ければnull。</returns>
                private static TankView Fly(ref Vector3 position, ref Vector3 direction, ref int bounces, float distance, float radius, TankView owner, out float usedTotal)
                {
                    usedTotal = Flat;

                    // この距離のあいだに起きる反射を、順に処理する。
                    for (int segment = 0; segment < MaxSegments && distance > MinStep; segment++)
                    {
                        int bouncesBefore = bounces;
                        Advance step = AdvanceOnce(position, direction, distance, radius, bounces, owner);
                        if (step.Used <= MinStep)
                            break;

                        position = step.Position;
                        direction = step.Direction;
                        bounces = step.Bounces;
                        distance -= step.Used;
                        usedTotal += step.Used;

                        if (step.Tank != null)
                            return step.Tank;

                        // 壁が無ければ、指定距離を進み終えている。
                        if (step.Bounces == bouncesBefore)
                            break;
                    }

                    return null;
                }


                /// <summary>
                /// 現在位置から、次の壁か戦車まで進める。
                /// </summary>
                /// <param name="position">現在の座標。</param>
                /// <param name="direction">現在の進行方向。</param>
                /// <param name="distance">調べる距離。</param>
                /// <param name="radius">弾丸の半径。</param>
                /// <param name="bounces">現在の反射回数。</param>
                /// <param name="owner">発射した戦車。</param>
                /// <returns>次の地点までの結果。</returns>
                private static Advance AdvanceOnce(Vector3 position, Vector3 direction, float distance, float radius, int bounces, TankView owner)
                {
                    // 水平方向だけを進める。
                    direction.y = Flat;
                    if (direction.sqrMagnitude < MinDirectionSqr)
                        direction = Vector3.forward;
                    direction.Normalize();

                    // 判定だけ壁の高さで行う（見た目の弾の高さはそのまま）
                    Vector3 castOrigin = position;
                    castOrigin.y = 0.5f;

                    // 半径を使って、壁の判定をする。
                    RaycastHit[] hits = Physics.SphereCastAll(castOrigin,Mathf.Max(radius, MinRadius),direction,distance,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);

                    // 距離の近い順にソートする。
                    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                    foreach (RaycastHit hit in hits)
                    {
                        // ほかの弾は通り抜ける。
                        if (hit.collider.GetComponentInParent<BulletView>() != null)
                            continue;

                        // 開始地点で重なっている当たりは、向きが決まらないので使わない。
                        if (hit.distance <= 0.0f)
                            continue;

                        // 最初の反射の前だけ、砲口が自分に埋まっても消さない。
                        TankView tank = hit.collider.GetComponentInParent<TankView>();
                        if (tank == owner && bounces == 0)
                            continue;

                        float used = Mathf.Min(hit.distance, distance);

                        // 戦車なら、そこで止める。
                        if (tank != null)
                        {
                            return new Advance
                            {
                                Position = position + direction * used,
                                Direction = direction,
                                Used = used,
                                Bounces = bounces,
                                Tank = tank
                            };
                        }

                        Transform root = hit.collider.transform.root;
                        string rootName = root != null ? root.name : string.Empty;
                        if (rootName.StartsWith("土台") || hit.collider.gameObject.name.StartsWith("土台"))
                            continue;

                        // 壁なら、入射角のまま跳ね返す。
                        Vector3 normal = hit.normal;
                        if (Mathf.Abs(normal.y) >= FlatNormalLimit)
                            normal = -direction;

                        // 反射後の方向を計算する。
                        normal.y = Flat;
                        if (normal.sqrMagnitude < MinDirectionSqr)
                            normal = -direction;

                        // 
                        Vector3 reflected = Vector3.Reflect(direction, normal.normalized);
                        reflected.y = Flat;
                        if (reflected.sqrMagnitude < MinDirectionSqr)
                            reflected = -direction;
                        reflected.Normalize();

                        float skin = Mathf.Min(PathSkin, Mathf.Max(Flat, distance - used));
                        return new Advance
                        {
                            Position = position + direction * used + reflected * skin,
                            Direction = reflected,
                            Used = used + skin,
                            Bounces = bounces + 1,
                            Tank = null
                        };
                    }

                    // 何も無ければ、残りの距離をまっすぐ進む。
                    return new Advance
                    {
                        Position = position + direction * distance,
                        Direction = direction,
                        Used = distance,
                        Bounces = bounces,
                        Tank = null
                    };
                }
            }
        }
    }
}