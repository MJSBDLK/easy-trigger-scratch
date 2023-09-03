using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameOverScreen : MonoBehaviour
{
    public TextMeshProUGUI gameOverText; // This is the correct type for TextMeshPro text components in the UI

    private string[] gameOverMessages = new string[] {
        "Why",
        "Thanks Obama",
        "Game Over",
        "Wow",
        "Whoops",
        "Pathetic",
        "You are dead",
        "You were the chosen one",
        "no",
        "bad",
        "not cool",
        "Player is kill",
        "What?",
        "Oops",
        "Ouch",
    };

    public void DisplayGameOverScreen()
    {
        gameObject.SetActive(true);

        int randomIndex = Random.Range(0, gameOverMessages.Length);
        gameOverText.text = gameOverMessages[randomIndex];
    }

}
