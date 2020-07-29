using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class DetectionModule : MonoBehaviour
{
    [Tooltip("敵AIのターゲット検出レイキャストのソースを表すポイント")]
    public Transform detectionSourcePoint;
    [Tooltip("敵がターゲットを見ることができる最大距離")]
    public float detectionRange = 20f;
    [Tooltip("敵がターゲットを攻撃できる最大距離")]
    public float attackRange = 10f;
    [Tooltip("敵がもう見えない既知のターゲットを放棄するまでの時間")]
    public float knownTargetTimeout = 4f;
    [Tooltip("OnShootアニメーション用のオプションのアニメーター")]
    public Animator animator;

    public UnityAction onDetectedTarget;
    public UnityAction onLostTarget;

    public GameObject knownDetectedTarget { get; private set; }
    public bool isTargetInAttackRange { get; private set; }
    public bool isSeeingTarget { get; private set; }
    public bool hadKnownTarget { get; private set; }

    protected float m_TimeLastSeenTarget = Mathf.NegativeInfinity;

    ActorsManager m_ActorsManager;

    const string k_AnimAttackParameter = "攻撃";
    const string k_AnimOnDamagedParameter = "ダメージ";

    protected virtual void Start()
    {
        m_ActorsManager = FindObjectOfType<ActorsManager>();
        DebugUtility.HandleErrorIfNullFindObject<ActorsManager, DetectionModule>(m_ActorsManager, this);
    }

    public virtual void HandleTargetDetection(Actor actor, Collider[] selfColliders)
    {
        //既知のターゲット検出タイムアウトを処理します
        if (knownDetectedTarget && !isSeeingTarget && (Time.time - m_TimeLastSeenTarget) > knownTargetTimeout)
        {
            knownDetectedTarget = null;
        }

        //最も近い目に見える敵対的なアクターを見つける
        float sqrDetectionRange = detectionRange * detectionRange;
        isSeeingTarget = false;
        float closestSqrDistance = Mathf.Infinity;
        foreach (Actor otherActor in m_ActorsManager.actors)
        {
            if (otherActor.affiliation != actor.affiliation)
            {
                float sqrDistance = (otherActor.transform.position - detectionSourcePoint.position).sqrMagnitude;
                if (sqrDistance < sqrDetectionRange && sqrDistance < closestSqrDistance)
                {
                    //障害物を確認します
                    RaycastHit[] hits = Physics.RaycastAll(detectionSourcePoint.position, (otherActor.aimPoint.position - detectionSourcePoint.position).normalized, detectionRange, -1, QueryTriggerInteraction.Ignore);
                    RaycastHit closestValidHit = new RaycastHit();
                    closestValidHit.distance = Mathf.Infinity;
                    bool foundValidHit = false;
                    foreach (var hit in hits)
                    {
                        if (!selfColliders.Contains(hit.collider) && hit.distance < closestValidHit.distance)
                        {
                            closestValidHit = hit;
                            foundValidHit = true;
                        }
                    }

                    if (foundValidHit)
                    {
                        Actor hitActor = closestValidHit.collider.GetComponentInParent<Actor>();
                        if (hitActor == otherActor)
                        {
                            isSeeingTarget = true;
                            closestSqrDistance = sqrDistance;

                            m_TimeLastSeenTarget = Time.time;
                            knownDetectedTarget = otherActor.aimPoint.gameObject;
                        }
                    }
                }
            }
        }

        isTargetInAttackRange = knownDetectedTarget != null && Vector3.Distance(transform.position, knownDetectedTarget.transform.position) <= attackRange;

        //検出イベント
        if (!hadKnownTarget &&
            knownDetectedTarget != null)
        {
            OnDetect();
        }

        if (hadKnownTarget &&
            knownDetectedTarget == null)
        {
            OnLostTarget();
        }

        //すでにターゲットを知っているかどうかを確認します（次のフレーム用）
        hadKnownTarget = knownDetectedTarget != null;
    }

    public virtual void OnLostTarget()
    {
        if (onLostTarget != null)
        {
            onLostTarget.Invoke();
        }
    }

    public virtual void OnDetect()
    {
        if (onDetectedTarget != null)
        {
            onDetectedTarget.Invoke();
        }
    }

    public virtual void OnDamaged(GameObject damageSource)
    {
        m_TimeLastSeenTarget = Time.time;
        knownDetectedTarget = damageSource;

        if (animator)
        {
            animator.SetTrigger(k_AnimOnDamagedParameter);
        }
    }

    public virtual void OnAttack()
    {
        if (animator)
        {
            animator.SetTrigger(k_AnimAttackParameter);
        }
    }
}
