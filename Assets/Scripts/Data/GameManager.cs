using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    //current instance
    public static GameManager gameManager;

    [SerializeField]
    private Transform player;

    private bool isPaused;


    // Start is called before the first frame update
    void Awake()
    {
        if (gameManager != this && gameManager == null)
        {
            Destroy(this);
        }

        gameManager = this;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (player == null)
        {
            player = FindObjectOfType<MovementAdvanced>().transform;
        }
    }

    private void Start()
    {
    }



    private void FixedUpdate()
    {
    }



    /*
    public void PauseToggle()
    {
       // if (gameOver || LoadingScreenManager.currentlyLoading)
            //return;

        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0.0000000000000000000001f : 1;
        pauseScreen.SetActive(isPaused);
        overlayUI.SetActive(isPaused);
        imageFade.gameObject.SetActive(false);
        Cursor.visible = isPaused;
    }*/

    public bool IsPaused()
    {
        return isPaused;
    }

    public Transform GetPlayer()
    {
        return player;
    }
}