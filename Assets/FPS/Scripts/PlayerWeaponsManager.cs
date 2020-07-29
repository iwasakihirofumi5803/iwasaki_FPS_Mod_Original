using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(PlayerInputHandler))]
//RequireComponent：必要なコンポーネントを依存関係として自動的に加えます。
//(typeof(PlayerInputHandler))：PlayerInputHandlerクラスのタイプ
public class PlayerWeaponsManager : MonoBehaviour
//格納 クラスPlayerWeaponsManager : MonoBehaviourは、すべてのUnityスクリプトが派生する基本クラスです。
{
    public enum WeaponSwitchState
    //格納 複数の定数をひとつにまとめておくことができる型 武器切り替え状態
    {
        Up,
        Down,
        PutDownPrevious,
        PutUpNew,
    }

    [Tooltip("プレーヤーが開始する武器のリスト")]
    public List<WeaponController> startingWeapons = new List<WeaponController>();
    //格納 リスト<武器コントローラー> 開始武器 = 新しいリスト<武器コントローラー>

    [Header("参照")]
    [Tooltip("武器がジオメトリを投げるのを避けるために使用されるセカンダリカメラ")]
    public Camera weaponCamera;
    //格納 カメラ 武器カメラ
    [Tooltip("すべての武器が階層に追加される親変換")]
    public Transform weaponParentSocket;
    //格納 変換 武器カメラ 武器の親ソケット
    [Tooltip("有効であるが積極的に狙っていない場合の武器の位置")]
    public Transform defaultWeaponPosition;
    //格納 変換 デフォルトの武器位置
    [Tooltip("狙ったときの武器の位置")]
    public Transform aimingWeaponPosition;
    //格納 変換 狙ったときの武器位置
    [Tooltip("非アクティブな武器の位置")]
    public Transform downWeaponPosition;
    //格納 変換 下の武器位置

    [Header("武器の揺れ")]
    [Tooltip("プレイヤーが動いているときに画面内で武器が動き回る頻度")]
    public float bobFrequency = 10f;
    //格納 小数値 揺れの周波数 = 10f
    [Tooltip("武器揺れが適用される速度、大きい値が最も速い")]
    public float bobSharpness = 10f;
    //格納 小数値 揺れのシャープさ = 10f
    [Tooltip("照準していないときの武器揺れの距離")]
    public float defaultBobAmount = 0.05f;
    //格納 小数値 デフォルトの揺れの量 = 0.05f
    [Tooltip("狙ったときの武器ボブの距離")]
    public float aimingBobAmount = 0.02f;
    //格納 小数値 デフォルトの揺れの量 = 0.02f

    [Header("武器の反動")]
    [Tooltip("これは、反動が武器を移動する速度、値が大きいほど、最速に影響します")]
    public float recoilSharpness = 50f;
    //格納 小数値 反動のシャープさ = 50f
    [Tooltip("反動が武器に影響を与える可能性のある最大距離")]
    public float maxRecoilDistance = 0.5f;
    //格納 小数値 最大反動距離 = 0.5f
    [Tooltip("反動が終了した後、武器が元の位置に戻る速度")]
    public float recoilRestitutionSharpness = 10f;
    //格納 小数値 反動反発のシャープさ = 10f

    [Header("その他")]
    [Tooltip("照準アニメーションが再生される速度")]
    public float aimingAnimationSpeed = 10f;
    //格納 小数値 照準アニメーションの速さ = 10f
    [Tooltip("照準しない場合の視野")]
    public float defaultFOV = 60f;
    //格納 小数値 デフォルトの視野角 = 60f
    [Tooltip("武器のカメラに適用する通常の視野角の部分")]
    public float weaponFOVMultiplier = 1f;
    //格納 小数値 武器 視野角 乗数 = 1f
    [Tooltip("マウスホイールからの複数の入力を受け取らないように、武器をもう一度切り替える前に遅延する")]
    public float weaponSwitchDelay = 1f;
    //格納 小数値 武器切り替えの遅延 = 1f
    [Tooltip("FPS武器のgameObjectsを設定するレイヤー")]
    public LayerMask FPSWeaponLayer;
    //格納 任意のレイヤーとだけ衝突させたいと言った場合にはレイヤーマスクを使います。FPS武器レイヤー

    public bool isAiming { get; private set; }
    //格納 真偽値 エイミング {get:値・参照は他のクラスからも取得できる，private set：値・参照は自クラスのみ設定可能}
    public bool isPointingAtEnemy { get; private set; }
    //格納 真偽値 敵を指している {get:値・参照は他のクラスからも取得できる，private set：値・参照は自クラスのみ設定可能}
    public int activeWeaponIndex { get; private set; }
    //格納 整数型 アクティブな武器指数 {get:値・参照は他のクラスからも取得できる，private set：値・参照は自クラスのみ設定可能}

    public UnityAction<WeaponController> onSwitchedToWeapon;
    public UnityAction<WeaponController, int> onAddedWeapon;
    public UnityAction<WeaponController, int> onRemovedWeapon;

    WeaponController[] m_WeaponSlots = new WeaponController[9]; // 9つの使用可能な武器スロット
    PlayerInputHandler m_InputHandler;
    PlayerCharacterController m_PlayerCharacterController;
    float m_WeaponBobFactor;
    Vector3 m_LastCharacterPosition;
    Vector3 m_WeaponMainLocalPosition;
    Vector3 m_WeaponBobLocalPosition;
    Vector3 m_WeaponRecoilLocalPosition;
    Vector3 m_AccumulatedRecoil;
    float m_TimeStartedWeaponSwitch;
    WeaponSwitchState m_WeaponSwitchState;
    int m_WeaponSwitchNewWeaponIndex;

    private void Start()
    {
        activeWeaponIndex = -1;
        m_WeaponSwitchState = WeaponSwitchState.Down;

        m_InputHandler = GetComponent<PlayerInputHandler>();
        DebugUtility.HandleErrorIfNullGetComponent<PlayerInputHandler, PlayerWeaponsManager>(m_InputHandler, this, gameObject);

        m_PlayerCharacterController = GetComponent<PlayerCharacterController>();
        DebugUtility.HandleErrorIfNullGetComponent<PlayerCharacterController, PlayerWeaponsManager>(m_PlayerCharacterController, this, gameObject);

        SetFOV(defaultFOV);

        onSwitchedToWeapon += OnWeaponSwitched;

        // Add starting weapons
        //開始武器を追加します
        foreach (var weapon in startingWeapons)
        {
            AddWeapon(weapon);
        }
        SwitchWeapon(true);
    }

    private void Update()
    {
        // shoot handling
        //撮影処理
        WeaponController activeWeapon = GetActiveWeapon();

        if (activeWeapon && m_WeaponSwitchState == WeaponSwitchState.Up)
        {
            // handle aiming down sights
            //下向きの照準を処理します
            isAiming = m_InputHandler.GetAimInputHeld();

            // handle shooting
            //射撃を処理する
            bool hasFired = activeWeapon.HandleShootInputs(
                m_InputHandler.GetFireInputDown(),
                m_InputHandler.GetFireInputHeld(),
                m_InputHandler.GetFireInputReleased());

            // Handle accumulating recoil
            //蓄積された反動を処理します
            if (hasFired)
            {
                m_AccumulatedRecoil += Vector3.back * activeWeapon.recoilForce;
                //蓄積反動 += 3Dベクトルを返す。* アクティブな武器.反動力;
                m_AccumulatedRecoil = Vector3.ClampMagnitude(m_AccumulatedRecoil, maxRecoilDistance);
                //蓄積反動 = 3Dベクトル。 クランプの大きさ（累積反動、最大反動距離）;
            }
        }

        // weapon switch handling
        //武器切り替え処理
        if (!isAiming &&
            (activeWeapon == null || !activeWeapon.isCharging) &&
            (m_WeaponSwitchState == WeaponSwitchState.Up || m_WeaponSwitchState == WeaponSwitchState.Down))
        {
            int switchWeaponInput = m_InputHandler.GetSwitchWeaponInput();
            if (switchWeaponInput != 0)
            {
                bool switchUp = switchWeaponInput > 0;
                SwitchWeapon(switchUp);
            }
            else
            {
                switchWeaponInput = m_InputHandler.GetSelectWeaponInput();
                if (switchWeaponInput != 0)
                {
                    if (GetWeaponAtSlotIndex(switchWeaponInput - 1) != null)
                        SwitchToWeaponIndex(switchWeaponInput - 1);
                }
            }
        }

        // Pointing at enemy handling
        //敵の扱いを指さす
        isPointingAtEnemy = false;
        //敵を指している = 非表示
        if (activeWeapon)
        //もし、アクティブな武器
        {
            if (Physics.Raycast(weaponCamera.transform.position, weaponCamera.transform.forward, out RaycastHit hit, 1000, -1, QueryTriggerInteraction.Ignore))
            //もし(物理レイキャスト(武器カメラ.変換.位置,　武器カメラ.変換.先端,外レイキャストヒット,　1000,-1,クエリはトリガーの発生を報告しません))
            {
                if (hit.collider.GetComponentInParent<EnemyController>())
                //もし(ヒット.コライダー.コンポーネントを親から取得<EnemyController>)
                {
                    isPointingAtEnemy = true;
                    //敵を指している = 表示
                }
            }
        }
    }


    // Update various animated features in LateUpdate because it needs to override the animated arm position
    //アニメートされた腕の位置をオーバーライドする必要があるため、LateUpdateのさまざまなアニメートされた機能を更新します
    private void LateUpdate()
    //LateUpdate は Update 関数が呼び出された後に実行されます。
    //例えばカメラを追従するには Update で移動された結果を基に常に LateUpdate で位置を更新する必要があります。
    {
        UpdateWeaponAiming();
        UpdateWeaponBob();
        UpdateWeaponRecoil();
        UpdateWeaponSwitching();

        // Set final weapon socket position based on all the combined animation influences
        //結合されたすべてのアニメーションの影響に基づいて、最終的な武器ソケット位置を設定します
        weaponParentSocket.localPosition = m_WeaponMainLocalPosition + m_WeaponBobLocalPosition + m_WeaponRecoilLocalPosition;
    }

    // Sets the FOV of the main camera and the weapon camera simultaneously
    //メインカメラと武器カメラの視野を同時に設定します
    public void SetFOV(float fov)
    {
        m_PlayerCharacterController.playerCamera.fieldOfView = fov;
        weaponCamera.fieldOfView = fov * weaponFOVMultiplier;
    }

    // Iterate on all weapon slots to find the next valid weapon to switch to
    //すべての武器スロットを反復して、次に切り替える有効な武器を見つけます
    public void SwitchWeapon(bool ascendingOrder)
    {
        int newWeaponIndex = -1;
        int closestSlotDistance = m_WeaponSlots.Length;
        for (int i = 0; i < m_WeaponSlots.Length; i++)
        {
            // If the weapon at this slot is valid, calculate its "distance" from the active slot index (either in ascending or descending order)
            //このスロットの武器が有効な場合、アクティブなスロットインデックスから「距離」を計算します（昇順または降順）
            // and select it if it's the closest distance yet
            //それが最も近い距離であれば、それを選択します
            if (i != activeWeaponIndex && GetWeaponAtSlotIndex(i) != null)
            {
                int distanceToActiveIndex = GetDistanceBetweenWeaponSlots(activeWeaponIndex, i, ascendingOrder);

                if (distanceToActiveIndex < closestSlotDistance)
                {
                    closestSlotDistance = distanceToActiveIndex;
                    newWeaponIndex = i;
                }
            }
        }

        // Handle switching to the new weapon index
        //新しい武器インデックスへの切り替えを処理します
        SwitchToWeaponIndex(newWeaponIndex);
    }

    // Switches to the given weapon index in weapon slots if the new index is a valid weapon that is different from our current one
    //新しいインデックスが現在のものとは異なる有効な武器である場合、武器スロット内の指定された武器インデックスに切り替えます
    public void SwitchToWeaponIndex(int newWeaponIndex, bool force = false)
    {
        if (force || (newWeaponIndex != activeWeaponIndex && newWeaponIndex >= 0))
        {
            // Store data related to weapon switching animation
            //武器切り替えアニメーションに関連するデータを保存します
            m_WeaponSwitchNewWeaponIndex = newWeaponIndex;
            m_TimeStartedWeaponSwitch = Time.time;

            // Handle case of switching to a valid weapon for the first time (simply put it up without putting anything down first)
            //初めて有効な武器に切り替えるケースを処理します（最初に何も置かずに単に置くだけです）
            if (GetActiveWeapon() == null)
            {
                m_WeaponMainLocalPosition = downWeaponPosition.localPosition;
                m_WeaponSwitchState = WeaponSwitchState.PutUpNew;
                activeWeaponIndex = m_WeaponSwitchNewWeaponIndex;

                WeaponController newWeapon = GetWeaponAtSlotIndex(m_WeaponSwitchNewWeaponIndex);
                if (onSwitchedToWeapon != null)
                {
                    onSwitchedToWeapon.Invoke(newWeapon);
                }
            }
            // otherwise, remember we are putting down our current weapon for switching to the next one
            //そうでなければ、次の武器に切り替えるために現在の武器を置いていることを思い出してください
            else
            {
                m_WeaponSwitchState = WeaponSwitchState.PutDownPrevious;
            }
        }
    }

    public bool HasWeapon(WeaponController weaponPrefab)
    {
        // Checks if we already have a weapon coming from the specified prefab
        //指定されたプレハブからの武器がすでにあるかどうかを確認します
        foreach (var w in m_WeaponSlots)
        {
            if(w != null && w.sourcePrefab == weaponPrefab.gameObject)
            {
                return true;
            }
        }

        return false;
    }

    // Updates weapon position and camera FoV for the aiming transition
    //照準遷移の武器位置とカメラFoVを更新します
    void UpdateWeaponAiming()
    {
        if (m_WeaponSwitchState == WeaponSwitchState.Up)
        {
            WeaponController activeWeapon = GetActiveWeapon();
            if (isAiming && activeWeapon)
            {
                m_WeaponMainLocalPosition = Vector3.Lerp(m_WeaponMainLocalPosition, aimingWeaponPosition.localPosition + activeWeapon.aimOffset, aimingAnimationSpeed * Time.deltaTime);
                SetFOV(Mathf.Lerp(m_PlayerCharacterController.playerCamera.fieldOfView, activeWeapon.aimZoomRatio * defaultFOV, aimingAnimationSpeed * Time.deltaTime));
            }
            else
            {
                m_WeaponMainLocalPosition = Vector3.Lerp(m_WeaponMainLocalPosition, defaultWeaponPosition.localPosition, aimingAnimationSpeed * Time.deltaTime);
                SetFOV(Mathf.Lerp(m_PlayerCharacterController.playerCamera.fieldOfView, defaultFOV, aimingAnimationSpeed * Time.deltaTime));
            }
        }
    }

    // Updates the weapon bob animation based on character speed
    //キャラクターの速度に基づいて武器のボブアニメーションを更新します
    void UpdateWeaponBob()
    {
        if (Time.deltaTime > 0f)
        {
            Vector3 playerCharacterVelocity = (m_PlayerCharacterController.transform.position - m_LastCharacterPosition) / Time.deltaTime;

            // calculate a smoothed weapon bob amount based on how close to our max grounded movement velocity we are
            //最大の地上移動速度にどれだけ近いかに基づいて、平滑化された武器のボブの量を計算します
            float characterMovementFactor = 0f;
            if (m_PlayerCharacterController.isGrounded)
            {
                characterMovementFactor = Mathf.Clamp01(playerCharacterVelocity.magnitude / (m_PlayerCharacterController.maxSpeedOnGround * m_PlayerCharacterController.sprintSpeedModifier));
            }
            m_WeaponBobFactor = Mathf.Lerp(m_WeaponBobFactor, characterMovementFactor, bobSharpness * Time.deltaTime);

            // Calculate vertical and horizontal weapon bob values based on a sine function
            //正弦関数に基づいて、武器の垂直および水平のボブ値を計算します
            float bobAmount = isAiming ? aimingBobAmount : defaultBobAmount;
            float frequency = bobFrequency;
            float hBobValue = Mathf.Sin(Time.time * frequency) * bobAmount * m_WeaponBobFactor;
            float vBobValue = ((Mathf.Sin(Time.time * frequency * 2f) * 0.5f) + 0.5f) * bobAmount * m_WeaponBobFactor;

            // Apply weapon bob
            //武器bobを適用します
            m_WeaponBobLocalPosition.x = hBobValue;
            m_WeaponBobLocalPosition.y = Mathf.Abs(vBobValue);

            m_LastCharacterPosition = m_PlayerCharacterController.transform.position;
        }
    }

    // Updates the weapon recoil animation
    //武器の反動アニメーションを更新します
    void UpdateWeaponRecoil()
    {
        // if the accumulated recoil is further away from the current position, make the current position move towards the recoil target
        //蓄積された反動が現在の位置からさらに離れている場合は、現在の位置を反動の目標に向かって移動させます
        if (m_WeaponRecoilLocalPosition.z >= m_AccumulatedRecoil.z * 0.99f)
        {
            m_WeaponRecoilLocalPosition = Vector3.Lerp(m_WeaponRecoilLocalPosition, m_AccumulatedRecoil, recoilSharpness * Time.deltaTime);
        }
        // otherwise, move recoil position to make it recover towards its resting pose
        //それ以外の場合は、リコイル位置を移動して、静止ポーズに向かって回復します
        else
        {
            m_WeaponRecoilLocalPosition = Vector3.Lerp(m_WeaponRecoilLocalPosition, Vector3.zero, recoilRestitutionSharpness * Time.deltaTime);
            m_AccumulatedRecoil = m_WeaponRecoilLocalPosition;
        }
    }

    // Updates the animated transition of switching weapons
    //武器の切り替えのアニメーション化された遷移を更新します
    void UpdateWeaponSwitching()
    {
        // Calculate the time ratio (0 to 1) since weapon switch was triggered
        //武器の切り替えがトリガーされてからの時間の比率（0から1）を計算します
        float switchingTimeFactor = 0f;
        if (weaponSwitchDelay == 0f)
        {
            switchingTimeFactor = 1f;
        }
        else
        {
            switchingTimeFactor = Mathf.Clamp01((Time.time - m_TimeStartedWeaponSwitch) / weaponSwitchDelay);
        }

        // Handle transiting to new switch state
        //新しいスイッチ状態への移行を処理します
        if (switchingTimeFactor >= 1f)
        {
            if (m_WeaponSwitchState == WeaponSwitchState.PutDownPrevious)
            {
                // Deactivate old weapon
                //古い武器を無効にします
                WeaponController oldWeapon = GetWeaponAtSlotIndex(activeWeaponIndex);
                if (oldWeapon != null)
                {
                    oldWeapon.ShowWeapon(false);
                }

                activeWeaponIndex = m_WeaponSwitchNewWeaponIndex;
                switchingTimeFactor = 0f;

                // Activate new weapon
                //新しい武器をアクティブにします
                WeaponController newWeapon = GetWeaponAtSlotIndex(activeWeaponIndex);
                if (onSwitchedToWeapon != null)
                {
                    onSwitchedToWeapon.Invoke(newWeapon);
                }

                if(newWeapon)
                {
                    m_TimeStartedWeaponSwitch = Time.time;
                    m_WeaponSwitchState = WeaponSwitchState.PutUpNew;
                }
                else
                {
                    // if new weapon is null, don't follow through with putting weapon back up
                    //新しい武器がnullの場合、武器を元に戻しません。
                    m_WeaponSwitchState = WeaponSwitchState.Down;
                }
            }
            else if (m_WeaponSwitchState == WeaponSwitchState.PutUpNew)
            {
                m_WeaponSwitchState = WeaponSwitchState.Up;
            }
        }

        // Handle moving the weapon socket position for the animated weapon switching
        //アニメーション化された武器切り替えのための武器ソケット位置の移動を処理します
        if (m_WeaponSwitchState == WeaponSwitchState.PutDownPrevious)
        {
            m_WeaponMainLocalPosition = Vector3.Lerp(defaultWeaponPosition.localPosition, downWeaponPosition.localPosition, switchingTimeFactor);
        }
        else if (m_WeaponSwitchState == WeaponSwitchState.PutUpNew)
        {
            m_WeaponMainLocalPosition = Vector3.Lerp(downWeaponPosition.localPosition, defaultWeaponPosition.localPosition, switchingTimeFactor);
        }
    }

    // Adds a weapon to our inventory
    //武器をインベントリに追加します
    public bool AddWeapon(WeaponController weaponPrefab)
    {
        // if we already hold this weapon type (a weapon coming from the same source prefab), don't add the weapon
        //この武器タイプ（同じソースプレハブからの武器）を既に保持している場合は、武器を追加しないでください
        if (HasWeapon(weaponPrefab))
        {
            return false;
        }

        // search our weapon slots for the first free one, assign the weapon to it, and return true if we found one. Return false otherwise
        //最初の空きスロットを探して武器スロットを探し、それに武器を割り当て、見つかった場合はtrueを返します。 それ以外の場合はfalseを返します
        for (int i = 0; i < m_WeaponSlots.Length; i++)
        {
            // only add the weapon if the slot is free
            //スロットが空いている場合にのみ武器を追加します
            if (m_WeaponSlots[i] == null)
            {
                // spawn the weapon prefab as child of the weapon socket
                //武器プレハブを武器ソケットの子として生成します
                WeaponController weaponInstance = Instantiate(weaponPrefab, weaponParentSocket);
                weaponInstance.transform.localPosition = Vector3.zero;
                weaponInstance.transform.localRotation = Quaternion.identity;

                // Set owner to this gameObject so the weapon can alter projectile/damage logic accordingly
                //所有者をこのgameObjectに設定して、武器が発射物/損傷ロジックを適宜変更できるようにします
                weaponInstance.owner = gameObject;
                weaponInstance.sourcePrefab = weaponPrefab.gameObject;
                weaponInstance.ShowWeapon(false);

                // Assign the first person layer to the weapon
                //一人称レイヤーを武器に割り当てます
                int layerIndex = Mathf.RoundToInt(Mathf.Log(FPSWeaponLayer.value, 2));
                //↑This function converts a layermask to a layer index
                //↑この関数は、レイヤーマスクをレイヤーインデックスに変換します
                foreach (Transform t in weaponInstance.gameObject.GetComponentsInChildren<Transform>(true))
                //foreach:繰り返しループ処理 (変換 tを武器のゲームオブジェクトインスタンスに変換します。子のコンポーネントを取得<変換>（true）)
                {
                    t.gameObject.layer = layerIndex;
                    //t.ゲームオブジェクトレイヤー = レイヤーインデックス;
                }

                m_WeaponSlots[i] = weaponInstance;
                //m_武器スロット[i] = 武器インスタンス

                if(onAddedWeapon != null)
                //もし、追加された武器がNullじゃなかったら
                {
                    onAddedWeapon.Invoke(weaponInstance, i);
                    //追加された武器を呼び出す（武器インスタンス、i）
                }

                return true;
                //trueを返す
            }
        }

        // Handle auto-switching to weapon if no weapons currently
        //現在武器がない場合は、武器への自動切り替えを処理します
        if (GetActiveWeapon() == null)
        {
            SwitchWeapon(true);
        }

        return false;
    }

    public bool RemoveWeapon(WeaponController weaponInstance)
    {
        // Look through our slots for that weapon
        //その武器のスロットを調べます
        for (int i = 0; i < m_WeaponSlots.Length; i++)
        {
            // when weapon found, remove it
            //武器が見つかったら、削除します
            if (m_WeaponSlots[i] == weaponInstance)
            {
                m_WeaponSlots[i] = null;

                if (onRemovedWeapon != null)
                {
                    onRemovedWeapon.Invoke(weaponInstance, i);
                }

                Destroy(weaponInstance.gameObject);

                // Handle case of removing active weapon (switch to next weapon)
                //アクティブな武器を削除するケースを処理します（次の武器に切り替えます）
                if (i == activeWeaponIndex)
                {
                    SwitchWeapon(true);
                }

                return true; 
            }
        }

        return false;
    }

    public WeaponController GetActiveWeapon()
    {
        return GetWeaponAtSlotIndex(activeWeaponIndex);
    }

    public WeaponController GetWeaponAtSlotIndex(int index)
    {
        // find the active weapon in our weapon slots based on our active weapon index
        //アクティブな武器のインデックスに基づいて、武器スロットでアクティブな武器を見つけます
        if (index >= 0 &&
            index < m_WeaponSlots.Length)
        {
            return m_WeaponSlots[index];
        }

        // if we didn't find a valid active weapon in our weapon slots, return null
        //武器スロットで有効な有効な武器が見つからなかった場合は、nullを返します
        return null;
    }

    // Calculates the "distance" between two weapon slot indexes
    // 2つの武器スロットインデックス間の「距離」を計算します
    // For example: if we had 5 weapon slots, the distance between slots #2 and #4 would be 2 in ascending order, and 3 in descending order
    //例：5つの武器スロットがある場合、スロット＃2と＃4の間の距離は昇順で2、降順で3になります。
    int GetDistanceBetweenWeaponSlots(int fromSlotIndex, int toSlotIndex, bool ascendingOrder)
    {
        int distanceBetweenSlots = 0;

        if (ascendingOrder)
        {
            distanceBetweenSlots = toSlotIndex - fromSlotIndex;
        }
        else
        {
            distanceBetweenSlots = -1 * (toSlotIndex - fromSlotIndex);
        }

        if (distanceBetweenSlots < 0)
        {
            distanceBetweenSlots = m_WeaponSlots.Length + distanceBetweenSlots;
        }

        return distanceBetweenSlots;
    }

    void OnWeaponSwitched(WeaponController newWeapon)
    {
        if (newWeapon != null)
        {
            newWeapon.ShowWeapon(true);
        }
    }
}
