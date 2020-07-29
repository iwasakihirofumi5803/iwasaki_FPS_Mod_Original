using UnityEngine;
using UnityEngine.Events;

public class ProjectileBase : MonoBehaviour                         //発射台
{
    public GameObject owner { get; private set; }
    //格納　ゲームオブジェクト　オーナー{値・参照の取得}
    public Vector3 initialPosition { get; private set; }
    //格納　3Dベクトル　初期位置{値・参照の取得}
    public Vector3 initialDirection { get; private set; }
    //格納　3Dベクトル　初期方向{値・参照の取得}
    public Vector3 inheritedMuzzleVelocity { get; private set; }
    //格納　3Dベクトル　継承されたマズル速度{値・参照の取得}
    public float initialCharge { get; private set; }
    //格納　3Dベクトル　初期費用{値・参照の取得}

    public UnityAction onShoot;
    //格納　ユニティアクション　シュート

    public void Shoot(WeaponController controller)
    //格納　シュート (武器コントローラー コントローラー)
    {
        owner = controller.owner;
        //オーナー = コントローラー.オーナー
        initialPosition = transform.position;
        //初期位置 = 変身。ポジション
        initialDirection = transform.forward;
        //初期方向 = 変身。前方
        inheritedMuzzleVelocity = controller.muzzleWorldVelocity;
        //継承されたマズルベロシティ = コントローラー.継承されたマズルベロシティ
        initialCharge = controller.currentCharge;
        //初期費用 = コントローラー.現在の料金
        if (onShoot != null)
        //もし(シュートがNullと同じじゃない時)
        {
            onShoot.Invoke();
            //シュート.設定した時間に関数を呼び出す()
        }
    }
}
