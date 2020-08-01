//
//左手に配置したコライダー（アタリ判定）が"敵"に当たったら
//敵に100ダメージを与える
//
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PunchDamage : MonoBehaviour
{
    [Header("ダメージ")]
    [Tooltip("パンチのダメージ")]
    public float m_PunchDamage = 100f;

    void OnTriggerEnter(Collider Collider)
    //物体がすり抜けた時(コライダー コライダー)
    {
        // パンチのダメージを表示
        if (Collider.CompareTag("Enemy"))
        {
            Collider.GetComponent<Damageable>().InflictDamage(m_PunchDamage, false, null);
        }
    }
}
