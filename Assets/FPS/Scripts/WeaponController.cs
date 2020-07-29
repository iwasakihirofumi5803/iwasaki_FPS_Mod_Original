using UnityEngine;
using UnityEngine.Events;

public enum WeaponShootType
{
    Manual,					//マニュアル
    Automatic,				//オート
    Charge,					//チャージ
}

[System.Serializable]
public struct CrosshairData
{
    [Tooltip("この武器の十字線に使用される画像")]
    public Sprite crosshairSprite;
    [Tooltip("十字線画像のサイズ")]
    public int crosshairSize;
    [Tooltip("十字線画像の色")]
    public Color crosshairColor;
}

[RequireComponent(typeof(AudioSource))]
public class WeaponController : MonoBehaviour
{
    [Header("情報")]
    [Tooltip("この武器のUIに表示される名前")]
    public string weaponName;
    [Tooltip("この武器のUIに表示される画像")]
    public Sprite weaponIcon;
    //スプライト は 2D グラフィックオブジェクト

    [Tooltip("十字線のデフォルトデータ")]
    public CrosshairData crosshairDataDefault;
    [Tooltip("敵を狙ったときの十字線のデータ")]
    public CrosshairData crosshairDataTargetInSight;

    [Header("内部参照")]
    [Tooltip("武器のルートオブジェクト。これは、武器がアクティブでないときに無効になるものです")]
    public GameObject weaponRoot;
    [Tooltip("発射物が発射される武器の先端")]
    public Transform weaponMuzzle;

    [Header("シュートパラメーター")]
    [Tooltip("武器の種類は発砲方法に影響します")]
    public WeaponShootType shootType;
    [Tooltip("弾丸プレハブ")]
    public ProjectileBase projectilePrefab;
    [Tooltip("2つのショット間の最小時間")]
    public float delayBetweenShots = 0.5f;
    [Tooltip("弾丸がランダムに発射される円錐の角度（0は広がりがないことを意味します）")]
    public float bulletSpreadAngle = 0f;
    [Tooltip("1発あたりの弾丸の量")]
    public int bulletsPerShot = 1;
    [Tooltip("各ショットの後に武器を押し戻す力")]
    [Range(0f, 2f)]
    public float recoilForce = 1;
    [Tooltip("照準中の武器のデフォルトの視野角の比率")]
    [Range(0f, 1f)]
    public float aimZoomRatio = 1f;
    [Tooltip("この武器で照準を合わせるときに武器の腕に適用する変換")]
    public Vector3 aimOffset;

    [Header("弾薬パラメータ")]
    [Tooltip("1秒あたりにリロードされる弾薬の量")]
    public float ammoReloadRate = 1f;
    [Tooltip("最後のショットからリロードを開始するまでの遅延")]
    public float ammoReloadDelay = 2f;
    [Tooltip("銃の最大弾薬量")]
    public float maxAmmo = 8;

    [Header("充電パラメーター（武器の充電のみ）")]
    [Tooltip("最大充電に達したときにショットをトリガーします")]
    public bool automaticReleaseOnCharged;
    [Tooltip("最大チャージに達するまでの時間")]
    public float maxChargeDuration = 2f;
    [Tooltip("チャージ開始時に使用した最初の弾薬")]
    public float ammoUsedOnStartCharge = 1f;
    [Tooltip("チャージが最大に達したときに使用される追加の弾薬")]
    public float ammoUsageRateWhileCharging = 1f;

    [Header("オーディオ＆ビジュアル")]
    [Tooltip("On Shootアニメーション用のオプションの武器アニメータ")]
    public Animator weaponAnimator;
    [Tooltip("マズルフラッシュのプレハブ")]
    public GameObject muzzleFlashPrefab;
    [Tooltip("スポーン時にマズルフラッシュインスタンスの親を解除する")]
    public bool unparentMuzzleFlash;
    [Tooltip("撮影時に鳴る音")]
    public AudioClip shootSFX;
    [Tooltip("この武器に変更したときに再生される音")]
    public AudioClip changeWeaponSFX;

    public UnityAction onShoot;

    float m_CurrentAmmo;
    float m_LastTimeShot = Mathf.NegativeInfinity;
    float m_TimeBeginCharge;
    Vector3 m_LastMuzzlePosition;

    public GameObject owner { get; set; }
    public GameObject sourcePrefab { get; set; }
    public bool isCharging { get; private set; }
    public float currentAmmoRatio { get; private set; }
    public bool isWeaponActive { get; private set; }
    public bool isCooling { get; private set; }
    public float currentCharge { get; private set; }
    public Vector3 muzzleWorldVelocity { get; private set; }
    public float GetAmmoNeededToShoot() => (shootType != WeaponShootType.Charge ? 1 : ammoUsedOnStartCharge) / maxAmmo;

    AudioSource m_ShootAudioSource;

    const string k_AnimAttackParameter = "Attack";

    void Awake()
    {
        m_CurrentAmmo = maxAmmo;
        m_LastMuzzlePosition = weaponMuzzle.position;

        m_ShootAudioSource = GetComponent<AudioSource>();
        DebugUtility.HandleErrorIfNullGetComponent<AudioSource, WeaponController>(m_ShootAudioSource, this, gameObject);
    }

    void Update()
    {
        UpdateAmmo();

        UpdateCharge();

        if (Time.deltaTime > 0)
        {
            muzzleWorldVelocity = (weaponMuzzle.position - m_LastMuzzlePosition) / Time.deltaTime;
            m_LastMuzzlePosition = weaponMuzzle.position;
        }
    }

    void UpdateAmmo()
    {
        if (m_LastTimeShot + ammoReloadDelay < Time.time && m_CurrentAmmo < maxAmmo && !isCharging)
        {
            // reloads weapon over time
            //時間の経過とともに武器をリロードします
            m_CurrentAmmo += ammoReloadRate * Time.deltaTime;

            // limits ammo to max value
            //弾薬を最大値に制限します
            m_CurrentAmmo = Mathf.Clamp(m_CurrentAmmo, 0, maxAmmo);

            isCooling = true;
        }
        else
        {
            isCooling = false;
        }

        if (maxAmmo == Mathf.Infinity)
        {
            currentAmmoRatio = 1f;
        }
        else
        {
            currentAmmoRatio = m_CurrentAmmo / maxAmmo;
        }
    }

    void UpdateCharge()
    {
        if (isCharging)
        {
            if (currentCharge < 1f)
            {
                float chargeLeft = 1f - currentCharge;

                // Calculate how much charge ratio to add this frame
                //このフレームを追加するためのチャージ比率を計算します
                float chargeAdded = 0f;
                if (maxChargeDuration <= 0f)
                {
                    chargeAdded = chargeLeft;
                }
                else
                {
                    chargeAdded = (1f / maxChargeDuration) * Time.deltaTime;
                }

                chargeAdded = Mathf.Clamp(chargeAdded, 0f, chargeLeft);

                // See if we can actually add this charge
                //このチャージを実際に追加できるかどうかを確認します
                float ammoThisChargeWouldRequire = chargeAdded * ammoUsageRateWhileCharging;
                if (ammoThisChargeWouldRequire <= m_CurrentAmmo)
                {
                    // Use ammo based on charge added
                    //追加された料金に基づいて弾薬を使用する
                    UseAmmo(ammoThisChargeWouldRequire);

                    //現在のチャージ率を設定します
                    currentCharge = Mathf.Clamp01(currentCharge + chargeAdded);
                }
            }
        }
    }

    public void ShowWeapon(bool show)
    {
        weaponRoot.SetActive(show);

        if (show && changeWeaponSFX)
        {
            m_ShootAudioSource.PlayOneShot(changeWeaponSFX);
        }

        isWeaponActive = show;
    }

    public void UseAmmo(float amount)
    {
        m_CurrentAmmo = Mathf.Clamp(m_CurrentAmmo - amount, 0f, maxAmmo);
        m_LastTimeShot = Time.time;
    }

    public bool HandleShootInputs(bool inputDown, bool inputHeld, bool inputUp)
    {
        switch (shootType)
        {
            case WeaponShootType.Manual:
                if (inputDown)
                {
                    return TryShoot();
                }
                return false;

            case WeaponShootType.Automatic:
                if (inputHeld)
                {
                    return TryShoot();
                }
                return false;

            case WeaponShootType.Charge:
                if (inputHeld)
                {
                    TryBeginCharge();
                }
                // Check if we released charge or if the weapon shoot autmatically when it's fully charged
                //チャージが解除されたかどうか、または完全に充電されたときに武器が自動的に発砲するかどうかを確認します
                if (inputUp || (automaticReleaseOnCharged && currentCharge >= 1f))
                {
                    return TryReleaseCharge();
                }
                return false;

            default:
                return false;
        }
    }

    bool TryShoot()
    {
        if (m_CurrentAmmo >= 1f 
            && m_LastTimeShot + delayBetweenShots < Time.time)
        {
            HandleShoot();
            m_CurrentAmmo -= 1;

            return true;
        }

        return false;
    }

    bool TryBeginCharge()
    {
        if (!isCharging 
            && m_CurrentAmmo >= ammoUsedOnStartCharge 
            && m_LastTimeShot + delayBetweenShots < Time.time)
        {
            UseAmmo(ammoUsedOnStartCharge); 
            isCharging = true;

            return true;
        }

        return false;
    }

    bool TryReleaseCharge()
    {
        if (isCharging)
        {
            HandleShoot();

            currentCharge = 0f;
            isCharging = false;

            return true;
        }
        return false;
    }

    void HandleShoot()
    {
        // spawn all bullets with random direction
        //ランダムな方向ですべての弾丸をスポーンします
        for (int i = 0; i < bulletsPerShot; i++)
        {
            Vector3 shotDirection = GetShotDirectionWithinSpread(weaponMuzzle);
            ProjectileBase newProjectile = Instantiate(projectilePrefab, weaponMuzzle.position, Quaternion.LookRotation(shotDirection));
            newProjectile.Shoot(this);
        }

        // muzzle flash
        //マズルフラッシュ
        if (muzzleFlashPrefab != null)
        {
            GameObject muzzleFlashInstance = Instantiate(muzzleFlashPrefab, weaponMuzzle.position, weaponMuzzle.rotation, weaponMuzzle.transform);
            // Unparent the muzzleFlashInstance
            //マズルフラッシュインスタンスのペアレント化を解除します
            if (unparentMuzzleFlash)
            {
                muzzleFlashInstance.transform.SetParent(null);
            }

            Destroy(muzzleFlashInstance, 2f);
        }

        m_LastTimeShot = Time.time;

        // play shoot SFX
        //発射SFXを再生します
        if (shootSFX)
        {
            m_ShootAudioSource.PlayOneShot(shootSFX);
        }

        // Trigger attack animation if there is any
        //攻撃アニメーションがある場合はトリガーします
        if (weaponAnimator)
        {
            weaponAnimator.SetTrigger(k_AnimAttackParameter);
        }

        // Callback on shoot
        //発射時のコールバック
        if (onShoot != null)
        {
            onShoot();
        }
    }

    public Vector3 GetShotDirectionWithinSpread(Transform shootTransform)
    //スプレッド内でショット方向を取得(変形 シュート変形)
    {
        float spreadAngleRatio = bulletSpreadAngle / 180f;
        //広がり角度比 = 弾丸の広がり角度 / 180
        Vector3 spreadWorldDirection = Vector3.Slerp(shootTransform.forward, UnityEngine.Random.insideUnitSphere, spreadAngleRatio);
        //世界の方向を広げる = Slerp(発射変換.先端, Uintyエンジンのランダム.ユニットスフィア内, 広がり角度比)

        return spreadWorldDirection;
        //世界の方向を広げる 値を返す
    }
}
