using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class GameManager : MonoBehaviour
{
    //current instance
    public static GameManager Instance;

    [SerializeField]
    private Transform m_player = null;

    [SerializeField]
    private HudManager m_hudManager = null;

    //[SerializeField]
    //private Image m_loadFadeImage = null;

    [SerializeField]
    private GameObject m_pauseScreen = null;

    [SerializeField]
    private GameObject m_loadFadeScreen = null;

    public static bool IsMultiplayer { get; private set; } = false;

    private bool m_isPaused = false;

    public bool CanPause = true;

    private bool m_pauseButtonReleased = true;

    private NetworkSpawnManager m_spawnManager;

    // Start is called before the first frame update
    void Awake()
    {
        if (Instance != this)
            Destroy(Instance);

        Instance = this;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (m_player == null)
        {
            // player = FindObjectOfType<MovementAdvanced>().transform;
        }
    }

    private void Start()
    {
        m_spawnManager = NetworkManager.Singleton.SpawnManager;
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


    public static void MultiplayerStart()
    {
        IsMultiplayer = true;
        Instance.m_spawnManager = NetworkManager.Singleton.SpawnManager;
    }

    public static void MultiplayerStop(bool discardMessageQueue = false)
    {
        IsMultiplayer = false;
        Instance.m_spawnManager = null;
        NetworkManager.Singleton.Shutdown(discardMessageQueue);
    }

    public void QuitToMenu()
    {
        MultiplayerStop();
        SceneManager.LoadScene(0);
    }

    public void QuitGame()
    {
        MultiplayerStop(true);
    }

    public static NetworkObject GetNetworkObject(ulong objectNetworkId)
    {
        Instance.m_spawnManager.SpawnedObjects.TryGetValue(objectNetworkId, out NetworkObject obj);
        return obj;
    }

    public static T GetComponentFromNetworkObject<T>(ulong objectNetworkId) where T : Component
    {
        T component = null;
        if (Instance.m_spawnManager.SpawnedObjects.TryGetValue(objectNetworkId, out NetworkObject obj))
            component = obj.GetComponent<T>();

        return component;
    }

    public static HudManager GetHudManager() { return Instance.m_hudManager; }

    public void PauseToggle()
    {
        if (!CanPause || LoadingScreenManager.currentlyLoading)
            return;

        if (m_loadFadeScreen) m_loadFadeScreen.SetActive(false);
        m_isPaused = !m_isPaused;
        Time.timeScale = m_isPaused && !IsMultiplayer ? 0 : 1;
        m_pauseScreen.SetActive(m_isPaused);
        Cursor.visible = m_isPaused;
        Cursor.lockState = m_isPaused ? CursorLockMode.None : CursorLockMode.Locked;
    }

    public static bool IsPaused()
    {
        return Instance.m_isPaused;
    }

    public static Transform GetPlayer()
    {
        return Instance.m_player;
    }

    public static void SetPlayer(Transform player)
    {
        Instance.m_player = player;
    }
}