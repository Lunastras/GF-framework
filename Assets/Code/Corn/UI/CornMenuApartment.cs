using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CornMenuApartment : MonoBehaviour
{
    [SerializeField] protected RectTransform m_optionsParent;

    [SerializeField] protected Vector2 m_optionsPosOffset;

    [SerializeField] protected float m_optionsLengthRatioOffset = 0.06f;

    List<GfxPanel> m_optionsPanelList = new(6);
    // Start is called before the first frame update

    void Awake()
    {
        GfcManagerGame.InitializeGfBase();
    }

    void Start()
    {
        DrawOptions();
    }

    void DrawOptions()
    {
        GfxPanelCreateData panelCreateData = GfxUiTools.GetDefaultPanelCreateData();
        panelCreateData.OnEventCallback = new(OnOptionsEventCallback);
        panelCreateData.PositionOffset = m_optionsPosOffset;

        GfxUiTools.CreatePanelList(m_optionsParent, panelCreateData, m_optionsPanelList, Axis.VERTICAL, (int)CornEventType.COUNT - 2, false, true, AlignmentHorizontal.MIDDLE, AlignmentVertical.BOTTOM);

        for (int i = 0; i < m_optionsPanelList.Count; ++i)
        {
            RectTransform rectTransform = m_optionsPanelList[i].GetMainRectTransform();
            Vector2 localPosition = rectTransform.localPosition;
            localPosition.x += panelCreateData.PanelSize.x * m_optionsLengthRatioOffset * i;

            rectTransform.localPosition = localPosition;
            m_optionsPanelList[i].SetTextOnly(((CornEventType)i).ToString());
        }
    }

    void OnOptionsEventCallback(GfxPanelCallbackType aCallbackType, GfxPanel aPanel, bool aState)
    {
        switch (aCallbackType)
        {
            case GfxPanelCallbackType.SUBMIT:
                CornEventType eventType = (CornEventType)aPanel.Index;
                CornManagerEvents.ExecuteEvent(new(eventType));
                break;
        }
    }
}
