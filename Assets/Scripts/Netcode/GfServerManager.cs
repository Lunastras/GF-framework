using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System;
using MEC;
using UnityEditor.ShaderGraph.Internal;

public class GfManagerServer : NetworkBehaviour
{
    public static GfManagerServer Instance { get; private set; } = null;

    protected NetworkVariable<short> m_sceneBuildIndex = new(-1);

    protected HashSet<NetworkClient> m_readyClients = new(16);

    public Action<ulong> OnPlayerReady = null;

    public Action<ulong> OnPlayerUnready = null;

    protected NetworkVariable<bool> m_hostReady = new(false);

    protected NetworkVariable<float> m_timeScale = new(1);

    protected NetworkList<float> m_characterTimeScales;

    protected NetworkVariable<float> m_targetFixedTimestep = new(0.02f);

    public static bool HostReady
    {
        get
        {
            return Instance && Instance.m_hostReady.Value;
        }
    }

    public static bool HasAuthority
    {
        get
        {
            return !NetworkManager.Singleton || NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer || !GfcManagerGame.IsMultiplayer;
        }
    }

    protected void OnTimeScaleChanged(float previousValue, float newValue)
    {
        Time.fixedDeltaTime = newValue * m_targetFixedTimestep.Value;
        Time.timeScale = newValue;
    }

    protected void OnTargetFixedTimestepChanged(float previousValue, float newValue)
    {
        Time.fixedDeltaTime = Time.timeScale / newValue;
    }

    public static void SetFixedDeltaTime(float value)
    {
        if (HasAuthority)
            Instance.m_targetFixedTimestep.Value = value;
    }

    public static float GetFixedDeltaTime()
    {
        return Instance.m_targetFixedTimestep.Value;
    }

    // Start is called before the first frame update
    void Awake()
    {
        if (Instance != this)
        {
            Destroy(Instance);
        }

        m_timeScale.OnValueChanged += OnTimeScaleChanged;
        m_targetFixedTimestep.OnValueChanged += OnTargetFixedTimestepChanged;

        if (HasAuthority)
        {
            m_characterTimeScales = new();
            m_targetFixedTimestep.Value = GfcManagerGame.GetInitialFixedDeltaTime;
        }

        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (HasAuthority)
        {
            for (int i = 0; i < GfcManagerCharacters.GetNumTypes(); ++i)
                m_characterTimeScales.Add(1);

            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
            ObjectVisibilityManager.AddExceptionObject(NetworkObject);
            if (!NetworkObject.IsSpawned)
                NetworkObject.Spawn();
        }

        Instance = this;
    }

    public static bool HasInstance
    {
        get
        {
            return Instance;

        }
    }

    public static void DestroyInstance()
    {
        if (Instance)
            Destroy(Instance.gameObject);
    }

    public static void OnClientDisconnectCallback(ulong clientId)
    {
        Instance.m_readyClients.Remove(NetworkManager.Singleton.ConnectedClients[clientId]);

        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            Destroy(Instance.gameObject);
            Instance = null;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
        }
    }

    public static void LoadScene(int sceneBuildIndex, ServerLoadingMode loadingMode)
    {
        if (HasAuthority)
        {
            LoadingScreenManager.LoadScene(sceneBuildIndex, loadingMode, GfcManagerGame.GameType);
            Instance.LevelChangeClientRpc(sceneBuildIndex, ServerLoadingMode.RECONNECT);
        }
    }

    public static void LoadScene(string sceneName, ServerLoadingMode loadingMode)
    {
        LoadScene(LoadingScreenManager.GetSceneBuildIndexByName(sceneName), loadingMode);
    }

    [ClientRpc]
    protected virtual void LevelChangeClientRpc(int sceneBuildIndex, ServerLoadingMode loadingMode)
    {
        if (!HasAuthority)
            LoadingScreenManager.LoadScene(sceneBuildIndex, loadingMode, GfcManagerGame.GameType);
    }

    public override void OnDestroy()
    {
        Instance = null;
        SetTimeScale(1);
        m_characterTimeScales.Dispose();
    }

    public static bool GetPlayerIsReady(NetworkClient player)
    {
        return Instance.m_readyClients.Contains(player);
    }

    public static void SetPlayerIsReady(bool ready)
    {
        Instance.SetPlayerIsReadyServerRpc(ready);
    }

    public static void SetPlayerIsReady(bool ready, ulong clientId)
    {
        if (HasAuthority)
        {
            NetworkClient client = NetworkManager.Singleton.ConnectedClients[clientId];

            //if is host
            if (client.ClientId == NetworkManager.Singleton.LocalClientId)
                Instance.m_hostReady.Value = ready;

            if (ready)
            {
                Instance.OnPlayerReady?.Invoke(clientId);
                Instance.m_readyClients.Add(client);
            }
            else
            {
                Instance.OnPlayerUnready?.Invoke(clientId);
                Instance.m_readyClients.Remove(client);
            }
        }
        else Debug.LogError("SetPlayerIsReady should only be called by the host or server.");
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerIsReadyServerRpc(bool ready, ServerRpcParams serverRpcParams = default)
    {
        SetPlayerIsReady(ready, serverRpcParams.Receive.SenderClientId);
    }

    public static short GetSceneBuildIndex()
    {
        if (Instance)
            return Instance.m_sceneBuildIndex.Value;
        else
            return -1;
    }

    public static void SetSceneBuildIndex(short index)
    {
        if (HasAuthority)
            Instance.m_sceneBuildIndex.Value = index;
    }

    public static void SetTimeScale(float timeScale, bool setAll = true, float smoothingTime = 1)
    {
        if (HasAuthority && Instance)
        {
            if (smoothingTime <= 0)
            {
                Instance.m_timeScale.Value = timeScale;
                Time.timeScale = timeScale;
            }
            else
            {
                Timing.RunCoroutine(Instance._SetMainTimeScale(timeScale, smoothingTime));
            }

            if (setAll)
            {
                int countTypes = Instance.m_characterTimeScales.Count;
                for (int i = 0; i < countTypes; ++i)
                    SetTimeScale(timeScale, (CharacterTypes)i, smoothingTime);
            }
        }
    }

    public static void SetTimeScale(float timeScale, CharacterTypes characterType, float smoothingTime = 1)
    {
        if (Instance && HasAuthority)
        {
            if (smoothingTime <= 0)
            {
                Instance.m_characterTimeScales[(int)characterType] = timeScale;
            }
            else
            {
                Timing.RunCoroutine(Instance._SetTimeScale(timeScale, characterType, smoothingTime));
            }
        }
    }

    public IEnumerator<float> _SetTimeScale(float targetScale, CharacterTypes characterType, float smoothTime)
    {

        int typeIndex = (int)characterType;
        float currentScale = m_characterTimeScales[typeIndex];
        float refSmooth = 0;

        while (null != m_characterTimeScales && MathF.Abs(currentScale - targetScale) > 0.001f && m_characterTimeScales[typeIndex] == currentScale) //stop coroutine if alpha is modified by somebody else
        {
            currentScale = Mathf.SmoothDamp(currentScale, targetScale, ref refSmooth, smoothTime, int.MaxValue, Time.unscaledDeltaTime);
            m_characterTimeScales[typeIndex] = currentScale;
            yield return Timing.WaitForOneFrame;
        }

        if (null != m_characterTimeScales && m_characterTimeScales[typeIndex] == currentScale)
            m_characterTimeScales[typeIndex] = targetScale;
    }

    public IEnumerator<float> _SetMainTimeScale(float targetScale, float smoothTime)
    {
        float currentScale = m_timeScale.Value;
        float refSmooth = 0;

        while (null != m_characterTimeScales && MathF.Abs(currentScale - targetScale) > 0.001f && m_timeScale.Value == currentScale) //stop coroutine if alpha is modified by somebody else
        {
            currentScale = Mathf.SmoothDamp(currentScale, targetScale, ref refSmooth, smoothTime, int.MaxValue, Time.unscaledDeltaTime);
            m_timeScale.Value = currentScale;
            yield return Timing.WaitForOneFrame;
        }

        if (null != m_characterTimeScales && m_timeScale.Value == currentScale)
        {
            m_timeScale.Value = targetScale;
        }
    }

    public static float GetDeltaTimeCoef(CharacterTypes characterType)
    {
        return Instance ? Instance.m_characterTimeScales[(int)characterType] / Instance.m_timeScale.Value : 1;
    }

    public static float GetTimeScale() { return Instance.m_timeScale.Value; }

    public static float GetTimeScale(CharacterTypes characterType) { return Instance.m_characterTimeScales[(int)characterType]; }
}
