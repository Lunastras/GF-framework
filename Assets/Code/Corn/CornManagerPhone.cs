using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CornManagerPhone : MonoBehaviour
{
    private static CornManagerPhone Instance;
    [SerializeField] private GfgInputTracker m_openPhoneInput;

    [SerializeField] private GfcGameState[] m_statesWhereToggleEnabled;

    // Start is called before the first frame update
    void Awake()
    {
        this.SetSingleton(ref Instance);
    }

    // Update is called once per frame
    void Update()
    {
        if (m_openPhoneInput.PressedSinceLastCheck())
        {
            bool validState = false;
            GfcGameState gameState = GfgManagerGame.GetGameState();

            for (int i = 0; !validState && i < m_statesWhereToggleEnabled.Length; i++)
            {
                validState = gameState == m_statesWhereToggleEnabled[i];
            }

            if (validState)
            {
                GfcGameState gameStateToSet = gameState == GfcGameState.PHONE ? GfgManagerGame.GetPreviousGameState() : GfcGameState.PHONE;
                GfgManagerGame.SetGameState(gameStateToSet);
            }
        }
    }
}
