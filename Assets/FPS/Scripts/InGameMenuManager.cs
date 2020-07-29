using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InGameMenuManager : MonoBehaviour		//InGameMenuManagerクラスを作成
{
    [Tooltip("Root GameObject of the menu used to toggle its activation")]		//マウスカーソルが項目に乗ったとき「Root GameObject of the menu used to toggle its activation」を表示する。
    public GameObject menuRoot;
    [Tooltip("Master volume when menu is open")]								//マウスカーソルが項目に乗ったとき「Master volume when menu is open」を表示する。
    [Range(0.001f, 1f)]
    public float volumeWhenMenuOpen = 0.5f;
    [Tooltip("Slider component for look sensitivity")]							//マウスカーソルが項目に乗ったとき「Slider component for look sensitivity」を表示する。
    public Slider lookSensitivitySlider;
    [Tooltip("Toggle component for shadows")]									//マウスカーソルが項目に乗ったとき「Toggle component for shadows」を表示する。
    public Toggle shadowsToggle;
    [Tooltip("Toggle component for invincibility")]								//マウスカーソルが項目に乗ったとき「Toggle component for invincibility」を表示する。
    public Toggle invincibilityToggle;
    [Tooltip("Toggle component for framerate display")]							//マウスカーソルが項目に乗ったとき「Toggle component for framerate display」を表示する。
    public Toggle framerateToggle;
    [Tooltip("GameObject for the controls")]									//マウスカーソルが項目に乗ったとき「GameObject for the controls」を表示する。
    public GameObject controlImage;

    PlayerInputHandler m_PlayerInputsHandler;									//プレイヤーの操作を受け取り「m_PlayerInputsHandler」に格納する
    Health m_PlayerHealth;
    FramerateCounter m_FramerateCounter;

    void Start()
    {
        m_PlayerInputsHandler = FindObjectOfType<PlayerInputHandler>();
        DebugUtility.HandleErrorIfNullFindObject<PlayerInputHandler, InGameMenuManager>(m_PlayerInputsHandler, this);

        m_PlayerHealth = m_PlayerInputsHandler.GetComponent<Health>();
        DebugUtility.HandleErrorIfNullGetComponent<Health, InGameMenuManager>(m_PlayerHealth, this, gameObject);

        m_FramerateCounter = FindObjectOfType<FramerateCounter>();
        DebugUtility.HandleErrorIfNullFindObject<FramerateCounter, InGameMenuManager>(m_FramerateCounter, this);

        menuRoot.SetActive(false);

        lookSensitivitySlider.value = m_PlayerInputsHandler.lookSensitivity;
        lookSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);

        shadowsToggle.isOn = QualitySettings.shadows != ShadowQuality.Disable;
        shadowsToggle.onValueChanged.AddListener(OnShadowsChanged);

        invincibilityToggle.isOn = m_PlayerHealth.invincible;
        invincibilityToggle.onValueChanged.AddListener(OnInvincibilityChanged);

        framerateToggle.isOn = m_FramerateCounter.uiText.gameObject.activeSelf;
        framerateToggle.onValueChanged.AddListener(OnFramerateCounterChanged);
    }

    private void Update()
    {
        // Lock cursor when clicking outside of menu
        if (!menuRoot.activeSelf && Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Input.GetButtonDown(GameConstants.k_ButtonNamePauseMenu)
            || (menuRoot.activeSelf && Input.GetButtonDown(GameConstants.k_ButtonNameCancel)))
        {
            if (controlImage.activeSelf)
            {
                controlImage.SetActive(false);
                return;
            }

            SetPauseMenuActivation(!menuRoot.activeSelf);

        }

        if (Input.GetAxisRaw(GameConstants.k_AxisNameVertical) != 0)
        {
            if (EventSystem.current.currentSelectedGameObject == null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                lookSensitivitySlider.Select();
            }
        }
    }

    public void ClosePauseMenu()
    {
        SetPauseMenuActivation(false);
    }

    void SetPauseMenuActivation(bool active)
    {
        menuRoot.SetActive(active);

        if (menuRoot.activeSelf)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f;
            AudioUtility.SetMasterVolume(volumeWhenMenuOpen);

            EventSystem.current.SetSelectedGameObject(null);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = 1f;
            AudioUtility.SetMasterVolume(1);
        }

    }

    void OnMouseSensitivityChanged(float newValue)
    {
        m_PlayerInputsHandler.lookSensitivity = newValue;
    }

    void OnShadowsChanged(bool newValue)
    {
        QualitySettings.shadows = newValue ? ShadowQuality.All : ShadowQuality.Disable;
    }

    void OnInvincibilityChanged(bool newValue)
    {
        m_PlayerHealth.invincible = newValue;
    }

    void OnFramerateCounterChanged(bool newValue)
    {
        m_FramerateCounter.uiText.gameObject.SetActive(newValue);
    }

    public void OnShowControlButtonClicked(bool show)
    {
        controlImage.SetActive(show);
    }
}
