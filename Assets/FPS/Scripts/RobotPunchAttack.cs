//
//マウスの中ボタンを押した時、ロボットのパンチアニメーションを再生させて
//コライダー（アタリ判定）を一時的にONにするスクリプト
//
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotPunchAttack : MonoBehaviour
{
    //PlayerのAnimatorコンポーネント保存用
    private Animator animator;
    //左手のコライダー
    private Collider handCollider;
 
	// Use this for initialization
	void Start () {
        //PlayerのAnimatorコンポーネントを取得する
        animator = GetComponent<Animator>();
        
        //左手のコライダーを取得
        handCollider = GameObject.Find("LeftHand").GetComponent<SphereCollider>();
	}
	
	// Update is called once per frame
	void Update () {
		
        //マウス中ボタンを押すとPunchアニメーションを実行
        if(Input.GetMouseButtonDown(2)){
            animator.SetBool("RobotArm_Punch", true);
		
        //左手コライダーをオンにする
        handCollider.enabled = true;

            //一定時間後にコライダーの機能をオフにする
            Invoke("ColliderReset",0.7f);
        }
	}
	
	//コライダーの設定をリセットする
    private void ColliderReset()
    {
        handCollider.enabled = false;
    }
}
