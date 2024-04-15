using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponTimestopCharge : WeaponChargeLevels
{
    [SerializeField]
    protected float m_timeChangeSmoothTime = 1;

    [SerializeField]
    protected float m_timeStopTimeScale = 0.05f;
    // Start is called before the first frame update
    void Start()
    {
        //GfServerManager.SetTimeScale(1f, true, -1);
        //GfServerManager.SetTimeScale(1, GetGfgStatsCharacter().GetCharacterType(), -1);
    }

    public override void FireBomb()
    {
        base.FireBomb();
        GfcManagerServer.SetTimeScale(m_timeStopTimeScale, true, m_timeChangeSmoothTime);
        GfcManagerServer.SetTimeScale(1, GetGfgStatsCharacter().GetCharacterType(), -1);
    }

    protected override void OnDischargeOver()
    {
        base.OnDischargeOver();
        GfcManagerServer.SetTimeScale(1.0f, true, m_timeChangeSmoothTime);
    }
}
