using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfConsoleManager : MonoBehaviour
{
    public GameObject m_consolePrefab = null;

    public KeyCode m_consoleKeycode = KeyCode.Backslash;

    private GameObject m_console = null;

    private RectTransform m_consoleRectTransform = null;

    RectTransform m_transformCanvas = null;

    void Awake()
    {
        GfCommandConsole.InitializeLog();

        m_console = Instantiate(m_consolePrefab);
        m_console.transform.SetParent(transform);
        m_console.SetActive(false);
        DontDestroyOnLoad(gameObject);
        m_consoleRectTransform = m_console.GetComponent<RectTransform>();
    }

    void OnDestroy()
    {
        GfCommandConsole.DeinitLog();
    }

    void Update()
    {
        if (Input.GetKeyDown(m_consoleKeycode))
        {
            m_console.SetActive(!m_console.activeSelf);
        }
    }
}
