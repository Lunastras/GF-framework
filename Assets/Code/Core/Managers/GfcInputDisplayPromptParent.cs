using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class GfcInputDisplayPromptParent : MonoBehaviour
{
    [SerializeField] private GameObject m_promptPrefab;
    [SerializeField] private Transform m_promptsParent;
    [SerializeField] private float m_promptsPadding = 5;

    private GfcInputDisplayPrompt m_lengthTestDisplayPrompt;

    void Start()
    {
        GfcPooling.SetPoolPermanent(m_promptPrefab);
    }

    public void UpdatePrompts(List<GfcDisplayedInputData> someDisplayInputs, int aPlayerId = 0)
    {
        if (m_promptsParent == null) m_promptsParent = transform;

        GfcPooling.DestroyChildren(m_promptsParent, false, true, false, someDisplayInputs.Count == 0);

        for (int i = 0; i < someDisplayInputs.Count; i++)
        {
            GfcInputDisplayPrompt inputPrompt = GfcPooling.Instantiate(m_promptPrefab).GetComponent<GfcInputDisplayPrompt>();
            RectTransform promptTransform = inputPrompt.GetComponent<RectTransform>();

            inputPrompt.SetDisplayPrompt(someDisplayInputs[i], m_promptsPadding, aPlayerId);
            Vector2 promptSize = promptTransform.sizeDelta;
            promptTransform.SetParent(m_promptsParent, false);
            promptTransform.SetPos(-promptSize.x * 0.5f, -i * (m_promptsPadding + promptTransform.sizeDelta.y));
        }
    }

    public void CalculateLength(ref GfcDisplayedInputData aDisplayData, int aPlayerId = 0)
    {
        if (m_lengthTestDisplayPrompt == null)
        {
            m_lengthTestDisplayPrompt = GfcPooling.Instantiate(m_promptPrefab).GetComponent<GfcInputDisplayPrompt>();
            m_lengthTestDisplayPrompt.transform.SetParent(transform, false);
            m_lengthTestDisplayPrompt.gameObject.SetActive(false);
        }

        aDisplayData.PromptLength = m_lengthTestDisplayPrompt.GetPromptLength(aDisplayData, m_promptsPadding, aPlayerId);
    }
}