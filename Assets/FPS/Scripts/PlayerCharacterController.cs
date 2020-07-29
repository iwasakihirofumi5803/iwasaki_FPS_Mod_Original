using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CharacterController), typeof(PlayerInputHandler), typeof(AudioSource))]
public class PlayerCharacterController : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("プレーヤーに使用されるメインカメラへの参照")]
    public Camera playerCamera;
    [Tooltip("足音、ジャンプなどのオーディオソース...")]
    public AudioSource audioSource;

    [Header("一般的な")]
    [Tooltip("空中にいるときに下向きに力がかかる")]
    public float gravityDownForce = 20f;
    [Tooltip("プレイヤーが接地していると見なすためにチェックされた物理レイヤー")]
    public LayerMask groundCheckLayers = -1;
    [Tooltip("キャラクタコントローラカプセルの底部からの接地距離をテストする距離")]
    public float groundCheckDistance = 0.05f;

    [Header("移動")]
    [Tooltip("接地時の最大移動速度（全力疾走時）")]
    public float maxSpeedOnGround = 10f;
    [Tooltip("接地時の動きの鋭さ、低い値はプレイヤーがゆっくりと加速および減速するようにし、高い値は反対になります")]
    public float movementSharpnessOnGround = 15;
    [Tooltip("しゃがみ時の最大移動速度")]
    [Range(0,1)]
    public float maxSpeedCrouchedRatio = 0.5f;
    [Tooltip("接地されていない場合の最大移動速度")]
    public float maxSpeedInAir = 10f;
    [Tooltip("空中での加速速度")]
    public float accelerationSpeedInAir = 25f;
    [Tooltip("スプリント速度の乗数（接地速度に基づく）")]
    public float sprintSpeedModifier = 2f;
    [Tooltip("マップから落ちたときにプレーヤーがすぐに死ぬ高さ")]
    public float killHeight = -50f;

    [Header("回転")]
    [Tooltip("カメラを動かすための回転速度")]
    public float rotationSpeed = 200f;
    [Range(0.1f, 1f)]
    [Tooltip("照準時の回転速度乗数")]
    public float aimingRotationMultiplier = 0.4f;

    [Header("ジャンプ")]
    [Tooltip("ジャンプ時に上向きに力がかかる")]
    public float jumpForce = 9f;

    [Header("スタンス")]
    [Tooltip("カメラが置かれるキャラクターの高さの比率（0-1）")]
    public float cameraHeightRatio = 0.9f;
    [Tooltip("立っているときのキャラクターの高さ")]
    public float capsuleHeightStanding = 1.8f;
    [Tooltip("しゃがみ時のキャラクターの高さ")]
    public float capsuleHeightCrouching = 0.9f;
    [Tooltip("しゃがみ遷移の速度")]
    public float crouchingSharpness = 10f;

    [Header("オーディオ")]
    [Tooltip("1メートル移動したときに再生される足音の量")]
    public float footstepSFXFrequency = 1f;
    [Tooltip("疾走中に1メートル移動したときに再生される足音の量")]
    public float footstepSFXFrequencyWhileSprinting = 1f;
    [Tooltip("足音を鳴らす音")]
    public AudioClip footstepSFX;
    [Tooltip("ジャンプ時に鳴る音")]
    public AudioClip jumpSFX;
    [Tooltip("着陸時に鳴る音")]
    public AudioClip landSFX;
    [Tooltip("落下によるダメージを受けたときに鳴る音")]
    public AudioClip fallDamageSFX;

    [Header("ダメージを受ける")]
    [Tooltip("高速で地面を打ったときにプレイヤーがダメージを受けるかどうか")]
    public bool recievesFallDamage;
    [Tooltip("落下ダメージを受ける最低落下速度")]
    public float minSpeedForFallDamage = 10f;
    [Tooltip("最大量の落下ダメージを受ける落下速度")]
    public float maxSpeedForFallDamage = 30f;
    [Tooltip("最低速度で落下したときに受けるダメージ")]
    public float fallDamageAtMinSpeed = 10f;
    [Tooltip("最高速度で落下したときに受けるダメージ")]
    public float fallDamageAtMaxSpeed = 50f;

    public UnityAction<bool> onStanceChanged;

    public Vector3 characterVelocity { get; set; }
    //プレイヤーキャラクターの向き
    public bool isGrounded { get; private set; }
    //プレイヤーキャラクターが地上か、空中か
    public bool hasJumpedThisFrame { get; private set; }
    //プレイヤーキャラクターがジャンプ中か
    public bool isDead { get; private set; }
    //プレイヤーキャラクターが死んでいるか
    public bool isCrouching { get; private set; }
    //プレイヤーキャラクターがしゃがんでいるか
    public float RotationMultiplier
    {
        get
        {
            if (m_WeaponsManager.isAiming)
            {
                return aimingRotationMultiplier;
            }

            return 1f;
        }
    }
        
    Health m_Health;
    PlayerInputHandler m_InputHandler;
    CharacterController m_Controller;
    PlayerWeaponsManager m_WeaponsManager;
    Actor m_Actor;
    Vector3 m_GroundNormal;
    Vector3 m_CharacterVelocity;
    Vector3 m_LatestImpactSpeed;
    float m_LastTimeJumped = 0f;
    float m_CameraVerticalAngle = 0f;
    float m_footstepDistanceCounter;
    float m_TargetCharacterHeight;

    const float k_JumpGroundingPreventionTime = 0.2f;
    const float k_GroundCheckDistanceInAir = 0.07f;

    void Start()
    {
        // fetch components on the same gameObject
        //同じgameObjectでコンポーネントをフェッチします
        m_Controller = GetComponent<CharacterController>();
        DebugUtility.HandleErrorIfNullGetComponent<CharacterController, PlayerCharacterController>(m_Controller, this, gameObject);

        m_InputHandler = GetComponent<PlayerInputHandler>();
        DebugUtility.HandleErrorIfNullGetComponent<PlayerInputHandler, PlayerCharacterController>(m_InputHandler, this, gameObject);

        m_WeaponsManager = GetComponent<PlayerWeaponsManager>();
        DebugUtility.HandleErrorIfNullGetComponent<PlayerWeaponsManager, PlayerCharacterController>(m_WeaponsManager, this, gameObject);

        m_Health = GetComponent<Health>();
        DebugUtility.HandleErrorIfNullGetComponent<Health, PlayerCharacterController>(m_Health, this, gameObject);

        m_Actor = GetComponent<Actor>();
        DebugUtility.HandleErrorIfNullGetComponent<Actor, PlayerCharacterController>(m_Actor, this, gameObject);

        m_Controller.enableOverlapRecovery = true;

        m_Health.onDie += OnDie;

        // force the crouch state to false when starting
        //開始時にクラウチの状態をfalseに強制します
        SetCrouchingState(false, true);
        UpdateCharacterHeight(true);
    }

    void Update()
    {
        // check for Y kill
        // Yキルをチェックします
        if (!isDead && transform.position.y < killHeight)
        {
            m_Health.Kill();
        }

        hasJumpedThisFrame = false;

        bool wasGrounded = isGrounded;
        GroundCheck();

        // landing
        //着陸
        if (isGrounded && !wasGrounded)
        {
            // Fall damage
            // ダメージを受ける
            float fallSpeed = -Mathf.Min(characterVelocity.y, m_LatestImpactSpeed.y);
            float fallSpeedRatio = (fallSpeed - minSpeedForFallDamage) / (maxSpeedForFallDamage - minSpeedForFallDamage);
            if (recievesFallDamage && fallSpeedRatio > 0f)
            {
                float dmgFromFall = Mathf.Lerp(fallDamageAtMinSpeed, fallDamageAtMaxSpeed, fallSpeedRatio);
                m_Health.TakeDamage(dmgFromFall, null);

                // fall damage SFX
                //落下ダメージSFX
                audioSource.PlayOneShot(fallDamageSFX);
            }
            else
            {
                // land SFX
                // SFXを着陸させる
                audioSource.PlayOneShot(landSFX);
            }
        }

        // crouching
        //しゃがみ
        if (m_InputHandler.GetCrouchInputDown())
        {
            SetCrouchingState(!isCrouching, false);
        }

        UpdateCharacterHeight(false);

        HandleCharacterMovement();
    }

    void OnDie()
    {
        isDead = true;

        // Tell the weapons manager to switch to a non-existing weapon in order to lower the weapon
        //武器マネージャに、武器を下げるために存在しない武器に切り替えるように伝えます
        m_WeaponsManager.SwitchToWeaponIndex(-1, true);
    }

    void GroundCheck()
    {
        // Make sure that the ground check distance while already in air is very small, to prevent suddenly snapping to ground
        //すでに地上にいるときの地上チェック距離が非常に小さいことを確認して、突然地面にスナップしないようにします
        float chosenGroundCheckDistance = isGrounded ? (m_Controller.skinWidth + groundCheckDistance) : k_GroundCheckDistanceInAir;

        // reset values before the ground check
        isGrounded = false;
        m_GroundNormal = Vector3.up;

        // only try to detect ground if it's been a short amount of time since last jump; otherwise we may snap to the ground instantly after we try jumping
        //地面を検出しようとするのは、最後のジャンプから短時間である場合のみです。 そうしないと、ジャンプしようとした直後に地面にスナップする可能性があります
        if (Time.time >= m_LastTimeJumped + k_JumpGroundingPreventionTime)
        {
            // if we're grounded, collect info about the ground normal with a downward capsule cast representing our character capsule
            //接地されている場合は、キャラクターカプセルを表す下向きのカプセルキャストで地面の法線に関する情報を収集します
            if (Physics.CapsuleCast(GetCapsuleBottomHemisphere(), GetCapsuleTopHemisphere(m_Controller.height), m_Controller.radius, Vector3.down, out RaycastHit hit, chosenGroundCheckDistance, groundCheckLayers, QueryTriggerInteraction.Ignore))
            {
                // storing the upward direction for the surface found
                //見つかったサーフェスの上方向を保存します
                m_GroundNormal = hit.normal;

                // Only consider this a valid ground hit if the ground normal goes in the same direction as the character up
                //地面の法線がキャラクターの上方向と同じ方向にある場合にのみ、これを有効な地面ヒットと見なします
                // and if the slope angle is lower than the character controller's limit
                //傾斜角度がキャラクターコントローラーの制限よりも小さい場合
                if (Vector3.Dot(hit.normal, transform.up) > 0f &&
                    IsNormalUnderSlopeLimit(m_GroundNormal))
                {
                    isGrounded = true;

                    // handle snapping to the ground
                    //地面へのスナップを処理します
                    if (hit.distance > m_Controller.skinWidth)
                    {
                        m_Controller.Move(Vector3.down * hit.distance);
                    }
                }
            }
        }
    }

    void HandleCharacterMovement()
    {
        // horizontal character rotation
        //水平キャラクター回転
        {
            // rotate the transform with the input speed around its local Y axis
            //ローカルY軸を中心に入力速度で変換を回転します
            transform.Rotate(new Vector3(0f, (m_InputHandler.GetLookInputsHorizontal() * rotationSpeed * RotationMultiplier), 0f), Space.Self);
        }

        // vertical camera rotation
        //垂直カメラ回転
        {
            // add vertical inputs to the camera's vertical angle
            //カメラの垂直角度に垂直入力を追加します
            m_CameraVerticalAngle += m_InputHandler.GetLookInputsVertical() * rotationSpeed * RotationMultiplier;

            // limit the camera's vertical angle to min/max
            //カメラの垂直角度を最小/最大に制限します
            m_CameraVerticalAngle = Mathf.Clamp(m_CameraVerticalAngle, -89f, 89f);

            // apply the vertical angle as a local rotation to the camera transform along its right axis (makes it pivot up and down)
            //垂直軸をローカルの回転としてカメラの変換の右軸に沿って適用します（上下に回転させます）
            playerCamera.transform.localEulerAngles = new Vector3(m_CameraVerticalAngle, 0, 0);
        }

        // character movement handling
        //キャラクターの動きの処理
        bool isSprinting = m_InputHandler.GetSprintInputHeld();
        {
            if (isSprinting)
            {
                isSprinting = SetCrouchingState(false, false);
            }

            float speedModifier = isSprinting ? sprintSpeedModifier : 1f;

            // converts move input to a worldspace vector based on our character's transform orientation
            //キャラクターの変換方向に基づいて移動入力をワールドスペースベクトルに変換します
            Vector3 worldspaceMoveInput = transform.TransformVector(m_InputHandler.GetMoveInput());

            // handle grounded movement
            //固定された動きを処理する
            if (isGrounded)
            {
                // calculate the desired velocity from inputs, max speed, and current slope
                //入力、最大速度、現在の勾配から目的の速度を計算します
                Vector3 targetVelocity = worldspaceMoveInput * maxSpeedOnGround * speedModifier;
                // reduce speed if crouching by crouch speed ratio
                //しゃがむ速度比でしゃがむ場合は速度を下げる
                if (isCrouching)
                    targetVelocity *= maxSpeedCrouchedRatio;
                targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, m_GroundNormal) * targetVelocity.magnitude;

                // smoothly interpolate between our current velocity and the target velocity based on acceleration speed
                //加速速度に基づいて現在の速度と目標速度をスムーズに補間します
                characterVelocity = Vector3.Lerp(characterVelocity, targetVelocity, movementSharpnessOnGround * Time.deltaTime);

                // jumping
                // ジャンピング
                if (isGrounded && m_InputHandler.GetJumpInputDown())
                {
                    // force the crouch state to false
                    //クラウチ状態をfalseに強制します
                    if (SetCrouchingState(false, false))
                    {
                        // start by canceling out the vertical component of our velocity
                        //速度の垂直成分をキャンセルすることから始めます
                        characterVelocity = new Vector3(characterVelocity.x, 0f, characterVelocity.z);

                        // then, add the jumpSpeed value upwards
                        //次に、jumpSpeed値を上に追加します
                        characterVelocity += Vector3.up * jumpForce;

                        // play sound
                        // 音を出す
                        audioSource.PlayOneShot(jumpSFX);

                        // remember last time we jumped because e need to prevent snapping to ground for a short time
                        //短時間ジャンプして地面にスナップしないようにする必要があるため、前回ジャンプしたときのことを思い出してください
                        m_LastTimeJumped = Time.time;
                        hasJumpedThisFrame = true;

                        // Force grounding to false
                        //強制的に接地します
                        isGrounded = false;
                        m_GroundNormal = Vector3.up;
                    }
                }

                // footsteps sound
                //足音
                float chosenFootstepSFXFrequency = (isSprinting ? footstepSFXFrequencyWhileSprinting : footstepSFXFrequency);
                if (m_footstepDistanceCounter >= 1f / chosenFootstepSFXFrequency)
                {
                    m_footstepDistanceCounter = 0f;
                    audioSource.PlayOneShot(footstepSFX);
                }

                // keep track of distance traveled for footsteps sound
                //足音音のために移動距離を追跡します
                m_footstepDistanceCounter += characterVelocity.magnitude * Time.deltaTime;
            }
            // handle air movement
            //空中の動きを処理します
            else
            {
                //空中加速度を追加します
                characterVelocity += worldspaceMoveInput * accelerationSpeedInAir * Time.deltaTime;

                // limit air speed to a maximum, but only horizontally
                //風速を最大に制限しますが、水平方向のみに制限します
                float verticalVelocity = characterVelocity.y;
                Vector3 horizontalVelocity = Vector3.ProjectOnPlane(characterVelocity, Vector3.up);
                horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, maxSpeedInAir * speedModifier);
                characterVelocity = horizontalVelocity + (Vector3.up * verticalVelocity);

                // apply the gravity to the velocity
                //重力を速度に適用します
                characterVelocity += Vector3.down * gravityDownForce * Time.deltaTime;
            }
        }

        // apply the final calculated velocity value as a character movement
        //最終的に計算された速度値をキャラクターの動きとして適用します
        Vector3 capsuleBottomBeforeMove = GetCapsuleBottomHemisphere();
        Vector3 capsuleTopBeforeMove = GetCapsuleTopHemisphere(m_Controller.height);
        m_Controller.Move(characterVelocity * Time.deltaTime);

        // detect obstructions to adjust velocity accordingly
        //障害物を検出し、それに応じて速度を調整します
        m_LatestImpactSpeed = Vector3.zero;
        if (Physics.CapsuleCast(capsuleBottomBeforeMove, capsuleTopBeforeMove, m_Controller.radius, characterVelocity.normalized, out RaycastHit hit, characterVelocity.magnitude * Time.deltaTime, -1, QueryTriggerInteraction.Ignore))
        {
            // We remember the last impact speed because the fall damage logic might need it
            //落下ダメージロジックで必要になる可能性があるため、最後の衝撃速度を記憶します
            m_LatestImpactSpeed = characterVelocity;

            characterVelocity = Vector3.ProjectOnPlane(characterVelocity, hit.normal);
        }
    }

    // Returns true if the slope angle represented by the given normal is under the slope angle limit of the character controller
    //指定された法線で表される傾斜角度がキャラクターコントローラーの傾斜角度制限を下回っている場合はtrueを返します
    bool IsNormalUnderSlopeLimit(Vector3 normal)
    {
        return Vector3.Angle(transform.up, normal) <= m_Controller.slopeLimit;
    }

    // Gets the center point of the bottom hemisphere of the character controller capsule
    //キャラクターコントローラカプセルの下半球の中心点を取得します
    Vector3 GetCapsuleBottomHemisphere()
    {
        return transform.position + (transform.up * m_Controller.radius);
    }

    // Gets the center point of the top hemisphere of the character controller capsule    
    //キャラクターコントローラカプセルの上半球の中心点を取得します
    Vector3 GetCapsuleTopHemisphere(float atHeight)
    {
        return transform.position + (transform.up * (atHeight - m_Controller.radius));
    }

    // Gets a reoriented direction that is tangent to a given slope
    //与えられた勾配に正接する方向変更された方向を取得します
    public Vector3 GetDirectionReorientedOnSlope(Vector3 direction, Vector3 slopeNormal)
    {
        Vector3 directionRight = Vector3.Cross(direction, transform.up);
        return Vector3.Cross(slopeNormal, directionRight).normalized;
    }

    void UpdateCharacterHeight(bool force)
    {
        // Update height instantly
        //すぐに高さを更新します
        if (force)
        {
            m_Controller.height = m_TargetCharacterHeight;
            m_Controller.center = Vector3.up * m_Controller.height * 0.5f;
            playerCamera.transform.localPosition = Vector3.up * m_TargetCharacterHeight * cameraHeightRatio;
            m_Actor.aimPoint.transform.localPosition = m_Controller.center;
        }
        // Update smooth height
        //滑らかな高さを更新します
        else if (m_Controller.height != m_TargetCharacterHeight)
        {
            // resize the capsule and adjust camera position
            //カプセルのサイズを変更し、カメラの位置を調整します
            m_Controller.height = Mathf.Lerp(m_Controller.height, m_TargetCharacterHeight, crouchingSharpness * Time.deltaTime);
            m_Controller.center = Vector3.up * m_Controller.height * 0.5f;
            playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, Vector3.up * m_TargetCharacterHeight * cameraHeightRatio, crouchingSharpness * Time.deltaTime);
            m_Actor.aimPoint.transform.localPosition = m_Controller.center;
        }
    }

    // returns false if there was an obstruction
    //障害物があった場合はfalseを返します
    bool SetCrouchingState(bool crouched, bool ignoreObstructions)
    {
        // set appropriate heights
        //適切な高さを設定します
        if (crouched)
        {
            m_TargetCharacterHeight = capsuleHeightCrouching;
        }
        else
        {
            // Detect obstructions
            //障害物を検出します
            if (!ignoreObstructions)
            {
                Collider[] standingOverlaps = Physics.OverlapCapsule(
                    GetCapsuleBottomHemisphere(),
                    GetCapsuleTopHemisphere(capsuleHeightStanding),
                    m_Controller.radius,
                    -1,
                    QueryTriggerInteraction.Ignore);
                foreach (Collider c in standingOverlaps)
                {
                    if (c != m_Controller)
                    {
                        return false;
                    }
                }
            }

            m_TargetCharacterHeight = capsuleHeightStanding;
        }

        if (onStanceChanged != null)
        {
            onStanceChanged.Invoke(crouched);
        }

        isCrouching = crouched;
        return true;
    }
}
