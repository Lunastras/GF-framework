using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Rewired;

public class GfcInputDisplayPrompt : MonoBehaviour
{
    public Transform m_spritesParent;
    public TextMeshProUGUI m_tmp;
    public GameObject m_prefabIconSprites;

    private RectTransform m_rectTransform;

    void Awake()
    {
        m_rectTransform = GetComponent<RectTransform>();
        this.GetComponentIfNull(ref m_tmp);
        Debug.Assert(m_tmp);
    }

    private void OnDisable()
    {
        //GfcPooling.DestroyChildren(m_tmp.transform, false, true, false, false);
    }

    public float GetPromptLength(GfcDisplayedInputData aDisplayInputData, float aPadding, int aPlayerId)
    {
        List<ActionElementMap> actionsElementsBuffer = GfcInput.ActionsElementsBuffer;
        GfcInput.GetPlayer(aPlayerId).controllers.maps.GetElementMapsWithAction((int)aDisplayInputData.Input, true, actionsElementsBuffer);

        m_tmp.SetText(aDisplayInputData.Label);

        int spritesCount = 0;
        for (int i = 0; i < actionsElementsBuffer.Count; i++)
        {
            //TODO only count valid sprites;
            //if(GfcInput.GetPlayer(aPlayerId).controllers.GetLastActiveController() == actionsElementsBuffer[i].controllerMap.controllerType) or something
            spritesCount++;
            //;
        }

        actionsElementsBuffer.Clear();

        return spritesCount * (aPadding + GetSpriteLength()) + aPadding + m_tmp.preferredWidth;
    }

    private float GetSpriteLength() { return m_rectTransform.sizeDelta.y; }

    public void SetDisplayPrompt(GfcDisplayedInputData aDisplayInputData, float aPadding, int aPlayerId)
    {
        GfcPooling.DestroyChildren(m_tmp.transform, false, true, false, false);

        m_tmp.enabled = true; //it sets itself to false randomly for some reason
        m_tmp.SetText(aDisplayInputData.Label);

        RectTransform textTransform = m_tmp.GetComponent<RectTransform>();
        float transformHeight = GetComponent<RectTransform>().sizeDelta.y;

        List<ActionElementMap> actionsElementsBuffer = GfcInput.ActionsElementsBuffer;
        if (aDisplayInputData.Input != GfcInputType.NONE)
        {
            GfcInput.GetPlayer(aPlayerId).controllers.maps.GetElementMapsWithAction((int)aDisplayInputData.Input, true, actionsElementsBuffer);
            float iconsOffset = m_tmp.preferredWidth + aPadding + transformHeight * 0.5f;

            //todo, set sprites instead of writing
            for (int i = 0; i < actionsElementsBuffer.Count; i++)
            {
                Sprite sprite = GfcInput.GetGlyph(actionsElementsBuffer[i].controllerMap.controllerType, actionsElementsBuffer[i].actionId, aPlayerId);

                Image spriteImage = GfcPooling.Instantiate(m_prefabIconSprites).GetComponent<Image>();
                spriteImage.sprite = sprite;
                spriteImage.color = Color.white;

                RectTransform spriteTransform = spriteImage.GetComponent<RectTransform>();
                spriteTransform.SetParent(textTransform, false);
                spriteTransform.SetSizeDelta(transformHeight, transformHeight);

                spriteTransform.SetPos(-i * (aPadding + transformHeight) - iconsOffset, 0);
            }
        }
        else //Write the category
        {
            Debug.LogError("TODO");
            //ReInput.mapping.ActionsInCategory((int)m_displayedInputs[i].Category, )
        }

        actionsElementsBuffer.Clear();
    }
}