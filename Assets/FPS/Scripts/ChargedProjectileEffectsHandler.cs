using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChargedProjectileEffectsHandler : MonoBehaviour
{
    [Tooltip("チャージスケールと色の変化の影響を受けるオブジェクト")]
    public GameObject chargingObject;
    [Tooltip("チャージに基づく帯電物体のスケール")]
    public MinMaxVector3 scale;
    [Tooltip("チャージに基づく帯電物体の色")]
    public MinMaxColor color;

    MeshRenderer[] m_AffectedRenderers;
    ProjectileBase m_ProjectileBase;

    private void OnEnable()
    {
        m_ProjectileBase = GetComponent<ProjectileBase>();
        DebugUtility.HandleErrorIfNullGetComponent<ProjectileBase, ChargedProjectileEffectsHandler>(m_ProjectileBase, this, gameObject);

        m_ProjectileBase.onShoot += OnShoot;

        m_AffectedRenderers = chargingObject.GetComponentsInChildren<MeshRenderer>();
        foreach (var ren in m_AffectedRenderers)
        {
            ren.sharedMaterial = Instantiate(ren.sharedMaterial);
        }
    }

    void OnShoot()
    {
        chargingObject.transform.localScale = scale.GetValueFromRatio(m_ProjectileBase.initialCharge);

        foreach (var ren in m_AffectedRenderers)
        {
            ren.sharedMaterial.SetColor("_Color", color.GetValueFromRatio(m_ProjectileBase.initialCharge));
        }
    }
}
