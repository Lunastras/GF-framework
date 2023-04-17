using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    //current instance
    public static GameManager Manager;

    [SerializeField]
    private Transform m_player = null;

    [SerializeField]
    private Image m_loadFadeImage = null;

    [SerializeField]
    private GameObject m_pauseScreen = null;

    public static bool isMultiplayer = false;

    private bool m_isPaused = false;

    public bool CanPause = true;

    private bool m_pauseButtonReleased = true;

    // Start is called before the first frame update
    void Awake()
    {
        if (Manager != this)
            Destroy(Manager);

        Manager = this;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (m_player == null)
        {
            // player = FindObjectOfType<MovementAdvanced>().transform;
        }
    }

    private void Start()
    {
    }

    private void Update()
    {
        if (Input.GetAxisRaw("Pause") > 0.1f)
        {
            if (m_pauseButtonReleased)
            {
                m_pauseButtonReleased = false;
                PauseToggle();
            }
        }
        else
        {
            m_pauseButtonReleased = true;
        }
    }

    public void QuitToMenu()
    {
        SceneManager.LoadScene(0);
    }

    public void QuitGame()
    {

    }

    public void PauseToggle()
    {
        if (!CanPause || LoadingScreenManager.currentlyLoading)
            return;

        m_isPaused = !m_isPaused;
        Time.timeScale = m_isPaused && !isMultiplayer ? 0 : 1;
        m_pauseScreen.SetActive(m_isPaused);
        Cursor.visible = m_isPaused;
        Cursor.lockState = m_isPaused ? CursorLockMode.None : CursorLockMode.Locked;
    }

    public static bool IsPaused()
    {
        return Manager.m_isPaused;
    }

    public static Transform GetPlayer()
    {
        return Manager.m_player;
    }
}