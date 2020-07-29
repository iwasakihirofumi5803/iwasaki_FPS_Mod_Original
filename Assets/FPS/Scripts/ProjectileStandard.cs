using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ProjectileBase))]
public class ProjectileStandard : MonoBehaviour
{
    [Header("一般的な")]
    [Tooltip("この発射体の衝突検出の半径")]
    public float radius = 0.01f;
    [Tooltip("発射物のルートを表す変換（正確な衝突検出に使用）")]
    public Transform root;
    [Tooltip("発射体の先端を表す変換（正確な衝突検出に使用）")]
    public Transform tip;
    [Tooltip("発射物の寿命")]
    public float maxLifeTime = 5f;
    [Tooltip("衝撃時に呼び出すエフェクトプレハブ")]
    public GameObject impactVFX;
    [Tooltip("破壊される前のエフェクトのライフタイム")]
    public float impactVFXLifetime = 5f;
    [Tooltip("エフェクトが生成されるヒット法線に沿ったオフセット")]
    public float impactVFXSpawnOffset = 0.1f;
    [Tooltip("インパクト時に再生するクリップ")]
    public AudioClip impactSFXClip;
    [Tooltip("この発射物が衝突する可能性のあるレイヤー")]
    public LayerMask hittableLayers = -1;

    [Header("移動")]
    [Tooltip("発射物の速度")]
    public float speed = 20f;
    [Tooltip("重力による下向きの加速")]
    public float gravityDownAcceleration = 0f;
    [Tooltip("発射物がコースを修正して目的の軌道に合わせる距離（発射物を一人称視点で画面の中心に向かってドリフトさせるために使用されます）0未満の値では、補正はありません")]
    public float trajectoryCorrectionDistance = -1;
    [Tooltip("発射物が発砲時に武器の銃口が持っていた速度を継承するかどうかを決定します")]
    public bool inheritWeaponVelocity = false;

    [Header("ダメージ")]
    [Tooltip("発射体のダメージ")]
    public float damage = 40f;
    [Tooltip("ダメージの範囲。 領域の損傷を望まない場合は空のままにしてください")]
    public DamageArea areaOfDamage;

    [Header("デバッグ")]
    [Tooltip("発射物半径デバッグビューの色")]
    public Color radiusColor = Color.cyan * 0.2f;

    ProjectileBase m_ProjectileBase;
    //クラス、変数、発射台
    Vector3 m_LastRootPosition;
    //3Dベクトル、変数、最後のルートの位置
    Vector3 m_Velocity;
    //3Dベクトル、変数、速度
    bool m_HasTrajectoryOverride;
    //真偽、変数、軌道オーバーライドあり 
    float m_ShootTime;
    //数値、変数、発射時間
    Vector3 m_TrajectoryCorrectionVector;
    //3Dベクトル、変数、軌道修正ベクトル
    Vector3 m_ConsumedTrajectoryCorrectionVector;
    //3Dベクトル、変数、消費された軌道修正ベクトル
    List<Collider> m_IgnoredColliders;
    //動的変更可能な配列<当たり判定>、無視されたコライダー

    const QueryTriggerInteraction k_TriggerInteraction = QueryTriggerInteraction.Collide;
    //固定定数　クリエトリガーインタラクション = クリエトリガー.衝突

    private void OnEnable()
    //有効時、他のクラスから見えるようにする
    {
        m_ProjectileBase = GetComponent<ProjectileBase>();
        //取得コンポーネントの<ProjectileBaseクラス>を、m_ProjectileBaseに入れる
        DebugUtility.HandleErrorIfNullGetComponent<ProjectileBase, ProjectileStandard>(m_ProjectileBase, this, gameObject);
        //コンポーネントがnullの場合にエラーを処理する<ProjectileBaseクラス.ProjectileStandardクラス>(変数、発射台,ゲームオブジェクト)
        m_ProjectileBase.onShoot += OnShoot;
        //変数、発射台.シュート += シュート
        Destroy(gameObject, maxLifeTime);
        //破壊（ゲームオブジェクト、最大ライフタイム）
    }

    void OnShoot()
    {
        m_ShootTime = Time.time;
        //発射時間
        m_LastRootPosition = root.position;
        //最後のルートの位置 = ルートの位置
        m_Velocity = transform.forward * speed;
        //速度 = 向き * 速さ
        m_IgnoredColliders = new List<Collider>();
        //無視されたコライダー = 新しいリストのコライダー
        transform.position += m_ProjectileBase.inheritedMuzzleVelocity * Time.deltaTime;
        //変換位置 += 発射台.継承されたマズル向き * 処理終了時間

        // Ignore colliders of owner
        //所有者のコライダーを無視します
        Collider[] ownerColliders = m_ProjectileBase.owner.GetComponentsInChildren<Collider>();
        //所有者コライダー = 発射台.オーナー.子コンポーネントの取得<コライダー>
        m_IgnoredColliders.AddRange(ownerColliders);
        //無視されたコライダー.範囲を追加（所有者のコライダー）

        // Handle case of player shooting (make projectiles not go through walls, and remember center-of-screen trajectory)
        //プレーヤーの射撃のケースを処理します（発射体が壁を通過しないようにし、画面の中心の軌跡を覚えておいてください）
        PlayerWeaponsManager playerWeaponsManager = m_ProjectileBase.owner.GetComponent<PlayerWeaponsManager>();
        //プレイヤーウェポンマネージャーのコンポーネント情報を取得
        if (playerWeaponsManager)
        {
            m_HasTrajectoryOverride = true;
            //軌道オーバーライド = 有効

            Vector3 cameraToMuzzle = (m_ProjectileBase.initialPosition - playerWeaponsManager.weaponCamera.transform.position);
            //3Dベクトル 銃口へのカメラ = (発射台.初期位置 - プレイヤーの武器マネージャー.武器カメラ.位置)

            m_TrajectoryCorrectionVector = Vector3.ProjectOnPlane(-cameraToMuzzle, playerWeaponsManager.weaponCamera.transform.forward);
            //軌道修正ベクトル = 3Dベクトル平面上のプロジェクト(-銃口にカメラ, プレイヤーの武器マネージャー.武器カメラ.変換フォワード)
            if (trajectoryCorrectionDistance == 0)
            //もし、軌道修正距離 == 0
            {
                transform.position += m_TrajectoryCorrectionVector;
                //変換位置 += 軌道修正ベクトル
                m_ConsumedTrajectoryCorrectionVector = m_TrajectoryCorrectionVector;
            }
            else if (trajectoryCorrectionDistance < 0)
            {
                m_HasTrajectoryOverride = false;
            }
            
            if (Physics.Raycast(playerWeaponsManager.weaponCamera.transform.position, cameraToMuzzle.normalized, out RaycastHit hit, cameraToMuzzle.magnitude, hittableLayers, k_TriggerInteraction))
            {
                if (IsHitValid(hit))
                {
                    OnHit(hit.point, hit.normal, hit.collider);
                    //ヒット(ヒットポイント, ヒット法線, ヒットコライダー)
                }
            }
        }
    }

    void Update()
    {
        // 動作
        transform.position += m_Velocity * Time.deltaTime;
        if (inheritWeaponVelocity)
        {
            transform.position += m_ProjectileBase.inheritedMuzzleVelocity * Time.deltaTime;
        }

        // 軌道オーバーライドに向けてドリフト（これは、発射体を中央に配置できるようにするためです） 
        // 実際の武器がオフセットされている場合でも、カメラの中心で）
        if (m_HasTrajectoryOverride && m_ConsumedTrajectoryCorrectionVector.sqrMagnitude < m_TrajectoryCorrectionVector.sqrMagnitude)
        {
            Vector3 correctionLeft = m_TrajectoryCorrectionVector - m_ConsumedTrajectoryCorrectionVector;
            float distanceThisFrame = (root.position - m_LastRootPosition).magnitude;
            Vector3 correctionThisFrame = (distanceThisFrame / trajectoryCorrectionDistance) * m_TrajectoryCorrectionVector;
            correctionThisFrame = Vector3.ClampMagnitude(correctionThisFrame, correctionLeft.magnitude);
            m_ConsumedTrajectoryCorrectionVector += correctionThisFrame;

            // 修正終了を検出
            if (m_ConsumedTrajectoryCorrectionVector.sqrMagnitude == m_TrajectoryCorrectionVector.sqrMagnitude)
            {
                m_HasTrajectoryOverride = false;
            }

            transform.position += correctionThisFrame;
        }

        // 速度に向ける
        transform.forward = m_Velocity.normalized;

        // 重力
        if (gravityDownAcceleration > 0)
        {
            // 弾道効果のために発射速度に重力を追加します
            m_Velocity += Vector3.down * gravityDownAcceleration * Time.deltaTime;
        }

        // ヒット検出
        {
            RaycastHit closestHit = new RaycastHit();
            //レイキャスト 最も近いヒット = 新しいレイキャストヒット
            closestHit.distance = Mathf.Infinity;
            //最も近いヒット距離 = Mathf.無限
            bool foundHit = false;
            //真偽値 ヒットが見つかりました = 見つからない

            // スフィアキャスト
            Vector3 displacementSinceLastFrame = tip.position - m_LastRootPosition;
            RaycastHit[] hits = Physics.SphereCastAll(m_LastRootPosition, radius, displacementSinceLastFrame.normalized, displacementSinceLastFrame.magnitude, hittableLayers, k_TriggerInteraction);
            foreach (var hit in hits)
            //foreach：コレクションの要素を１つ１つ変数に格納して、要素の数だけ繰り返す
            {
                if (IsHitValid(hit) && hit.distance < closestHit.distance)
                //もし、有効ヒット（ヒット）且つ　ヒット距離 < 最も近いヒット距離
                {
                    foundHit = true;
                    //ヒットが見つかりました = 見つけた
                    closestHit = hit;
                    //最も近いヒット = ヒット
                }
            }

            if (foundHit)
            //もし、ヒットが見つかったら
            {
                // Handle case of casting while already inside a collider
                // すでにコライダーの中にいるときにキャストのケースを処理する
                if (closestHit.distance <= 0f)
                //もし、最も近いヒット距離 <= 0
                {
                    closestHit.point = root.position;
                    //最も近いヒットポイント = ルート位置
                    closestHit.normal = -transform.forward;
                    //最も近い法線 = 変換、前
                }

                OnHit(closestHit.point, closestHit.normal, closestHit.collider);
                //ヒット(最も近いヒットポイント, 最も近いヒット法線, 最も近いヒットコライダー)
            }
        }

        m_LastRootPosition = root.position;
        //最後のルート位置 = ルート位置
    }

    bool IsHitValid(RaycastHit hit)
    //真偽 ヒットしました(例キャストヒット)
    {
        // ignore hits with an ignore component
        // 無視コンポーネントでヒットを無視する
        if (hit.collider.GetComponent<IgnoreHitDetection>())
        {
            return false;
            //返す 見つからない
        }

        // ignore hits with triggers that don't have a Damageable component
        // 損傷可能なコンポーネントがないトリガーでのヒットを無視する
        if (hit.collider.isTrigger && hit.collider.GetComponent<Damageable>() == null)
        {
            return false;
            //見つからないを返す
        }

        // ignore hits with specific ignored colliders (self colliders, by default)
        // 特定の無視されたコライダー（デフォルトではセルフコライダー）のヒットを無視する
        if (m_IgnoredColliders != null && m_IgnoredColliders.Contains(hit.collider))
        {
            return false;
            //見つからないを返す
        }

        return true;
    }

    void OnHit(Vector3 point, Vector3 normal, Collider collider)
    //ヒット時(ポイントの向き、大きさ, コライダー)
    {
        // damage
        if (areaOfDamage)
        {
            // area damage
            areaOfDamage.InflictDamageInArea(damage, point, hittableLayers, k_TriggerInteraction, m_ProjectileBase.owner);
        }
        else
        {
            //ポイントダメージ
            Damageable damageable = collider.GetComponent<Damageable>();
            //ダメージ ダメージ = コライダー.コンポーネントを取得<破損>
            if (damageable)
            {
                damageable.InflictDamage(damage, false, m_ProjectileBase.owner);
                //ダメージ ダメージを与える(ダメージ,false,m_ProjectileBase. オーナー)
            }
        }

        // impact vfx
        if (impactVFX)
        {
            GameObject impactVFXInstance = Instantiate(impactVFX, point + (normal * impactVFXSpawnOffset), Quaternion.LookRotation(normal));
            if (impactVFXLifetime > 0)
            {
                Destroy(impactVFXInstance.gameObject, impactVFXLifetime);
            }
        }

        // impact sfx
        if (impactSFXClip)
        {
            AudioUtility.CreateSFX(impactSFXClip, point, AudioUtility.AudioGroups.Impact, 1f, 3f);
        }

        // Self Destruct
        Destroy(this.gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = radiusColor;
        Gizmos.DrawSphere(transform.position, radius);
    }
}