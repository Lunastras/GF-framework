using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button m_serverBtn = null;
    [SerializeField] private Button m_hostBtn = null;
    [SerializeField] private Button m_clientBtn = null;

    // Start is called before the first frame update
    void Awake()
    {
        m_serverBtn.onClick.AddListener(() =>
        {
            GameManager.IsMultiplayer = true;
            NetworkManager.Singleton.StartServer();
        });

        m_hostBtn.onClick.AddListener(() =>
        {
            GameManager.IsMultiplayer = true;
            NetworkManager.Singleton.StartHost();
        });

        m_clientBtn.onClick.AddListener(() =>
        {
            GameManager.IsMultiplayer = true;
            NetworkManager.Singleton.StartClient();
        });
    }

    // Update is called once per frame
    void Update()
    {

    }
}
