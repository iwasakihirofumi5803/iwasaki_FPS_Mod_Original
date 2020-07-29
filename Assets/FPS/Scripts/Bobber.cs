//移動距離に対して、上下移動させるスクリプト
//ロボット移動時のカメラ上下移動表現に使用した。
//参考サイト：https://sharpcoderblog.com/blog/head-bobbing-effect-in-unity-3d?fbclid=IwAR0a-ecGK8jlHk1-Anp1Hsl5YGbJAVLsmUsSNNLuJybISToi_JWhHhJwwtU
	using UnityEngine;

	public class Bobber : MonoBehaviour
	{
	    public float walkingBobbingSpeed = 14f;			//移動スピードの初期設定
	    public float bobbingAmount = 0.05f;				//上下運動値の初期設定
	    //public SC_CharacterController controller;		//SC_CharacterControllerを取得
	    public PlayerCharacterController controller;	//PlayerCharacterControllerを取得

	    float defaultPosY = 0;							//defaultPosYを0にする
	    float timer = 0;								//timerを0にする

	    // Start is called before the first frame update
	    void Start()
	    {
	        defaultPosY = transform.localPosition.y;	//親の Transform オブジェクトから見たYの相対的な位置
	    }

	    // Update is called once per frame
	    void Update()
	    {
	    	//もし、controller.moveDirection.xの値が0.1fより大きい時
	    	//且つ、controller.moveDirection.zの値が0.1fより大きい時
	        //if(Mathf.Abs(controller.moveDirection.x) > 0.1f || Mathf.Abs(controller.moveDirection.z) > 0.1f)
	        if(Mathf.Abs(controller.characterVelocity.x) > 0.1f || Mathf.Abs(controller.characterVelocity.z) > 0.1f)
	        {
	            //Playerが動いていた時
	            timer += Time.deltaTime * walkingBobbingSpeed;
	            //上下移動値、処理
	            transform.localPosition = new Vector3(transform.localPosition.x, defaultPosY + Mathf.Sin(timer) * bobbingAmount, transform.localPosition.z);
	        }
	        else
	        {
	            //Playerが待機状態の時
	            timer = 0;
	            transform.localPosition = new Vector3(transform.localPosition.x, Mathf.Lerp(transform.localPosition.y, defaultPosY, Time.deltaTime * walkingBobbingSpeed), transform.localPosition.z);
	        }
	    }
	}