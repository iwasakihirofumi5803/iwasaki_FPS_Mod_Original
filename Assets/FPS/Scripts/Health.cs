using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [Tooltip("健康の最大量")]
    public float maxHealth = 10f;
    [Tooltip("クリティカルヘルスビネットが出現し始めるヘルス比率")]
    public float criticalHealthRatio = 0.3f;

    public UnityAction<float, GameObject> onDamaged;
    public UnityAction<float> onHealed;
    public UnityAction onDie;

    public float currentHealth { get; set; }
    //格納 小数値 現在の健康 {値を知りたい時はget,値を代入したい場合はsetを使用し、それぞれが呼び出されます}
    public bool invincible { get; set; }
    //格納 真偽値 無敵 {値を知りたい時はget,値を代入したい場合はsetを使用し、それぞれが呼び出されます}
    public bool canPickup() => currentHealth < maxHealth;
    //格納 真偽を表わす ピックアップできます => 現在の健康 < 最大健康

    public float getRatio() => currentHealth / maxHealth;
    //格納 小数値 比率を取得() => 現在の健康 / 最大健康
    public bool isCritical() => getRatio() <= criticalHealthRatio;
    //格納 真偽値 クリティカルです() => 比率を取得() <= クリティカルヘルス比

    bool m_IsDead;

    private void Start()
    //予約語　スタート時
    {
        currentHealth = maxHealth;
        //現在のヘルス = 最大ヘルス
    }

    //回復処理
    public void Heal(float healAmount)
    {
        float healthBefore = currentHealth;
        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        // call OnHeal action
        //癒しのアクションを呼び出す
        float trueHealAmount = currentHealth - healthBefore;
        if (trueHealAmount > 0f && onHealed != null)
        {
            onHealed.Invoke(trueHealAmount);
        }
    }

    //ダメージ処理
    public void TakeDamage(float damage, GameObject damageSource)
    //格納 ダメージを受ける(小数を表わすダメージ, ゲームオブジェクトダメージソース)
    {
        if (invincible)
        //もし(無敵)
            return;
        //返す


        /*パンチダメージ処理追加はじめ
        Debug.Log(damageSource.name);
        if (damageSource.name.Equals("PlayerCoclpit"))
        {
            
            Debug.Log(damageSource.name);
            damage = 100;
            //currentHealth -= damage;
            
        }
        パンチダメージ処理追加おわり
        */


        float healthBefore = currentHealth;
        //小数を表わす 以前の健康 = 現在の健康
        currentHealth -= damage;
        //現在の健康 -= ダメージ
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        //現在の健康 = 制限をかける(現在の健康, 0, 現在の健康)
        //Mathf.Clampを使ってHPに制限をかけている。
        //ダメージ値が大きいとき、HP値がマイナスなどにならないよう、値の制限をしている

        // call OnDamage action
        //ダメージアクションを呼び出します
        float trueDamageAmount = healthBefore - currentHealth;
        //小数値 真のダメージ量 = 以前の健康 - 現在の健康
        if (trueDamageAmount > 0f && onDamaged != null)
        //もし(真のダメージ量 > 0f 且つ onダメージ 同じじゃないとき null)
        {
            onDamaged.Invoke(trueDamageAmount, damageSource);
            //onダメージ 呼び出し（真のダメージ量、ダメージソース）
        }
        HandleDeath();
    }

    public void Kill()
    {
        currentHealth = 0f;

        // call OnDamage action
        // OnDamageアクションを呼び出します
        if (onDamaged != null)
        {
            onDamaged.Invoke(maxHealth, null);
            //ダメージを呼び出す（最大ヘルス、null）
        }

        HandleDeath();
    }

    private void HandleDeath()
    {
        if (m_IsDead)
            return;

        // call OnDie action
        // 死亡アクションを呼び出します
        if (currentHealth <= 0f)
        {
            if (onDie != null)
            {
                m_IsDead = true;
                onDie.Invoke();
            }
        }
    }
}
