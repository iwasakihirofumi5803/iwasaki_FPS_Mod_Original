using UnityEngine;

public class PlayerInputHandler : MonoBehaviour
{
    [Tooltip("カメラを動かすための感度乗数")]
    public float lookSensitivity = 1f;
    [Tooltip("WebGL用の追加の感度乗数")]
    public float webglLookSensitivityMultiplier = 0.25f;
    [Tooltip("コントローラーでトリガーを使用するときに入力を考慮する制限")]
    public float triggerAxisThreshold = 0.4f;
    [Tooltip("垂直入力軸を反転するために使用されます")]
    public bool invertYAxis = false;
    [Tooltip("水平入力軸を反転するために使用されます")]
    public bool invertXAxis = false;

    GameFlowManager m_GameFlowManager;
    PlayerCharacterController m_PlayerCharacterController;
    bool m_FireInputWasHeld;

    private void Start()
    {
        m_PlayerCharacterController = GetComponent<PlayerCharacterController>();
        DebugUtility.HandleErrorIfNullGetComponent<PlayerCharacterController, PlayerInputHandler>(m_PlayerCharacterController, this, gameObject);
        m_GameFlowManager = FindObjectOfType<GameFlowManager>();
        DebugUtility.HandleErrorIfNullFindObject<GameFlowManager, PlayerInputHandler>(m_GameFlowManager, this);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        m_FireInputWasHeld = GetFireInputHeld();
    }

    public bool CanProcessInput()
    {
        return Cursor.lockState == CursorLockMode.Locked && !m_GameFlowManager.gameIsEnding;
    }

    public Vector3 GetMoveInput()
    {
        if (CanProcessInput())
        {
            Vector3 move = new Vector3(Input.GetAxisRaw(GameConstants.k_AxisNameHorizontal), 0f, Input.GetAxisRaw(GameConstants.k_AxisNameVertical));

            //移動入力を最大値1に制限します。そうしないと、斜めの移動が定義された最大移動速度を超える可能性があります
            move = Vector3.ClampMagnitude(move, 1);

            return move;
        }

        return Vector3.zero;
    }

    public float GetLookInputsHorizontal()
    {
        return GetMouseOrStickLookAxis(GameConstants.k_MouseAxisNameHorizontal, GameConstants.k_AxisNameJoystickLookHorizontal);
    }

    public float GetLookInputsVertical()
    {
        return GetMouseOrStickLookAxis(GameConstants.k_MouseAxisNameVertical, GameConstants.k_AxisNameJoystickLookVertical);
    }

    public bool GetJumpInputDown()
    {
        if (CanProcessInput())
        {
            return Input.GetButtonDown(GameConstants.k_ButtonNameJump);
        }

        return false;
    }

    public bool GetJumpInputHeld()
    {
        if (CanProcessInput())
        {
            return Input.GetButton(GameConstants.k_ButtonNameJump);
        }

        return false;
    }

    public bool GetFireInputDown()
    {
        return GetFireInputHeld() && !m_FireInputWasHeld;
    }

    public bool GetFireInputReleased()
    {
        return !GetFireInputHeld() && m_FireInputWasHeld;
    }

    public bool GetFireInputHeld()
    {
        if (CanProcessInput())
        {
            bool isGamepad = Input.GetAxis(GameConstants.k_ButtonNameGamepadFire) != 0f;
            if (isGamepad)
            {
                return Input.GetAxis(GameConstants.k_ButtonNameGamepadFire) >= triggerAxisThreshold;
            }
            else
            {
                return Input.GetButton(GameConstants.k_ButtonNameFire);
            }
        }

        return false;
    }

    public bool GetAimInputHeld()
    {
        if (CanProcessInput())
        {
            bool isGamepad = Input.GetAxis(GameConstants.k_ButtonNameGamepadAim) != 0f;
            bool i = isGamepad ? (Input.GetAxis(GameConstants.k_ButtonNameGamepadAim) > 0f) : Input.GetButton(GameConstants.k_ButtonNameAim);
            return i;
        }

        return false;
    }

    public bool GetSprintInputHeld()
    {
        if (CanProcessInput())
        {
            return Input.GetButton(GameConstants.k_ButtonNameSprint);
        }

        return false;
    }

    public bool GetCrouchInputDown()
    {
        if (CanProcessInput())
        {
            return Input.GetButtonDown(GameConstants.k_ButtonNameCrouch);
        }

        return false;
    }

    public bool GetCrouchInputReleased()
    {
        if (CanProcessInput())
        {
            return Input.GetButtonUp(GameConstants.k_ButtonNameCrouch);
        }

        return false;
    }

    public int GetSwitchWeaponInput()
    {
        if (CanProcessInput())
        {

            bool isGamepad = Input.GetAxis(GameConstants.k_ButtonNameGamepadSwitchWeapon) != 0f;
            string axisName = isGamepad ? GameConstants.k_ButtonNameGamepadSwitchWeapon : GameConstants.k_ButtonNameSwitchWeapon;

            if (Input.GetAxis(axisName) > 0f)
                return -1;
            else if (Input.GetAxis(axisName) < 0f)
                return 1;
            else if (Input.GetAxis(GameConstants.k_ButtonNameNextWeapon) > 0f)
                return -1;
            else if (Input.GetAxis(GameConstants.k_ButtonNameNextWeapon) < 0f)
                return 1;
        }

        return 0;
    }

    public int GetSelectWeaponInput()
    {
        if (CanProcessInput())
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                return 1;
            else if (Input.GetKeyDown(KeyCode.Alpha2))
                return 2;
            else if (Input.GetKeyDown(KeyCode.Alpha3))
                return 3;
            else if (Input.GetKeyDown(KeyCode.Alpha4))
                return 4;
            else if (Input.GetKeyDown(KeyCode.Alpha5))
                return 5;
            else if (Input.GetKeyDown(KeyCode.Alpha6))
                return 6;
            else
                return 0;
        }

        return 0;
    }

    float GetMouseOrStickLookAxis(string mouseInputName, string stickInputName)
    {
        if (CanProcessInput())
        {
            //このLook入力がマウスからのものかどうかを確認します
            bool isGamepad = Input.GetAxis(stickInputName) != 0f;
            float i = isGamepad ? Input.GetAxis(stickInputName) : Input.GetAxisRaw(mouseInputName);

            //垂直入力の反転を処理します
            if (invertYAxis)
                i *= -1f;

            //感度乗数を適用します
            i *= lookSensitivity;

            if (isGamepad)
            {
                //マウス入力は既にdeltaTimeに依存しているため、スティックからのものである場合は、フレーム時間でのみ入力をスケーリングします
                i *= Time.deltaTime;
            }
            else
            {
                //マウスの入力量を減らしてスティックの動きと同等にします
                i *= 0.01f;
#if UNITY_WEBGL
                //マウスアクセラレーションにより、マウスはWebGLでさらに敏感になる傾向があるため、さらに減少させます
                i *= webglLookSensitivityMultiplier;
#endif
            }

            return i;
        }

        return 0f;
    }
}
