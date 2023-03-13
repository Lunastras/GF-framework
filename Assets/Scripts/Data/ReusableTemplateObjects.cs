using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReusableTemplateObjects : MonoBehaviour
{
    private static ReusableTemplateObjects m_instance = null;

    private Dictionary<GameObject, GameObject> m_objDictionary = null;

    // Start is called before the first frame update
    void Awake()
    {
        if (m_instance) Destroy(m_instance);
        m_objDictionary = new(8);

        m_instance = this;
    }

    public static GameObject GetInstanceFromTemplate(GameObject objTemplate)
    {
        var objDictionary = m_instance.m_objDictionary;
        if (null == objDictionary) objDictionary = new(4);

        GameObject obj = null;
        bool keyNotFound = false;

        if ((keyNotFound = !objDictionary.TryGetValue(objTemplate, out obj)) || null == obj)
        {

            obj = GfPooling.Instantiate(objTemplate);
            if (keyNotFound) objDictionary.Add(objTemplate, obj);
        }

        return obj;
    }
}
