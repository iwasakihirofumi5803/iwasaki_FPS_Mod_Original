using UnityEngine;

//このクラスには、アクター（プレーヤーまたは敵）を説明する一般的な情報が含まれています。
//主にAI検出ロジックと、アクターが味方か、味方かを決定するために使用されます
public class Actor : MonoBehaviour
{
    [Tooltip("アクターの所属（またはチーム）を表します。 同じ所属のアクターは互いに友好的です")]
    public int affiliation;
    [Tooltip("他のアクターがこのアクターを攻撃するときに狙うポイントを表します")]
    public Transform aimPoint;

    ActorsManager m_ActorsManager;

    private void Start()
    {
        m_ActorsManager = GameObject.FindObjectOfType<ActorsManager>();
        DebugUtility.HandleErrorIfNullFindObject<ActorsManager, Actor>(m_ActorsManager, this);

        //アクターとして登録します
        if (!m_ActorsManager.actors.Contains(this))
        {
            m_ActorsManager.actors.Add(this); 
        }
    }

    private void OnDestroy()
    {
        //アクターとしての登録を解除します
        if (m_ActorsManager)
        {
            m_ActorsManager.actors.Remove(this);
        }
    }
}
