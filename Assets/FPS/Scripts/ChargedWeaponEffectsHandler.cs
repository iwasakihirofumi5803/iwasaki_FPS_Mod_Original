using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ChargedWeaponEffectsHandler : MonoBehaviour
{
    [Header("ビジュアル")]
    [Tooltip("チャージスケールと色の変化の影響を受けるオブジェクト")]
    public GameObject chargingObject;
    [Tooltip("回転フレーム")]
    public GameObject spinningFrame;
    [Tooltip("チャージに基づく帯電物体のスケール")]
    public MinMaxVector3 scale;

    [Header("粒子")]
    [Tooltip("チャージ時に作成するパーティクル")]
    public GameObject diskOrbitParticlePrefab;
    [Tooltip("チャージ粒子のローカル位置オフセット（この変換を基準にして）")]
    public Vector3 offset;
    [Tooltip("チャージに基づくチャージ粒子の軌道速度")]
    public MinMaxFloat orbitY;
    [Tooltip("チャージに基づく荷電粒子の半径")]
    public MinMaxVector3 radius;
    [Tooltip("チャージに基づくフレームの待機回転速度")]
    public MinMaxFloat spinningSpeed;

    [Header("サウンド")]
    [Tooltip("チャージSFX用オーディオクリップ")]
    public AudioClip chargeSound;
    [Tooltip("この武器の変更がいっぱいになった後にループで再生されるサウンド")]
    public AudioClip loopChargeWeaponSFX;
    [Tooltip("チャージとループ音の間のクロスフェードの持続時間")]
    public float fadeLoopDuration = 0.5f;

    public GameObject particleInstance { get; set; }

    ParticleSystem m_DiskOrbitParticle;
    WeaponController m_WeaponController;
    ParticleSystem.VelocityOverLifetimeModule m_VelocityOverTimeModule;

    AudioSource m_AudioSource;
    AudioSource m_AudioSourceLoop;

    float m_ChargeRatio;
    float m_EndchargeTime;

    void Awake()
    {
        //チャージエフェクトは他の銃の音の上で再生されるため、独自のAudioSourceが必要です
        m_AudioSource = gameObject.AddComponent<AudioSource>();
        m_AudioSource.clip = chargeSound;
        m_AudioSource.playOnAwake = false;
        m_AudioSource.outputAudioMixerGroup = AudioUtility.GetAudioGroup(AudioUtility.AudioGroups.WeaponChargeBuildup);

        // 2番目のオーディオソースを作成し、遅延してサウンドを再生します
        m_AudioSourceLoop = gameObject.AddComponent<AudioSource>();
        m_AudioSourceLoop.clip = loopChargeWeaponSFX;
        m_AudioSourceLoop.playOnAwake = false;
        m_AudioSourceLoop.loop = true;
        m_AudioSourceLoop.outputAudioMixerGroup = AudioUtility.GetAudioGroup(AudioUtility.AudioGroups.WeaponChargeLoop);
    }

    void SpawnParticleSystem()
    {
        particleInstance = Instantiate(diskOrbitParticlePrefab, transform);
        particleInstance.transform.localPosition += offset;

        FindReferences();
    }

    public void FindReferences()
    {
        m_DiskOrbitParticle = particleInstance.GetComponent<ParticleSystem>();
        DebugUtility.HandleErrorIfNullGetComponent<ParticleSystem, ChargedWeaponEffectsHandler>(m_DiskOrbitParticle, this, particleInstance.gameObject);

        m_WeaponController = GetComponent<WeaponController>();
        DebugUtility.HandleErrorIfNullGetComponent<WeaponController, ChargedWeaponEffectsHandler>(m_WeaponController, this, gameObject);

        m_VelocityOverTimeModule = m_DiskOrbitParticle.velocityOverLifetime;
    }

    void Update()
    {
        if (particleInstance == null)
            SpawnParticleSystem();

        m_DiskOrbitParticle.gameObject.SetActive(m_WeaponController.isWeaponActive);
        m_ChargeRatio = m_WeaponController.currentCharge;

        chargingObject.transform.localScale = scale.GetValueFromRatio(m_ChargeRatio);
        if (spinningFrame != null)
        {
            spinningFrame.transform.localRotation *= Quaternion.Euler(0, spinningSpeed.GetValueFromRatio(m_ChargeRatio) * Time.deltaTime, 0);
        }

        m_VelocityOverTimeModule.orbitalY = orbitY.GetValueFromRatio(m_ChargeRatio);
        m_DiskOrbitParticle.transform.localScale = radius.GetValueFromRatio(m_ChargeRatio * 1.1f);

        //サウンドのボリュームとピッチを更新します
        if (m_ChargeRatio > 0)
        {
            if (!m_AudioSource.isPlaying && m_ChargeRatio < 0.1f)
            {
                m_EndchargeTime = Time.time + chargeSound.length;

                m_AudioSource.Play();
                m_AudioSourceLoop.Play();
            }

            float volumeRatio = Mathf.Clamp01((m_EndchargeTime - Time.time - fadeLoopDuration) / fadeLoopDuration);

            m_AudioSource.volume = volumeRatio;
            m_AudioSourceLoop.volume = 1 - volumeRatio;
        }
        else
        {
            m_AudioSource.Stop();
            m_AudioSourceLoop.Stop();
        }
    }
}
