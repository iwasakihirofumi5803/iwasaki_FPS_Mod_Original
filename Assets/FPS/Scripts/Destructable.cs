using UnityEngine;

public class Destructable : MonoBehaviour
//MonoBehaviourとは、Unityプログラミングにおいて用いられる全てのクラスの基底クラス(親クラス)となるクラスです。
{
    Health m_Health;
    //ヘルス変数

    void Start()
    //void：返す型	Start：メソッドの名前	()：メソッドで渡すオブジェクト
    {
        m_Health = GetComponent<Health>();
        //取得したコンポーネントのヘルスを、m_Healthに代入
        DebugUtility.HandleErrorIfNullGetComponent<Health, Destructable>(m_Health, this, gameObject);
        //エラー処理 Null Getコンポーネントの場合<ヘルス、破壊可能>(m_Health,この,ゲームオブジェクト)

        //ダメージと死のアクションをサブスクライブします
        m_Health.onDie += OnDie;
        m_Health.onDamaged += OnDamaged;
    }

    void OnDamaged(float damage, GameObject damageSource)
    {
        // TODO：ダメージリアクション
    }

    void OnDie()
    {
        //これはOnDestroy関数を呼び出します
        Destroy(gameObject);
    }
}
