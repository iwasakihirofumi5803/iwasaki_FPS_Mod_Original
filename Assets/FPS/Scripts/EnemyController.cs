using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[RequireComponent(typeof(Health), typeof(Actor), typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
//格納 クラスEnemyController : MonoBehaviourは、すべてのUnityスクリプトが派生する基本クラスです。
{
    [System.Serializable]
    //シリアライズ
    public struct RendererIndexData
    //publicの意味は、「他のクラスから見えるようにする」
    //レンダラーインデックスデータ
    {
        public Renderer renderer;
        public int materialIndex;

        public RendererIndexData(Renderer renderer, int index)
        {
            this.renderer = renderer;
            this.materialIndex = index;
        }
    }

    [Header("パラメーター")]
    [Tooltip("敵が自動的に殺されるYの高さ（レベルから落ちた場合）")]
    public float selfDestructYHeight = -20f;
    [Tooltip("敵が現在のパスの目的地点に到達したと見なす距離")]
    public float pathReachingRadius = 2f;
    [Tooltip("敵が回転する速度")]
    public float orientationSpeed = 10f;
    [Tooltip("死後、GameObjectが破棄されるまでの遅延（アニメーションを可能にするため）")]
    public float deathDuration = 0f;


    [Header("武器パラメータ")]
    [Tooltip("この敵の武器交換を許可する")]
    public bool swapToNextWeapon = false;
    [Tooltip("武器の交換から次の攻撃までの時間の遅れ")]
    public float delayAfterWeaponSwap = 0f;

    [Header("目の色")]
    [Tooltip("目の色の素材")]
    public Material eyeColorMaterial;
    [Tooltip("ボットの目のデフォルトの色")]
    [ColorUsageAttribute(true, true)]
    public Color defaultEyeColor;
    [Tooltip("ボットの目の攻撃色")]
    [ColorUsageAttribute(true, true)]
    public Color attackEyeColor;

    [Header("ヒット時にフラッシュ")]
    [Tooltip("ホバーボットの本体に使用されている素材")]
    public Material bodyMaterial;
    [Tooltip("ヒット時のフラッシュの色を表すグラデーション")]
    [GradientUsageAttribute(true)]
    public Gradient onHitBodyGradient;
    [Tooltip("ヒット時のフラッシュの持続時間")]
    public float flashOnHitDuration = 0.5f;

    [Header("サウンド")]
    [Tooltip("ダメージを受けたときに鳴る音")]
    public AudioClip damageTick;

    [Header("VFX")]
    [Tooltip("VFXプレハブは敵が死亡したときに生成されます")]
    public GameObject deathVFX;
    [Tooltip("デスVFXが生成されるポイント")]
    public Transform deathVFXSpawnPoint;

    [Header("戦利品")]
    [Tooltip("この敵が死ぬときにドロップできるオブジェクト")]
    public GameObject lootPrefab;
    [Tooltip("オブジェクトが落下する可能性")]
    [Range(0, 1)]
    public float dropRate = 1f;

    [Header("デバッグ表示")]
    [Tooltip("範囲に到達するパスを表す球ギズモの色")]
    public Color pathReachingRangeColor = Color.yellow;
    [Tooltip("攻撃範囲を表す球ギズモの色")]
    public Color attackRangeColor = Color.red;
    [Tooltip("検出範囲を表す球ギズモの色")]
    public Color detectionRangeColor = Color.blue;

    public UnityAction onAttack;
    public UnityAction onDetectedTarget;
    public UnityAction onLostTarget;
    public UnityAction onDamaged;


    List<RendererIndexData> m_BodyRenderers = new List<RendererIndexData>();
    MaterialPropertyBlock m_BodyFlashMaterialPropertyBlock;
    float m_LastTimeDamaged = float.NegativeInfinity;

    RendererIndexData m_EyeRendererData;
    MaterialPropertyBlock m_EyeColorMaterialPropertyBlock;

    public PatrolPath patrolPath { get; set; }
    public GameObject knownDetectedTarget => m_DetectionModule.knownDetectedTarget;
    public bool isTargetInAttackRange => m_DetectionModule.isTargetInAttackRange;
    public bool isSeeingTarget => m_DetectionModule.isSeeingTarget;
    public bool hadKnownTarget => m_DetectionModule.hadKnownTarget;
    public NavMeshAgent m_NavMeshAgent { get; private set; }
    public DetectionModule m_DetectionModule { get; private set; }

    int m_PathDestinationNodeIndex;
    EnemyManager m_EnemyManager;
    ActorsManager m_ActorsManager;
    Health m_Health;
    Actor m_Actor;
    Collider[] m_SelfColliders;
    GameFlowManager m_GameFlowManager;
    bool m_WasDamagedThisFrame;
    float m_LastTimeWeaponSwapped = Mathf.NegativeInfinity;
    int m_CurrentWeaponIndex;
    WeaponController m_CurrentWeapon;
    WeaponController[] m_Weapons;
    NavigationModule m_NavigationModule;

    void Start()
    {
        m_EnemyManager = FindObjectOfType<EnemyManager>();
        DebugUtility.HandleErrorIfNullFindObject<EnemyManager, EnemyController>(m_EnemyManager, this);

        m_ActorsManager = FindObjectOfType<ActorsManager>();
        DebugUtility.HandleErrorIfNullFindObject<ActorsManager, EnemyController>(m_ActorsManager, this);

        m_EnemyManager.RegisterEnemy(this);

        m_Health = GetComponent<Health>();
        DebugUtility.HandleErrorIfNullGetComponent<Health, EnemyController>(m_Health, this, gameObject);

        m_Actor = GetComponent<Actor>();
        DebugUtility.HandleErrorIfNullGetComponent<Actor, EnemyController>(m_Actor, this, gameObject);

        m_NavMeshAgent = GetComponent<NavMeshAgent>();
        m_SelfColliders = GetComponentsInChildren<Collider>();

        m_GameFlowManager = FindObjectOfType<GameFlowManager>();
        DebugUtility.HandleErrorIfNullFindObject<GameFlowManager, EnemyController>(m_GameFlowManager, this);

        //ダメージと死のアクションをサブスクライブします
        m_Health.onDie += OnDie;
        m_Health.onDamaged += OnDamaged;

        //すべての武器を見つけて初期化します
        FindAndInitializeAllWeapons();
        var weapon = GetCurrentWeapon();
        weapon.ShowWeapon(true);

        var detectionModules = GetComponentsInChildren<DetectionModule>();
        DebugUtility.HandleErrorIfNoComponentFound<DetectionModule, EnemyController>(detectionModules.Length, this, gameObject);
        DebugUtility.HandleWarningIfDuplicateObjects<DetectionModule, EnemyController>(detectionModules.Length, this, gameObject);
        //検出モジュールを初期化します
        m_DetectionModule = detectionModules[0];
        m_DetectionModule.onDetectedTarget += OnDetectedTarget;
        m_DetectionModule.onLostTarget += OnLostTarget;
        onAttack += m_DetectionModule.OnAttack;

        var navigationModules = GetComponentsInChildren<NavigationModule>();
        DebugUtility.HandleWarningIfDuplicateObjects<DetectionModule, EnemyController>(detectionModules.Length, this, gameObject);
        // navmeshエージェントのデータを上書きします
        if (navigationModules.Length > 0)
        {
            m_NavigationModule = navigationModules[0];
            m_NavMeshAgent.speed = m_NavigationModule.moveSpeed;
            m_NavMeshAgent.angularSpeed = m_NavigationModule.angularSpeed;
            m_NavMeshAgent.acceleration = m_NavigationModule.acceleration;
        }

        foreach (var renderer in GetComponentsInChildren<Renderer>(true))
        {
            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                if (renderer.sharedMaterials[i] == eyeColorMaterial)
                {
                    m_EyeRendererData = new RendererIndexData(renderer, i);
                }

                if (renderer.sharedMaterials[i] == bodyMaterial)
                {
                    m_BodyRenderers.Add(new RendererIndexData(renderer, i));
                }
            }
        }

        m_BodyFlashMaterialPropertyBlock = new MaterialPropertyBlock();

        //この敵のアイレンダラーがあるかどうかを確認します
        if (m_EyeRendererData.renderer != null)
        {
            m_EyeColorMaterialPropertyBlock = new MaterialPropertyBlock();
            m_EyeColorMaterialPropertyBlock.SetColor("_EmissionColor", defaultEyeColor);
            m_EyeRendererData.renderer.SetPropertyBlock(m_EyeColorMaterialPropertyBlock, m_EyeRendererData.materialIndex);
        }
    }

    void Update()
    {
        EnsureIsWithinLevelBounds();

        m_DetectionModule.HandleTargetDetection(m_Actor, m_SelfColliders);

        Color currentColor = onHitBodyGradient.Evaluate((Time.time - m_LastTimeDamaged) / flashOnHitDuration);
        m_BodyFlashMaterialPropertyBlock.SetColor("_EmissionColor", currentColor);
        foreach (var data in m_BodyRenderers)
        {
            data.renderer.SetPropertyBlock(m_BodyFlashMaterialPropertyBlock, data.materialIndex);
        }

        m_WasDamagedThisFrame = false;
    }

    void EnsureIsWithinLevelBounds()
    {
        //すべてのフレームで、敵を殺すための条件をテストします
        if (transform.position.y < selfDestructYHeight)
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnLostTarget()
    {
        onLostTarget.Invoke();

        //目のレンダラーが設定されている場合、目の攻撃の色とプロパティブロックを設定します
        if (m_EyeRendererData.renderer != null)
        {
            m_EyeColorMaterialPropertyBlock.SetColor("_EmissionColor", defaultEyeColor);
            m_EyeRendererData.renderer.SetPropertyBlock(m_EyeColorMaterialPropertyBlock, m_EyeRendererData.materialIndex);
        }
    }

    void OnDetectedTarget()
    {
        onDetectedTarget.Invoke();

        //目のレンダラーが設定されている場合、目のデフォルトの色とプロパティブロックを設定します
        if (m_EyeRendererData.renderer != null)
        {
            m_EyeColorMaterialPropertyBlock.SetColor("_EmissionColor", attackEyeColor);
            m_EyeRendererData.renderer.SetPropertyBlock(m_EyeColorMaterialPropertyBlock, m_EyeRendererData.materialIndex);
        }
    }

    public void OrientTowards(Vector3 lookPosition)
    {
        Vector3 lookDirection = Vector3.ProjectOnPlane(lookPosition - transform.position, Vector3.up).normalized;
        if (lookDirection.sqrMagnitude != 0f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * orientationSpeed);
        }
    }

    private bool IsPathValid()
    {
        return patrolPath && patrolPath.pathNodes.Count > 0;
    }

    public void ResetPathDestination()
    {
        m_PathDestinationNodeIndex = 0;
    }

    public void SetPathDestinationToClosestNode()
    {
        if (IsPathValid())
        {
            int closestPathNodeIndex = 0;
            for (int i = 0; i < patrolPath.pathNodes.Count; i++)
            {
                float distanceToPathNode = patrolPath.GetDistanceToNode(transform.position, i);
                if (distanceToPathNode < patrolPath.GetDistanceToNode(transform.position, closestPathNodeIndex))
                {
                    closestPathNodeIndex = i;
                }
            }

            m_PathDestinationNodeIndex = closestPathNodeIndex;
        }
        else
        {
            m_PathDestinationNodeIndex = 0;
        }
    }

    public Vector3 GetDestinationOnPath()
    {
        if (IsPathValid())
        {
            return patrolPath.GetPositionOfPathNode(m_PathDestinationNodeIndex);
        }
        else
        {
            return transform.position;
        }
    }

    public void SetNavDestination(Vector3 destination)
    {
        if (m_NavMeshAgent)
        {
            m_NavMeshAgent.SetDestination(destination);
        }
    }

    public void UpdatePathDestination(bool inverseOrder = false)
    {
        if (IsPathValid())
        {
            //パスの宛先に到達したかどうかを確認します
            if ((transform.position - GetDestinationOnPath()).magnitude <= pathReachingRadius)
            {
                //パス宛先インデックスを増分します
                m_PathDestinationNodeIndex = inverseOrder ? (m_PathDestinationNodeIndex - 1) : (m_PathDestinationNodeIndex + 1);
                if (m_PathDestinationNodeIndex < 0)
                {
                    m_PathDestinationNodeIndex += patrolPath.pathNodes.Count;
                }
                if (m_PathDestinationNodeIndex >= patrolPath.pathNodes.Count)
                {
                    m_PathDestinationNodeIndex -= patrolPath.pathNodes.Count;
                }
            }
        }
    }

    void OnDamaged(float damage, GameObject damageSource)
    {
        //損傷の原因がプレイヤーかどうかをテストします
        if (damageSource && damageSource.GetComponent<PlayerCharacterController>())
        {
            //プレーヤーを追跡します
            m_DetectionModule.OnDamaged(damageSource);

            if (onDamaged != null)
            {
                onDamaged.Invoke();
            }
            m_LastTimeDamaged = Time.time;

            //ダメージティックサウンドを再生します
            if (damageTick && !m_WasDamagedThisFrame)
                AudioUtility.CreateSFX(damageTick, transform.position, AudioUtility.AudioGroups.DamageTick, 0f);

            m_WasDamagedThisFrame = true;
        }
    }

    void OnDie()
    {
        //死んだときにパーティクルシステムをスポーンする
        var vfx = Instantiate(deathVFX, deathVFXSpawnPoint.position, Quaternion.identity);
        Destroy(vfx, 5f);

        //ゲームフローマネージャーに敵の破壊を処理するように伝えます
        m_EnemyManager.UnregisterEnemy(this);

        //オブジェクトを略奪します
        if (TryDropItem())
        {
            Instantiate(lootPrefab, transform.position, Quaternion.identity);
        }

        //これはOnDestroy関数を呼び出します
        Destroy(gameObject, deathDuration);
    }

    private void OnDrawGizmosSelected()
    {
        //範囲に到達するパス
        Gizmos.color = pathReachingRangeColor;
        Gizmos.DrawWireSphere(transform.position, pathReachingRadius);

        if (m_DetectionModule != null)
        {
            //検出範囲
            Gizmos.color = detectionRangeColor;
            Gizmos.DrawWireSphere(transform.position, m_DetectionModule.detectionRange);

            // 攻撃範囲
            Gizmos.color = attackRangeColor;
            Gizmos.DrawWireSphere(transform.position, m_DetectionModule.attackRange);
        }
    }

    public void OrientWeaponsTowards(Vector3 lookPosition)
    {
        for (int i = 0; i < m_Weapons.Length; i++)
        {
            //武器をプレイヤーに向けます
            Vector3 weaponForward = (lookPosition - m_Weapons[i].weaponRoot.transform.position).normalized;
            m_Weapons[i].transform.forward = weaponForward;
        }
    }

    public bool TryAtack(Vector3 enemyPosition)
    {
        if (m_GameFlowManager.gameIsEnding)
            return false;

        OrientWeaponsTowards(enemyPosition);

        if ((m_LastTimeWeaponSwapped + delayAfterWeaponSwap) >= Time.time)
            return false;

        //武器を撃ちます
        bool didFire = GetCurrentWeapon().HandleShootInputs(false, true, false);

        if (didFire && onAttack != null)
        {
            onAttack.Invoke();

            if (swapToNextWeapon && m_Weapons.Length > 1)
            {
                int nextWeaponIndex = (m_CurrentWeaponIndex + 1) % m_Weapons.Length;
                SetCurrentWeapon(nextWeaponIndex);
            }
        }

        return didFire;
    }

    public bool TryDropItem()
    {
        if (dropRate == 0 || lootPrefab == null)
            return false;
        else if (dropRate == 1)
            return true;
        else
            return (Random.value <= dropRate);
    }

    void FindAndInitializeAllWeapons()
    {
        //武器をすでに見つけて初期化しているかどうかを確認します
        if (m_Weapons == null)
        {
            m_Weapons = GetComponentsInChildren<WeaponController>();
            DebugUtility.HandleErrorIfNoComponentFound<WeaponController, EnemyController>(m_Weapons.Length, this, gameObject);

            for (int i = 0; i < m_Weapons.Length; i++)
            {
                m_Weapons[i].owner = gameObject;
            }
        }
    }

    public WeaponController GetCurrentWeapon()
    {
        FindAndInitializeAllWeapons();
        //現在武器が選択されていないか確認します
        if (m_CurrentWeapon == null)
        {
            //武器リストの最初の武器を現在の武器として設定します
            SetCurrentWeapon(0);
        }
        DebugUtility.HandleErrorIfNullGetComponent<WeaponController, EnemyController>(m_CurrentWeapon, this, gameObject);

        return m_CurrentWeapon;
    }

    void SetCurrentWeapon(int index)
    {
        m_CurrentWeaponIndex = index;
        m_CurrentWeapon = m_Weapons[m_CurrentWeaponIndex];
        if (swapToNextWeapon)
        {
            m_LastTimeWeaponSwapped = Time.time;
        }
        else
        {
            m_LastTimeWeaponSwapped = Mathf.NegativeInfinity;
        }
    }
}
