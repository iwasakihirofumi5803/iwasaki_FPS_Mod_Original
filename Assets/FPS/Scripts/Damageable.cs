using UnityEngine;

public class Damageable : MonoBehaviour
//Damageableという名前のclassを作っている
{
    [Tooltip("受けたダメージに適用する乗数")]
    public float damageMultiplier = 1f;
    //格納 小数値 ダメージ乗数 = 1
    [Range(0, 1)]												
    //最小値：0　最大値：1
    [Tooltip("自己ダメージに適用する乗数")]				
    public float sensibilityToSelfdamage = 0.5f;
    //格納 小数値 自己ダメージに対する感性 = 0.5

    public Health health { get; private set; }
    //格納 Health関数に、healthの値・参照の取得情報を格納 {値を知りたい時はget,値を代入したい場合はsetを使用し、それぞれが呼び出されます}

    void Awake()
    //void：読み込み関数（Startよりも優先的に読み込みされる）
    {
        // find the health component either at the same level, or higher in the hierarchy
        //ヘルスコンポーネントを階層内で同じレベル、またはそれ以上に見つける。
        health = GetComponent<Health>();						//ヘルスコンポーネントを取得する
        if (!health)
        {
            health = GetComponentInParent<Health>();
            //親のヘルスコンポーネントを取得して、ヘルス関数に導入する
        }
    }

    public void InflictDamage(float damage, bool isExplosionDamage, GameObject damageSource)
    //格納 ダメージを与える(小数値 ダメージ, 真偽を表わす 爆発ダメージです, ゲームオブジェクトのダメージソース)
    {
        if (health)
        {
            var totalDamage = damage;
            //var は、コンパイル時に型の評価が行われる
            //総ダメージ = ダメージ

            // skip the crit multiplier if it's from an explosion
            //爆発の場合はクリティカル乗数をスキップします
            if (!isExplosionDamage)
            {
                totalDamage *= damageMultiplier;
            }

            // potentially reduce damages if inflicted by self		
            //翻訳：自分が負った場合、潜在的に被害を減らす
            if (health.gameObject == damageSource)
            {
                //totalDamage *= sensibilityToSelfdamage;
                //総ダメージ *= 自己ダメージに対する感性
                totalDamage = 0;
                //プレイヤーにダメージが入らない(0ダメージ)設定
            }

            // apply the damages									
            //ダメージを与える
            health.TakeDamage(totalDamage, damageSource);
            //ヘルスがダメージを受ける（トータルダメージ、ダメージソース）
        }
    }
}
