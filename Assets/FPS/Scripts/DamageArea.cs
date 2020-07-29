using System.Collections.Generic;
using UnityEngine;

public class DamageArea : MonoBehaviour									//ダメージエリアのクラス名
{
    [Tooltip("発射物が何かに当たったときのダメージエリア")]
    public float areaOfEffectDistance = 5f;
    [Tooltip("ダメージエリアの距離に対して、エフェクトの拡大率")]
    public AnimationCurve damageRatioOverDistance;

    [Header("デバッグ")]
    [Tooltip("ダメージエリアの領域のエフェクトの色")]
    public Color areaOfEffectColor = Color.red * 0.5f;					//？？？05f毎に赤くする

    public void InflictDamageInArea(float damage, Vector3 center, LayerMask layers, QueryTriggerInteraction interaction, GameObject owner)
    //エリアにダメージを与える（floatダメージ、Vector3センター、レイヤーマスクレイヤー、クエリトリガーインタラクションインタラクション、ゲームオブジェクトオーナー）
    {
        Dictionary<Health, Damageable> uniqueDamagedHealths = new Dictionary<Health, Damageable>();
        //辞書<Health、Damageable>固有のDamaged Healths =新しい辞書<Health、Damageable>

        //影響を受ける領域でダメージを受ける可能性のある固有のヘルスコンポーネントのコレクションを作成します（同じエンティティに複数回損傷を与えないようにするため）
        //？？？ダメージエリア内での多重ヒットをさせない処理
        Collider[] affectedColliders = Physics.OverlapSphere(center, areaOfEffectDistance, layers, interaction);
        //コライダー[] 影響を受けるコライダー = 物理、オーバーラップスフィア（中央、効果範囲、レイヤー、相互作用）;
        foreach (var coll in affectedColliders)
        //foreach：コレクションのすべての要素を１つ１つ取得するときに使用する。
        {
            Damageable damageable = coll.GetComponent<Damageable>();
            //ダメージ = 取得したコンポーネント<ダメージ>
            if (damageable)
            {
                Health health = damageable.GetComponentInParent<Health>();
                //ヘルス = ダメージ、 取得する。親コンポーネント内の<ヘルス>
                if (health && !uniqueDamagedHealths.ContainsKey(health))
                //もしヘルス且つ　ユニークダメージヘルス、含むキー（ヘルス）
                {
                    uniqueDamagedHealths.Add(health, damageable);
                    //ユニークダメージヘルス、追加（ヘルス、ダメージ）;
                }
            }
        }

        //距離の減衰でダメージを与える
        foreach (Damageable uniqueDamageable in uniqueDamagedHealths.Values)
        {
            float distance = Vector3.Distance(uniqueDamageable.transform.position, transform.position);
            uniqueDamageable.InflictDamage(damage * damageRatioOverDistance.Evaluate(distance / areaOfEffectDistance), true, owner);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = areaOfEffectColor;
        Gizmos.DrawSphere(transform.position, areaOfEffectDistance);
    }
}
