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

    [SerializeField] private Button m_singlePlayerButton = null;

    // Start is called before the first frame update
    void Awake()
    {
        m_serverBtn.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartServer();
            GameManager.MultiplayerStart();
        });

        m_hostBtn.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
            GameManager.MultiplayerStart();
        });

        m_clientBtn.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
            GameManager.MultiplayerStart();
        });

        m_singlePlayerButton.onClick.AddListener(() =>
       {
           NetworkManager.Singleton.StartHost();
           GameManager.SingleplayerStart();
       });
    }

    // Update is called once per frame
    void Update()
    {

    }
}
