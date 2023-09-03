using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class SceneManager : MonoBehaviour
{
        public GameOverScreen gameOverScreen;

    
    void Update()
    {
        if (Input.GetButtonDown("Cancel"))
        {
            Debug.Log("Resetting...");
            ReloadScene();
        }
    }

    public void GameOver() {
        gameOverScreen.DisplayGameOverScreen();
    }

    public void ReloadScene()
    {
        // Get the current scene and reload it
        Scene currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEngine.SceneManagement.SceneManager.LoadScene(currentScene.name);
    }
}
