using UnityEngine;
using UnityEngine.SceneManagement;

public class AngelKillCounter : MonoBehaviour
{
    [Tooltip("The EXACT name of the scene file to load when the player wins.")]
    public string winSceneName = "WinScene";

    [Tooltip("The number of enemies the player needs to kill to win.")]
    public int enemiesToWin = 20;

    private int enemiesKilled = 0;

    // Static instance to allow easy access from other scripts
    public static AngelKillCounter Instance { get; private set; }

    void Awake()
    {
        // Ensure only one instance of the tracker exists
        if (Instance == null)
        {
            Instance = this;
            // Optional: Prevent the tracker object from being destroyed when loading new scenes
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("Multiple EnemyKillTracker instances found! Destroying the extra one.");
            Destroy(gameObject);
        }
    }

    // Public method to be called by the enemy script when an enemy is killed
    public void EnemyKilled()
    {
        enemiesKilled++;
        Debug.Log($"Enemy Killed! Total Kills: {enemiesKilled}");

        // Check if the win condition has been met
        if (enemiesKilled >= enemiesToWin)
        {
            Debug.Log($"Player has reached {enemiesToWin} kills! Loading win scene: {winSceneName}");
            LoadWinScene();
        }
    }

    private void LoadWinScene()
    {
        SceneManager.LoadScene(winSceneName);
    }

    // Optional: Method to get the current kill count if you want to display it in the UI
    public int GetKillCount()
    {
        return enemiesKilled;
    }
}